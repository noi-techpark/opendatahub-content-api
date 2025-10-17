// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Amazon.Runtime.Internal.Transform;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using LTSAPI.Parser;
using Newtonsoft.Json.Linq;
using SqlKata.Execution;
using SuedtirolWein;
using SuedtirolWein.Parser;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers.SuedtirolWein
{
    public class SuedtirolWeinCompanyImportHelper : ImportHelper, IImportHelper
    {
        public SuedtirolWeinCompanyImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

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
            mywinedata.Add("de", await GetSuedtirolWeinData.GetSueditrolWineCompaniesAsync(
                settings.SuedtirolWeinConfig.ServiceUrl,
                "de"
            ));
            mywinedata.Add("it", await GetSuedtirolWeinData.GetSueditrolWineCompaniesAsync(
                settings.SuedtirolWeinConfig.ServiceUrl,
                "it"
            ));
            mywinedata.Add("en", await GetSuedtirolWeinData.GetSueditrolWineCompaniesAsync(
                settings.SuedtirolWeinConfig.ServiceUrl,
                "en"
            ));
            mywinedata.Add("ru", await GetSuedtirolWeinData.GetSueditrolWineCompaniesAsync(
                settings.SuedtirolWeinConfig.ServiceUrl,
                "ru"
            ));
            mywinedata.Add("jp", await GetSuedtirolWeinData.GetSueditrolWineCompaniesAsync(
                settings.SuedtirolWeinConfig.ServiceUrl,
                "jp"
            ));
            mywinedata.Add("us", await GetSuedtirolWeinData.GetSueditrolWineCompaniesAsync(
                settings.SuedtirolWeinConfig.ServiceUrl,
                "us"
            ));

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

            ////For AdditionalInfos
            //List<string> languagelistcategories = new List<string>()
            //{
            //    "de",
            //    "it",
            //    "en",
            //    "nl",
            //    "cs",
            //    "pl",
            //    "fr",
            //    "ru",
            //};

            ////Getting valid Tags for Weinkellereien
            //var validtagsforcategories = await ODHTagHelper.GetODHTagsValidforCategories(
            //    QueryFactory,
            //    new List<string>() { "Essen Trinken" }
            //); //Essen Trinken ??

            //Load the json Data
            IDictionary<string, JArray> jsondata = default(Dictionary<string, JArray>);

            jsondata = await LTSAPIImportHelper.LoadJsonFiles(
            settings.JsonConfig.Jsondir,
            new List<string>()
                {
                        "ODHTagsSourceIDMLTS",                        
                        "ActivityPoiDisplayAsCategory",
                }
            );

            var metainfosidm = await QueryFactory
                .Query("odhactivitypoimetainfos")
                .Select("data")
                .Where("id", "metainfoexcelsmgpoi")
                .GetObjectSingleAsync<MetaInfosOdhActivityPoi>();


            //Loading WineAwards
            var wineawardlist = await WineAwardHelper.GetReducedWithWineAwardList(QueryFactory);

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

                //language de is always present
                mywinecompanies.Add("de", winedata);
                haslanguage.Add("de");

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

                var mysuedtirolweinquery = QueryFactory
                    .Query("smgpois")
                    .Select("data")
                    .Where("id", dataid.ToLower());

                var suedtirolweinpoi =
                    await mysuedtirolweinquery.GetObjectSingleAsync<ODHActivityPoiLinked>();

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
                suedtirolweinpoi.SmgActive = true;

                //Create LocationInfo
                //We set the LocationInfo only on new Objects because often the LocationInfo is wrongly added so we can edit it 
                if (newwinecompany)
                {
                    suedtirolweinpoi.LocationInfo = await suedtirolweinpoi.UpdateLocationInfoExtension(
                        QueryFactory
                    );
                }
                
                //If no locationInfo is set set to inactive?
                if(suedtirolweinpoi.LocationInfo.Equals(new LocationInfo()))
                {
                    setinactive = true;
                }

                //DistanceCalculation
                await suedtirolweinpoi.UpdateDistanceCalculation(QueryFactory);

                //Fill AdditionalInfos.Categories


                //Fill RelatedContent


                //Fill ODHTags (Essen Trinken, Weinkellereien)

                //Fill TagIds

                //Create Tags out of TagIds

                //Fill AdditionalProperties














                ////Tags
                //suedtirolweinpoi.SmgTags ??= new List<string>();
                //if (
                //    suedtiroltypemain?.Id is { }
                //    && !suedtirolweinpoi.SmgTags.Contains(suedtiroltypemain.Id.ToLower())
                //)
                //    suedtirolweinpoi.SmgTags.Add(suedtiroltypemain.Id.ToLower());
                //if (
                //    suedtiroltypesub?.Id is { }
                //    && !suedtirolweinpoi.SmgTags.Contains(suedtiroltypesub.Id.ToLower())
                //)
                //    suedtirolweinpoi.SmgTags.Add(suedtiroltypesub.Id.ToLower());

                ////Setting Categorization by Valid Tags
                //var currentcategories = validtagsforcategories.Where(x =>
                //    suedtirolweinpoi.SmgTags.Contains(x.Id.ToLower())
                //);
                //foreach (var smgtagtotranslate in currentcategories)
                //{
                //    foreach (var languagecategory in languagelistcategories)
                //    {
                //        if (
                //            suedtirolweinpoi.AdditionalPoiInfos[languagecategory].Categories == null
                //        )
                //            suedtirolweinpoi.AdditionalPoiInfos[languagecategory].Categories =
                //                new List<string>();

                //        if (
                //            smgtagtotranslate.TagName.ContainsKey(languagecategory)
                //            && (
                //                !suedtirolweinpoi
                //                    .AdditionalPoiInfos[languagecategory]
                //                    .Categories?.Contains(
                //                        smgtagtotranslate.TagName[languagecategory].Trim()
                //                    ) ?? false
                //            )
                //        )
                //            suedtirolweinpoi
                //                .AdditionalPoiInfos[languagecategory]
                //                .Categories?.Add(
                //                    smgtagtotranslate.TagName[languagecategory].Trim()
                //                );
                //    }
                //}




                //RELATED CONTENT
                //Wineids als RElated Content
                if (!String.IsNullOrEmpty(winedata.Element("wineids").Value))
                {
                    List<RelatedContent> myrelatedcontentlist = new List<RelatedContent>();

                    var mywines = wineawardreducelist.Where(x => x.CompanyId == dataid).ToList();

                    foreach (var mywine in mywines)
                    {
                        RelatedContent relatedcontent = new RelatedContent();
                        relatedcontent.Id = mywine.Id;
                        //relatedcontent.Name = mywine.Name;
                        relatedcontent.Type = "wineaward";

                        myrelatedcontentlist.Add(relatedcontent);
                    }

                    suedtirolweinpoi.RelatedContent = myrelatedcontentlist.ToList();
                }

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
                suedtirolweinpoi.SyncSourceInterface = "suedtirolwein-company";
                suedtirolweinpoi.SyncUpdateMode = "Full";
                suedtirolweinpoi.LastChange = DateTime.Now;
         
                //Set Main Type
                ODHActivityPoiHelper.SetMainCategorizationForODHActivityPoi(suedtirolweinpoi);

                //Add Mapping
                var suedtirolweinid = new Dictionary<string, string>() { { "id", dataid } };
                suedtirolweinpoi.Mapping.TryAddOrUpdate("suedtirolwein", suedtirolweinid);

                //Set Tags based on OdhTags
                await GenericTaggingHelper.AddTagsToODHActivityPoi(
                    suedtirolweinpoi,
                    settings.JsonConfig.Jsondir
                );
                //Create Tag Objects
                suedtirolweinpoi.TagIds =
                    suedtirolweinpoi.Tags != null
                        ? suedtirolweinpoi.Tags.Select(x => x.Id).ToList()
                        : null;

                var result = await InsertDataToDB(
                    suedtirolweinpoi,
                    new KeyValuePair<string, XElement>(dataid, winedata)
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

        #region CompatibilityHelpers

        //Adds all Redactional Assigned Tags from the old Record to the new Record
        private async Task MergeTags(ODHActivityPoiLinked poiNew, ODHActivityPoiLinked poiOld)
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


        //Assign ODHTags and preserve old Tags

        //Assign Tags

        //Assign Categorization

        #endregion
    }
}
