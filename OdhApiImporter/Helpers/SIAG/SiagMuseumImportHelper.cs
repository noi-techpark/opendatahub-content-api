// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DataModel.helpers;
using Helper;
using Helper.Generic;
using Helper.IDM;
using Helper.Location;
using Helper.Tagging;
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SIAG;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers
{
    public class SiagMuseumImportHelper : ImportHelper, IImportHelper
    {
        public SiagMuseumImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //Import the actual museums List from SIAG
            var museumslist = await ImportList(cancellationToken);
            //Parse siag data single and import each museum
            var updateresult = await ImportData(museumslist, cancellationToken);
            //If in the DB there are museums no more listed in the siag response set this data to inactive
            var deleteresult = await SetDataNotinListToInactive(museumslist, cancellationToken);

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }

        private async Task<XDocument> ImportList(CancellationToken cancellationToken)
        {
            var myxml = await SIAG.GetMuseumFromSIAG.GetMuseumList(
                settings.MusportConfig.ServiceUrl
            );

            XDocument mymuseumlist = new XDocument();
            XElement mymuseums = new XElement("Museums");

            XNamespace ns = "http://service.kks.siag";
            XNamespace ax211 = "http://data.service.kks.siag/xsd";

            var mymuseumlist2 =
                myxml.Root?.Element(ns + "return")?.Elements(ax211 + "museums")
                ?? Enumerable.Empty<XElement>();

            foreach (XElement idtoimport in mymuseumlist2)
            {
                XElement mymuseum = new XElement("Museum");
                mymuseum.Add(
                    new XAttribute("ID", idtoimport.Element(ax211 + "museId")?.Value ?? "")
                );
                mymuseum.Add(new XAttribute("PLZ", idtoimport.Element(ax211 + "plz")?.Value ?? ""));

                mymuseums.Add(mymuseum);
            }

            mymuseumlist.Add(mymuseums);

            WriteLog.LogToConsole(
                "",
                "dataimport",
                "list.siagmuseum",
                new ImportLog()
                {
                    sourceid = "",
                    sourceinterface = "siag.museum",
                    success = true,
                    error = "",
                }
            );

            return mymuseumlist;
        }

        private async Task<UpdateDetail> ImportData(
            XDocument mymuseumlist,
            CancellationToken cancellationToken
        )
        {
            XElement? mymuseumroot = mymuseumlist.Root;

            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            //Load the json Data
            IDictionary<string, JArray> jsondata = default(Dictionary<string, JArray>);

            jsondata = await LTSAPIImportHelper.LoadJsonFiles(
            settings.JsonConfig.Jsondir,
            new List<string>()
                {
                        "ODHTagsSourceIDMLTS",
                        "ActivityPoiDisplayAsCategory",
                        "AutoPublishTags",
                }
            );

            var metainfosidm = await QueryFactory
               .Query("odhactivitypoimetainfos")
               .Select("data")
               .Where("id", "metainfoexcelsmgpoi")
               .GetObjectSingleAsync<MetaInfosOdhActivityPoi>();

            foreach (
                XElement mymuseumelement in mymuseumroot?.Elements("Museum")
                    ?? Enumerable.Empty<XElement>()
            )
            {
                var importresult = await ImportDataSingle(
                    mymuseumelement,
                    metainfosidm,
                    jsondata
                );

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
            XElement mymuseumelement,
            MetaInfosOdhActivityPoi metainfosidm,
            IDictionary<string, JArray> jsondata
        )
        {
            string idtoreturn = "";

            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            try
            {
                XNamespace ns = "http://service.kks.siag";

                string museumid = mymuseumelement.Attribute("ID")?.Value ?? "";
                idtoreturn = museumid;

                string plz = mymuseumelement.Attribute("PLZ")?.Value ?? "";

                //Import Museum data from Siag
                var mymuseumdata = await SIAG.GetMuseumFromSIAG.GetMuseumDetail(
                    settings.MusportConfig.ServiceUrl,
                    museumid
                );
                var mymuseumxml = mymuseumdata?.Root?.Element(ns + "return");

                //GET Museum TODO CHANGE HERE LOAD HERE WITH THE MAPPING QUERY because some Ids are not matching this query
                //var mymuseum = await LoadDataFromDB<ODHActivityPoiLinked>("smgpoi" + museumid + "siag", IDStyle.lowercase);
                var mymuseumquery = QueryFactory
                   .Query("smgpois")
                   .Select("data")
                   .WhereRaw("data->'Mapping'->'siag'->>'museId' = $$", museumid)
                   .Where("gen_syncsourceinterface", "museumdata");

                var mymuseum = await mymuseumquery.GetObjectSingleAsync<ODHActivityPoiLinked>();


                bool newmuseum = false;

                XNamespace ax211 = "http://data.service.kks.siag/xsd";
                string originalid = mymuseumxml?.Element(ax211 + "museId")?.Value ?? "";

                if (mymuseum == null)
                {
                    //New data
                    mymuseum = new ODHActivityPoiLinked();
                    mymuseum.FirstImport = DateTime.Now;
                    newmuseum = true;                    
                    mymuseum.Id = "smgpoi" + originalid + "siag";
                }
                else
                    await RemoveAllAutomaticallyassignedTags(mymuseum);

                mymuseum.Active = true;
                string gemeindeid = mymuseumxml?.Element(ax211 + "gemeindeId")?.Value ?? "";
                if (mymuseumxml is { })
                    SIAG.Parser.ParseMuseum.ParseMuseumToPG(mymuseum, mymuseumxml, plz);

                //Add Mapping
                var museummuseId = new Dictionary<string, string>() { { "museId", originalid } };
                mymuseum.Mapping.TryAddOrUpdate("siag", museummuseId);

                //Add Shortname
                mymuseum.Shortname = mymuseum.Detail["de"].Title?.Trim();

                //Add Haslanguage
                List<string> haslanguagelist = new List<string>() { "de", "it", "en" };
                mymuseum.HasLanguage = haslanguagelist;

                //LocationInfo
                //To check we set the LocationInfo only on new Objects because often the LocationInfo is wrongly added so we can edit it 
                if (newmuseum)
                {
                    mymuseum.LocationInfo = await mymuseum.UpdateLocationInfoExtension(
                        QueryFactory
                    );
                }

                //To check Bergwerke/Naturparkhäuser

                //Assign ODHTags (do not delete redactional assigned tags, Kultur & Sehenswürdigkeiten/Museen/Bergwerke)                
                await AssignODHTags(mymuseum);

                //Fill TagIds
                await AssignTags(mymuseum);

                //Fill AdditionalInfos.Categories
                SetAdditionalInfosCategoriesByODHTags(mymuseum, jsondata);

                //Fill AdditionalProperties
                mymuseum.FillSiagMuseumAdditionalProperties();

                //Add Meta Title
                await AddIDMMetaTitleAndDescription(mymuseum, metainfosidm);
                
                //DistanceCalculation
                await mymuseum.UpdateDistanceCalculation(QueryFactory);

                //Setting Common Infos
                mymuseum.Source = "siag";
                mymuseum.SyncSourceInterface = "museumdata";
                mymuseum.SyncUpdateMode = "full";
                mymuseum.LastChange = DateTime.Now;

                //ADD MAPPING
                var mappingid = new Dictionary<string, string>() { 
                    { "museId", museumid },
                    { "gemeindeId", gemeindeid }
                };
                mymuseum.Mapping.TryAddOrUpdate("siag", mappingid);
                
                //Create Tags and preserve the old TagEntries
                await mymuseum.UpdateTagsExtension(QueryFactory);
                
                if (mymuseumdata?.Root is { })
                {
                    var result = await InsertDataToDB(
                        mymuseum,
                        new KeyValuePair<string, XElement>(museumid, mymuseumdata.Root),
                        jsondata
                    );
                    newcounter = newcounter + result.created ?? 0;
                    updatecounter = updatecounter + result.updated ?? 0;
                    if (mymuseum.Id is { })
                        WriteLog.LogToConsole(
                            mymuseum.Id,
                            "dataimport",
                            "single.siagmuseum",
                            new ImportLog()
                            {
                                sourceid = mymuseum.Id,
                                sourceinterface = "siag.museum",
                                success = true,
                                error = "",
                            }
                        );
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    idtoreturn,
                    "dataimport",
                    "single.siagmuseum",
                    new ImportLog()
                    {
                        sourceid = idtoreturn,
                        sourceinterface = "siag.museum",
                        success = false,
                        error = ex.Message,
                    }
                );

                errorcounter = errorcounter + 1;
            }

            return new UpdateDetail()
            {
                created = newcounter,
                updated = updatecounter,
                deleted = 0,
                error = errorcounter,
            };
        }

        private async Task<UpdateDetail> SetDataNotinListToInactive(
            XDocument mymuseumlist,
            CancellationToken cancellationToken
        )
        {
            int updateresult = 0;
            int deleteresult = 0;
            int errorresult = 0;

            try
            {
                List<string?> mymuseumroot =
                    mymuseumlist
                        .Root?.Elements("Museum")
                        .Select(x => x.Attribute("ID")?.Value)
                        .ToList() ?? new();

                var mymuseumquery = QueryFactory
                    .Query("smgpois")
                    .SelectRaw("data->'Mapping'->'siag'->>'museId'")                    
                    .Where("gen_syncsourceinterface", "museumdata");

                var mymuseumsondb = await mymuseumquery.GetAsync<string>();

                var idstodelete = mymuseumsondb.Where(p => !mymuseumroot.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var result = await DeleteOrDisableData<ODHActivityPoiLinked>(idtodelete, false);

                    updateresult = updateresult + result.Item1;
                    deleteresult = deleteresult + result.Item2;
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.siagmuseum",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "siag.museum",
                        success = false,
                        error = ex.Message,
                    }
                );

                errorresult = errorresult + 1;
            }

            return new UpdateDetail()
            {
                created = 0,
                updated = updateresult,
                deleted = deleteresult,
                error = errorresult,
            };
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            ODHActivityPoiLinked odhactivitypoi,
            KeyValuePair<string, XElement> siagmuseumdata,
            IDictionary<string, JArray>? jsonfiles
        )
        {
            odhactivitypoi.Id = odhactivitypoi.Id?.ToLower();

            //Set LicenseInfo
            odhactivitypoi.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                odhactivitypoi,
                Helper.LicenseHelper.GetLicenseforOdhActivityPoi
            );

            //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
            odhactivitypoi._Meta = MetadataHelper.GetMetadataobject(odhactivitypoi);

            //Set Publishedon
            //Add the PublishedOn Logic
            //Exception here all Tags with autopublish has to be passed
            var autopublishtaglist = jsonfiles != null && jsonfiles["AutoPublishTags"] != null ? jsonfiles["AutoPublishTags"].ToObject<List<AllowedTags>>() : null;
            //Set PublishedOn with allowedtaglist
            odhactivitypoi.CreatePublishedOnList(autopublishtaglist);

            var rawdataid = await InsertInRawDataDB(siagmuseumdata);

            return await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                odhactivitypoi,
                new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("siag.museum.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid
            );
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, XElement> siagmuseumdata)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "siag",
                    importdate = DateTime.Now,
                    raw = siagmuseumdata.Value.ToString(),
                    sourceinterface = "museumdata",
                    sourceid = siagmuseumdata.Key,
                    sourceurl = settings.MusportConfig.ServiceUrl,
                    type = "odhactivitypoi.museum",
                    license = "open",
                    rawformat = "xml",
                }
            );
        }

        #region Helpers

        //Removes all Tags 
        private async Task RemoveAllAutomaticallyassignedTags(ODHActivityPoiLinked poi)
        {
            if (poi != null)
            {
                //Readd all Redactional Tags to check if this query fits
                var redactionalassignedTags = poi.Tags != null ? poi.Tags.Where(x => x.Source != "idm" && x.Source != "siag").ToList() : null;
                if (redactionalassignedTags != null)
                {
                    if(poi.TagIds == null)
                        poi.TagIds = new List<string>();

                    poi.TagIds = redactionalassignedTags.Select(x => x.Id).ToList();
                }

                if(poi.SmgTags != null)
                {
                    poi.SmgTags.Remove("museen");
                    poi.SmgTags.Remove("museen kultur");
                    poi.SmgTags.Remove("museen natur");
                    poi.SmgTags.Remove("museen technik");
                    poi.SmgTags.Remove("museen kunst");
                    poi.SmgTags.Remove("bergwerke");
                    poi.SmgTags.Remove("naturparkhäuser");
                    poi.SmgTags.Remove("barrierefrei");
                    poi.SmgTags.Remove("familientip");
                    poi.SmgTags.Remove("kultur sehenswürdigkeiten");
                    poi.SmgTags.Remove("poi");
                }
            }            
        }

        //Assign ODHTags and preserve old Tags
        private async Task AssignODHTags(ODHActivityPoiLinked poiNew)
        {
            //Simply Ensure that tat Essen Trinken & Weinkellerei is assigned
            if (poiNew.SmgTags == null)
                poiNew.SmgTags = new List<string>();

            if (!poiNew.SmgTags.Contains("poi"))
                poiNew.SmgTags.Add("poi");
            if (!poiNew.SmgTags.Contains("kultur sehenswürdigkeiten"))
                poiNew.SmgTags.Add("kultur sehenswürdigkeiten");
            if (!poiNew.SmgTags.Contains("museen"))
                poiNew.SmgTags.Add("museen");
        }

        //Assign Tags
        private async Task AssignTags(ODHActivityPoiLinked poiNew)
        {
            //Simply Ensure that tat Essen Trinken & Weinkellerei is assigned
            if (poiNew.TagIds == null)
                poiNew.TagIds = new List<string>();

            //Old Tags
            if (!poiNew.TagIds.Contains("culture attractions"))
                poiNew.TagIds.Add("culture attractions");
            if (!poiNew.TagIds.Contains("museums"))
                poiNew.TagIds.Add("museums");
            if (!poiNew.TagIds.Contains("poi"))
                poiNew.TagIds.Add("poi");
        }

        //Assign Categorization
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

        //Metadata assignment detailde.MetaTitle = detailde.Title + " | suedtirol.info";
        private async Task AddIDMMetaTitleAndDescription(ODHActivityPoiLinked activityNew, MetaInfosOdhActivityPoi metainfo)
        {
            IDMCustomHelper.SetMetaInfoForActivityPoi(activityNew, metainfo);
        }

        #endregion
    }
}
