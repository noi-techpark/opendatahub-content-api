// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Tagging;
using OdhApiImporter.Helpers.RAVEN;
using OdhNotifier;
using RAVEN;
using SqlKata;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class RavenImportHelper
    {
        private readonly QueryFactory QueryFactory;
        private readonly ISettings settings;
        private string importerURL;
        private IOdhPushNotifier OdhPushnotifier;

        public RavenImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
            this.importerURL = importerURL;
            this.OdhPushnotifier = odhpushnotifier;
        }

        #region ODHRAVEN Helpers

        //TODO Check if passed id has to be tranformed to lowercase or uppercase


        public async Task<Tuple<string, List<UpdateDetail>>> GetFromRavenAndTransformToPGObject(
            string id,
            string datatype,
            CancellationToken cancellationToken
        )
        {
            var mydata = default(IIdentifiable);
            var mypgdata = default(IIdentifiable);

            var myupdateresult = default(UpdateDetail);
            var updateresultreduced = default(UpdateDetail);

            switch (datatype.ToLower())
            {
                case "accommodation":
                    bool accommodationhasnopushchannels = false;
                    var updateresultstomerge = new List<UpdateDetail>();

                    mydata = await GetDataFromRaven.GetRavenData<AccommodationRaven>(
                        datatype,
                        id,
                        settings.RavenConfig.ServiceUrl,
                        settings.RavenConfig.User,
                        settings.RavenConfig.Password,
                        cancellationToken
                    );
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<
                            AccommodationRaven,
                            AccommodationV2
                        >(
                            (AccommodationRaven)mydata,
                            TransformToPGObject.GetAccommodationPGObjectV2
                        );
                    else
                        throw new Exception("No data found!");

                    //Add the PublishedOn Logic
                    ((AccommodationV2)mypgdata).CreatePublishedOnList();

                    //Update LTS CIN Code
                    await LtsApiv2Operations.UpdateAccommodationWithLTSV2Data(
                        (AccommodationV2)mypgdata,
                        QueryFactory,
                        settings,
                        true,
                        true,
                        true,
                        true
                    );

                    var myupdateresultacco = await SaveRavenObjectToPG<AccommodationV2>(
                        (AccommodationV2)mypgdata,
                        "accommodations",
                        true,
                        true,
                        true
                    );
                    updateresultstomerge.Add(myupdateresultacco);

                    //Check if accommodation has no push channels assigned
                    if (myupdateresultacco.pushchannels == null || myupdateresultacco.pushchannels.Count == 0)
                        accommodationhasnopushchannels = true;

                    //Check if data has to be reduced and save it
                    if (
                        ReduceDataTransformer.ReduceDataCheck<AccommodationV2>(
                            (AccommodationV2)mypgdata
                        ) == true
                    )
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject(
                            (AccommodationV2)mypgdata,
                            ReduceDataTransformer.CopyLTSAccommodationToReducedObject
                        );

                        updateresultreduced = await SaveRavenObjectToPG<AccommodationV2>(
                            (AccommodationV2)reducedobject,
                            "accommodations",
                            false,
                            false,
                            false
                        );
                    }

                    bool roomschanged = false;

                    //UPDATE ACCOMMODATIONROOMS
                    var myroomdatalist = await GetDataFromRaven.GetRavenData<
                        IEnumerable<AccommodationRoomLinked>
                    >(
                        "accommodationroom",
                        id,
                        settings.RavenConfig.ServiceUrl,
                        settings.RavenConfig.User,
                        settings.RavenConfig.Password,
                        cancellationToken,
                        "AccommodationRoom?getall=true&accoid="
                    );

                    //TODO make a call on all rooms, save the processed rooms and delete all rooms that are no more on the list
                    var currentassignedroomids = await QueryFactory
                        .Query("accommodationrooms")
                        .Select("id")
                        .Where("gen_a0rid", "ILIKE", mypgdata.Id)
                        .GetAsync<string>();

                    if (currentassignedroomids.Count() > 0)
                    {
                        var roomdataidsactual = myroomdatalist.Select(x => x.Id.ToUpper()).ToList();

                        var roomidstodelete = currentassignedroomids.Except(roomdataidsactual);

                        if (roomidstodelete.Count() > 0)
                        {
                            //DELETE this rooms
                            foreach (var roomid in roomidstodelete)
                            {
                                var roomdeleteresult =
                                    await DeleteRavenObjectFromPG<AccommodationRoomLinked>(
                                        roomid,
                                        "accommodationrooms",
                                        false
                                    );

                                updateresultstomerge.Add(roomdeleteresult);

                                roomschanged = true;
                            }
                        }
                    }

                    if (myroomdatalist != null)
                    {
                        Tuple<string, bool>? roomsourcecheck = null;
                        if (
                            ((AccommodationV2)mypgdata).AccoRoomInfo != null
                            && ((AccommodationV2)mypgdata)
                                .AccoRoomInfo.Select(x => x.Source)
                                .Distinct()
                                .Count() > 1
                        )
                            roomsourcecheck = Tuple.Create("hgv", true);

                        foreach (var myroomdata in myroomdatalist)
                        {
                            var mypgroomdata = TransformToPGObject.GetPGObject<
                                AccommodationRoomLinked,
                                AccommodationRoomLinked
                            >(
                                (AccommodationRoomLinked)myroomdata,
                                TransformToPGObject.GetAccommodationRoomPGObject
                            );

                            //Add the PublishedOn Logic
                            ((AccommodationRoomLinked)mypgroomdata).CreatePublishedOnList(
                                null,
                                roomsourcecheck
                            );

                            var accoroomresult = await SaveRavenObjectToPG<AccommodationRoomLinked>(
                                (AccommodationRoomLinked)mypgroomdata,
                                "accommodationrooms",
                                true,
                                true,
                                true
                            );

                            if (accoroomresult.objectchanged > 0)
                                roomschanged = true;

                            updateresultstomerge.Add(accoroomresult);
                        }
                    }

                    //TODO Add a check where if the Accommodation Object has no Push Channels assigned the Accommodation Room Objects push channels
                    //are cleared
                    if(accommodationhasnopushchannels)
                    {
                        foreach(var updatesulttoclear in updateresultstomerge)
                        {
                            if(updatesulttoclear.pushchannels != null)
                                updatesulttoclear.pushchannels.Clear();
                        }
                    }

                    //Merge with updateresult
                    myupdateresult = GenericResultsHelper.MergeUpdateDetailIntoOne(updateresultstomerge);

                    //Remove Exception not all accommodations have rooms
                    //else
                    //    throw new Exception("No data found!");


                    //Check if the Object has Changed and Push all infos to the channels
                    myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                        myupdateresult,
                        mypgdata.Id,
                        datatype,
                        new Dictionary<string, bool>() { { "roomschanged", roomschanged } }
                    );

                    break;

                case "gastronomy":
                    //mydata = await GetDataFromRaven.GetRavenData<GastronomyRaven>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<
                    //        GastronomyRaven,
                    //        GastronomyLinked
                    //    >((GastronomyRaven)mydata, TransformToPGObject.GetGastronomyPGObject);
                    //else
                    //    throw new Exception("No data found!");

                    //myupdateresult = await SaveRavenObjectToPG<GastronomyLinked>(
                    //    (GastronomyLinked)mypgdata,
                    //    "gastronomies",
                    //    false,
                    //    false,
                    //    true
                    //);

                    ////No need for Publishedon, neither comparing data since this data is from a deprecated endpoint

                    ////Check if data has to be reduced and save it
                    //if (
                    //    ReduceDataTransformer.ReduceDataCheck<GastronomyLinked>(
                    //        (GastronomyLinked)mypgdata
                    //    ) == true
                    //)
                    //{
                    //    var reducedobject = ReduceDataTransformer.GetReducedObject(
                    //        (GastronomyLinked)mypgdata,
                    //        ReduceDataTransformer.CopyLTSGastronomyToReducedObject
                    //    );

                    //    updateresultreduced = await SaveRavenObjectToPG<GastronomyLinked>(
                    //        (GastronomyLinkedReduced)reducedobject,
                    //        "gastronomies",
                    //        false,
                    //        false,
                    //        false
                    //    );
                    //}

                    throw new Exception("Gastronomy Update Raven Migrated!");

                case "activity":
                    //mydata = await GetDataFromRaven.GetRavenData<LTSActivityLinked>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<
                    //        LTSActivityLinked,
                    //        LTSActivityLinked
                    //    >((LTSActivityLinked)mydata, TransformToPGObject.GetActivityPGObject);
                    //else
                    //    throw new Exception("No data found!");

                    //myupdateresult = await SaveRavenObjectToPG<LTSActivityLinked>(
                    //    (LTSActivityLinked)mypgdata,
                    //    "activities",
                    //    false,
                    //    false,
                    //    true
                    //);

                    ////No need for Publishedon, neither comparing data since this data is from a deprecated endpoint

                    ////Check if data has to be reduced and save it
                    //if (
                    //    ReduceDataTransformer.ReduceDataCheck<LTSActivityLinked>(
                    //        (LTSActivityLinked)mypgdata
                    //    ) == true
                    //)
                    //{
                    //    var reducedobject = ReduceDataTransformer.GetReducedObject(
                    //        (LTSActivityLinked)mypgdata,
                    //        ReduceDataTransformer.CopyLTSActivityToReducedObject
                    //    );

                    //    updateresultreduced = await SaveRavenObjectToPG<LTSActivityLinked>(
                    //        (LTSActivityLinkedReduced)reducedobject,
                    //        "activities",
                    //        false,
                    //        false,
                    //        false
                    //    );
                    //}

                    throw new Exception("Activity Update Raven Migrated!");                    

                case "poi":
                    //mydata = await GetDataFromRaven.GetRavenData<LTSPoiLinked>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<LTSPoiLinked, LTSPoiLinked>(
                    //        (LTSPoiLinked)mydata,
                    //        TransformToPGObject.GetPoiPGObject
                    //    );
                    //else
                    //    throw new Exception("No data found!");

                    //myupdateresult = await SaveRavenObjectToPG<LTSPoiLinked>(
                    //    (LTSPoiLinked)mypgdata,
                    //    "pois",
                    //    false,
                    //    false,
                    //    true
                    //);

                    ////No need for Publishedon, neither comparing data since this data is from a deprecated endpoint

                    ////Check if data has to be reduced and save it
                    //if (
                    //    ReduceDataTransformer.ReduceDataCheck<LTSPoiLinked>((LTSPoiLinked)mypgdata)
                    //    == true
                    //)
                    //{
                    //    var reducedobject = ReduceDataTransformer.GetReducedObject(
                    //        (LTSPoiLinked)mypgdata,
                    //        ReduceDataTransformer.CopyLTSPoiToReducedObject
                    //    );

                    //    updateresultreduced = await SaveRavenObjectToPG<LTSPoiLinked>(
                    //        (LTSPoiLinkedReduced)reducedobject,
                    //        "pois",
                    //        false,
                    //        false,
                    //        false                            
                    //    );
                    //}

                    //break;

                    throw new Exception("Poi Update Raven Migrated!");

                case "odhactivitypoi":
                    //mydata = await GetDataFromRaven.GetRavenData<ODHActivityPoiLinked>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //{
                    //    if (((ODHActivityPoiLinked)mydata).Source.ToLower() == "lts")
                    //        throw new Exception("Activity, Poi, Gastronomy Update Raven Migrated!");


                    //    mypgdata = TransformToPGObject.GetPGObject<
                    //        ODHActivityPoiLinked,
                    //        ODHActivityPoiLinked
                    //    >(
                    //        (ODHActivityPoiLinked)mydata,
                    //        TransformToPGObject.GetODHActivityPoiPGObject
                    //    );
                    //}
                    //else
                    //    throw new Exception("No data found!");

                    ////Special Operations

                    ////Traduce all Tags with Source IDM to english tags
                    //await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                    //    mypgdata,
                    //    await GenericTaggingHelper.GetAllGenericTagsfromJson(settings.JsonConfig.Jsondir)
                    //);

                    ////Create Tag Info
                    ////Populate Tags (Id/Source/Type) TO TEST
                    //await (mypgdata as IHasTagInfo).UpdateTagsExtension(QueryFactory);

                    ////TODO Recreate LocationInfo
                    ////TODO Recreate Categories

                    ////Add the PublishedOn Logic
                    ////Exception here all Tags with autopublish has to be passed
                    //var autopublishtaglist =
                    //    await GenericTaggingHelper.GetAllAutoPublishTagsfromJson(
                    //        settings.JsonConfig.Jsondir
                    //    );
                    //((ODHActivityPoiLinked)mypgdata).CreatePublishedOnList(autopublishtaglist);

                    //myupdateresult = await SaveRavenObjectToPG<ODHActivityPoiLinked>(
                    //    (ODHActivityPoiLinked)mypgdata,
                    //    "smgpois",
                    //    true,
                    //    true,
                    //    true
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype
                    //);

                    ////Check if data has to be reduced and save it
                    //if (
                    //    ReduceDataTransformer.ReduceDataCheck<ODHActivityPoiLinked>(
                    //        (ODHActivityPoiLinked)mypgdata
                    //    ) == true
                    //)
                    //{
                    //    var reducedobject = ReduceDataTransformer.GetReducedObject(
                    //        (ODHActivityPoiLinked)mypgdata,
                    //        ReduceDataTransformer.CopyLTSODHActivtyPoiToReducedObject
                    //    );

                    //    updateresultreduced = await SaveRavenObjectToPG<ODHActivityPoiLinked>(
                    //        (LTSODHActivityPoiReduced)reducedobject,
                    //        "smgpois",
                    //        false,
                    //        false,
                    //        false                            
                    //    );
                    //}
                    //break;

                    throw new Exception("ODHActivityPoi Update Raven Migrated!");                    

                case "event":
                    //mydata = await GetDataFromRaven.GetRavenData<EventRaven>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<EventRaven, EventLinked>(
                    //        (EventRaven)mydata,
                    //        TransformToPGObject.GetEventPGObject
                    //    );
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((EventLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<EventLinked>(
                    //    (EventLinked)mypgdata,
                    //    "events",
                    //    true,
                    //    true,
                    //    true
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype
                    //);

                    ////Check if data has to be reduced and save it
                    //if (
                    //    ReduceDataTransformer.ReduceDataCheck<EventLinked>((EventLinked)mypgdata)
                    //    == true
                    //)
                    //{
                    //    var reducedobject = ReduceDataTransformer.GetReducedObject(
                    //        (EventLinked)mypgdata,
                    //        ReduceDataTransformer.CopyLTSEventToReducedObject
                    //    );

                    //    updateresultreduced = await SaveRavenObjectToPG<EventLinked>(
                    //        (EventLinkedReduced)reducedobject,
                    //        "events",
                    //        false,
                    //        false,
                    //        false
                    //    );
                    //}

                    throw new Exception("Events Update Raven Migrated!");

                case "webcam":
                    //mydata = await GetDataFromRaven.GetRavenData<WebcamInfoRaven>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken,
                    //    "WebcamInfo/"
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<
                    //        WebcamInfoRaven,
                    //        WebcamInfoLinked
                    //    >((WebcamInfoRaven)mydata, TransformToPGObject.GetWebcamInfoPGObject);
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((WebcamInfoLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<WebcamInfoLinked>(
                    //    (WebcamInfoLinked)mypgdata,
                    //    "webcams",
                    //    true,
                    //    false,
                    //    true
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype,
                    //    null,
                    //    "redactional.push"
                    //);

                    ////Check if data has to be reduced and save it
                    //if (
                    //    ReduceDataTransformer.ReduceDataCheck<WebcamInfoLinked>(
                    //        (WebcamInfoLinked)mypgdata
                    //    ) == true
                    //)
                    //{
                    //    var reducedobject = ReduceDataTransformer.GetReducedObject(
                    //        (WebcamInfoLinked)mypgdata,
                    //        ReduceDataTransformer.CopyLTSWebcamInfoToReducedObject
                    //    );

                    //    updateresultreduced = await SaveRavenObjectToPG<WebcamInfoLinked>(
                    //        (WebcamInfoLinkedReduced)reducedobject,
                    //        "webcams",
                    //        false,
                    //        false,
                    //        false
                    //    );
                    //}

                    //break;

                    throw new Exception("Webcam Update Raven Migrated!");

                case "metaregion":
                    //mydata = await GetDataFromRaven.GetRavenData<MetaRegion>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<MetaRegion, MetaRegionLinked>(
                    //        (MetaRegion)mydata,
                    //        TransformToPGObject.GetMetaRegionPGObject
                    //    );
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((MetaRegionLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<MetaRegionLinked>(
                    //    (MetaRegionLinked)mypgdata,
                    //    "metaregions",
                    //    true,
                    //    true,
                    //    false
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype,
                    //    null,
                    //    "redactional.push"
                    //);

                    //break;

                    throw new Exception("MetaRegion Update Raven Migrated!");

                case "region":
                    //mydata = await GetDataFromRaven.GetRavenData<Region>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<Region, RegionLinked>(
                    //        (Region)mydata,
                    //        TransformToPGObject.GetRegionPGObject
                    //    );
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((RegionLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<RegionLinked>(
                    //    (RegionLinked)mypgdata,
                    //    "regions",
                    //    true,
                    //    true,
                    //    false
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype,
                    //    null,
                    //    "redactional.push"
                    //);
                    //break;

                    throw new Exception("Region Update Raven Migrated!");

                case "tv":
                    //mydata = await GetDataFromRaven.GetRavenData<Tourismverein>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken,
                    //    "TourismAssociation/"
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<
                    //        Tourismverein,
                    //        TourismvereinLinked
                    //    >((Tourismverein)mydata, TransformToPGObject.GetTourismAssociationPGObject);
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((TourismvereinLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<TourismvereinLinked>(
                    //    (TourismvereinLinked)mypgdata,
                    //    "tvs",
                    //    true,
                    //    true,
                    //    false
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype,
                    //    null,
                    //    "redactional.push"
                    //);

                    //break;

                    throw new Exception("TV Update Raven Migrated!");

                case "municipality":
                    //mydata = await GetDataFromRaven.GetRavenData<Municipality>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<
                    //        Municipality,
                    //        MunicipalityLinked
                    //    >((Municipality)mydata, TransformToPGObject.GetMunicipalityPGObject);
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((MunicipalityLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<MunicipalityLinked>(
                    //    (MunicipalityLinked)mypgdata,
                    //    "municipalities",
                    //    true,
                    //    false,
                    //    false
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype,
                    //    null,
                    //    "redactional.push"
                    //);

                    //break;

                    throw new Exception("Municipality Update Raven Migrated!");

                case "district":
                    //mydata = await GetDataFromRaven.GetRavenData<District>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<District, DistrictLinked>(
                    //        (District)mydata,
                    //        TransformToPGObject.GetDistrictPGObject
                    //    );
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((DistrictLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<DistrictLinked>(
                    //    (DistrictLinked)mypgdata,
                    //    "districts",
                    //    true,
                    //    false,
                    //    false
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype,
                    //    null,
                    //    "redactional.push"
                    //);

                    //break;

                    throw new Exception("Districts Update Raven Migrated!");

                case "experiencearea":
                //mydata = await GetDataFromRaven.GetRavenData<ExperienceArea>(
                //    datatype,
                //    id,
                //    settings.RavenConfig.ServiceUrl,
                //    settings.RavenConfig.User,
                //    settings.RavenConfig.Password,
                //    cancellationToken
                //);
                //if (mydata != null)
                //    mypgdata = TransformToPGObject.GetPGObject<
                //        ExperienceArea,
                //        ExperienceAreaLinked
                //    >((ExperienceArea)mydata, TransformToPGObject.GetExperienceAreaPGObject);
                //else
                //    throw new Exception("No data found!");

                ////Add the PublishedOn Logic
                //((ExperienceAreaLinked)mypgdata).CreatePublishedOnList();

                //myupdateresult = await SaveRavenObjectToPG<ExperienceAreaLinked>(
                //    (ExperienceAreaLinked)mypgdata,
                //    "experienceareas",
                //    true,
                //    false,
                //    false
                //);

                ////Check if the Object has Changed and Push all infos to the channels
                //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                //    myupdateresult,
                //    mypgdata.Id,
                //    datatype,
                //    null,
                //    "redactional.push"
                //);

                //break;

                throw new Exception("ExperienceArea Update Raven Migrated!");

                case "skiarea":
                    //mydata = await GetDataFromRaven.GetRavenData<SkiAreaRaven>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<SkiAreaRaven, SkiAreaLinked>(
                    //        (SkiAreaRaven)mydata,
                    //        TransformToPGObject.GetSkiAreaPGObject
                    //    );
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((SkiAreaLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<SkiAreaLinked>(
                    //    (SkiAreaLinked)mypgdata,
                    //    "skiareas",
                    //    true,
                    //    true,
                    //    false
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype,
                    //    null,
                    //    "redactional.push"
                    //);

                    //break;

                    throw new Exception("SkiArea Update Raven Migrated!");

                case "skiregion":
                    //mydata = await GetDataFromRaven.GetRavenData<SkiRegion>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<SkiRegion, SkiRegionLinked>(
                    //        (SkiRegion)mydata,
                    //        TransformToPGObject.GetSkiRegionPGObject
                    //    );
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((SkiRegionLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<SkiRegionLinked>(
                    //    (SkiRegionLinked)mypgdata,
                    //    "skiregions",
                    //    true,
                    //    true,
                    //    false
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype,
                    //    null,
                    //    "redactional.push"
                    //);

                    //break;

                    throw new Exception("SkiRegion Update Raven Migrated!");

                case "article":
                    mydata = await GetDataFromRaven.GetRavenData<ArticlesLinked>(
                        datatype,
                        id,
                        settings.RavenConfig.ServiceUrl,
                        settings.RavenConfig.User,
                        settings.RavenConfig.Password,
                        cancellationToken
                    );
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<ArticlesLinked, ArticlesLinked>(
                            (ArticlesLinked)mydata,
                            TransformToPGObject.GetArticlePGObject
                        );
                    else
                        throw new Exception("No data found!");

                    //Add the PublishedOn Logic
                    ((ArticlesLinked)mypgdata).CreatePublishedOnList();

                    myupdateresult = await SaveRavenObjectToPG<ArticlesLinked>(
                        (ArticlesLinked)mypgdata,
                        "articles",
                        true,
                        true,
                        false
                    );

                    //Check if the Object has Changed and Push all infos to the channels
                    myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                        myupdateresult,
                        mypgdata.Id,
                        datatype,
                        null,
                        "redactional.push"
                    );

                    break;

                case "odhtag":
                    mydata = await GetDataFromRaven.GetRavenData<ODHTagLinked>(
                        datatype,
                        id,
                        settings.RavenConfig.ServiceUrl,
                        settings.RavenConfig.User,
                        settings.RavenConfig.Password,
                        cancellationToken
                    );
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<ODHTagLinked, ODHTagLinked>(
                            (ODHTagLinked)mydata,
                            TransformToPGObject.GetODHTagPGObject
                        );
                    else
                        throw new Exception("No data found!");

                    //LicenseInfo Logic added on TransformToPGObject because on the sinfo instance there is no license info
                    //PublishedOn Logic added on TransformToPGObject because ODHTag not implementing ISource

                    myupdateresult = await SaveRavenObjectToPG<ODHTagLinked>(
                        (ODHTagLinked)mypgdata,
                        "smgtags",
                        true,
                        false,
                        false
                    );

                    //Check if the Object has Changed and Push all infos to the channels
                    myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                        myupdateresult,
                        mypgdata.Id,
                        datatype,
                        null,
                        "redactional.push"
                    );

                    break;

                case "measuringpoint":
                    //mydata = await GetDataFromRaven.GetRavenData<MeasuringpointRaven>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken,
                    //    "Weather/Measuringpoint/"
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<
                    //        MeasuringpointRaven,
                    //        MeasuringpointLinked
                    //    >(
                    //        (MeasuringpointRaven)mydata,
                    //        TransformToPGObject.GetMeasuringpointPGObject
                    //    );
                    //else
                    //    throw new Exception("No data found!");

                    ////Measuringpoint, Fill SkiAreaIds
                    //var areaids = ((MeasuringpointLinked)mypgdata).AreaIds;
                    //if (areaids != null)
                    //    ((MeasuringpointLinked)mypgdata).SkiAreaIds = await QueryFactory
                    //        .Query()
                    //        .GetSkiAreaIdsfromSkiAreasAsync(areaids, cancellationToken);

                    ////Add the PublishedOn Logic
                    //((MeasuringpointLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<MeasuringpointLinked>(
                    //    (MeasuringpointLinked)mypgdata,
                    //    "measuringpoints",
                    //    true,
                    //    false,
                    //    true
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype
                    //);

                    ////Check if data has to be reduced and save it
                    //if (
                    //    ReduceDataTransformer.ReduceDataCheck<MeasuringpointLinked>(
                    //        (MeasuringpointLinked)mypgdata
                    //    ) == true
                    //)
                    //{
                    //    var reducedobject = ReduceDataTransformer.GetReducedObject(
                    //        (MeasuringpointLinked)mypgdata,
                    //        ReduceDataTransformer.CopyLTSMeasuringpointToReducedObject
                    //    );

                    //    updateresultreduced = await SaveRavenObjectToPG<MeasuringpointLinked>(
                    //        (MeasuringpointLinkedReduced)reducedobject,
                    //        "measuringpoints",
                    //        false,
                    //        false,
                    //        false
                    //    );
                    //}

                    //break;

                    throw new Exception("MeasuringPoint Update Raven Migrated!");

                case "venue":
                    //mydata = await GetDataFromRaven.GetRavenData<DDVenue>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);

                    //if (mydata != null)
                    //{
                    //    mypgdata = TransformToPGObject.GetPGObject<DDVenue, DDVenue>(
                    //        (DDVenue)mydata,
                    //        TransformToPGObject.GetVenuePGObject
                    //    );
                    //    mydata = TransformToPGObject.GetPGObject<DDVenue, VenueLinked>(
                    //        (DDVenue)mydata,
                    //        TransformToPGObject.GetVenuePGObjectV2
                    //    );
                    //}
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((VenueLinked)mydata).CreatePublishedOnList();
                    //((DDVenue)mypgdata).odhdata.PublishedOn = (
                    //    (VenueLinked)mydata
                    //).PublishedOn.ToList();

                    ////TODO Compare result
                    //myupdateresult = await SaveRavenDestinationdataObjectToPG<VenueLinked, DDVenue>(
                    //    (VenueLinked)mydata,
                    //    (DDVenue)mypgdata,
                    //    "venues_v2",
                    //    true
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype
                    //);

                    ////Check if data has to be reduced and save it
                    //if (ReduceDataTransformer.ReduceDataCheck<DDVenue>((DDVenue)mypgdata) == true)
                    //{
                    //    var reducedobject = ReduceDataTransformer.GetReducedObject(
                    //        (VenueLinked)mydata,
                    //        ReduceDataTransformer.CopyLTSVenueToReducedObject
                    //    );
                    //    var reducedobjectdd = ReduceDataTransformer.GetReducedObject(
                    //        (DDVenue)mypgdata,
                    //        ReduceDataTransformer.CopyLTSVenueToReducedObject
                    //    );

                    //    updateresultreduced = await SaveRavenDestinationdataObjectToPG<
                    //        VenueLinked,
                    //        DDVenue
                    //    >(
                    //        (VenueReduced)reducedobject,
                    //        (DDVenueReduced)reducedobjectdd,
                    //        "venues_v2",
                    //        false
                    //    );
                    //}
                    //break;

                    throw new Exception("Venue Update Raven Migrated!");

                case "wine":
                    //mydata = await GetDataFromRaven.GetRavenData<WineLinked>(
                    //    "WineAward",
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //    mypgdata = TransformToPGObject.GetPGObject<WineLinked, WineLinked>(
                    //        (WineLinked)mydata,
                    //        TransformToPGObject.GetWinePGObject
                    //    );
                    //else
                    //    throw new Exception("No data found!");

                    ////Add the PublishedOn Logic
                    //((WineLinked)mypgdata).CreatePublishedOnList();

                    //myupdateresult = await SaveRavenObjectToPG<WineLinked>(
                    //    (WineLinked)mypgdata,
                    //    "wines",
                    //    true,
                    //    false,
                    //    false
                    //);

                    ////Check if the Object has Changed and Push all infos to the channels
                    //myupdateresult.pushed = await CheckIfObjectChangedAndPush(
                    //    myupdateresult,
                    //    mypgdata.Id,
                    //    datatype,
                    //    null,
                    //    "redactional.push"
                    //);
                    //break;

                    throw new Exception("Wine Update Raven Migrated!");
                    
                default:
                    throw new Exception("no match found");
            }

            var mergelist = new List<UpdateDetail>() { myupdateresult };

            if (
                updateresultreduced.updated != null
                || updateresultreduced.created != null
                || updateresultreduced.deleted != null
            )
                mergelist.Add(updateresultreduced);

            return Tuple.Create<string, List<UpdateDetail>>(
                mypgdata.Id,
                mergelist
            );
        }

        public async Task<Tuple<string, List<UpdateDetail>>> DeletePGObject(
            string id,
            string datatype,
            CancellationToken cancellationToken
        )
        {
            //var mypgdata = default(IIdentifiable);
            //var table = ODHTypeHelper.TranslateTypeString2Table(datatype.ToLower());

            var deleteresult = default(UpdateDetail);
            var deleteresultreduced = default(UpdateDetail);

            //Check passed id style here?

            switch (datatype.ToLower())
            {
                case "accommodation":

                    deleteresult = await DeleteRavenObjectFromPG<AccommodationLinked>(
                        id,
                        "accommodations",
                        true
                    );
                    deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);
                    //TODO DELETE also rooms

                    break;

                case "gastronomy":

                    //deleteresult = await DeleteRavenObjectFromPG<ODHActivityPoiLinked>(
                    //    id,
                    //    "gastronomies",
                    //    true
                    //);

                    throw new Exception("Gastronomy Delete Raven Migrated!");                   

                case "activity":

                    //deleteresult = await DeleteRavenObjectFromPG<LTSActivityLinked>(
                    //    id,
                    //    "activities",
                    //    true
                    //);

                    //break;

                    throw new Exception("Activity Delete Raven Migrated!");

                case "poi":

                    //deleteresult = await DeleteRavenObjectFromPG<LTSPoiLinked>(
                    //    id, 
                    //    "pois", 
                    //    true
                    //);

                    //break;

                    throw new Exception("Poi Delete Raven Migrated!");

                case "odhactivitypoi":

                    ////Check Source
                    //var mydata = await GetDataFromRaven.GetRavenData<ODHActivityPoiLinked>(
                    //    datatype,
                    //    id,
                    //    settings.RavenConfig.ServiceUrl,
                    //    settings.RavenConfig.User,
                    //    settings.RavenConfig.Password,
                    //    cancellationToken
                    //);
                    //if (mydata != null)
                    //{
                    //    if (((ODHActivityPoiLinked)mydata).Source.ToLower() == "lts")
                    //        throw new Exception("Activity, Poi, Gastronomy Delete Raven Migrated!");
                    //}


                    ////Delete
                    //deleteresult = await DeleteRavenObjectFromPG<ODHActivityPoiLinked>(
                    //id,
                    //"smgpois",
                    //true
                    //);

                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    //break;

                    throw new Exception("ODHActivityPoi Delete Raven Migrated!");

                case "event":

                    //Delete
                    //deleteresult = await DeleteRavenObjectFromPG<EventLinked>(id, "events", true);
                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    throw new Exception("Events Delete Raven Migrated!");                    

                case "metaregion":

                    //deleteresult = await DeleteRavenObjectFromPG<MetaRegionLinked>(
                    //    id,
                    //    "metaregions",
                    //    false
                    //);

                    //break;

                    throw new Exception("Metaregion Delete Raven Migrated!");

                case "region":

                    //deleteresult = await DeleteRavenObjectFromPG<RegionLinked>(
                    //    id,
                    //    "regions",
                    //    false
                    //);
                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    //break;

                    throw new Exception("Region Delete Raven Migrated!");

                case "tv":

                    //deleteresult = await DeleteRavenObjectFromPG<TourismvereinLinked>(
                    //    id,
                    //    "tvs",
                    //    false
                    //);
                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    //break;

                    throw new Exception("TV Delete Raven Migrated!");

                case "municipality":

                    //deleteresult = await DeleteRavenObjectFromPG<MunicipalityLinked>(
                    //    id,
                    //    "municipalities",
                    //    false
                    //);
                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    //break;

                    throw new Exception("Municipality Delete Raven Migrated!");

                case "district":

                    //deleteresult = await DeleteRavenObjectFromPG<DistrictLinked>(
                    //    id,
                    //    "districts",
                    //    false
                    //);
                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    //break;

                    throw new Exception("District Delete Raven Migrated!");

                case "experiencearea":

                    //deleteresult = await DeleteRavenObjectFromPG<ExperienceAreaLinked>(
                    //    id,
                    //    "experienceareas",
                    //    false
                    //);

                    //break;

                    throw new Exception("ExperienceArea Delete Raven Migrated!");

                case "skiarea":

                    ////Delete
                    //deleteresult = await DeleteRavenObjectFromPG<SkiAreaLinked>(
                    //    id,
                    //    "skiareas",
                    //    false
                    //);
                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    //break;

                    throw new Exception("SkiArea Delete Raven Migrated!");

                case "skiregion":

                    ////Delete
                    //deleteresult = await DeleteRavenObjectFromPG<SkiRegionLinked>(
                    //    id,
                    //    "skiregions",
                    //    false
                    //);
                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    //break;

                    throw new Exception("SkiRegion Delete Raven Migrated!");

                case "article":

                    deleteresult = await DeleteRavenObjectFromPG<ArticlesLinked>(
                        id,
                        "articles",
                        false
                    );

                    break;

                case "odhtag":

                    //Delete
                    deleteresult = await DeleteRavenObjectFromPG<ODHTagLinked>(
                        id,
                        "smgtags",
                        false
                    );
                    deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    break;

                case "measuringpoint":

                    //deleteresult = await DeleteRavenObjectFromPG<MeasuringpointLinked>(
                    //    id,
                    //    "measuringpoints",
                    //    true
                    //);
                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    //break;

                    throw new Exception("Measuringpoint Delete Raven Migrated!");

                case "venue":

                    //deleteresult = await DeleteRavenObjectFromPG<VenueLinked>(
                    //    id,
                    //    "venues_v2",
                    //    true
                    //);
                    //deleteresult.pushed = await PushDeletedObject(deleteresult, id, datatype);

                    //break;

                    throw new Exception("Venue Delete Raven Migrated!");

                case "wine":

                    //deleteresult = await DeleteRavenObjectFromPG<WineLinked>(id, "wines", false);

                    //break;

                    throw new Exception("Wine Delete Raven Migrated!");

                case "webcam":
                    //TODO DELETE ALL ASSIGNMENTS OF THIS Webcam
                    //deleteresult = await DeleteRavenObjectFromPG<VenueLinked>(id, "webcams", true);

                    //break;

                    throw new Exception("Webcam Delete Raven Migrated!");

                default:
                    throw new Exception("no match found");
            }

            var mergelist = new List<UpdateDetail>() { deleteresult };

            if (
                deleteresultreduced.updated != null
                || deleteresultreduced.created != null
                || deleteresultreduced.deleted != null
            )
                mergelist.Add(deleteresultreduced);

            return Tuple.Create<string, List<UpdateDetail>>(
                id,
                mergelist
            );
        }

        /// <summary>
        /// Save and Compare Object and Image Changes, requires Object implementing IIdentifiable, IMetaData, IImportDateassigneable, ILicenseInfo, IPublishedOn and IImageGalleryAware
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="datatosave"></param>
        /// <param name="table"></param>
        /// <param name="compareresult"></param>
        /// <param name="compareimagechange"></param>
        /// <returns></returns>
        private async Task<UpdateDetail> SaveRavenObjectToPG<T>(
            T datatosave,
            string table,
            bool compareresult,
            bool compareimagechange,
            bool deletereduceddata
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, ILicenseInfo, new()
        {
            datatosave._Meta.LastUpdate = datatosave.LastChange;

            //Update or Insert depending on data
            var result = await QueryFactory.UpsertData<T>(
                datatosave,
                new DataInfo(table, CRUDOperation.Update) { ErrorWhendataIsNew = false },
                new EditInfo("lts.push.import", importerURL),
                new CRUDConstraints(),
                new CompareConfig(compareresult, compareimagechange)
            );

            //Delete the reduced data if available
            if (deletereduceddata)
                await QueryFactory.DeleteData<T>(
                    datatosave.Id + "_reduced",
                    new DataInfo(table, CRUDOperation.Delete),
                    new CRUDConstraints()
                );

            return result;
        }
      
        public async Task<IDictionary<string, NotifierResponse>?> CheckIfObjectChangedAndPush(
            UpdateDetail myupdateresult,
            string id,
            string datatype,
            IDictionary<string, bool>? additionalpushinfo = null,
            string pushorigin = "lts.push"
        )
        {
            IDictionary<string, NotifierResponse>? pushresults = default(IDictionary<
                string,
                NotifierResponse
            >);

            //Check if data has changed and Push To all channels
            if (
                myupdateresult.objectchanged != null
                && myupdateresult.objectchanged > 0
                && myupdateresult.pushchannels != null
                && myupdateresult.pushchannels.Count > 0
            )
            {
                if (additionalpushinfo == null)
                    additionalpushinfo = new Dictionary<string, bool>();

                //Check if image has changed and add it to the dictionary
                if (
                    myupdateresult.objectimagechanged != null
                    && myupdateresult.objectimagechanged.Value > 0
                )
                    additionalpushinfo.TryAdd("imageschanged", true);
                else
                    additionalpushinfo.TryAdd("imageschanged", false);

                pushresults = await OdhPushnotifier.PushToPublishedOnServices(
                    id,
                    datatype.ToLower(),
                    pushorigin,
                    additionalpushinfo,
                    false,
                    "api",
                    myupdateresult.pushchannels.ToList()
                );
            }

            return pushresults;
        }

        private async Task<UpdateDetail> DeleteRavenObjectFromPG<T>(
            string id,
            string table,
            bool deletereduced
        )
            where T : IIdentifiable,
                IImportDateassigneable,
                IMetaData,
                ILicenseInfo,
                IPublishedOn,
                new()
        {
            var result = await QueryFactory.DeleteData<T>(
                id,
                new DataInfo(table, CRUDOperation.Delete),
                new CRUDConstraints()
            );

            var reducedresult = new UpdateDetail()
            {
                changes = null,
                objectcompared = 0,
                created = 0,
                deleted = 0,
                error = 0,
                id = "",
                objectchanged = 0,
                objectimagechanged = 0,
                operation = "DELETE",
                pushchannels = null,
                updated = 0,
            };

            //Check if reduced object has to be deleted
            if (deletereduced)
            {
                reducedresult = await QueryFactory.DeleteData<T>(
                    id + "_reduced",
                    new DataInfo(table, CRUDOperation.Delete),
                    new CRUDConstraints()
                );
            }

            return reducedresult;
        }

        private async Task<IDictionary<string, NotifierResponse>?> PushDeletedObject(
            UpdateDetail myupdateresult,
            string id,
            string datatype,
            string pushorigin = "lts.push"
        )
        {
            IDictionary<string, NotifierResponse>? pushresults = default(IDictionary<
                string,
                NotifierResponse
            >);

            //Check if data has changed and Push To all channels
            if (myupdateresult.deleted > 0 && myupdateresult.pushchannels.Count > 0)
            {
                pushresults = await OdhPushnotifier.PushToPublishedOnServices(
                    id,
                    datatype.ToLower(),
                    pushorigin,
                    null,
                    true,
                    "api",
                    myupdateresult.pushchannels.ToList()
                );
            }

            return pushresults;
        }

        #endregion
    }
}
