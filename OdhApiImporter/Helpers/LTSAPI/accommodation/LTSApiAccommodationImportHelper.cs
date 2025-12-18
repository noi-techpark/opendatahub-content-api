// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using Helper.Tagging;
using LTSAPI;
using LTSAPI.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhApiImporter.Helpers.RAVEN;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiAccommodationImportHelper : ImportHelper, IImportHelper
    {
        public bool opendata = false;
        
        public LTSApiAccommodationImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        //Not implemented here
        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public async Task<UpdateDetail> SaveSingleDataToODH(
            string id,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            opendata = reduced;

            //Import the List
            var accommodationlts = await GetAccommodationsFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (accommodationlts != null && accommodationlts.FirstOrDefault().ContainsKey("success") && (Boolean)accommodationlts.FirstOrDefault()["success"])
            {
                //Import Single Data & Deactivate Data
                return await SaveAccommodationsToPG(accommodationlts);
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (accommodationlts != null && accommodationlts.FirstOrDefault().ContainsKey("status") && ((int)accommodationlts.FirstOrDefault()["status"] == 403 || (int)accommodationlts.FirstOrDefault()["status"] == 404))
            {
                var resulttoreturn = default(UpdateDetail);

                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    resulttoreturn = await DeleteOrDisableAccommodationsData(id, false, false);
                    if (accommodationlts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(accommodationlts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + accommodationlts.FirstOrDefault()["message"].ToString() + "|";
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    resulttoreturn = await DeleteOrDisableAccommodationsData(id, true, true);
                    if (accommodationlts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(accommodationlts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + "opendata:" + accommodationlts.FirstOrDefault()["message"].ToString() + "|";
                }

                return resulttoreturn;
            }
            else
            {
                return new UpdateDetail()
                {
                    updated = 0,
                    created = 0,
                    deleted = 0,
                    error = 1,
                };
            }
        }

        public async Task<List<string>> GetLastChangedData(
           DateTime lastchanged,
           bool reduced = false,
           CancellationToken cancellationToken = default
       )
        {
            //Import the List
            var lastchangedlts = await GetAccommodationsFromLTSV2(null, lastchanged, null, null);
            List<string> lastchangedlist = new List<string>();

            if (lastchangedlts != null && lastchangedlts.FirstOrDefault().ContainsKey("success") && (Boolean)lastchangedlts.FirstOrDefault()["success"])
            {
                var lastchangedrids = lastchangedlts.FirstOrDefault()["data"].ToObject<List<LtsRidList>>();

                lastchangedlist = lastchangedrids.Select(x => x.rid).ToList();
            }
            else
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "lastchanged.accommodations",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.accommodations",
                        success = false,
                        error = "Could not fetch last changed List",
                    }
                );
            }

            return lastchangedlist;
        }

        public async Task<List<string>> GetLastDeletedData(
            DateTime deletedfrom,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            //Import the List
            var deletedlts = await GetAccommodationsFromLTSV2(null, null, deletedfrom, null);
            List<string> lastdeletedlist = new List<string>();

            if (deletedlts != null && deletedlts.FirstOrDefault().ContainsKey("success") && (Boolean)deletedlts.FirstOrDefault()["success"])
            {
                var lastchangedrids = deletedlts.FirstOrDefault()["data"].ToObject<List<LtsRidList>>();

                lastdeletedlist = lastchangedrids.Select(x => x.rid).ToList();
            }
            else
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deleted.accommodations",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.accommodations",
                        success = false,
                        error = "Could not fetch deleted List",
                    }
                );
            }

            return lastdeletedlist;
        }

        public async Task<List<string>> GetActiveList(
            bool active,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            opendata = reduced;

            //Import the List
            var activelistlts = await GetAccommodationsFromLTSV2(null, null, null, active);
            List<string> activeList = new List<string>();

            if (activelistlts != null && activelistlts.FirstOrDefault().ContainsKey("success") && (Boolean)activelistlts.FirstOrDefault()["success"])
            {
                var activerids = activelistlts.FirstOrDefault()["data"].ToObject<List<LtsRidList>>();

                activeList = activerids.Select(x => x.rid).ToList();
            }
            else
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "active.accommodations",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.accommodations",
                        success = false,
                        error = "Could not fetch active List",
                    }
                );
            }

            return activeList;
        }


        private async Task<List<JObject>> GetAccommodationsFromLTSV2(
            string accoid,
            DateTime? lastchanged,
            DateTime? deletedfrom, 
            bool? activelist
        )
        {
            try
            {
                LtsApi ltsapi = GetLTSApi(opendata);
                if (accoid != null)
                {
                    //Get Single Accommodation

                    var qs = new LTSQueryStrings() { page_size = 1 };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.AccommodationDetailRequest(accoid, dict);
                }
                else if (lastchanged != null)
                {
                    //Get the Last Changed Accommodations list

                    var qs = new LTSQueryStrings() { fields = "rid", filter_marketingGroupRids = "9E72B78AC5B14A9DB6BED6C2592483BF" }; //To check filter_onlyTourismOrganizationMember
                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.AccommodationListRequest(dict, true);
                }
                else if (deletedfrom != null)
                {
                    //Get the Active Accommodations list with 

                    var qs = new LTSQueryStrings() { fields = "rid", filter_marketingGroupRids = "9E72B78AC5B14A9DB6BED6C2592483BF" };

                    if (deletedfrom != null)
                        qs.filter_lastUpdate = deletedfrom;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.AccommodationDeleteRequest(dict, true);
                }
                else if (activelist != null)
                {
                    //Get the Active Accommodations list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_marketingGroupRids = "9E72B78AC5B14A9DB6BED6C2592483BF", filter_representationMode = "full" };

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.ActivityListRequest(dict, true);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.accommodations",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.accommodations",
                        success = false,
                        error = ex.Message,
                    }
                );

                return null;
            }
        }


        private async Task<UpdateDetail> SaveAccommodationsToPG(List<JObject> ltsdata)
        {
            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();
                List<LTSAcco> accosdata = new List<LTSAcco>();


                //Load the json and xml Data

                var xmlfiles = ImportUtils.LoadXmlFiles(
                    Path.Combine(".\\xml\\"),
                    new List<string>()
                    {
                        "AccoCategories",
                        "AccoTypes",
                        "Alpine",
                        "Boards",
                        "City",
                        "Dolomites",
                        "Mediterranean",
                        "NearSkiArea",
                        "RoomAmenities",
                        "Vinum",
                        "Wine",
                    }
                );

                var jsonfiles = await ImportUtils.LoadJsonFiles(
                    Path.Combine(".\\json\\"),
                    new List<string>() { "Features" }
                );

                foreach (var ltsdatasingle in ltsdata)
                {
                    accosdata.Add(
                        ltsdatasingle.ToObject<LTSAcco>()
                    );
                }

                foreach (var data in accosdata)
                {
                    string id = data.data.rid.ToUpper();

                    var accommodationparsed = AccommodationParser.ParseLTSAccommodation(data.data, false, xmlfiles, jsonfiles);

                    //POPULATE LocationInfo TO CHECK if this works for new activities...
                    accommodationparsed.LocationInfo = await accommodationparsed.UpdateLocationInfoExtension(
                        QueryFactory
                    );

                    //DistanceCalculation
                    await accommodationparsed.UpdateDistanceCalculation(QueryFactory);

                    //GET OLD Accommodation
                    var activityindb = await LoadDataFromDB<ODHActivityPoiLinked>("smgpoi" + id, IDStyle.lowercase);

                   
                    if (!opendata)
                    {
                        //TODO Update All ROOMS

                        var accommodationsroomparsed = AccommodationParser.ParseLTSAccommodationRoom(data.data, false, xmlfiles, jsonfiles);

                        foreach (var accommodationroom in accommodationsroomparsed)
                        {

                        }

                        //TODO Delete Deleted ROOMS

                        //TODO Regenerated AccoRooms List on Accommodation object (make sure, HGV rooms are updated first)
                    }



                    //FINALLY UPDATE ACCOMMODATION ROOT OBJECT

                    //Create Tags and preserve the old TagEntries
                    await accommodationparsed.UpdateTagsExtension(QueryFactory);


                    var result = await InsertDataToDB(accommodationparsed, data.data, jsonfiles);

                    updatedetails.Add(new UpdateDetail()
                    {
                        created = result.created,
                        updated = result.updated,
                        deleted = result.deleted,
                        error = result.error,
                        objectchanged = result.objectchanged,
                        objectimagechanged = result.objectimagechanged,
                        comparedobjects =
                        result.compareobject != null && result.compareobject.Value ? 1 : 0,
                        pushchannels = result.pushchannels,
                        changes = result.changes,
                    });

                    idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.accommodations",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.accommodations",
                            success = true,
                            error = "",
                        }
                    );
                }
            }
            else
            {
                updatedetails.Add(new UpdateDetail()
                {
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    comparedobjects = 0,
                    pushchannels = null,
                    changes = null
                });
            }

            return updatedetails.FirstOrDefault();
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            AccommodationV2 objecttosave,
            LTSAccoData data,
            IDictionary<string, JArray>? jsonfiles
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                    objecttosave,
                    Helper.LicenseHelper.GetLicenseforAccommodation
                );

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave);

                //Set PublishedOn
                objecttosave.CreatePublishedOnList();

                //Populate Tags (Id/Source/Type)
                await objecttosave.UpdateTagsExtension(QueryFactory);

                var rawdataid = await InsertInRawDataDB(data);

                return await QueryFactory.UpsertData<AccommodationV2>(
                    objecttosave,
                    new DataInfo("accommodations", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("lts.accommodations.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(LTSAccoData data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(data),
                    sourceinterface = "accommodations",
                    sourceid = data.rid,
                    sourceurl = "https://go.lts.it/api/v1/accommodations",
                    type = "accommodations",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        public async Task<UpdateDetail> DeleteOrDisableAccommodationsData(string id, bool delete, bool reduced)
        {
            UpdateDetail deletedisableresult = default(UpdateDetail);

            PGCRUDResult result = default(PGCRUDResult);

            if (delete)
            {
                result = await QueryFactory.DeleteData<AccommodationV2>(
                id.ToUpper(),
                new DataInfo("accommodations", CRUDOperation.Delete),
                new CRUDConstraints(),
                reduced
                );

                if (result.errorreason != "Data Not Found")
                {
                    deletedisableresult = new UpdateDetail()
                    {
                        created = result.created,
                        updated = result.updated,
                        deleted = result.deleted,
                        error = result.error,
                        objectchanged = result.objectchanged,
                        objectimagechanged = result.objectimagechanged,
                        comparedobjects =
                            result.compareobject != null && result.compareobject.Value ? 1 : 0,
                        pushchannels = result.pushchannels,
                        changes = result.changes,
                    };
                }
            }
            else
            {
                var query = QueryFactory.Query(table).Select("data").Where("id", id.ToUpper());

                var data = await query.GetObjectSingleAsync<AccommodationV2>();

                if (data != null)
                {
                    if (
                        data.Active != false
                        || (data is ISmgActive && ((ISmgActive)data).SmgActive != false)
                    )
                    {
                        data.Active = false;
                        if (data is ISmgActive)
                            ((ISmgActive)data).SmgActive = false;

                        result = await QueryFactory.UpsertData<AccommodationV2>(
                               data,
                               new DataInfo("accommodations", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                               new EditInfo("lts.accommodations.import.deactivate", importerURL),
                               new CRUDConstraints(),
                               new CompareConfig(true, false)
                        );

                        deletedisableresult = new UpdateDetail()
                        {
                            created = result.created,
                            updated = result.updated,
                            deleted = result.deleted,
                            error = result.error,
                            objectchanged = result.objectchanged,
                            objectimagechanged = result.objectimagechanged,
                            comparedobjects = result.compareobject != null && result.compareobject.Value ? 1 : 0,
                            pushchannels = result.pushchannels,
                            changes = result.changes,
                        };
                    }
                }
            }

            return deletedisableresult;
        }

        public async Task<UpdateDetail> DeleteOrDisableAccommodationRoomsData(string id, bool delete, bool reduced)
        {
            UpdateDetail deletedisableresult = default(UpdateDetail);

            PGCRUDResult result = default(PGCRUDResult);

            if (delete)
            {
                result = await QueryFactory.DeleteData<AccommodationRoomV2>(
                id.ToUpper(),
                new DataInfo("accommodationrooms", CRUDOperation.Delete),
                new CRUDConstraints(),
                reduced
                );

                if (result.errorreason != "Data Not Found")
                {
                    deletedisableresult = new UpdateDetail()
                    {
                        created = result.created,
                        updated = result.updated,
                        deleted = result.deleted,
                        error = result.error,
                        objectchanged = result.objectchanged,
                        objectimagechanged = result.objectimagechanged,
                        comparedobjects =
                            result.compareobject != null && result.compareobject.Value ? 1 : 0,
                        pushchannels = result.pushchannels,
                        changes = result.changes,
                    };
                }
            }
            else
            {
                var query = QueryFactory.Query("accommodationrooms").Select("data").Where("id", id.ToUpper());

                var data = await query.GetObjectSingleAsync<AccommodationRoomV2>();

                if (data != null)
                {
                    if (
                        data.Active != false
                        || (data is ISmgActive && ((ISmgActive)data).SmgActive != false)
                    )
                    {
                        data.Active = false;
                        if (data is ISmgActive)
                            ((ISmgActive)data).SmgActive = false;

                        result = await QueryFactory.UpsertData<AccommodationRoomV2>(
                               data,
                               new DataInfo("accommodationrooms", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                               new EditInfo("lts.accommodations.roows.import.deactivate", importerURL),
                               new CRUDConstraints(),
                               new CompareConfig(true, false)
                        );

                        deletedisableresult = new UpdateDetail()
                        {
                            created = result.created,
                            updated = result.updated,
                            deleted = result.deleted,
                            error = result.error,
                            objectchanged = result.objectchanged,
                            objectimagechanged = result.objectimagechanged,
                            comparedobjects = result.compareobject != null && result.compareobject.Value ? 1 : 0,
                            pushchannels = result.pushchannels,
                            changes = result.changes,
                        };
                    }
                }
            }

            return deletedisableresult;
        }

        #region Helpers

        public async Task<UpdateDetail> ReGeneratedAccommodationRoomsList(string id)
        {

        }

        #endregion

    }

}
