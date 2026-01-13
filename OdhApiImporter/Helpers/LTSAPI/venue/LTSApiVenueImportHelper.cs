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
using SqlKata.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiVenueImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiVenueImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }


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
            var venuelts = await GetVenuesFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (venuelts != null && venuelts.FirstOrDefault().ContainsKey("success") && (Boolean)venuelts.FirstOrDefault()["success"]) //&& gastronomylts.FirstOrDefault()["Success"] == true
            {     //Import Single Data & Deactivate Data
                var result = await SaveVenuesToPG(venuelts);
                return result;
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (venuelts != null && venuelts.FirstOrDefault().ContainsKey("status") && ((int)venuelts.FirstOrDefault()["status"] == 403 || (int)venuelts.FirstOrDefault()["status"] == 404))
            {
                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    return await DeleteOrDisableVenuesData(id, false, false);
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    return await DeleteOrDisableVenuesData(id, true, true);
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
            var lastchangedlts = await GetVenuesFromLTSV2(null, lastchanged, null, null);
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
                    "lastchanged.venues",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.venues",
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
            var deletedlts = await GetVenuesFromLTSV2(null, null, deletedfrom, null);
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
                    "deleted.venues",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.venues",
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
            var activelistlts = await GetVenuesFromLTSV2(null, null, null, active);
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
                    "active.venues",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.venues",
                        success = false,
                        error = "Could not fetch active List",
                    }
                );
            }

            return activeList;
        }

        private async Task<List<JObject>> GetVenuesFromLTSV2(string venueid, DateTime? lastchanged, DateTime? deletedfrom, bool? activelist)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi(opendata);
                
                if(venueid != null)
                {
                    //Get Single Venue

                    var qs = new LTSQueryStrings() { page_size = 1 };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.VenueDetailRequest(venueid, dict);
                }
                else if (lastchanged != null)
                {
                    //Get the Last Changed Venues list

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyTourismOrganizationMember = false }; //To check filter_onlyTourismOrganizationMember

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.VenueListRequest(dict, true);
                }
                else if (deletedfrom != null)
                {
                    //Get the Active Venues list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyTourismOrganizationMember = false };
                
                    if (deletedfrom != null)
                        qs.filter_lastUpdate = deletedfrom;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.VenueDeletedRequest(dict, true);
                }
                else if (activelist != null)
                {
                    //Get the Active Venues list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyActive = true, filter_onlyTourismOrganizationMember = false, filter_representationMode = "full" };
                
                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.VenueListRequest(dict, true);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.venues",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.venues",
                        success = false,
                        error = ex.Message,
                    }
                );
                return null;
            }
        }

        private async Task<UpdateDetail> SaveVenuesToPG(List<JObject> ltsdata)
        {
            //var newimportcounter = 0;
            //var updateimportcounter = 0;
            //var errorimportcounter = 0;
            //var deleteimportcounter = 0;

            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSVenue> venuedata = new List<LTSVenue>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    venuedata.Add(
                        ltsdatasingle.ToObject<LTSVenue>()
                    );
                }

                //Load the json Data
                //IDictionary<string, JArray> jsondata = default(Dictionary<string, JArray>);

                //if (!opendata)
                //{
                //    jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                //    settings.JsonConfig.Jsondir,
                //    new List<string>()
                //        {
                //           "GenericTags",
                //           "AutoPublishTags"
                //        }
                //    );
                //}

                foreach (var data in venuedata)
                {
                    string id = data.data.rid.ToLower();

                    var venueparsed = VenueParser.ParseLTSVenue(data.data, false);

                    //POPULATE LocationInfo
                    venueparsed.LocationInfo = await venueparsed.UpdateLocationInfoExtension(
                        QueryFactory
                    );

                    //DistanceCalculation
                    await venueparsed.UpdateDistanceCalculation(QueryFactory);

                    //GET OLD Venue
                    var venueindb = await LoadDataFromDB<VenueV2>(id, IDStyle.uppercase);

                    //Add manual assigned Tags to TagIds TO check if this should be activated
                    await MergeVenueTags(venueparsed, venueindb);
              
                    if (!opendata)
                    {                        
                        //Create Tags and preserve the old TagEntries                        
                        foreach (var hallfeature in venueparsed.RoomDetails)
                        {
                            await hallfeature.UpdateTagsExtension(QueryFactory, await FillTagsObject.GetTagEntrysToPreserve(hallfeature));
                        }
                    }

                    //Create Tags and preserve the old TagEntries
                    await venueparsed.UpdateTagsExtension(QueryFactory, null);

                    var result = await InsertDataToDB(venueparsed, data.data);

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
                        "single.venues",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.venues",
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
            VenueV2 objecttosave,
            LTSVenueData venuelts
            //IDictionary<string, JArray>? jsonfiles
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = LicenseHelper.GetLicenseforVenue(objecttosave, opendata);
                
                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave, opendata);

                //Add the PublishedOn Logic
                //Exception here all Tags with autopublish has to be passed
                //var autopublishtaglist = jsonfiles != null && jsonfiles["AutoPublishTags"] != null ? jsonfiles["AutoPublishTags"].ToObject<List<AllowedTags>>() : null;
                //Set PublishedOn with allowedtaglist
                objecttosave.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(venuelts);
                
                objecttosave.Id = objecttosave.Id.ToUpper();

                return await QueryFactory.UpsertData<VenueV2>(
                    objecttosave,
                    new DataInfo("venues", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                    new EditInfo("lts.venues.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(LTSVenueData venuelts)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(venuelts),
                    sourceinterface = "venues",
                    sourceid = venuelts.rid,
                    sourceurl = "https://go.lts.it/api/v1/venues",
                    type = "venue",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
        
        public async Task<UpdateDetail> DeleteOrDisableVenuesData(string id, bool delete, bool reduced)
        {
            UpdateDetail deletedisableresult = default(UpdateDetail);

            PGCRUDResult result = default(PGCRUDResult);

            if (delete)
            {
                result = await QueryFactory.DeleteData<VenueV2>(
                id.ToLower(),
                new DataInfo("venues", CRUDOperation.Delete),
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

                var data = await query.GetObjectSingleAsync<VenueV2>();

                if (data != null)
                {
                    if (
                        data.Active != false
                        || (data.PublishedOn != null && data.PublishedOn.Count > 0)
                    )
                    {
                        data.Active = false;

                        //Recreate PublishedOn Helper for not active Items
                        data.CreatePublishedOnList();

                        result = await QueryFactory.UpsertData<VenueV2>(
                               data,
                               new DataInfo("venue", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                               new EditInfo("lts.venues.import.deactivate", importerURL),
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

     
        private async Task MergeVenueTags(VenueV2 vNew, VenueV2 vOld)
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
    }
}
