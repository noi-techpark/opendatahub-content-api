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
    public class LTSApiGastronomyImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiGastronomyImportHelper(
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
            var gastronomylts = await GetGastronomiesFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (gastronomylts != null && gastronomylts.FirstOrDefault().ContainsKey("success") && (Boolean)gastronomylts.FirstOrDefault()["success"]) //&& gastronomylts.FirstOrDefault()["Success"] == true
            {     //Import Single Data & Deactivate Data
                var result = await SaveGastronomiesToPG(gastronomylts);
                return result;
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (gastronomylts != null && gastronomylts.FirstOrDefault().ContainsKey("status") && ((int)gastronomylts.FirstOrDefault()["status"] == 403 || (int)gastronomylts.FirstOrDefault()["status"] == 404))
            {
                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    return await DeleteOrDisableGastronomiesData(id, false);
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    return await DeleteOrDisableGastronomiesData(id + "_REDUCED", true);
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

        private LtsApi GetLTSApi()
        {
            if (!opendata)
            {
                return new LtsApi(
                   settings.LtsCredentials.serviceurl,
                   settings.LtsCredentials.username,
                   settings.LtsCredentials.password,
                   settings.LtsCredentials.ltsclientid,
                   false
               );
            }
            else
            {
                return new LtsApi(
                settings.LtsCredentialsOpen.serviceurl,
                settings.LtsCredentialsOpen.username,
                settings.LtsCredentialsOpen.password,
                settings.LtsCredentialsOpen.ltsclientid,
                true
            );
            }
        }

        private async Task<List<JObject>> GetGastronomiesFromLTSV2(string gastroid, DateTime? lastchanged, DateTime? deletedfrom, bool? activelist)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi();
                
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
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    return await ltsapi.GastronomyListRequest(dict, true);
                }
                else if (deletedfrom != null)
                {
                    //Get the Active Gastronomies list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyTourismOrganizationMember = false };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    if (deletedfrom != null)
                        qs.filter_lastUpdate = deletedfrom;

                    return await ltsapi.GastronomyDeletedRequest(dict, true);
                }
                else if (activelist != null)
                {
                    //Get the Active Gastronomies list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyActive = true, filter_onlyTourismOrganizationMember = false, filter_representationMode = "full" };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

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

                foreach (var data in gastrodata)
                {
                    string id = data.data.rid;

                    var gastroparsed = GastronomyParser.ParseLTSGastronomy(data.data, false);

                    //TODO Add the Code Here for POST Processing Data

                    //POPULATE LocationInfo
                    gastroparsed.LocationInfo = await gastroparsed.UpdateLocationInfoExtension(
                        QueryFactory
                    );

                    //DistanceCalculation
                    await gastroparsed.UpdateDistanceCalculation(QueryFactory);

                    //GET OLD Gastronomy
                    var gastroindb = await LoadDataFromDB<ODHActivityPoiLinked>(id);

                    //Add manual assigned Tags to TagIds TO check if this should be activated
                    await MergeGastronomyTags(gastroparsed, gastroindb);

                    //Create Tags
                    await gastroparsed.UpdateTagsExtension(QueryFactory);

                    if (!opendata)
                    {
                        //Add the SmgTags for IDM
                        await AddODHTags(gastroparsed);

                        //Add the MetaTitle for IDM
                        await AddMetaTitle(gastroparsed);
                    }

                    //TODO Add all compatibility 

                    var result = await InsertDataToDB(gastroparsed, data.data);

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

                //Deactivate this in the meantime
                //if (idlistlts.Count > 0)
                //{
                //    //Begin SetDataNotinListToInactive
                //    var idlistdb = await GetAllDataBySourceAndType(
                //        new List<string>() { "lts" },
                //        new List<string>() { "eventcategory" }
                //    );

                //    var idstodelete = idlistdb.Where(p => !idlistlts.Any(p2 => p2 == p));

                //    foreach (var idtodelete in idstodelete)
                //    {
                //        var deletedisableresult = await DeleteOrDisableData<TagLinked>(
                //            idtodelete,
                //            false
                //        );

                //        if (deletedisableresult.Item1 > 0)
                //            WriteLog.LogToConsole(
                //                idtodelete,
                //                "dataimport",
                //                "single.events.categories.deactivate",
                //                new ImportLog()
                //                {
                //                    sourceid = idtodelete,
                //                    sourceinterface = "lts.events.categories",
                //                    success = true,
                //                    error = "",
                //                }
                //            );
                //        else if (deletedisableresult.Item2 > 0)
                //            WriteLog.LogToConsole(
                //                idtodelete,
                //                "dataimport",
                //                "single.events.categories.delete",
                //                new ImportLog()
                //                {
                //                    sourceid = idtodelete,
                //                    sourceinterface = "lts.events.categories",
                //                    success = true,
                //                    error = "",
                //                }
                //            );

                //        deleteimportcounter =
                //            deleteimportcounter
                //            + deletedisableresult.Item1
                //            + deletedisableresult.Item2;
                //    }
                //}
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
            LTSGastronomyData gastrolts            
        )
        {
            try
            {
                //Set LicenseInfo
                //objecttosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                //    objecttosave,
                //    Helper.LicenseHelper.GetLicenseforEvent(
                //);

                //TODO
                //objecttosave.LicenseInfo = LicenseHelper.GetLicenseforOdhActivityPoi(objecttosave, opendata);

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave, opendata);

                //Set PublishedOn
                objecttosave.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(gastrolts);

                //Prefix Gastronomy with "smgpoi" Id
                objecttosave.Id = "smgpoi" + objecttosave.Id;

                return await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                    objecttosave,
                    new DataInfo("odhactivitypoi", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
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
        
        public async Task<UpdateDetail> DeleteOrDisableGastronomiesData(string id, bool delete)
        {
            UpdateDetail deletedisableresult = default(UpdateDetail);

            PGCRUDResult result = default(PGCRUDResult);

            if (delete)
            {
                result =  await QueryFactory.DeleteData<EventLinked>(
                    id,
                    new DataInfo("smgpoi", CRUDOperation.Delete),
                    new CRUDConstraints()
                );

                deletedisableresult = new UpdateDetail() {
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
            else
            {
                var query = QueryFactory.Query(table).Select("data").Where("id", id);

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
                               new DataInfo("smgpoi", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
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
                gastroNew.SmgTags = gastroOld.SmgTags;

                //Readd all Redactional Tags
                var redactionalassignedTags = gastroOld.Tags != null ? gastroOld.Tags.Where(x => x.Source != "lts").ToList() : null;
                if (redactionalassignedTags != null)
                {
                    foreach (var tag in redactionalassignedTags)
                    {
                        gastroNew.TagIds.Add(tag.Id);
                    }
                }
            }
            //TODO import the Redactional Tags from Events into Tags?
        }

        //Gastronomies ODHTags assignment
        private async Task AddODHTags(ODHActivityPoiLinked gastroNew)
        {
            var odhtags = GetODHTagListGastroCategory(gastroNew.CategoryCodes, gastroNew.Facilities, gastroNew.SmgTags.ToList());
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

        public static List<string> GetODHTagListGastroCategory(ICollection<CategoryCodesLinked> categorycodes, ICollection<FacilitiesLinked> facilitycodes, List<string> smgtaglist)
        {
            if (smgtaglist == null)
                smgtaglist = new List<string>();

            if (!smgtaglist.Contains("Essen Trinken"))
                smgtaglist.Add("Essen Trinken");

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
                            if (!smgtaglist.Contains("Restaurants Gasthäuser"))
                                smgtaglist.Add("Restaurants Gasthäuser");
                            if (!smgtaglist.Contains("Restaurants"))
                                smgtaglist.Add("Restaurants");
                            break;
                        //Bar / Café / Bistro
                        case "9095FC003A3E2F393D63A54682359B37":
                            if (!smgtaglist.Contains("Restaurants Gasthäuser"))
                                smgtaglist.Add("Restaurants Gasthäuser");
                            if (!smgtaglist.Contains("Bars Cafes Bistros"))
                                smgtaglist.Add("Bars Cafes Bistros");
                            break;
                        //Pub / Disco
                        case "59FE0B38EB7F4AC3951A5F477A0E1FA2":
                            if (!smgtaglist.Contains("Andere Gastronomiebetriebe"))
                                smgtaglist.Add("Andere Gastronomiebetriebe");
                            if (!smgtaglist.Contains("Pub Disco"))
                                smgtaglist.Add("Pub Disco");
                            break;
                        //Apres Ski
                        case "43D095A3FE8A450099D33926BBC1ADF8":
                            if (!smgtaglist.Contains("Andere Gastronomiebetriebe"))
                                smgtaglist.Add("Andere Gastronomiebetriebe");
                            if (!smgtaglist.Contains("Apres Ski"))
                                smgtaglist.Add("Apres Ski");
                            break;
                        //Jausenstation
                        case "8176B5A707E2067708AF18045E068E15":
                            if (!smgtaglist.Contains("Restaurants Gasthäuser"))
                                smgtaglist.Add("Restaurants Gasthäuser");
                            if (!smgtaglist.Contains("Gasthäuser Gasthöfe"))
                                smgtaglist.Add("Gasthäuser Gasthöfe");
                            if (!smgtaglist.Contains("Jausenstation"))
                                smgtaglist.Add("Jausenstation");
                            break;
                        //Pizzeria
                        case "AC56B3717C3152A428A1D338A638C570":
                            if (!smgtaglist.Contains("Restaurants Gasthäuser"))
                                smgtaglist.Add("Restaurants Gasthäuser");
                            if (!smgtaglist.Contains("Pizzerias"))
                                smgtaglist.Add("Pizzerias");
                            break;
                        //Bäuerlicher Schankbetrieb
                        case "E8883A596A2463A9B3E1586C9E780F17":
                            if (!smgtaglist.Contains("Bäuerliche Schankbetriebe"))
                                smgtaglist.Add("Bäuerliche Schankbetriebe");
                            break;
                        //Buschenschank
                        case "700B02F1BE96B01C34CCF7A637DB3054":
                            if (!smgtaglist.Contains("Bäuerliche Schankbetriebe"))
                                smgtaglist.Add("Bäuerliche Schankbetriebe");
                            if (!smgtaglist.Contains("Buschen Hofschänke"))
                                smgtaglist.Add("Buschen Hofschänke");
                            if (!smgtaglist.Contains("Buschenschank"))
                                smgtaglist.Add("Buschenschank");
                            break;
                        //Hofschank
                        case "4A14E16888CB07C18C65A6B59C5A19A7":
                            if (!smgtaglist.Contains("Bäuerliche Schankbetriebe"))
                                smgtaglist.Add("Bäuerliche Schankbetriebe");
                            if (!smgtaglist.Contains("Buschen Hofschänke"))
                                smgtaglist.Add("Buschen Hofschänke");
                            if (!smgtaglist.Contains("Hofschank"))
                                smgtaglist.Add("Hofschank");
                            break;
                        //Törggele Lokale
                        case "AB320B063588EA95F45505E940903115":
                            if (!smgtaglist.Contains("Andere Gastronomiebetriebe"))
                                smgtaglist.Add("Andere Gastronomiebetriebe");
                            if (!smgtaglist.Contains("Törggele Lokal"))
                                smgtaglist.Add("Törggele Lokal");
                            break;
                        //Schnellimbiss
                        case "33B86F5B91A08A0EFD6854DEB0207205":
                            if (!smgtaglist.Contains("Andere Gastronomiebetriebe"))
                                smgtaglist.Add("Andere Gastronomiebetriebe");
                            if (!smgtaglist.Contains("Schnellimbiss"))
                                smgtaglist.Add("Schnellimbiss");
                            break;
                        //Mensa
                        case "29BC7A9AE7CF173FBCCE6A48DD001229":
                            if (!smgtaglist.Contains("Andere Gastronomiebetriebe"))
                                smgtaglist.Add("Andere Gastronomiebetriebe");
                            if (!smgtaglist.Contains("Mensa"))
                                smgtaglist.Add("Mensa");
                            break;
                        //Vinothek / Weinhaus / Taverne
                        case "C3CC9C83C32BFA4E9A05133291EA9FFB":
                            if (!smgtaglist.Contains("Restaurants Gasthäuser"))
                                smgtaglist.Add("Restaurants Gasthäuser");
                            if (!smgtaglist.Contains("Vinotheken"))
                                smgtaglist.Add("Vinotheken");
                            break;
                        //Eisdiele
                        case "6A2A32E2BFEE270083351B0CFD9BA2E3":
                            if (!smgtaglist.Contains("Andere Gastronomiebetriebe"))
                                smgtaglist.Add("Andere Gastronomiebetriebe");
                            if (!smgtaglist.Contains("Eisdiele"))
                                smgtaglist.Add("Eisdiele");
                            break;
                        //Gasthaus
                        case "9B158D17F03509C46037C3C7B23F2FE4":
                            if (!smgtaglist.Contains("Restaurants Gasthäuser"))
                                smgtaglist.Add("Restaurants Gasthäuser");
                            if (!smgtaglist.Contains("Gasthäuser Gasthöfe"))
                                smgtaglist.Add("Gasthäuser Gasthöfe");
                            if (!smgtaglist.Contains("Gasthaus"))
                                smgtaglist.Add("Gasthaus");
                            break;
                        //Gasthof
                        case "D8B8ABEDD17A139DEDA2695545C420D6":
                            if (!smgtaglist.Contains("Restaurants Gasthäuser"))
                                smgtaglist.Add("Restaurants Gasthäuser");
                            if (!smgtaglist.Contains("Gasthäuser Gasthöfe"))
                                smgtaglist.Add("Gasthäuser Gasthöfe");
                            if (!smgtaglist.Contains("Gasthof"))
                                smgtaglist.Add("Gasthof");
                            break;
                        //Braugarten
                        case "902D9BA559B1ED889694284F05CFA41E":
                            if (!smgtaglist.Contains("Andere Gastronomiebetriebe"))
                                smgtaglist.Add("Andere Gastronomiebetriebe");
                            if (!smgtaglist.Contains("Braugarten"))
                                smgtaglist.Add("Braugarten");
                            break;
                        //Schutzhütte
                        case "2328C37167BBBC5776831B8A262A6C36":
                            if (!smgtaglist.Contains("Hütten Almen"))
                                smgtaglist.Add("Hütten Almen");
                            if (!smgtaglist.Contains("Schutzhütten"))
                                smgtaglist.Add("Schutzhütten");
                            break;
                        //Alm
                        case "8025DB5CFCBA4FF281DDDE1F2B1D19A2":
                            if (!smgtaglist.Contains("Hütten Almen"))
                                smgtaglist.Add("Hütten Almen");
                            if (!smgtaglist.Contains("Almen"))
                                smgtaglist.Add("Almen");
                            break;
                        //Skihütte
                        case "B916489A77C94D8D92B03184EE587A31":
                            if (!smgtaglist.Contains("Hütten Almen"))
                                smgtaglist.Add("Hütten Almen");
                            if (!smgtaglist.Contains("Skihütten"))
                                smgtaglist.Add("Skihütten");
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
                            if (!smgtaglist.Contains("Roter Hahn"))
                                smgtaglist.Add("Roter Hahn");
                            break;
                    }
                }
            }


            return smgtaglist;
        }

        public static List<string> GetOdhTagListValidforTranslations()
        {
            List<string> myvalidsmgtagstotranslate = new List<string>();
            myvalidsmgtagstotranslate.Add("Restaurants Gasthäuser");
            myvalidsmgtagstotranslate.Add("Restaurants");
            myvalidsmgtagstotranslate.Add("Gasthäuser Gasthöfe");
            myvalidsmgtagstotranslate.Add("Pizzerias");
            myvalidsmgtagstotranslate.Add("Vinotheken");
            myvalidsmgtagstotranslate.Add("Bars Cafes Bistros");
            myvalidsmgtagstotranslate.Add("Hütten Almen");
            myvalidsmgtagstotranslate.Add("Schutzhütten");
            myvalidsmgtagstotranslate.Add("Almen");
            myvalidsmgtagstotranslate.Add("Skihütten");
            myvalidsmgtagstotranslate.Add("Bäuerliche Schankbetriebe");
            myvalidsmgtagstotranslate.Add("Buschen Hofschänke");
            myvalidsmgtagstotranslate.Add("Weinkellereien");
            myvalidsmgtagstotranslate.Add("Roter Hahn");

            //TODO ADD THE ANDERE CATEGORIES
            myvalidsmgtagstotranslate.Add("Andere Gastronomiebetriebe");
            myvalidsmgtagstotranslate.Add("Pub Disco");
            myvalidsmgtagstotranslate.Add("Apres Ski");
            myvalidsmgtagstotranslate.Add("Jausenstation");
            myvalidsmgtagstotranslate.Add("Buschenschank");
            myvalidsmgtagstotranslate.Add("Hofschank");
            myvalidsmgtagstotranslate.Add("Törggele Lokal");
            myvalidsmgtagstotranslate.Add("Schnellimbiss");
            myvalidsmgtagstotranslate.Add("Mensa");
            myvalidsmgtagstotranslate.Add("Eisdiele");
            myvalidsmgtagstotranslate.Add("Gasthaus");
            myvalidsmgtagstotranslate.Add("Gasthof");
            myvalidsmgtagstotranslate.Add("Braugarten");

            return myvalidsmgtagstotranslate;
        }


        #endregion
    }
}
