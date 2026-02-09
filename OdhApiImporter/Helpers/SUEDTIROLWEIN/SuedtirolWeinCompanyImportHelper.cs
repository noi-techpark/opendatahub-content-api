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
using SqlKata.Execution;
using SuedtirolWein;
using SuedtirolWein.Parser;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers.SuedtirolWein
{
    public class SuedtirolWeinCompanyImportHelper : ImportHelper, IImportHelper
    {
        private IOdhPushNotifier OdhPushnotifier;

        public SuedtirolWeinCompanyImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL) 
        {
            this.OdhPushnotifier = odhpushnotifier;
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            var winegastrolist = await ImportList(cancellationToken);

            var updateresult = await ImportData(winegastrolist, cancellationToken);

            var deleteresult = await SetDataNotinListToInactive(winegastrolist, cancellationToken);

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }

        public async Task<IDictionary<string, XDocument>> ImportList(
            CancellationToken cancellationToken = default
        )
        {
            IDictionary<string, XDocument> mywinedata = new Dictionary<string, XDocument>();
            List<string> languagestoretrieve = new List<string>() { "de", "it", "en", "ru", "jp", "us" };

            foreach(var language in languagestoretrieve)
            {
                mywinedata.Add(language, await GetSuedtirolWeinData.GetSuedtirolWineCompaniesAsync(
                settings.SuedtirolWeinConfig.ServiceUrl,
                language));
            }
                       
            return mywinedata;
        }

        private async Task<UpdateDetail> ImportData(
            IDictionary<string, XDocument> wineddatalist,
            CancellationToken cancellationToken = default
        )
        {
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
                        "GastronomyDisplayAsCategory",
                }
            );

            var metainfosidm = await QueryFactory
                .Query("odhactivitypoimetainfos")
                .Select("data")
                .Where("id", "metainfoexcelsmgpoi")
                .GetObjectSingleAsync<MetaInfosOdhActivityPoi>();

            //Loading WineAwards
            var wineawardlist = await WineAwardHelper.GetReducedWithWineAwardList(QueryFactory);

            //Loop trouth the de list which contains all Elements
            foreach (
                var winedata in wineddatalist["de"].Root?.Elements("item")
                    ?? Enumerable.Empty<XElement>()
            )
            {
                var importresult = await ImportDataSingle(
                    winedata,
                    wineddatalist,
                    wineawardlist.ToList(),
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

        public async Task<UpdateDetail> ImportDataSingle(
            XElement winedata,
            IDictionary<string, XDocument> winedatalist,                       
            List<ReducedWineAward> wineawardreducelist,
            MetaInfosOdhActivityPoi metainfosidm,
            IDictionary<string, JArray> jsondata
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            string dataid = winedata.Element("id").Value;

            try
            {
                IDictionary<string, XElement> mywinecompanies = new Dictionary<string, XElement>();
                List<string> haslanguage = new List<string>();

                foreach (var winedatalang in winedatalist)
                {
                    if(winedatalang.Value?.Root?.Elements("item")
                    .Where(x => x.Element("id").Value == dataid)
                    .FirstOrDefault() != null)
                    {
                        haslanguage.Add(winedatalang.Key);
                        mywinecompanies.Add(winedatalang.Key, winedatalang.Value?.Root?.Elements("item").Where(x => x.Element("id").Value == dataid).FirstOrDefault());                
                    }
                }

                bool newwinecompany = false;
                bool setinactive = false;
                
                //GET Wine Company
                var suedtirolweinpoi = await LoadDataFromDB<ODHActivityPoiLinked>(dataid.ToLower(), IDStyle.lowercase);

                if (suedtirolweinpoi == null)
                {
                    suedtirolweinpoi = new ODHActivityPoiLinked();
                    suedtirolweinpoi.FirstImport = DateTime.Now;
                    newwinecompany = true;
                }

                suedtirolweinpoi = ParseCompanyData.ParsetheCompanyData(
                    suedtirolweinpoi,
                    mywinecompanies,
                    haslanguage
                );

                suedtirolweinpoi.Active = true;
                //suedtirolweinpoi.SmgActive = true;


                //Create LocationInfo
                //We set the LocationInfo only on new Objects because often the LocationInfo is wrongly added so we can edit it 
                if (newwinecompany)
                {
                    suedtirolweinpoi.LocationInfo = await suedtirolweinpoi.UpdateLocationInfoExtension(
                        QueryFactory
                    );
                }

                //If no locationInfo is set set to inactive?
                if (suedtirolweinpoi.LocationInfo.Equals(new LocationInfo()))
                {
                    setinactive = true;
                }

                //DistanceCalculation
                await suedtirolweinpoi.UpdateDistanceCalculation(QueryFactory);

                //Fill AdditionalInfos.Categories
                SetAdditionalInfosCategoriesByODHTags(suedtirolweinpoi, jsondata);

                //Fill RelatedContent
                FillRelatedContent(winedata, dataid, suedtirolweinpoi, winedatalist, wineawardreducelist);

                //Fill ODHTags (Essen Trinken, Weinkellereien)
                await AssignODHTags(suedtirolweinpoi);

                //Fill TagIds
                await AssignTags(suedtirolweinpoi);

                //Fill AdditionalProperties
                suedtirolweinpoi.FillSuedtirolWeinCompanyAdditionalProperties();

                //Add Meta Title
                await AddIDMMetaTitleAndDescription(suedtirolweinpoi, metainfosidm);

                if (setinactive)
                {
                    suedtirolweinpoi.Active = false;
                    suedtirolweinpoi.SmgActive = false;
                }
                else
                {
                    suedtirolweinpoi.Active = true;
                    suedtirolweinpoi.SmgActive = true;
                }

                suedtirolweinpoi.HasLanguage = haslanguage;

                //Setting Common Infos
                suedtirolweinpoi.Source = "suedtirolwein";
                suedtirolweinpoi.SyncSourceInterface = "suedtirolweincompany";
                suedtirolweinpoi.SyncUpdateMode = "full";
                suedtirolweinpoi.LastChange = DateTime.Now;
                       
                //Add Mapping
                var suedtirolweinid = new Dictionary<string, string>() { { "id", dataid } };
                suedtirolweinpoi.Mapping.TryAddOrUpdate("suedtirolwein", suedtirolweinid);
                
                //Create Tags and preserve the old TagEntries
                await suedtirolweinpoi.UpdateTagsExtension(QueryFactory);

                var result = await InsertDataToDB(
                    suedtirolweinpoi,
                    new KeyValuePair<string, XElement>(dataid, winedata)
                );
                
                //Push Data if changed
                //push modified data to all published Channels
                //TODO adding the push status to the response
                result.pushed = await ImportUtils.CheckIfObjectChangedAndPush(
                    OdhPushnotifier,
                    result,
                    result.id,
                    "poi",
                    null,
                    "suedtirolwein.update"
                );


                newcounter = newcounter + result.created ?? 0;
                updatecounter = updatecounter + result.updated ?? 0;

                if (suedtirolweinpoi.Id is { })
                    WriteLog.LogToConsole(
                        dataid,
                        "dataimport",
                        "single.suedtirolweincompany",
                        new ImportLog()
                        {
                            sourceid = dataid,
                            sourceinterface = "suedtirolwein.company",
                            success = true,
                            error = "",
                        }
                    );
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    dataid,
                    "dataimport",
                    "single.suedtirolweincompany",
                    new ImportLog()
                    {
                        sourceid = dataid,
                        sourceinterface = "suedtirolwein.company",
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
            IDictionary<string, XDocument> mywinecompanylist,
            CancellationToken cancellationToken
        )
        {
            int updateresult = 0;
            int deleteresult = 0;
            int errorresult = 0;

            try
            {
                //The service returns always the same ids in each language
                List<string?> winecompaniesonsource =
                    mywinecompanylist["de"]
                        .Root?.Elements("item")
                        .Select(x => x.Attribute("id")?.Value)
                        .ToList() ?? new();

                var myquery = QueryFactory
                    .Query("smgpois")
                    .SelectRaw("data->'Mapping'->'suedtirolwein'->>'id'")
                    .Where("gen_syncsourceinterface", "suedtirolwein");

                var winecompaniesondb = await myquery.GetAsync<string>();

                var idstodelete = winecompaniesondb.Where(p =>
                    !winecompaniesonsource.Any(p2 => p2 == p)
                );

                foreach (var idtodelete in idstodelete)
                {
                    var result = await DeleteOrDisableSuedtirolWeinData(idtodelete, false);

                    //Push Data if changed
                    //push modified data to all published Channels
                    //TODO adding the push status to the response
                    result.pushed = await ImportUtils.CheckIfObjectChangedAndPush(
                        OdhPushnotifier,
                        result,
                        idtodelete,
                        "poi",
                        null,
                        "suedtirolwein.update"
                    );

                    updateresult += result.updated ?? 0;
                    deleteresult += result.deleted ?? 0;
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.suedtirolweincompany",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "suedtirolwein.company",
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
            KeyValuePair<string, XElement> suedtirolweindata
        )
        {
            odhactivitypoi.Id = odhactivitypoi.Id?.ToLower();

            //Set LicenseInfo
            odhactivitypoi.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<ODHActivityPoi>(
                odhactivitypoi,
                Helper.LicenseHelper.GetLicenseforOdhActivityPoi
            );

            //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
            odhactivitypoi._Meta = MetadataHelper.GetMetadataobject(odhactivitypoi);

            //Set PublishedOn to marketplace and suedtirolwein
            odhactivitypoi.CreatePublishedOnList(
                new List<AllowedTags>()
                {
                    new AllowedTags()
                    {
                        Id = "weinkellereien",
                        PublishDataWithTagOn = new Dictionary<string, bool>()
                        {
                            { "idm-marketplace", true },
                            { "suedtirolwein.com", true },
                        },
                    },
                }
            );

            var rawdataid = await InsertInRawDataDB(suedtirolweindata);

            return await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                odhactivitypoi,
                new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("suedtirolwein.company.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid
            );
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, XElement> suedtirolweindata)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "suedtirolwein",
                    importdate = DateTime.Now,
                    raw = suedtirolweindata.Value.ToString(),
                    sourceinterface = "suedtirolwein-company",
                    sourceid = suedtirolweindata.Key,
                    sourceurl = "https://suedtirolwein.secure.consisto.net/companies.ashx",
                    type = "odhactivitypoi.winecompany",
                    license = "open",
                    rawformat = "xml",
                }
            );
        }

        public async Task<UpdateDetail> DeleteOrDisableSuedtirolWeinData(string id, bool delete)
        {
            UpdateDetail deletedisableresult = default(UpdateDetail);

            PGCRUDResult result = default(PGCRUDResult);

            if (delete)
            {
                result = await QueryFactory.DeleteData<ODHActivityPoiLinked>(
                    id,
                    new DataInfo(table, CRUDOperation.Delete),
                    new CRUDConstraints()
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
                var query = QueryFactory.Query(table).Select("data").Where("id", id);

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
                        data.CreatePublishedOnList(
                            new List<AllowedTags>()
                            {
                                new AllowedTags()
                                {
                                    Id = "weinkellereien",
                                    PublishDataWithTagOn = new Dictionary<string, bool>()
                                    {
                                        { "idm-marketplace", true },
                                        { "suedtirolwein.com", true },
                                    },
                                },
                                }
                            );

                        result = await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                               data,
                               new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                               new EditInfo("suedtirolwein.companies.import.deactivate", importerURL),
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


        #region CompatibilityHelpers

        //Assign ODHTags
        private async Task AssignODHTags(ODHActivityPoiLinked poiNew)
        {
            //Simply Ensure that tat Essen Trinken & Weinkellerei is assigned
            if(poiNew.SmgTags == null)
                poiNew.SmgTags = new List<string>();

            if (!poiNew.SmgTags.Contains("gastronomy"))
                poiNew.SmgTags.Add("gastronomy");
            if (!poiNew.SmgTags.Contains("essen trinken"))
                poiNew.SmgTags.Add("essen trinken");
            if (!poiNew.SmgTags.Contains("weinkellereien"))
                poiNew.SmgTags.Add("weinkellereien");
        }

        //Assign Tags
        private async Task AssignTags(ODHActivityPoiLinked poiNew)
        {
            //Simply Ensure that tat Essen Trinken & Weinkellerei is assigned
            if (poiNew.TagIds == null)
                poiNew.TagIds = new List<string>();

            //Old Tags
            if (!poiNew.TagIds.Contains("gastronomy"))
                poiNew.TagIds.Add("gastronomy");
            if (!poiNew.TagIds.Contains("eating drinking"))
                poiNew.TagIds.Add("eating drinking");
            if (!poiNew.TagIds.Contains("wineries"))
                poiNew.TagIds.Add("wineries");

            //LTS Rids
            //Kellereien und Winzer
            if (!poiNew.TagIds.Contains("6EFED925DF3B4EF5B69495E994F446AC"))
                poiNew.TagIds.Add("6EFED925DF3B4EF5B69495E994F446AC");
            //Produktionsstätten
            if (!poiNew.TagIds.Contains("28CDEF87206E464D9B179FBCAF506457"))
                poiNew.TagIds.Add("28CDEF87206E464D9B179FBCAF506457");
        }

   
        //Assign Categorization
        private static void SetAdditionalInfosCategoriesByODHTags(ODHActivityPoiLinked poi, IDictionary<string, JArray>? jsonfiles)
        {
            //If a Tag is found in 
            //SET ADDITIONALINFOS
            //Setting Categorization by Valid Tags
            var validcategorylist = jsonfiles != null && jsonfiles["GastronomyDisplayAsCategory"] != null ? jsonfiles["GastronomyDisplayAsCategory"].ToObject<List<CategoriesTags>>() : null;

            if (validcategorylist != null && poi.SmgTags != null)
            {
                var currentcategories = validcategorylist.Where(x => poi.SmgTags.Select(y => y.ToLower()).Contains(x.Id.ToLower())).ToList();

                if (currentcategories != null)
                {
                    if (poi.AdditionalPoiInfos == null)
                        poi.AdditionalPoiInfos = new Dictionary<string, AdditionalPoiInfos>();

                    foreach (var languagecategory in new List<string>() { "de", "it", "en", "nl", "cs", "pl", "fr", "ru" })
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

                        poi.AdditionalPoiInfos.TryAddOrUpdate(languagecategory, additionalPoiInfos);
                    }
                }
            }
        }

        //Fill Related Content for Wine Awards
        private static void FillRelatedContent(XElement winedata, string companyid, ODHActivityPoiLinked poi, IDictionary<string, XDocument> winedatalist, List<ReducedWineAward> wineawardreducelist)
        {
            //RELATED CONTENT
            //Wineids als RElated Content
            if (!String.IsNullOrEmpty(winedata.Element("wineids").Value))
            {
                List<RelatedContent> myrelatedcontentlist = new List<RelatedContent>();

                var mywines = wineawardreducelist.Where(x => x.CompanyId == companyid).ToList();

                foreach (var mywine in mywines)
                {
                    RelatedContent relatedcontent = new RelatedContent();
                    relatedcontent.Id = mywine.Id;
                    //relatedcontent.Name = mywine.Name;
                    relatedcontent.Type = "wineaward";

                    myrelatedcontentlist.Add(relatedcontent);
                }

                poi.RelatedContent = myrelatedcontentlist.ToList();
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
