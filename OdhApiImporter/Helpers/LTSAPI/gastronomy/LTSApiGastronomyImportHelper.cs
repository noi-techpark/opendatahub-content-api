// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DataModel.helpers;
using Helper;
using Helper.Extensions;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiGastronomyImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiGastronomyImportHelper(
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
            var gastronomylts = await GetGastronomiesFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (gastronomylts != null && gastronomylts.FirstOrDefault().ContainsKey("success") && (Boolean)gastronomylts.FirstOrDefault()["success"]) //&& gastronomylts.FirstOrDefault()["Success"] == true
            {     
                //Import Single Data & Deactivate Data
                return await SaveGastronomiesToPG(gastronomylts);
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (gastronomylts != null && gastronomylts.FirstOrDefault().ContainsKey("status") && ((int)gastronomylts.FirstOrDefault()["status"] == 403 || (int)gastronomylts.FirstOrDefault()["status"] == 404))
            {
                var resulttoreturn = default(UpdateDetail);

                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    resulttoreturn = await DeleteOrDisableGastronomiesData(id, false, false);
                    if (gastronomylts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(gastronomylts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + gastronomylts.FirstOrDefault()["message"].ToString() + "|";
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    resulttoreturn = await DeleteOrDisableGastronomiesData(id, true, true);
                    if (gastronomylts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(gastronomylts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + "opendata:" + gastronomylts.FirstOrDefault()["message"].ToString() + "|";
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
            var lastchangedlts = await GetGastronomiesFromLTSV2(null, lastchanged, null, null);
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
                    "lastchanged.gastronomies",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.gastronomies",
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
            var deletedlts = await GetGastronomiesFromLTSV2(null, null, deletedfrom, null);
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
                    "deleted.gastronomies",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.gastronomies",
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
            var activelistlts = await GetGastronomiesFromLTSV2(null, null, null, active);
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
                    "active.gastronomies",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.gastronomies",
                        success = false,
                        error = "Could not fetch active List",
                    }
                );
            }

            return activeList;
        }

        private async Task<List<JObject>> GetGastronomiesFromLTSV2(string gastroid, DateTime? lastchanged, DateTime? deletedfrom, bool? activelist)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi(opendata);
                
                if(gastroid != null)
                {
                    //Get Single Gastronomy

                    var qs = new LTSQueryStrings() { page_size = 1 };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.GastronomyDetailRequest(gastroid, dict);
                }
                else if (lastchanged != null)
                {                    
                    //Get the Last Changes Gastronomies list

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyTourismOrganizationMember = false };
               
                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.GastronomyListRequest(dict, true);
                }
                else if (deletedfrom != null)
                {
                    //Get the Active Gastronomies list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyTourismOrganizationMember = false };
                
                    if (deletedfrom != null)
                        qs.filter_lastUpdate = deletedfrom;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.GastronomyDeletedRequest(dict, true);
                }
                else if (activelist != null)
                {
                    //Get the Active Gastronomies list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    //Toggle representationmode filtering for active inactive sync
                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyActive = true, filter_onlyTourismOrganizationMember = false };
                    //var qs = new LTSQueryStrings() { fields = "rid", filter_onlyActive = true, filter_onlyTourismOrganizationMember = false, filter_representationMode = "full" };

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.GastronomyListRequest(dict, true);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.gastronomies",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.gastronomies",
                        success = false,
                        error = ex.Message,
                    }
                );
                return null;
            }
        }

        private async Task<UpdateDetail> SaveGastronomiesToPG(List<JObject> ltsdata)
        {
            //var newimportcounter = 0;
            //var updateimportcounter = 0;
            //var errorimportcounter = 0;
            //var deleteimportcounter = 0;

            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSGastronomy> gastrodata = new List<LTSGastronomy>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    gastrodata.Add(
                        ltsdatasingle.ToObject<LTSGastronomy>()
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
                            "CategoryCodes",
                            "DishRates",
                            "Facilities",
                            "CapacityCeremonies",
                            "GastronomyDisplayAsCategory",
                            "AutoPublishTags",
                            "ODHTagsSourceIDMLTS",
                            "GenericTags",
                        }
                    );
                }
                else
                {
                    jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                    settings.JsonConfig.Jsondir,
                    new List<string>()
                        {
                            "CategoryCodes",
                            "GastronomyDisplayAsCategory",
                            "GenericTags",
                            "ODHTagsSourceIDMLTS",
                        }
                    );

                }

                foreach (var data in gastrodata)
                {
                    string id = data.data.rid.ToLower();

                    var gastroparsed = GastronomyParser.ParseLTSGastronomy(data.data, false, jsondata);

                    //POPULATE LocationInfo not working on Gastronomies because DistrictInfo is prefilled! DistrictId not available on root level...
                    gastroparsed.LocationInfo = await gastroparsed.UpdateLocationInfoExtension(
                        QueryFactory
                    );

                    //DistanceCalculation
                    await gastroparsed.UpdateDistanceCalculation(QueryFactory);

                    //GET OLD Gastronomy
                    var gastroindb = await LoadDataFromDB<ODHActivityPoiLinked>("smgpoi" + id, IDStyle.lowercase);

                    //Add manual assigned Tags to TagIds TO check if this should be activated
                    await MergeGastronomyTags(gastroparsed, gastroindb);

                    //Add the SmgTags for IDM
                    await AssignODHTags(gastroparsed, gastroindb);

                    if (!opendata)
                    {
                        await SetODHActiveBasedOnRepresentationMode(gastroparsed);

                        //Add the MetaTitle for IDM
                        await AddMetaTitle(gastroparsed);

                        //Add the values to Tags (TagEntry) not needed anymore?
                        //await AddTagEntryToTags(gastroparsed);                        
                    }

                    //When requested with opendata Interface does not return isActive field
                    //All data returned by opendata interface are active by default
                    if(opendata)
                    {
                        gastroparsed.Active = true;
                        gastroparsed.SmgActive = true;
                    }                        

                    SetAdditionalInfosCategoriesByODHTags(gastroparsed, jsondata);

                    //TODO Maybe we can disable this withhin the Api Switch
                    //Traduce all Tags with Source IDM to english tags , CONSIDER TagId "gastronomy" is added here
                    await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                            gastroparsed,
                            jsondata != null && jsondata["GenericTags"] != null ? jsondata["GenericTags"].ToObject<List<TagLinked>>() : null
                        );

                    //Create Tags and preserve the old TagEntries
                    await gastroparsed.UpdateTagsExtension(QueryFactory,await FillTagsObject.GetTagEntrysToPreserve(gastroparsed));

                    //Fill AdditionalProperties
                    gastroparsed.FillLTSGastronomyAdditionalProperties();

                    var result = await InsertDataToDB(gastroparsed, data.data, jsondata);

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
                        "single.gastronomies",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.gastronomies",
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
            LTSGastronomyData gastrolts,
            IDictionary<string, JArray>? jsonfiles
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = LicenseHelper.GetLicenseforGastronomy(objecttosave, opendata);

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave, opendata);

                
                //Set PublishedOn (only full data)
                if (!opendata)
                {
                    //Add the PublishedOn Logic
                    //Exception here all Tags with autopublish has to be passed
                    var autopublishtaglist = jsonfiles != null && jsonfiles["AutoPublishTags"] != null ? jsonfiles["AutoPublishTags"].ToObject<List<AllowedTags>>() : null;

                    objecttosave.CreatePublishedOnList(autopublishtaglist);
                }
                else
                    objecttosave.PublishedOn = new List<string>();

                var rawdataid = await InsertInRawDataDB(gastrolts);

                //Prefix Gastronomy with "smgpoi" Id
                objecttosave.Id = "smgpoi" + objecttosave.Id.ToLower();

                return await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                    objecttosave,
                    new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                    new EditInfo("lts.gastronomies.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(LTSGastronomyData gastrolts)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(gastrolts),
                    sourceinterface = "gastronomies",
                    sourceid = gastrolts.rid,
                    sourceurl = "https://go.lts.it/api/v1/gastronomies",
                    type = "odhactivitypoi",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
        
        public async Task<UpdateDetail> DeleteOrDisableGastronomiesData(string id, bool delete, bool reduced)
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
                               new EditInfo("lts.gastronomies.import.deactivate", importerURL),
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


        private async Task MergeGastronomyTags(ODHActivityPoiLinked gastroNew, ODHActivityPoiLinked gastroOld)
        {
            if (gastroOld != null)
            {
                //Readd all Redactional Tags to check if this query fits
                var redactionalassignedTags = gastroOld.Tags != null ? gastroOld.Tags.Where(x => x.Source != "lts" && (x.Source == "idm" && x.Type != "odhcategory")).ToList() : null;
                if (redactionalassignedTags != null)
                {
                    foreach (var tag in redactionalassignedTags)
                    {
                        gastroNew.TagIds.Add(tag.Id);
                    }
                }
            }

            //TODO import the Redactional Tags from SmgTags into Tags?
        }

        //Gastronomies ODHTags assignment
        private async Task AssignODHTags(ODHActivityPoiLinked gastroNew, ODHActivityPoiLinked gastroOld)
        {
            List<string> tagstopreserve = new List<string>();
            //Remove all ODHTags that where automatically assigned         
            if(gastroOld != null && gastroOld.SmgTags != null)
                tagstopreserve = gastroOld.SmgTags.Except(GetOdhTagListAssigned()).ToList();
            
            gastroNew.SmgTags = GetODHTagListGastroCategory(gastroNew.CategoryCodes, gastroNew.Facilities, tagstopreserve);

            gastroNew.SmgTags.RemoveEmptyStrings();
        }

        //Switched to import logic
        private async Task AddTagEntryToTags(ODHActivityPoiLinked gastroNew)
        {
            //CeremeonyCodes
            foreach(var tag in gastroNew.Tags.Where(x => x.Type == "dishcodes"))
            {
                if(gastroNew.DishRates != null)
                {
                    var dishcode = gastroNew.DishRates.Where(x => x.Id == tag.Id).FirstOrDefault();

                    if (dishcode != null)
                    {
                        tag.TagEntry = new Dictionary<string, string>();
                        tag.TagEntry.TryAddOrUpdate("MaxAmount", dishcode.MaxAmount.ToString());
                        tag.TagEntry.TryAddOrUpdate("MinAmount", dishcode.MinAmount.ToString());
                        tag.TagEntry.TryAddOrUpdate("CurrencyCode", dishcode.CurrencyCode);
                    }
                }
            }
            //CeremonyCodes
            foreach (var tag in gastroNew.Tags.Where(x => x.Type == "ceremonycodes"))
            {
                if (gastroNew.DishRates != null)
                {
                    var capacityceremony = gastroNew.CapacityCeremony.Where(x => x.Id == tag.Id).FirstOrDefault();

                    if (capacityceremony != null)
                    {
                        tag.TagEntry = new Dictionary<string, string>();
                        tag.TagEntry.TryAddOrUpdate("MaxSeatingCapacity", capacityceremony.MaxSeatingCapacity.ToString());
                    }
                }
            }
        }

        //Metadata assignment detailde.MetaTitle = detailde.Title + " | suedtirol.info";
        private async Task AddMetaTitle(ODHActivityPoiLinked gastroNew)
        {
            if (gastroNew != null && gastroNew.Detail != null)
            {
                if (gastroNew.Detail.ContainsKey("de"))
                {
                    string city = GetCityForGastroSeo("de", gastroNew);

                    gastroNew.Detail["de"].MetaTitle = gastroNew.Detail["de"].Title + " • " + city + " (Südtirol)";
                    gastroNew.Detail["de"].MetaDesc = "Kontakt •  Reservierung •  Öffnungszeiten → " + gastroNew.Detail["de"].Title + ", " + city + ". Hier finden Feinschmecker das passende Restaurant, Cafe, Almhütte, uvm.";
                }
                if (gastroNew.Detail.ContainsKey("it"))
                {
                    string city = GetCityForGastroSeo("it", gastroNew);

                    gastroNew.Detail["it"].MetaTitle = gastroNew.Detail["it"].Title + " • " + city + " (Alto Adige)";
                    gastroNew.Detail["it"].MetaDesc = "Contatto • prenotazione • orari d'apertura → " + gastroNew.Detail["it"].Title + ", " + city + ". Il posto giusto per i buongustai: ristorante, cafè, baita, e tanto altro.";
                }
                if (gastroNew.Detail.ContainsKey("en"))
                {
                    string city = GetCityForGastroSeo("en", gastroNew);

                    gastroNew.Detail["en"].MetaTitle = gastroNew.Detail["en"].Title + " • " + city + " (South Tyrol)";
                    gastroNew.Detail["en"].MetaDesc = "•  Contact •  reservation •  opening times →  " + gastroNew.Detail["en"].Title + ". Find the perfect restaurant, cafe, alpine chalet in South Tyrol.";
                }

                //foreach (var detail in gastroNew.Detail)
                //{
                //    //Check this
                //    detail.Value.MetaTitle = detail.Value.Title + " | suedtirol.info";
                //}
            }
        }

        private async Task SetODHActiveBasedOnRepresentationMode(ODHActivityPoiLinked gastroNew)
        {
            if(gastroNew.Mapping != null && gastroNew.Mapping.ContainsKey("lts") && gastroNew.Mapping["lts"].ContainsKey("representationMode"))
            {                
                var representationmode = gastroNew.Mapping["lts"]["representationMode"];
                if (representationmode == "full")
                {
                    gastroNew.SmgActive = true;
                }
            }
            else
                gastroNew.SmgActive = false;
        }

        #region OLD Compatibility Stuff

        private static string GetCityForGastroSeo(string lang, ODHActivityPoiLinked currentpoi)
        {
            return GetCityForGastroSeoHelper(lang, currentpoi.LocationInfo, currentpoi.ContactInfos);
        }

        private static string GetCityForGastroSeoHelper(string lang, LocationInfo loc, IDictionary<string, ContactInfos> con)
        {
            bool returncontactcity = false;

            string returnstring = "";

            //If Locationinfo has municipality take this

            if (loc != null)
            {
                if (loc.MunicipalityInfo != null)
                {
                    if (loc.MunicipalityInfo.Name[lang] != null)
                    {
                        returnstring = loc.MunicipalityInfo.Name[lang];
                    }
                    else
                    {
                        returncontactcity = true;
                    }
                }
                else
                    returncontactcity = true;
            }
            else
                returncontactcity = true;

            if (returncontactcity)
            {
                //If no municipality set use ContactInfo City

                if (lang == "en")
                {
                    if (con.ContainsKey("it"))
                        returnstring = con["it"].City;

                }
                else
                {
                    if (con.ContainsKey(lang))
                        returnstring = con[lang].City;

                }
            }

            return returnstring;
        }

        public static ICollection<string> GetODHTagListGastroCategory(ICollection<CategoryCodesLinked> categorycodes, ICollection<FacilitiesLinked> facilitycodes, ICollection<string>? smgtaglist)
        {
            if (smgtaglist == null)
                smgtaglist = new List<string>();

            if (!smgtaglist.Contains("essen trinken"))
                smgtaglist.Add("essen trinken");

            //IDM Categorization
            //Restaurants & Gasthäuser	
            //    Restaurants
            //    Gasthäuser & Gasthöfe  
            //    Pizzerias
            //    Vinotheken 
            //    Bars/Cafés/Bistros
            //    Gault Millau Südtirol 
            //    Michelin-Sternerestaurants
            //    Guida Espresso
            //Hütten & Almen	
            //    Schutzhütten
            //    Almen
            //    Skihütten
            //Bäuerliche Schankbetriebe	
            //    Buschen- und Hofschänke
            //Törggelen	
            //Weinkellereien	

            if (categorycodes != null)
            {
                foreach (var categorycode in categorycodes)
                {
                    switch (categorycode.Id)
                    {
                        //Restaurant
                        case "B0BDC4C2C5938D9B734D97B09C8A47A4":
                            if (!smgtaglist.Contains("restaurants gasthäuser"))
                                smgtaglist.Add("restaurants gasthäuser");
                            if (!smgtaglist.Contains("restaurants"))
                                smgtaglist.Add("restaurants");
                            break;
                        //Bar / Café / Bistro
                        case "9095FC003A3E2F393D63A54682359B37":
                            if (!smgtaglist.Contains("restaurants gasthäuser"))
                                smgtaglist.Add("restaurants gasthäuser");
                            if (!smgtaglist.Contains("bars cafes bistros"))
                                smgtaglist.Add("bars cafes bistros");
                            break;
                        //Pub / Disco
                        case "59FE0B38EB7F4AC3951A5F477A0E1FA2":
                            if (!smgtaglist.Contains("andere gastronomiebetriebe"))
                                smgtaglist.Add("andere gastronomiebetriebe");
                            if (!smgtaglist.Contains("pub disco"))
                                smgtaglist.Add("pub disco");
                            break;
                        //Apres Ski
                        case "43D095A3FE8A450099D33926BBC1ADF8":
                            if (!smgtaglist.Contains("andere gastronomiebetriebe"))
                                smgtaglist.Add("andere gastronomiebetriebe");
                            if (!smgtaglist.Contains("apres ski"))
                                smgtaglist.Add("apres ski");
                            break;
                        //Jausenstation
                        case "8176B5A707E2067708AF18045E068E15":
                            if (!smgtaglist.Contains("restaurants gasthäuser"))
                                smgtaglist.Add("restaurants gasthäuser");
                            if (!smgtaglist.Contains("gasthäuser gasthöfe"))
                                smgtaglist.Add("gasthäuser gasthöfe");
                            if (!smgtaglist.Contains("jausenstation"))
                                smgtaglist.Add("jausenstation");
                            break;
                        //Pizzeria
                        case "AC56B3717C3152A428A1D338A638C570":
                            if (!smgtaglist.Contains("restaurants gasthäuser"))
                                smgtaglist.Add("restaurants gasthäuser");
                            if (!smgtaglist.Contains("pizzerias"))
                                smgtaglist.Add("pizzerias");
                            break;
                        //Bäuerlicher Schankbetrieb
                        case "E8883A596A2463A9B3E1586C9E780F17":
                            if (!smgtaglist.Contains("bäuerliche schankbetriebe"))
                                smgtaglist.Add("bäuerliche schankbetriebe");
                            break;
                        //Buschenschank
                        case "700B02F1BE96B01C34CCF7A637DB3054":
                            if (!smgtaglist.Contains("bäuerliche schankbetriebe"))
                                smgtaglist.Add("bäuerliche schankbetriebe");
                            if (!smgtaglist.Contains("buschen hofschänke"))
                                smgtaglist.Add("buschen hofschänke");
                            if (!smgtaglist.Contains("buschenschank"))
                                smgtaglist.Add("buschenschank");
                            break;
                        //Hofschank
                        case "4A14E16888CB07C18C65A6B59C5A19A7":
                            if (!smgtaglist.Contains("bäuerliche schankbetriebe"))
                                smgtaglist.Add("bäuerliche schankbetriebe");
                            if (!smgtaglist.Contains("buschen hofschänke"))
                                smgtaglist.Add("buschen hofschänke");
                            if (!smgtaglist.Contains("hofschank"))
                                smgtaglist.Add("hofschank");
                            break;
                        //Törggele Lokale
                        case "AB320B063588EA95F45505E940903115":
                            if (!smgtaglist.Contains("andere gastronomiebetriebe"))
                                smgtaglist.Add("andere gastronomiebetriebe");
                            if (!smgtaglist.Contains("törggele lokal"))
                                smgtaglist.Add("törggele lokal");
                            break;
                        //Schnellimbiss
                        case "33B86F5B91A08A0EFD6854DEB0207205":
                            if (!smgtaglist.Contains("andere gastronomiebetriebe"))
                                smgtaglist.Add("andere gastronomiebetriebe");
                            if (!smgtaglist.Contains("schnellimbiss"))
                                smgtaglist.Add("schnellimbiss");
                            break;
                        //Mensa
                        case "29BC7A9AE7CF173FBCCE6A48DD001229":
                            if (!smgtaglist.Contains("andere gastronomiebetriebe"))
                                smgtaglist.Add("andere gastronomiebetriebe");
                            if (!smgtaglist.Contains("mensa"))
                                smgtaglist.Add("mensa");
                            break;
                        //Vinothek / Weinhaus / Taverne
                        case "C3CC9C83C32BFA4E9A05133291EA9FFB":
                            if (!smgtaglist.Contains("restaurants gasthäuser"))
                                smgtaglist.Add("restaurants gasthäuser");
                            if (!smgtaglist.Contains("vinotheken"))
                                smgtaglist.Add("vinotheken");
                            break;
                        //Eisdiele
                        case "6A2A32E2BFEE270083351B0CFD9BA2E3":
                            if (!smgtaglist.Contains("andere gastronomiebetriebe"))
                                smgtaglist.Add("andere gastronomiebetriebe");
                            if (!smgtaglist.Contains("eisdiele"))
                                smgtaglist.Add("eisdiele");
                            break;
                        //Gasthaus
                        case "9B158D17F03509C46037C3C7B23F2FE4":
                            if (!smgtaglist.Contains("restaurants gasthäuser"))
                                smgtaglist.Add("restaurants gasthäuser");
                            if (!smgtaglist.Contains("gasthäuser gasthöfe"))
                                smgtaglist.Add("gasthäuser gasthöfe");
                            if (!smgtaglist.Contains("gasthaus"))
                                smgtaglist.Add("gasthaus");
                            break;
                        //Gasthof
                        case "D8B8ABEDD17A139DEDA2695545C420D6":
                            if (!smgtaglist.Contains("restaurants gasthäuser"))
                                smgtaglist.Add("restaurants gasthäuser");
                            if (!smgtaglist.Contains("gasthäuser gasthöfe"))
                                smgtaglist.Add("gasthäuser gasthöfe");
                            if (!smgtaglist.Contains("gasthof"))
                                smgtaglist.Add("gasthof");
                            break;
                        //Braugarten
                        case "902D9BA559B1ED889694284F05CFA41E":
                            if (!smgtaglist.Contains("andere gastronomiebetriebe"))
                                smgtaglist.Add("andere gastronomiebetriebe");
                            if (!smgtaglist.Contains("braugarten"))
                                smgtaglist.Add("braugarten");
                            break;
                        //Schutzhütte
                        case "2328C37167BBBC5776831B8A262A6C36":
                            if (!smgtaglist.Contains("hütten almen"))
                                smgtaglist.Add("hütten almen");
                            if (!smgtaglist.Contains("schutzhütten"))
                                smgtaglist.Add("schutzhütten");
                            break;
                        //Alm
                        case "8025DB5CFCBA4FF281DDDE1F2B1D19A2":
                            if (!smgtaglist.Contains("hütten almen"))
                                smgtaglist.Add("hütten almen");
                            if (!smgtaglist.Contains("almen"))
                                smgtaglist.Add("almen");
                            break;
                        //Skihütte
                        case "B916489A77C94D8D92B03184EE587A31":
                            if (!smgtaglist.Contains("hütten almen"))
                                smgtaglist.Add("hütten almen");
                            if (!smgtaglist.Contains("skihütten"))
                                smgtaglist.Add("skihütten");
                            break;
                    }
                }
            }

            if (facilitycodes != null)
            {
                foreach (var facilitycode in facilitycodes)
                {
                    switch (facilitycode.Id)
                    {
                        //Restaurant
                        case "ED4028BEE0164BF185B923B3DD4FF9A0":
                            if (!smgtaglist.Contains("roter hahn"))
                                smgtaglist.Add("roter hahn");
                            break;
                    }
                }
            }


            return smgtaglist;
        }

        public static IEnumerable<string> GetOdhTagListAssigned()
        {
            List<string> myvalidsmgtagstotranslate = new List<string>();
            myvalidsmgtagstotranslate.Add("restaurants gasthäuser");
            myvalidsmgtagstotranslate.Add("restaurants");
            myvalidsmgtagstotranslate.Add("gasthäuser gasthöfe");
            myvalidsmgtagstotranslate.Add("pizzerias");
            myvalidsmgtagstotranslate.Add("vinotheken");
            myvalidsmgtagstotranslate.Add("bars cafes bistros");
            myvalidsmgtagstotranslate.Add("hütten almen");
            myvalidsmgtagstotranslate.Add("schutzhütten");
            myvalidsmgtagstotranslate.Add("almen");
            myvalidsmgtagstotranslate.Add("skihütten");
            myvalidsmgtagstotranslate.Add("bäuerliche schankbetriebe");
            myvalidsmgtagstotranslate.Add("buschen hofschänke");
            myvalidsmgtagstotranslate.Add("weinkellereien");
            myvalidsmgtagstotranslate.Add("roter hahn");            
            myvalidsmgtagstotranslate.Add("andere gastronomiebetriebe");
            myvalidsmgtagstotranslate.Add("pub disco");
            myvalidsmgtagstotranslate.Add("apres ski");
            myvalidsmgtagstotranslate.Add("jausenstation");
            myvalidsmgtagstotranslate.Add("buschenschank");
            myvalidsmgtagstotranslate.Add("hofschank");
            myvalidsmgtagstotranslate.Add("törggele lokal");
            myvalidsmgtagstotranslate.Add("schnellimbiss");
            myvalidsmgtagstotranslate.Add("mensa");
            myvalidsmgtagstotranslate.Add("eisdiele");
            myvalidsmgtagstotranslate.Add("gasthaus");
            myvalidsmgtagstotranslate.Add("gasthof");
            myvalidsmgtagstotranslate.Add("braugarten");

            return myvalidsmgtagstotranslate;
        }

        private static void SetAdditionalInfosCategoriesByODHTags(ODHActivityPoiLinked gastroNew, IDictionary<string, JArray>? jsonfiles)
        {
            //If a Tag is found in 
            //SET ADDITIONALINFOS
            //Setting Categorization by Valid Tags
            var validcategorylist = jsonfiles != null && jsonfiles["GastronomyDisplayAsCategory"] != null ? jsonfiles["GastronomyDisplayAsCategory"].ToObject<List<CategoriesTags>>() : null;

            if (validcategorylist != null && gastroNew.SmgTags != null)
            {
                var currentcategories = validcategorylist.Where(x => gastroNew.SmgTags.Select(y => y.ToLower()).Contains(x.Id.ToLower())).ToList();

                if (currentcategories != null)
                {
                    if(gastroNew.AdditionalPoiInfos == null)
                        gastroNew.AdditionalPoiInfos = new Dictionary<string, AdditionalPoiInfos>();

                    foreach (var languagecategory in new List<string>() { "de","it","en","nl","cs","pl","fr","ru" })
                    {
                        AdditionalPoiInfos additionalPoiInfos = new AdditionalPoiInfos() { Language = languagecategory, Categories = new List<string>() };

                        //Reassigning Categories
                        foreach (var smgtagtotranslate in currentcategories)
                        {
                            if (smgtagtotranslate.TagName.ContainsKey(languagecategory))
                            {
                                additionalPoiInfos.Categories.Add(smgtagtotranslate.TagName[languagecategory].Trim());
                            }                            
                        }

                        gastroNew.AdditionalPoiInfos.Add(languagecategory, additionalPoiInfos);
                    }
                }                
            }
        }

        #endregion
    }
}
