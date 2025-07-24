﻿// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
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
    public class LTSApiActivityImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiActivityImportHelper(
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
            var activitylts = await GetActivitiesFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (activitylts != null && activitylts.FirstOrDefault().ContainsKey("success") && (Boolean)activitylts.FirstOrDefault()["success"]) //&& gastronomylts.FirstOrDefault()["Success"] == true
            {     //Import Single Data & Deactivate Data
                var result = await SavePoisToPG(activitylts);
                return result;
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (activitylts != null && activitylts.FirstOrDefault().ContainsKey("status") && ((int)activitylts.FirstOrDefault()["status"] == 403 || (int)activitylts.FirstOrDefault()["status"] == 404))
            {
                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    return await DeleteOrDisableActivitiesData(id, false, false);
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    return await DeleteOrDisableActivitiesData(id, true, true);
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
                    //Get Single Poi

                    var qs = new LTSQueryStrings() { page_size = 1 };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.ActivityDetailRequest(poiid, dict);
                }
                else if (lastchanged != null)
                {                    
                    //Get the Last Changed Pois list

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

                List<LTSActivity> activitydata = new List<LTSActivity>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    activitydata.Add(
                        ltsdatasingle.ToObject<LTSActivity>()
                    );
                }

                //TO CHECK ???? Load the json Data
                //IDictionary<string,JArray> jsondata = default(Dictionary<string, JArray>);

                //if (!opendata)
                //{
                //    jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                //    settings.JsonConfig.Jsondir,
                //    new List<string>()
                //        {
                //            "LTSTags"
                //        }
                //    );
                //}

                foreach (var data in activitydata)
                {
                    string id = data.data.rid.ToLower();

                    var activityparsed = ActivityParser.ParseLTSActivity(data.data, false);

                    //POPULATE LocationInfo not working on Gastronomies because DistrictInfo is prefilled! DistrictId not available on root level...
                    activityparsed.LocationInfo = await activityparsed.UpdateLocationInfoExtension(
                        QueryFactory
                    );

                    //DistanceCalculation
                    await activityparsed.UpdateDistanceCalculation(QueryFactory);

                    //GET OLD Activity
                    var activityindb = await LoadDataFromDB<ODHActivityPoiLinked>(id, IDStyle.lowercase);

                    //Add manual assigned Tags to TagIds TO check if this should be activated
                    await MergeActivityTags(activityparsed, activityindb);
              
                    if (!opendata)
                    {
                        //TO CHECK
                        //Add the SmgTags for IDM
                        //await AssignODHTags(poiparsed, poiindb);

                        //TO CHECK
                        //await SetODHActiveBasedOnRepresentationMode(poiparsed);

                        //TO CHECK
                        //Add the MetaTitle for IDM
                        //await AddMetaTitle(poiparsed);

                        //Add the values to Tags (TagEntry) not needed anymore?
                        //await AddTagEntryToTags(poiparsed);

                        //Traduce all Tags with Source IDM to english tags
                        await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                            activityparsed,
                            settings.JsonConfig.Jsondir
                        );
                    }

                    //Create Tags and preserve the old TagEntries
                    await activityparsed.UpdateTagsExtension(QueryFactory, null);


                    var result = await InsertDataToDB(activityparsed, data.data);

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


            //To check, this works only for single updates             
            //return new UpdateDetail()
            //{
            //    updated = updateimportcounter,
            //    created = newimportcounter,
            //    deleted = deleteimportcounter,
            //    error = errorimportcounter,
            //};

            return updatedetails.FirstOrDefault();
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            ODHActivityPoiLinked objecttosave,
            LTSActivityData poilts            
        )
        {
            try
            {
                //TODO!
                //Set LicenseInfo
                //objecttosave.LicenseInfo = LicenseHelper.GetLicenseforOdhActivityPoi(objecttosave, opendata);

                //TODO!
                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave, opendata);

                //Add the PublishedOn Logic
                //Exception here all Tags with autopublish has to be passed
                var autopublishtaglist =
                    await GenericTaggingHelper.GetAllAutoPublishTagsfromJson(
                        settings.JsonConfig.Jsondir
                    );               
                //Set PublishedOn with allowedtaglist
                objecttosave.CreatePublishedOnList(autopublishtaglist);

                var rawdataid = await InsertInRawDataDB(poilts);
                
                objecttosave.Id = objecttosave.Id.ToLower();

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
                id.ToLower(),
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
                var query = QueryFactory.Query(table).Select("data").Where("id", id.ToLower());

                var data = await query.GetObjectSingleAsync<ODHActivityPoiLinked>();

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

                        //updateresult = await QueryFactory
                        //    .Query(table)
                        //    .Where("id", id)
                        //    .UpdateAsync(new JsonBData() { id = id, data = new JsonRaw(data) });

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

            //TODO import ODHTags (eating drinking, gastronomy etc...) to Tags?

            //TODO import the Redactional Tags from SmgTags into Tags?
        }

        //TODO Pois ODHTags assignment
        //private async Task AssignODHTags(ODHActivityPoiLinked gastroNew, ODHActivityPoiLinked gastroOld)
        //{
        //    List<string> tagstopreserve = new List<string>();
        //    //Remove all ODHTags that where automatically assigned         
        //    if(gastroOld != null && gastroOld.SmgTags != null)
        //        tagstopreserve = gastroOld.SmgTags.Except(GetOdhTagListAssigned()).ToList();

        //    gastroNew.SmgTags = GetODHTagListGastroCategory(gastroNew.CategoryCodes, gastroNew.Facilities, tagstopreserve);
        //}

        //TODO Metatitle + metadesc
        //Metadata assignment detailde.MetaTitle = detailde.Title + " | suedtirol.info";
        //private async Task AddMetaTitle(ODHActivityPoiLinked gastroNew)
        //{
        //    if (gastroNew != null && gastroNew.Detail != null)
        //    {
        //        if (gastroNew.Detail.ContainsKey("de"))
        //        {
        //            string city = GetCityForGastroSeo("de", gastroNew);

        //            gastroNew.Detail["de"].MetaTitle = gastroNew.Detail["de"].Title + " • " + city + " (Südtirol)";
        //            gastroNew.Detail["de"].MetaDesc = "Kontakt •  Reservierung •  Öffnungszeiten → " + gastroNew.Detail["de"].Title + ", " + city + ". Hier finden Feinschmecker das passende Restaurant, Cafe, Almhütte, uvm.";
        //        }
        //        if (gastroNew.Detail.ContainsKey("it"))
        //        {
        //            string city = GetCityForGastroSeo("it", gastroNew);

        //            gastroNew.Detail["it"].MetaTitle = gastroNew.Detail["it"].Title + " • " + city + " (Alto Adige)";
        //            gastroNew.Detail["it"].MetaDesc = "Contatto • prenotazione • orari d'apertura → " + gastroNew.Detail["it"].Title + ", " + city + ". Il posto giusto per i buongustai: ristorante, cafè, baita, e tanto altro.";
        //        }
        //        if (gastroNew.Detail.ContainsKey("en"))
        //        {
        //            string city = GetCityForGastroSeo("en", gastroNew);

        //            gastroNew.Detail["en"].MetaTitle = gastroNew.Detail["en"].Title + " • " + city + " (South Tyrol)";
        //            gastroNew.Detail["en"].MetaDesc = "•  Contact •  reservation •  opening times →  " + gastroNew.Detail["en"].Title + ". Find the perfect restaurant, cafe, alpine chalet in South Tyrol.";
        //        }

        //        //foreach (var detail in gastroNew.Detail)
        //        //{
        //        //    //Check this
        //        //    detail.Value.MetaTitle = detail.Value.Title + " | suedtirol.info";
        //        //}
        //    }
        //}

        //to check
        //private async Task SetODHActiveBasedOnRepresentationMode(ODHActivityPoiLinked gastroNew)
        //{
        //    if(gastroNew.Mapping != null && gastroNew.Mapping.ContainsKey("lts") && gastroNew.Mapping["lts"].ContainsKey("representationMode"))
        //    {                
        //            var representationmode = gastroNew.Mapping["lts"]["representationMode"];
        //        if (representationmode == "full")
        //        {
        //            gastroNew.SmgActive = true;
        //        }
        //    }
        //}


    }
}
