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
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhApiImporter.Helpers.RAVEN;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiMeasuringpointImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiMeasuringpointImportHelper(
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
            var measuringpointlts = await GetMeasuringpointsFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (measuringpointlts != null && measuringpointlts.FirstOrDefault().ContainsKey("success") && (Boolean)measuringpointlts.FirstOrDefault()["success"]) //&& gastronomylts.FirstOrDefault()["Success"] == true
            {     //Import Single Data & Deactivate Data
                var result = await SaveMeasuringpointsToPG(measuringpointlts, cancellationToken);
                return result;
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (measuringpointlts != null && measuringpointlts.FirstOrDefault().ContainsKey("status") && ((int)measuringpointlts.FirstOrDefault()["status"] == 403 || (int)measuringpointlts.FirstOrDefault()["status"] == 404))
            {
                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    return await DeleteOrDisableMeasuringpointsData(id, false, false);
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    return await DeleteOrDisableMeasuringpointsData(id, true, true);
                }
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
            var lastchangedlts = await GetMeasuringpointsFromLTSV2(null, lastchanged, null, null);
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
                    "lastchanged.measuringpoints",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.measuringpoints",
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
            var deletedlts = await GetMeasuringpointsFromLTSV2(null, null, deletedfrom, null);
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
                    "deleted.measuringpoints",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.measuringpoints",
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
            var activelistlts = await GetMeasuringpointsFromLTSV2(null, null, null, active);
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
                    "active.measuringpoints",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.measuringpoints",
                        success = false,
                        error = "Could not fetch active List",
                    }
                );
            }

            return activeList;
        }

        private async Task<List<JObject>> GetMeasuringpointsFromLTSV2(string measuringpointid, DateTime? lastchanged, DateTime? deletedfrom, bool? activelist)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi(opendata);
                
                if(measuringpointid != null)
                {
                    //Get Single Venue

                    var qs = new LTSQueryStrings() { page_size = 1 };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.WeatherSnowDetailRequest(measuringpointid, dict);
                }
                else if (lastchanged != null)
                {
                    //Get the Last Changed Measuringpoints list

                    var qs = new LTSQueryStrings() { fields = "rid" };

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.WeatherSnowListRequest(dict, true);
                }
                else if (deletedfrom != null)
                {
                    //Get the Active Measuringpoints list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid" };
                
                    if (deletedfrom != null)
                        qs.filter_lastUpdate = deletedfrom;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.WeatherSnowDeletedRequest(dict, true);
                }
                else if (activelist != null)
                {
                    //Get the Active Measuringpoints list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyActive = true, filter_representationMode = "full" };
                
                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.WeatherSnowListRequest(dict, true);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.measuringpoints",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.measuringpoints",
                        success = false,
                        error = ex.Message,
                    }
                );
                return null;
            }
        }

        private async Task<UpdateDetail> SaveMeasuringpointsToPG(List<JObject> ltsdata, CancellationToken cancellationToken = default)
        {            
            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSWeatherSnows> weathersnowdata = new List<LTSWeatherSnows>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    weathersnowdata.Add(
                        ltsdatasingle.ToObject<LTSWeatherSnows>()
                    );
                }

                //Load the json Data
                IDictionary<string, JArray> jsondata = default(Dictionary<string, JArray>);

                if (!opendata)
                {
                    jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                    settings.JsonConfig.Jsondir,
                    new List<string>()
                        {
                           "GenericTags"
                        }
                    );
                }

                foreach (var data in weathersnowdata)
                {
                    string id = data.data.rid.ToUpper();

                    var measuringpointparsed = MeasuringpointParser.ParseLTSMeasuringpoint(data.data, false);

                    //GET OLD Measuringpoint TO CHECK MeasuringpointV2 vs MeasuringpointLinked
                    var measuringpointindb = await LoadDataFromDB<MeasuringpointV2>(id, IDStyle.uppercase);

                    await MergeMeasuringpointTags(measuringpointparsed, measuringpointindb);

                    //POPULATE LocationInfo not working on Gastronomies because DistrictInfo is prefilled! DistrictId not available on root level...
                    measuringpointparsed.LocationInfo = await measuringpointparsed.UpdateLocationInfoExtension(
                        QueryFactory
                    );

                    //DistanceCalculation
                    await measuringpointparsed.UpdateDistanceCalculation(QueryFactory);

                    //Create Tags and preserve the old TagEntries
                    await measuringpointparsed.UpdateTagsExtension(QueryFactory);

                    await AssignSkiAreaIDs(measuringpointparsed, cancellationToken);

                    var result = await InsertDataToDB(measuringpointparsed, data.data, jsondata);
          
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
                        "single.measuringpoints",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.measuringpoints",
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
            MeasuringpointV2 objecttosave,
            LTSWeatherSnowsData weathersnowlts,
            IDictionary<string, JArray>? jsonfiles
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = LicenseHelper.GetLicenseforMeasuringpoint(objecttosave, opendata);

                //TODO!
                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave, opendata);

                //Add the PublishedOn Logic
                //Exception here all Tags with autopublish has to be passed
                //var autopublishtaglist = jsonfiles != null && jsonfiles["AutoPublishTags"] != null ? jsonfiles["AutoPublishTags"].ToObject<List<AllowedTags>>() : null;
                //Set PublishedOn with allowedtaglist
                objecttosave.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(weathersnowlts);
                
                objecttosave.Id = objecttosave.Id.ToLower();

                return await QueryFactory.UpsertData<MeasuringpointV2>(
                    objecttosave,
                    new DataInfo("measuringpoints", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                    new EditInfo("lts.measuringpoints.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid,
                    opendata
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(LTSWeatherSnowsData weathersnowslts)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(weathersnowslts),
                    sourceinterface = "weathersnows",
                    sourceid = weathersnowslts.rid,
                    sourceurl = "https://go.lts.it/api/v1/weathersnows",
                    type = "measuringpoint",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
        
        public async Task<UpdateDetail> DeleteOrDisableMeasuringpointsData(string id, bool delete, bool reduced)
        {
            UpdateDetail deletedisableresult = default(UpdateDetail);

            PGCRUDResult result = default(PGCRUDResult);

            if (delete)
            {
                result = await QueryFactory.DeleteData<MeasuringpointV2>(
                id.ToLower(),
                new DataInfo("measuringpoints", CRUDOperation.Delete),
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
                var query = QueryFactory.Query(table).Select("data").Where("id", id.ToLower());

                var data = await query.GetObjectSingleAsync<MeasuringpointV2>();

                if (data != null)
                {
                    if (
                        data.Active != false
                        || (data is ISmgActive && ((ISmgActive)data).SmgActive != false)
                        || (data.PublishedOn != null && data.PublishedOn.Count > 0)
                    )
                    {
                        data.Active = false;
                        if (data is ISmgActive)
                            ((ISmgActive)data).SmgActive = false;

                        //Recreate PublishedOn Helper for not active Items
                        data.CreatePublishedOnList();

                        result = await QueryFactory.UpsertData<MeasuringpointV2>(
                               data,
                               new DataInfo("measuringpoints", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                               new EditInfo("lts.measuringpoints.import.deactivate", importerURL),
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
                            comparedobjects =
                        result.compareobject != null && result.compareobject.Value ? 1 : 0,
                            pushchannels = result.pushchannels,
                            changes = result.changes,
                        };
                    }
                }
            }

            return deletedisableresult;
        }

     
        private async Task MergeMeasuringpointTags(MeasuringpointV2 vNew, MeasuringpointV2 vOld)
        {
            if (vOld != null)
            {
                //Readd all Redactional Tags to check if this query fits
                var redactionalassignedTags = vOld.Tags != null ? vOld.Tags.Where(x => x.Source != "lts" && x.Source != "idm").ToList() : null;
                if (redactionalassignedTags != null)
                {
                    foreach (var tag in redactionalassignedTags)
                    {
                        vNew.TagIds.Add(tag.Id);
                    }
                }
            }
        }
        
        private async Task AssignSkiAreaIDs(MeasuringpointV2 measuringpoint, CancellationToken cancellationToken)
        {
            //Measuringpoint, Fill SkiAreaIds
            if (measuringpoint.AreaIds != null)
            {
                measuringpoint.SkiAreaIds = await QueryFactory
                    .Query()
                    .GetSkiAreaIdsfromSkiAreasAsync(measuringpoint.AreaIds, cancellationToken);
            }
        }
    }
}
