// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DataModel.helpers;
using Helper;
using Helper.Extensions;
using Helper.Generic;
using Helper.IDM;
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiActivityImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiActivityImportHelper(
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
            var activitylts = await GetActivitiesFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (activitylts != null && activitylts.FirstOrDefault().ContainsKey("success") && (Boolean)activitylts.FirstOrDefault()["success"]) //&& gastronomylts.FirstOrDefault()["Success"] == true
            {     
                //Import Single Data & Deactivate Data
                return await SaveActivitiesToPG(activitylts);                
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (activitylts != null && activitylts.FirstOrDefault().ContainsKey("status") && ((int)activitylts.FirstOrDefault()["status"] == 403 || (int)activitylts.FirstOrDefault()["status"] == 404))
            {
                var resulttoreturn = default(UpdateDetail);

                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    resulttoreturn = await DeleteOrDisableActivitiesData(id, false, false);
                    if(activitylts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(activitylts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + activitylts.FirstOrDefault()["message"].ToString() +"|";
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    resulttoreturn = await DeleteOrDisableActivitiesData(id, true, true);
                    if (activitylts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(activitylts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + "opendata:" + activitylts.FirstOrDefault()["message"].ToString() + "|";
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
            var lastchangedlts = await GetActivitiesFromLTSV2(null, lastchanged, null, null);
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
                    "lastchanged.activities",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.activities",
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
            var deletedlts = await GetActivitiesFromLTSV2(null, null, deletedfrom, null);
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
                    "deleted.activities",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.activities",
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
            var activelistlts = await GetActivitiesFromLTSV2(null, null, null, active);
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
                    "active.activities",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.activities",
                        success = false,
                        error = "Could not fetch active List",
                    }
                );
            }

            return activeList;
        }
    
        private async Task<List<JObject>> GetActivitiesFromLTSV2(string poiid, DateTime? lastchanged, DateTime? deletedfrom, bool? activelist)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi(opendata);
                
                if(poiid != null)
                {
                    //Get Single Activity

                    var qs = new LTSQueryStrings() { page_size = 1 };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.ActivityDetailRequest(poiid, dict);
                }
                else if (lastchanged != null)
                {
                    //Get the Last Changed Activities list

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyTourismOrganizationMember = false }; //To check filter_onlyTourismOrganizationMember

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.ActivityListRequest(dict, true);
                }
                else if (deletedfrom != null)
                {
                    //Get the Active Activities list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyTourismOrganizationMember = false };
                
                    if (deletedfrom != null)
                        qs.filter_lastUpdate = deletedfrom;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.ActivityDeletedRequest(dict, true);
                }
                else if (activelist != null)
                {
                    //Get the Active Activies list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyActive = true, filter_onlyTourismOrganizationMember = false, filter_representationMode = "full" };
                
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
                    "list.activities",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.activities",
                        success = false,
                        error = ex.Message,
                    }
                );
                return null;
            }
        }

        private async Task<UpdateDetail> SaveActivitiesToPG(List<JObject> ltsdata)
        {            
            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSActivity> activitydata = new List<LTSActivity>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    activitydata.Add(
                        ltsdatasingle.ToObject<LTSActivity>()
                    );
                }

                //Load the json Data
                IDictionary<string, JArray> jsondata = default(Dictionary<string, JArray>);
                
                jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                settings.JsonConfig.Jsondir,
                new List<string>()
                    {
                        "GenericTags",
                        "ODHTagsSourceIDMLTS",
                        "LTSTagsAndTins",                            
                        "ActivityPoiDisplayAsCategory",
                        "AutoPublishTags"
                    }
                );                              

                var metainfosidm = await QueryFactory
                    .Query("odhactivitypoimetainfos")
                    .Select("data")
                    .Where("id", "metainfoexcelsmgpoi")
                    .GetObjectSingleAsync<MetaInfosOdhActivityPoi>();
                

                foreach (var data in activitydata)
                {
                    string id = data.data.rid.ToLower();

                    var activityparsed = ActivityParser.ParseLTSActivity(data.data, false);

                    //POPULATE LocationInfo TO CHECK if this works for new activities...
                    activityparsed.LocationInfo = await activityparsed.UpdateLocationInfoExtension(
                        QueryFactory
                    );

                    //DistanceCalculation
                    await activityparsed.UpdateDistanceCalculation(QueryFactory);

                    //GET OLD Activity
                    var activityindb = await LoadDataFromDB<ODHActivityPoiLinked>("smgpoi" + id, IDStyle.lowercase);

                    await CompleteLTSTagsAndAddLTSParentAsTag(activityparsed, jsondata);
                    
                    //Add manual assigned Tags to TagIds TO check if this should be activated
                    await MergeActivityTags(activityparsed, activityindb);
               
                    //**BEGIN If on opendata IDM Categorization is no more wanted move this to the if(!opendata) section

                    //Preserves all manually assigned ODHTags, and adds all Mapped ODHTags
                    await AssignODHTags(activityparsed, activityindb, jsondata);

                    //Add Difficulty as Tag, Skilift type as Tag
                    AddActivitySpecialCases(activityparsed);

                    //TODO Maybe we can disable this withhin the Api Switch
                    //Traduce all Tags with Source IDM to english tags, CONSIDER TagId "activity" is added here
                    await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                        activityparsed,
                        jsondata != null && jsondata["GenericTags"] != null ? jsondata["GenericTags"].ToObject<List<TagLinked>>() : null
                    );

                    //**END

                    if (!opendata)
                    {
                        //Reassign Outdooractive Sync Values
                        await ReassignOutdooractiveMapping(activityparsed, activityindb);

                        //Add the MetaTitle for IDM
                        await AddIDMMetaTitleAndDescription(activityparsed, metainfosidm);                        
                    }

                    //When requested with opendata Interface does not return isActive field
                    //All data returned by opendata interface are active by default
                    if (opendata)
                    {
                        activityparsed.Active = true;
                        activityparsed.SmgActive = true;
                    }

                    SetAdditionalInfosCategoriesByODHTags(activityparsed, jsondata);

                    //Create Tags and preserve the old TagEntries
                    await activityparsed.UpdateTagsExtension(QueryFactory, await FillTagsObject.GetTagEntrysToPreserve(activityparsed));

                    //Preserve old Values
                    PreserveOldValues(activityparsed, activityindb);

                    //Fill AdditionalProperties
                    activityparsed.FillLTSActivityAdditionalProperties();
                    activityparsed.FillIDMPoiAdditionalProperties();

                    var result = await InsertDataToDB(activityparsed, data.data, jsondata);

                    //newimportcounter = newimportcounter + result.created ?? 0;
                    //updateimportcounter = updateimportcounter + result.updated ?? 0;
                    //errorimportcounter = errorimportcounter + result.error ?? 0;

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
                        "single.activities",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.activities",
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
            ODHActivityPoiLinked objecttosave,
            LTSActivityData poilts,
            IDictionary<string, JArray>? jsonfiles
        )
        {
            try
            {                
                //Set LicenseInfo
                objecttosave.LicenseInfo = LicenseHelper.GetLicenseforOdhActivityPoi(objecttosave, opendata);

                //TODO!
                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave, opendata);

                if (!opendata)
                {
                    //Add the PublishedOn Logic
                    //Exception here all Tags with autopublish has to be passed
                    var autopublishtaglist = jsonfiles != null && jsonfiles["AutoPublishTags"] != null ? jsonfiles["AutoPublishTags"].ToObject<List<AllowedTags>>() : null;
                    //Set PublishedOn with allowedtaglist
                    objecttosave.CreatePublishedOnList(autopublishtaglist);
                }
                else
                    objecttosave.PublishedOn = new List<string>();

                var rawdataid = await InsertInRawDataDB(poilts);

                //Prefix Activity with "smgpoi" Id
                objecttosave.Id = "smgpoi" + objecttosave.Id.ToLower();                

                return await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                    objecttosave,
                    new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                    new EditInfo("lts.activities.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(LTSActivityData activitylts)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(activitylts),
                    sourceinterface = "activities",
                    sourceid = activitylts.rid,
                    sourceurl = "https://go.lts.it/api/v1/activities",
                    type = "odhactivitypoi",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
        
        public async Task<UpdateDetail> DeleteOrDisableActivitiesData(string id, bool delete, bool reduced)
        {
            UpdateDetail deletedisableresult = default(UpdateDetail);

            PGCRUDResult result = default(PGCRUDResult);

            if (delete)
            {
                result = await QueryFactory.DeleteData<ODHActivityPoiLinked>(
                "smgpoi" + id.ToLower(),
                new DataInfo("smgpois", CRUDOperation.Delete),
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
                var query = QueryFactory.Query(table).Select("data").Where("id", "smgpoi" + id.ToLower());

                var data = await query.GetObjectSingleAsync<ODHActivityPoiLinked>();

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

                        result = await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                               data,
                               new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                               new EditInfo("lts.activities.import.deactivate", importerURL),
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
     
        //Adds all Redactional Assigned Tags from the old Record to the new Record
        private async Task MergeActivityTags(ODHActivityPoiLinked poiNew, ODHActivityPoiLinked poiOld)
        {
            if (poiOld != null)
            {                                
                //Readd all Redactional Tags to check if this query fits
                var redactionalassignedTags = poiOld.Tags != null ? poiOld.Tags.Where(x => x.Source != "lts" && x.Source != "idm").ToList() : null;
                if (redactionalassignedTags != null)
                {
                    foreach (var tag in redactionalassignedTags)
                    {
                        poiNew.TagIds.Add(tag.Id);
                    }
                }
            }
            
            //TODO import the Redactional Tags from SmgTags into Tags?

            //TODO same procedure on Tags? (Remove all Tags that come from the sync and readd the redactional assigned Tags)
        }

        #region OLD Compatibility Stufff

        //Activities ODHTags assignment. Removes all automatically added Tags and readds all manual assigned Tags
        private async Task AssignODHTags(ODHActivityPoiLinked activityNew, ODHActivityPoiLinked activityOld, IDictionary<string, JArray>? jsonfiles)
        {
            List<ODHTagLinked> tagstoremove = jsonfiles != null && jsonfiles["ODHTagsSourceIDMLTS"] != null ? jsonfiles["ODHTagsSourceIDMLTS"].ToObject<List<ODHTagLinked>>() : null;

            List<string> tagstopreserve = new List<string>();
            if (activityNew.SmgTags == null)
                activityNew.SmgTags = new List<string>();

            //Remove all ODHTags that where automatically assigned
            if (activityNew != null && activityOld != null && activityOld.SmgTags != null && tagstoremove != null)
                tagstopreserve = activityOld.SmgTags.Except(tagstoremove.Select(x => x.Id)).ToList();

            //Add the activity Tag
            if (!activityNew.SmgTags.Contains("activity"))
                activityNew.SmgTags.Add("activity");

            //Readd all mapped Tags
            foreach (var ltstag in activityNew.TagIds)
            {
                if (tagstoremove != null)
                {
                    //load
                    var ltstagsinlist = tagstoremove.Where(x => x.LTSTaggingInfo != null && x.LTSTaggingInfo.LTSRID == ltstag);

                    if (ltstagsinlist != null)
                    {
                        foreach (var ltstaginlist in ltstagsinlist)
                        {
                            //Add LTS Tag id
                            if (!activityNew.SmgTags.Contains(ltstaginlist.Id))
                                activityNew.SmgTags.Add(ltstaginlist.Id);
                            //Add the mapped Tags
                            foreach (var mappedtag in ltstaginlist.MappedTagIds)
                            {
                                if (!activityNew.SmgTags.Contains(mappedtag))
                                    activityNew.SmgTags.Add(mappedtag);
                            }

                            //Handle also the LTS Parent Tags
                            if (ltstaginlist.Mapping != null && ltstaginlist.Mapping.ContainsKey("lts"))
                            {
                                if (ltstaginlist.Mapping["lts"].ContainsKey("parent_id"))
                                {
                                    if (!activityNew.SmgTags.Contains(ltstaginlist.Mapping["lts"]["parent_id"]))
                                        activityNew.SmgTags.Add(ltstaginlist.Mapping["lts"]["parent_id"]);
                                }
                            }
                        }
                    }
                }
            }

            //Readd Tags to preserve
            foreach (var tagtopreserve in tagstopreserve)
            {
                activityNew.SmgTags.Add(tagtopreserve);
            }

            activityNew.SmgTags.RemoveEmptyStrings();
        }

        //Metadata assignment detailde.MetaTitle = detailde.Title + " | suedtirol.info";
        private async Task AddIDMMetaTitleAndDescription(ODHActivityPoiLinked activityNew, MetaInfosOdhActivityPoi metainfo)
        {
            IDMCustomHelper.SetMetaInfoForActivityPoi(activityNew, metainfo);
        }

        private async Task ReassignOutdooractiveMapping(ODHActivityPoiLinked poiNew, ODHActivityPoiLinked poiOld)
        {
            var oamapping = new Dictionary<string, string>() {  };

            if (poiOld != null)
            {
                if (poiOld.OutdooractiveElevationID != null)
                {
                    poiNew.OutdooractiveElevationID = poiOld.OutdooractiveElevationID;
                    oamapping.Add("elevationid", poiOld.OutdooractiveElevationID);
                }

                if (poiOld.OutdooractiveElevationID != null)
                {
                    poiNew.OutdooractiveID = poiOld.OutdooractiveID;
                    oamapping.Add("id", poiOld.OutdooractiveID);
                }
            }

            //Add to Mapping
            if (oamapping.Count > 0)
                poiNew.Mapping.Add("outdooractive", oamapping);
        }        

        private async Task CompleteLTSTagsAndAddLTSParentAsTag(ODHActivityPoiLinked poiNew, IDictionary<string, JArray>? jsonfiles)
        {
            var ltstagsandtins = jsonfiles != null && jsonfiles["LTSTagsAndTins"] != null ? jsonfiles["LTSTagsAndTins"].ToObject<List<TagLinked>>() : null;

            var tagstoadd = new List<string>();

            //TO TEST
            if (ltstagsandtins != null)
            {
                foreach(var tag in poiNew.TagIds)
                {
                    GetAllLTSParentTagsRecursively(tag, tagstoadd, ltstagsandtins);
                }
            }

            foreach (var tag in tagstoadd)
            {
                if(!poiNew.TagIds.Contains(tag))
                    poiNew.TagIds.Add(tag);

                if(poiNew.LTSTags.Where(x => x.LTSRID == tag).Count() == 0)
                    poiNew.LTSTags.Add(new LTSTagsLinked() { LTSRID = tag });
            }

            if (ltstagsandtins != null)
            {
                //Complete LTSTags
                foreach (var tag in poiNew.LTSTags)
                {
                    //Search the Tag and check if it has a Parent
                    var ltstag = ltstagsandtins.Where(x => x.Id == tag.LTSRID).FirstOrDefault();
                    if (ltstag != null)
                    {
                        tag.TagName = ltstag.TagName?.Where(kvp => poiNew.HasLanguage?.Contains(kvp.Key) == true)
                                      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                                      ?? new Dictionary<string, string>();
                        tag.Level = ltstag.Mapping != null && ltstag.Mapping.ContainsKey("lts") && ltstag.Mapping["lts"].ContainsKey("level") && int.TryParse(ltstag.Mapping["lts"]["level"], out int taglevel) ? taglevel : 0;
                        tag.Id = ltstag.TagName.ContainsKey("de") ? ltstag.TagName["de"].ToLower() : "";

                        //Add the TIN Info
                        if(tag.LTSTins != null && tag.LTSTins.Count > 0)
                        {
                            foreach(var tin in tag.LTSTins)
                            {
                                var ltstin = ltstagsandtins.Where(x => x.Id == tin.LTSRID).FirstOrDefault();
                                if (ltstin != null)
                                {
                                    tin.TinName = ltstin.TagName?.Where(kvp => poiNew.HasLanguage?.Contains(kvp.Key) == true)
                                      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                                      ?? new Dictionary<string, string>();
                                    tin.Id = ltstag.TagName.ContainsKey("de") ? ltstag.TagName["de"].ToLower() : "";
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void GetAllLTSParentTagsRecursively(string ltstagid, List<string> parenttags, List<TagLinked>? ltstagsandtins)
        {            
            //Search the Tag and check if it has a Parent
            var ltstag = ltstagsandtins.Where(x => x.Id == ltstagid).FirstOrDefault();

            if (ltstag != null)
            {
                if (ltstag.Mapping != null && ltstag.Mapping.ContainsKey("lts") && ltstag.Mapping["lts"].ContainsKey("parentTagRid") && ltstag.Mapping["lts"]["parentTagRid"] != null)
                {
                    if(!parenttags.Contains(ltstag.Mapping["lts"]["parentTagRid"]))
                        parenttags.Add(ltstag.Mapping["lts"]["parentTagRid"]);

                    if (ltstag.Mapping != null && ltstag.Mapping.ContainsKey("lts") && ltstag.Mapping["lts"].ContainsKey("level") && ltstag.Mapping["lts"]["level"] == "2")
                    {
                        GetAllLTSParentTagsRecursively(ltstag.Mapping["lts"]["parentTagRid"], parenttags, ltstagsandtins);
                    }
                }
            }
        }

        private static void SetAdditionalInfosCategoriesByODHTags(ODHActivityPoiLinked activityNew, IDictionary<string, JArray>? jsonfiles)
        {
            //TO CHECK
            //SET ADDITIONALINFOS
            //Setting Categorization by Valid Tags
            var validcategorylist = jsonfiles != null && jsonfiles["ActivityPoiDisplayAsCategory"] != null ? jsonfiles["ActivityPoiDisplayAsCategory"].ToObject<List<CategoriesTags>>() : null;

            if (validcategorylist != null && activityNew.SmgTags != null)
            {
                var currentcategories = validcategorylist.Where(x => activityNew.SmgTags.Select(y => y.ToLower()).Contains(x.Id.ToLower())).ToList();

                if (currentcategories != null)
                {
                    if (activityNew.AdditionalPoiInfos == null)
                        activityNew.AdditionalPoiInfos = new Dictionary<string, AdditionalPoiInfos>();

                    foreach (var languagecategory in new List<string>() { "de", "it", "en", "nl", "cs", "pl", "fr", "ru" })
                    {
                        //Do not overwrite Novelty
                        string? novelty = null;
                        if (activityNew.AdditionalPoiInfos.ContainsKey(languagecategory) && !String.IsNullOrEmpty(activityNew.AdditionalPoiInfos[languagecategory].Novelty))
                            novelty = activityNew.AdditionalPoiInfos[languagecategory].Novelty;


                        AdditionalPoiInfos additionalPoiInfos = new AdditionalPoiInfos() { Language = languagecategory, Categories = new List<string>(), Novelty = novelty };

                        //Reassigning Categories
                        foreach (var smgtagtotranslate in currentcategories)
                        {
                            if (smgtagtotranslate.TagName.ContainsKey(languagecategory))
                            {
                                if (!additionalPoiInfos.Categories.Contains(smgtagtotranslate.TagName[languagecategory].Trim()))
                                    additionalPoiInfos.Categories.Add(smgtagtotranslate.TagName[languagecategory].Trim());
                            }
                        }

                        activityNew.AdditionalPoiInfos.TryAddOrUpdate(languagecategory, additionalPoiInfos);
                    }
                }
            }
        }

        private static void AddActivitySpecialCases(ODHActivityPoiLinked poiNew)
        {
            //If it is a slope / skitrack activity add the difficulty as ODHTag

            if (poiNew != null && poiNew.LTSTags != null &&
                (poiNew.LTSTags.Select(x => x.LTSRID).ToList().Contains("D544A6312F8A47CF80CC4DFF8833FE50") || poiNew.LTSTags.Select(x => x.Id).ToList().Contains("EB5D6F10C0CB4797A2A04818088CD6AB")) 
                && !String.IsNullOrEmpty(poiNew.Difficulty))
            {
                if(poiNew.SmgTags == null)
                    poiNew.SmgTags = new List<string>();

                if (poiNew.Difficulty == "1" || poiNew.Difficulty == "2")
                    if(!poiNew.SmgTags.Contains("blau"))
                        poiNew.SmgTags.Add("blau");
                if (poiNew.Difficulty == "3" || poiNew.Difficulty == "4")
                    if (!poiNew.SmgTags.Contains("rot"))
                        poiNew.SmgTags.Add("rot");
                if (poiNew.Difficulty == "5" || poiNew.Difficulty == "6")
                    if (!poiNew.SmgTags.Contains("schwarz"))
                        poiNew.SmgTags.Add("schwarz");
            }


            //If it is a lift, add the Mapping.liftType and Mapping.liftCapacityType as ODHTag
            if (poiNew != null && poiNew.LTSTags != null &&
                    poiNew.LTSTags.Select(x => x.LTSRID).ToList().Contains("E23AA37B2AE3477F96D1C0782195AFDF"))
            {
                if (poiNew.Mapping != null && poiNew.Mapping.ContainsKey("lts"))
                {
                    if (poiNew.SmgTags == null)
                        poiNew.SmgTags = new List<string>();

                    if (poiNew.Mapping["lts"].ContainsKey("liftType"))
                    {
                        switch (poiNew.Mapping["lts"]["liftType"])
                        {
                            case "gondolaRopeway":
                                if (!poiNew.SmgTags.Contains("Kabinenbahn".ToLower()))
                                    poiNew.SmgTags.Add("Kabinenbahn".ToLower());
                                break;
                            case "chairlift":
                                if (!poiNew.SmgTags.Contains("Sessellift".ToLower()))
                                    poiNew.SmgTags.Add("Sessellift".ToLower());
                                break;
                            case "skiLift":
                                if (!poiNew.SmgTags.Contains("Skilift".ToLower()))
                                    poiNew.SmgTags.Add("Skilift".ToLower());
                                break;
                            case "cableCar":
                                if (!poiNew.SmgTags.Contains("Seilbahn".ToLower()))
                                    poiNew.SmgTags.Add("Seilbahn".ToLower());
                                break;
                            case "detachableGondolaRopeway":
                                if (!poiNew.SmgTags.Contains("Umlaufbahn".ToLower()))
                                    poiNew.SmgTags.Add("Umlaufbahn".ToLower());
                                break;
                            case "skibus":
                                if (!poiNew.SmgTags.Contains("Skibus".ToLower()))
                                    poiNew.SmgTags.Add("Skibus".ToLower());
                                break;
                            case "inclinedLift":
                                if (!poiNew.SmgTags.Contains("Schrägaufzug".ToLower()))
                                    poiNew.SmgTags.Add("Schrägaufzug".ToLower());
                                break;
                            case "conveyorBelt":
                                if (!poiNew.SmgTags.Contains("Förderband".ToLower()))
                                    poiNew.SmgTags.Add("Förderband".ToLower());
                                break;
                            case "undergroundRopeway":
                                if (!poiNew.SmgTags.Contains("Unterirdische Bahn".ToLower()))
                                    poiNew.SmgTags.Add("Unterirdische Bahn".ToLower());
                                break;
                            case "telemixLift":
                                if (!poiNew.SmgTags.Contains("Telemix".ToLower()))
                                    poiNew.SmgTags.Add("Telemix".ToLower());
                                break;
                            case "cableRailway":
                                if (!poiNew.SmgTags.Contains("Standseilbahn Zahnradbahn".ToLower()))
                                    poiNew.SmgTags.Add("Standseilbahn Zahnradbahn".ToLower());
                                break;
                            case "train":
                                if (!poiNew.SmgTags.Contains("Zug".ToLower()))
                                    poiNew.SmgTags.Add("Zug".ToLower());
                                break;
                        }

                    }
                    if (poiNew.Mapping["lts"].ContainsKey("liftCapacityType"))
                    {
                        switch (poiNew.Mapping["lts"]["liftCapacityType"])
                        {
                            case "chairliftForOnePerson":
                                if (!poiNew.SmgTags.Contains("1er Sessellift kuppelbar".ToLower()))
                                    poiNew.SmgTags.Add("1er Sessellift kuppelbar".ToLower());
                                break;
                            case "chairliftForTwoPersons":
                                if (!poiNew.SmgTags.Contains("2er Sessellift kuppelbar".ToLower()))
                                    poiNew.SmgTags.Add("2er Sessellift kuppelbar".ToLower());
                                break;
                            case "chairliftForThreePersons":
                                if (!poiNew.SmgTags.Contains("3er Sessellift kuppelbar".ToLower()))
                                    poiNew.SmgTags.Add("3er Sessellift kuppelbar".ToLower());
                                break;
                            case "chairliftForFourPersons":
                                if (!poiNew.SmgTags.Contains("4er Sessellift kuppelbar".ToLower()))
                                    poiNew.SmgTags.Add("4er Sessellift kuppelbar".ToLower());
                                break;
                            case "chairliftForSixPersons":
                                if (!poiNew.SmgTags.Contains("6er Sessellift kuppelbar".ToLower()))
                                    poiNew.SmgTags.Add("6er Sessellift kuppelbar".ToLower());
                                break;
                            case "chairliftForEightPersons":
                                if (!poiNew.SmgTags.Contains("8er Sessellift kuppelbar".ToLower()))
                                    poiNew.SmgTags.Add("8er Sessellift kuppelbar".ToLower());
                                break;
                            case "lowProfileSkiLift":
                                if (!poiNew.SmgTags.Contains("Kleinskilift".ToLower()))
                                    poiNew.SmgTags.Add("Kleinskilift".ToLower());
                                break;                            
                        }
                    }
                }
            }
        }

        //Insert here all manually edited values to preserve
        private static void PreserveOldValues(ODHActivityPoiLinked poiNew, ODHActivityPoiLinked poiOld)
        {
            if (poiOld != null)
            {
                if (poiOld.AgeFrom != null && poiOld.AgeFrom > 0)
                    poiNew.AgeFrom = poiOld.AgeFrom;
                if (poiOld.AgeTo != null && poiOld.AgeTo > 0)
                    poiNew.AgeTo = poiOld.AgeTo;
            }
        }

        #endregion

    }
}
