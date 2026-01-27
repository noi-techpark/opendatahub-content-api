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
using SqlKata.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiPoiImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiPoiImportHelper(
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
            var poilts = await GetPoisFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (poilts != null && poilts.FirstOrDefault().ContainsKey("success") && (Boolean)poilts.FirstOrDefault()["success"]) //&& gastronomylts.FirstOrDefault()["Success"] == true
            {     
                //Import Single Data & Deactivate Data
                return await SavePoisToPG(poilts);
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (poilts != null && poilts.FirstOrDefault().ContainsKey("status") && ((int)poilts.FirstOrDefault()["status"] == 403 || (int)poilts.FirstOrDefault()["status"] == 404))
            {
                var resulttoreturn = default(UpdateDetail);

                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    resulttoreturn = await DeleteOrDisablePoisData(id, false, false);
                    if (poilts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(poilts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + poilts.FirstOrDefault()["message"].ToString() + "|";
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    resulttoreturn = await DeleteOrDisablePoisData(id, true, true);
                    if (poilts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(poilts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + "opendata:" + poilts.FirstOrDefault()["message"].ToString() + "|";
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
            var lastchangedlts = await GetPoisFromLTSV2(null, lastchanged, null, null);
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
                    "lastchanged.pointofinterests",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.pointofinterests",
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
            var deletedlts = await GetPoisFromLTSV2(null, null, deletedfrom, null);
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
                    "deleted.pointofinterests",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.pointofinterests",
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
            var activelistlts = await GetPoisFromLTSV2(null, null, null, active);
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
                    "active.pointofinterests",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.pointofinterests",
                        success = false,
                        error = "Could not fetch active List",
                    }
                );
            }

            return activeList;
        }

        private async Task<List<JObject>> GetPoisFromLTSV2(string poiid, DateTime? lastchanged, DateTime? deletedfrom, bool? activelist)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi(opendata);
                
                if(poiid != null)
                {
                    //Get Single Poi

                    var qs = new LTSQueryStrings() { page_size = 1 };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.PoiDetailRequest(poiid, dict);
                }
                else if (lastchanged != null)
                {                    
                    //Get the Last Changed Pois list

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyTourismOrganizationMember = false }; //To check filter_onlyTourismOrganizationMember

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.PoiListRequest(dict, true);
                }
                else if (deletedfrom != null)
                {
                    //Get the Active Pois list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyTourismOrganizationMember = false };
                
                    if (deletedfrom != null)
                        qs.filter_lastUpdate = deletedfrom;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.PoiDeletedRequest(dict, true);
                }
                else if (activelist != null)
                {
                    //Get the Active Pois list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyActive = true, filter_onlyTourismOrganizationMember = false, filter_representationMode = "full" };
                
                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.PoiListRequest(dict, true);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.pointofinterests",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.pointofinterests",
                        success = false,
                        error = ex.Message,
                    }
                );
                return null;
            }
        }

        private async Task<UpdateDetail> SavePoisToPG(List<JObject> ltsdata)
        {
            //var newimportcounter = 0;
            //var updateimportcounter = 0;
            //var errorimportcounter = 0;
            //var deleteimportcounter = 0;

            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSPointofInterest> poidata = new List<LTSPointofInterest>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    poidata.Add(
                        ltsdatasingle.ToObject<LTSPointofInterest>()
                    );
                }

                //Load the json Data
                IDictionary<string, JArray> jsondata = default(Dictionary<string, JArray>);

                jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                settings.JsonConfig.Jsondir,
                new List<string>()
                    {
                        "ODHTagsSourceIDMLTS",
                        "LTSTagsAndTins",
                        "ActivityPoiDisplayAsCategory",                        
                        "AutoPublishTags",
                        "GenericTags",
                    }
                );

                var metainfosidm = await QueryFactory
                    .Query("odhactivitypoimetainfos")
                    .Select("data")
                    .Where("id", "metainfoexcelsmgpoi")
                    .GetObjectSingleAsync<MetaInfosOdhActivityPoi>();

                foreach (var data in poidata)
                {
                    string id = data.data.rid.ToLower();

                    var poiparsed = PointofInterestParser.ParseLTSPointofInterest(data.data, false);

                    //POPULATE LocationInfo TO CHECK if this works for new pois...
                    poiparsed.LocationInfo = await poiparsed.UpdateLocationInfoExtension(
                        QueryFactory
                    );

                    //DistanceCalculation
                    await poiparsed.UpdateDistanceCalculation(QueryFactory);

                    //GET OLD Poi
                    var poiindb = await LoadDataFromDB<ODHActivityPoiLinked>("smgpoi" + id, IDStyle.lowercase); ;

                    await CompleteLTSTagsAndAddLTSParentAsTag(poiparsed, jsondata);

                    //AddPoiSpecialCases(poiparsed);

                    //Add manual assigned Tags to TagIds TO check if this should be activated
                    await MergePoiTags(poiparsed, poiindb);

                    //**BEGIN If on opendata IDM Categorization is no more wanted move this to the if(!opendata) section

                    //Preserves all manually assigned ODHTags, and adds tall Mapped ODHTags
                    await AssignODHTags(poiparsed, poiindb, jsondata);

                    //TODO Maybe we can disable this withhin the Api Switch
                    //Traduce all Tags with Source IDM to english tags, CONSIDER TagId "poi" is added here
                    await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                        poiparsed,
                        jsondata != null && jsondata["GenericTags"] != null ? jsondata["GenericTags"].ToObject<List<TagLinked>>() : null
                    );

                    //**END

                    if (!opendata)
                    {
                        //Reassign Outdooractive Sync Values
                        await ReassignOutdooractiveMapping(poiparsed, poiindb);

                        //Add the MetaTitle for IDM
                        await AddIDMMetaTitleAndDescription(poiparsed, metainfosidm);
                    }

                    //When requested with opendata Interface does not return isActive field
                    //All data returned by opendata interface are active by default
                    if (opendata)
                    {
                        poiparsed.Active = true;
                        poiparsed.SmgActive = true;
                    }

                    SetAdditionalInfosCategoriesByODHTags(poiparsed, jsondata);

                    //Create Tags and preserve the old TagEntries
                    await poiparsed.UpdateTagsExtension(QueryFactory, await FillTagsObject.GetTagEntrysToPreserve(poiparsed));

                    //Preserve old Values
                    PreserveOldValues(poiparsed, poiindb);

                    //Fill AdditionalProperties
                    poiparsed.FillLTSPoiAdditionalProperties();
                    poiparsed.FillIDMPoiAdditionalProperties();

                    var result = await InsertDataToDB(poiparsed, data.data,jsondata);

                    //newimportcounter = newimportcounter + result.created ?? 0;
                    //updateimportcounter = updateimportcounter + result.updated ?? 0;
                    //errorimportcounter = errorimportcounter + result.error ?? 0;

                    updatedetails.Add(result);

                    idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.pointofinterests",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.pointofinterests",
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
                    objectcompared = 0,
                    pushchannels = null,
                    changes = null                    
                });
            }

            return updatedetails.FirstOrDefault();
        }

        private async Task<UpdateDetail> InsertDataToDB(
            ODHActivityPoiLinked objecttosave,
            LTSPointofInterestData poilts,
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

                //Prefix Poi with "smgpoi" Id
                objecttosave.Id = "smgpoi" + objecttosave.Id.ToLower();

                return await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                    objecttosave,
                    new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                    new EditInfo("lts.pointofinterests.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(LTSPointofInterestData poilts)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(poilts),
                    sourceinterface = "pointofinterests",
                    sourceid = poilts.rid,
                    sourceurl = "https://go.lts.it/api/v1/pointofinterests",
                    type = "odhactivitypoi",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
        
        public async Task<UpdateDetail> DeleteOrDisablePoisData(string id, bool delete, bool reduced)
        {          
            UpdateDetail result = default(UpdateDetail);

            if (delete)
            {
                result = await QueryFactory.DeleteData<ODHActivityPoiLinked>(
                "smgpoi" + id.ToLower(),
                new DataInfo("smgpois", CRUDOperation.Delete),
                new CRUDConstraints(),
                reduced
                );                
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

                        //updateresult = await QueryFactory
                        //    .Query(table)
                        //    .Where("id", id)
                        //    .UpdateAsync(new JsonBData() { id = id, data = new JsonRaw(data) });

                        result = await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                               data,
                               new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                               new EditInfo("lts.pointofinterests.import.deactivate", importerURL),
                               new CRUDConstraints(),
                               new CompareConfig(true, false)
                        );                        
                    }
                }
            }

            return result;
        }

     
        private async Task MergePoiTags(ODHActivityPoiLinked poiNew, ODHActivityPoiLinked poiOld)
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

            //TODO import ODHTags (eating drinking, gastronomy etc...) to Tags?

            //TODO import the Redactional Tags from SmgTags into Tags?
        }

        #region OLD Compatibility Stufff

        //TODO Pois ODHTags assignment
        private async Task AssignODHTags(ODHActivityPoiLinked poiNew, ODHActivityPoiLinked poiOld, IDictionary<string, JArray>? jsonfiles)
        {
            List<ODHTagLinked> tagstoremove = jsonfiles != null && jsonfiles["ODHTagsSourceIDMLTS"] != null ? jsonfiles["ODHTagsSourceIDMLTS"].ToObject<List<ODHTagLinked>>() : null;

            List<string> tagstopreserve = new List<string>();
            if (poiNew.SmgTags == null)
                poiNew.SmgTags = new List<string>();

            //Remove all ODHTags that where automatically assigned
            if (poiNew != null && poiOld != null && poiOld.SmgTags != null && tagstoremove != null)
                tagstopreserve = poiOld.SmgTags.Except(tagstoremove.Select(x => x.Id)).ToList();

            //Add the activity Tag
            if (!poiNew.SmgTags.Contains("poi"))
                poiNew.SmgTags.Add("poi");

            //Readd all mapped Tags
            foreach (var ltstag in poiNew.TagIds)
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
                            if (!poiNew.SmgTags.Contains(ltstaginlist.Id))
                                poiNew.SmgTags.Add(ltstaginlist.Id);
                            //Add the mapped Tags
                            foreach (var mappedtag in ltstaginlist.MappedTagIds)
                            {
                                if (!poiNew.SmgTags.Contains(mappedtag))
                                    poiNew.SmgTags.Add(mappedtag);
                            }

                            //Handle also the LTS Parent Tags
                            if (ltstaginlist.Mapping != null && ltstaginlist.Mapping.ContainsKey("lts"))
                            {
                                if (ltstaginlist.Mapping["lts"].ContainsKey("parent_id"))
                                {
                                    if (!poiNew.SmgTags.Contains(ltstaginlist.Mapping["lts"]["parent_id"]))
                                        poiNew.SmgTags.Add(ltstaginlist.Mapping["lts"]["parent_id"]);
                                }
                            }
                        }
                    }
                }
            }

            //Readd Tags to preserve
            foreach (var tagtopreserve in tagstopreserve)
            {
                poiNew.SmgTags.Add(tagtopreserve);
            }

            poiNew.SmgTags.RemoveEmptyStrings();
        }


        //Metadata assignment detailde.MetaTitle = detailde.Title + " | suedtirol.info";
        private async Task AddIDMMetaTitleAndDescription(ODHActivityPoiLinked poiNew, MetaInfosOdhActivityPoi metainfo)
        {
            IDMCustomHelper.SetMetaInfoForActivityPoi(poiNew, metainfo);
        }

        private async Task ReassignOutdooractiveMapping(ODHActivityPoiLinked poiNew, ODHActivityPoiLinked poiOld)
        {
            var oamapping = new Dictionary<string, string>() { };

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
                foreach (var tag in poiNew.TagIds)
                {
                    GetAllLTSParentTagsRecursively(tag, tagstoadd, ltstagsandtins);
                }
            }

            foreach (var tag in tagstoadd)
            {
                if (!poiNew.TagIds.Contains(tag))
                    poiNew.TagIds.Add(tag);

                if (poiNew.LTSTags.Where(x => x.LTSRID == tag).Count() == 0)
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
                            //.Where(kvp => poiNew.HasLanguage != null ? poiNew.HasLanguage.Contains(kvp.Key) : !String.IsNullOrEmpty(kvp.Key))
                            //.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        tag.Level = ltstag.Mapping != null && ltstag.Mapping.ContainsKey("lts") && ltstag.Mapping["lts"].ContainsKey("level") && int.TryParse(ltstag.Mapping["lts"]["level"], out int taglevel) ? taglevel : 0;
                        tag.Id = ltstag.TagName.ContainsKey("de") ? ltstag.TagName["de"].ToLower() : "";

                        //Add the TIN Info
                        if (tag.LTSTins != null && tag.LTSTins.Count > 0)
                        {
                            foreach (var tin in tag.LTSTins)
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
                    if (!parenttags.Contains(ltstag.Mapping["lts"]["parentTagRid"]))
                        parenttags.Add(ltstag.Mapping["lts"]["parentTagRid"]);

                    if (ltstag.Mapping != null && ltstag.Mapping.ContainsKey("lts") && ltstag.Mapping["lts"].ContainsKey("level") && ltstag.Mapping["lts"]["level"] == "2")
                    {
                        GetAllLTSParentTagsRecursively(ltstag.Mapping["lts"]["parentTagRid"], parenttags, ltstagsandtins);
                    }
                }
            }
        }

        private static void SetAdditionalInfosCategoriesByODHTags(ODHActivityPoiLinked poiNew, IDictionary<string, JArray>? jsonfiles)
        {
            //TO CHECK
            //SET ADDITIONALINFOS
            //Setting Categorization by Valid Tags
            var validcategorylist = jsonfiles != null && jsonfiles["ActivityPoiDisplayAsCategory"] != null ? jsonfiles["ActivityPoiDisplayAsCategory"].ToObject<List<CategoriesTags>>() : null;

            if (validcategorylist != null && poiNew.SmgTags != null)
            {
                var currentcategories = validcategorylist.Where(x => poiNew.SmgTags.Select(y => y.ToLower()).Contains(x.Id.ToLower())).ToList();

                if (currentcategories != null)
                {
                    if (poiNew.AdditionalPoiInfos == null)
                        poiNew.AdditionalPoiInfos = new Dictionary<string, AdditionalPoiInfos>();

                    foreach (var languagecategory in new List<string>() { "de", "it", "en", "nl", "cs", "pl", "fr", "ru" })
                    {
                        //Do not overwrite Novelty
                        string? novelty = null;
                        if (poiNew.AdditionalPoiInfos.ContainsKey(languagecategory) && !String.IsNullOrEmpty(poiNew.AdditionalPoiInfos[languagecategory].Novelty))
                            novelty = poiNew.AdditionalPoiInfos[languagecategory].Novelty;


                        AdditionalPoiInfos additionalPoiInfos = new AdditionalPoiInfos() { Language = languagecategory, Categories = new List<string>(), Novelty = novelty };

                        //Reassigning Categories
                        foreach (var smgtagtotranslate in currentcategories)
                        {
                            if (smgtagtotranslate.TagName.ContainsKey(languagecategory))
                            {
                                if(!additionalPoiInfos.Categories.Contains(smgtagtotranslate.TagName[languagecategory].Trim()))
                                    additionalPoiInfos.Categories.Add(smgtagtotranslate.TagName[languagecategory].Trim());
                            }
                        }

                        poiNew.AdditionalPoiInfos.TryAddOrUpdate(languagecategory, additionalPoiInfos);
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
