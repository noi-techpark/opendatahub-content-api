// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using MOMENTUS;
using Helper;
using Helper.Extensions;
using Helper.Generic;
using Helper.Tagging;
using Newtonsoft.Json;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MOMENTUS.Model;

namespace OdhApiImporter.Helpers
{
    public class MomentusVenuesImportHelper : ImportHelper, IImportHelper
    {        
        public MomentusVenuesImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        #region MOMENTUS Helpers

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            var result = await GetDataFromMomentus.RequestMomentusRooms(
                settings.MomentusConfig.ServiceUrl, 
                settings.MomentusConfig.ClientId, 
                settings.MomentusConfig.ClientSecret, 
                settings.MomentusConfig.AuthUrl);
            
            var updateresult = await ImportData(result, cancellationToken);

            return updateresult;
        }

        private async Task<UpdateDetail> ImportData(
            IEnumerable<MomentusRoom> result,
            CancellationToken cancellationToken            
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            foreach (var momentusroom in result.GroupBy(x => x.Group).ToList())
            {
                //Load NOI Room

                var importresult = await ImportDataSingle(null, momentusroom);

                newcounter = newcounter + importresult.created ?? newcounter;
                updatecounter = updatecounter + importresult.updated ?? updatecounter;
                errorcounter = errorcounter + importresult.error ?? errorcounter;
            }

            return new UpdateDetail()
            {
                created = newcounter,
                updated = updatecounter,
                deleted = 0,
                error = errorcounter,
            };
        }

        private async Task<UpdateDetail> ImportDataSingle(
            VenueV2 venue,
            IGrouping<string, MomentusRoom> momentusroom
        )
        {
            string idtoreturn = "";
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            //try
            //{
            //    var query = QueryFactory
            //        .Query("eventeuracnoi")
            //        .Select("data")
            //        .Where("id", eventshort.Id);

            //    var eventindb = await query.GetObjectSingleAsync<EventShortLinked>();

            //    //currenteventshort.Where(x => x.EventId == eventshort.EventId).FirstOrDefault();

            //    var changedonDB = DateTime.Now;

            //    //Fields to not overwrite
            //    var imagegallery = new List<ImageGallery>();

            //    IDictionary<string, Detail> detaildict = new Dictionary<string, Detail>();

            //    var videourl = "";
            //    Nullable<bool> activeweb = null;
            //    Nullable<bool> activecommunity = null;
            //    List<string>? technologyfields = null;
            //    List<string>? customtagging = null;
            //    var webadress = "";
            //    //List<DocumentPDF>? eventdocument = new List<DocumentPDF>();
            //    IDictionary<string, List<Document>?> document =
            //        new Dictionary<string, List<Document>?>();

            //    bool? soldout = false;
            //    bool? externalorganizer = false;
            //    IDictionary<string, string> eventText = new Dictionary<string, string>();
            //    ICollection<string>? publishedon = new List<string>();

            //    ICollection<string>? tagids = null;

            //    if (eventindb == null)
            //    {
            //        eventshort.FirstImport = DateTime.Now;
            //    }

            //    if (eventindb != null)
            //    {
            //        changedonDB = eventindb.ChangedOn;
            //        imagegallery =
            //            eventindb.ImageGallery != null ? eventindb.ImageGallery.ToList() : null;

            //        detaildict = eventindb.Detail;                 

            //        activeweb = eventindb.ActiveWeb;
            //        activecommunity = eventindb.ActiveCommunityApp;

            //        videourl = eventindb.VideoUrl;
            //        technologyfields = eventindb.TechnologyFields;
            //        customtagging = eventindb.CustomTagging;
            //        webadress = eventindb.WebAddress;
            //        externalorganizer = eventindb.ExternalOrganizer;

            //        //eventdocument = eventindb.EventDocument;
            //        document = eventindb.Documents;

            //        soldout = eventindb.SoldOut;

            //        publishedon = eventindb.PublishedOn;

            //        tagids = eventindb.TagIds;
            //    }

            //    if (changedonDB != eventshort.ChangedOn || forceupdate)
            //    {
            //        eventshort.ImageGallery = imagegallery;
            //        //eventshort.EventTextDE = eventTextDE;
            //        //eventshort.EventTextIT = eventTextIT;
            //        //eventshort.EventTextEN = eventTextEN;

            //        //eventshort.EventText = eventText;

            //        //Preserve detaildict
            //        if(detaildict != null && detaildict.Count > 0)
            //        {
            //            //Readd Event Title to the Detail Dictionary
            //            foreach (var eventtitle in eventshort.Detail)
            //            {
            //                if (detaildict.ContainsKey(eventtitle.Key))
            //                    detaildict[eventtitle.Key].Title = eventtitle.Value.Title;
            //                else
            //                {
            //                    detaildict.TryAddOrUpdate(eventtitle.Key, eventtitle.Value);
            //                }
            //            }

            //            eventshort.Detail = detaildict;
            //        }
                    
                   
            //        //foreach (var eventtitle in eventshort.Detail)
            //        //{
            //        //    if(eventshort.Detail.ContainsKey(eventtitle.Key))
            //        //        eventshort.Detail[eventtitle.Key].Title = eventtitle.Value.Title;
            //        //    else
            //        //    {
            //        //        eventshort.Detail.TryAddOrUpdate(eventtitle.Key, eventtitle.Value);
            //        //    }
            //        //}

            //        //eventshort.ActiveWeb = activeweb;
            //        //eventshort.ActiveCommunityApp = activecommunity;

            //        eventshort.PublishedOn = publishedon;

            //        eventshort.VideoUrl = videourl;
            //        //eventshort.TechnologyFields = technologyfields;
            //        //eventshort.CustomTagging = customtagging;

            //        eventshort.TagIds = tagids;

            //        if (!String.IsNullOrEmpty(webadress))
            //            eventshort.WebAddress = webadress;

            //        eventshort.SoldOut = soldout;

            //        //eventshort.EventDocument = eventdocument;
            //        eventshort.Documents = document;

            //        eventshort.ExternalOrganizer = externalorganizer;

            //        //New If CompanyName is Noi - blablabla assign TechnologyField automatically and Write to Display5 if not empty "NOI"
            //        if (
            //            !String.IsNullOrEmpty(eventshort.CompanyName)
            //            && eventshort.CompanyName.StartsWith("NOI - ")
            //        )
            //        {
            //            if (String.IsNullOrEmpty(eventshort.Display5))
            //                eventshort.Display5 = "NOI";

            //            //MODIFIED
            //            eventshort.TagIds = AssignTechnologyfieldsautomatically(
            //                eventshort.CompanyName,
            //                eventshort.TechnologyFields
            //            );
            //            //eventshort.TechnologyFields = AssignTechnologyfieldsautomatically(eventshort.CompanyName, eventshort.TechnologyFields);
            //        }

            //        ////Set Publishers in base of Displays
            //        ////Eurac Videowall
            //        //if (eventshort.Display1 == "Y")
            //        //    publishedon.TryAddOrUpdateOnList("eurac-videowall");
            //        //if (eventshort.Display1 == "N")
            //        //    publishedon.TryRemoveOnList("eurac-videowall");
            //        ////Eurac Videowall
            //        //if (eventshort.Display2 == "Y")
            //        //    publishedon.TryAddOrUpdateOnList("eurac-seminarroom");
            //        //if (eventshort.Display2 == "N")
            //        //    publishedon.TryRemoveOnList("eurac-seminarroom");
            //        ////Eurac Videowall
            //        //if (eventshort.Display3 == "Y")
            //        //    publishedon.TryAddOrUpdateOnList("noi-totem");
            //        //if (eventshort.Display3 == "N")
            //        //    publishedon.TryRemoveOnList("noi-totem");
            //        ////today.noi.bz.it
            //        //if (eventshort.Display4 == "Y")
            //        //    publishedon.TryAddOrUpdateOnList("today.noi.bz.it");
            //        //if (eventshort.Display4 == "N")
            //        //    publishedon.TryRemoveOnList("today.noi.bz.it");

            //        PublishedOnHelper.CreatePublishedOnList<EventShortLinked>(eventshort);


            //        //Fix when TagIds are set lets update the Tags Object
            //        if (eventshort.TagIds != null && eventshort.TagIds.Count > 0)
            //        {
            //            //Populate Tags (Id/Source/Type)
            //            await eventshort.UpdateTagsExtension(QueryFactory);
            //        }

            //        var queryresult = await InsertDataToDB(
            //            eventshort,
            //            new KeyValuePair<string, EBMSEventREST>(
            //                eventebms.EventId.ToString(),
            //                eventebms
            //            )
            //        );

            //        newcounter = newcounter + queryresult.created ?? 0;
            //        updatecounter = updatecounter + queryresult.updated ?? 0;

            //        WriteLog.LogToConsole(
            //            idtoreturn,
            //            "dataimport",
            //            "single.eventeuracnoi",
            //            new ImportLog()
            //            {
            //                sourceid = idtoreturn,
            //                sourceinterface = "ebms.eventeuracnoi",
            //                success = true,
            //                error = "",
            //            }
            //        );
            //    }
            //}
            //catch (Exception ex)
            //{
            //    WriteLog.LogToConsole(
            //        idtoreturn,
            //        "dataimport",
            //        "single.eventeuracnoi",
            //        new ImportLog()
            //        {
            //            sourceid = idtoreturn,
            //            sourceinterface = "ebms.eventeuracnoi",
            //            success = false,
            //            error = ex.Message,
            //        }
            //    );

            //    errorcounter = errorcounter + 1;
            //}

            return new UpdateDetail()
            {
                created = newcounter,
                updated = updatecounter,
                deleted = 0,
                error = errorcounter,
            };
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            VenueV2 venue,
            KeyValuePair<string, MomentusRoom> momentusroom
        )
        {
            try
            {
                //Setting LicenseInfo
                //eventshort.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<VenueV2>(
                //    eventshort,
                //    Helper.LicenseHelper.GetLicenseforVenue
                //);
                //Check Languages
                //eventshort.CheckMyInsertedLanguages();

                //Remove Set PublishedOn not set automatically
                //eventshort.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(momentusroom);

                return await QueryFactory.UpsertData<VenueV2>(
                    venue,
                    new DataInfo("venue", Helper.Generic.CRUDOperation.CreateAndUpdate, true),
                    new EditInfo("momentus.venue.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, MomentusRoom> momentusroom)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "eurac",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(momentusroom.Value),
                    sourceinterface = "momentus",
                    sourceid = momentusroom.Key,
                    sourceurl = "https://api.eu-venueops.com/v1/general-setup/rooms",
                    type = "venue",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private static List<string>? AssignTechnologyfieldsautomatically(
            string companyname,
            List<string>? technologyfields
        )
        {
            if (technologyfields == null)
                technologyfields = new List<string>();

            //Digital, Alpine, Automotive/Automation, Food, Green

            AssignTechnologyFields(companyname, "Digital", "Digital", technologyfields);
            AssignTechnologyFields(companyname, "Alpine", "Alpine", technologyfields);
            AssignTechnologyFields(
                companyname,
                "Automotive",
                "Automotive/Automation",
                technologyfields
            );
            AssignTechnologyFields(companyname, "Food", "Food", technologyfields);
            AssignTechnologyFields(companyname, "Green", "Green", technologyfields);

            if (technologyfields.Count == 0)
                return null;
            else
                return technologyfields;
        }

        private static void AssignTechnologyFields(
            string companyname,
            string tocheck,
            string toassign,
            List<string> automatictechnologyfields
        )
        {
            if (companyname.Contains(tocheck))
                if (!automatictechnologyfields.Contains(toassign))
                    automatictechnologyfields.Add(toassign);
        }
      
        #endregion
    }
}
