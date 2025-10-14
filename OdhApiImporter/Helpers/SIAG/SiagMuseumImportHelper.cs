// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using SIAG;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class SiagMuseumImportHelper : ImportHelper, IImportHelper
    {
        public SiagMuseumImportHelper(
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

            //For AdditionalInfos
            List<string> languagelistcategories = new List<string>()
            {
                "de",
                "it",
                "en",
                "nl",
                "cs",
                "pl",
                "fr",
                "ru",
            };

            //Getting valid Tags for Museums
            var validtagsforcategories = await ODHTagHelper.GetODHTagsValidforCategories(
                QueryFactory,
                new List<string>() { "Kultur Sehenswürdigkeiten" }
            );

            //Loading District & Municipality data
            var districtreducedinfo = await GpsHelper.GetReducedWithGPSInfoList(
                QueryFactory,
                "districts"
            );
            var municipalityreducedinfo = await GpsHelper.GetReducedWithGPSInfoList(
                QueryFactory,
                "municipalities"
            );

            foreach (
                XElement mymuseumelement in mymuseumroot?.Elements("Museum")
                    ?? Enumerable.Empty<XElement>()
            )
            {
                var importresult = await ImportDataSingle(
                    mymuseumelement,
                    languagelistcategories,
                    validtagsforcategories,
                    municipalityreducedinfo.ToList(),
                    districtreducedinfo.ToList()
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
            List<string> languagelistcategories,
            IEnumerable<SmgTags> validtagsforcategories,
            List<ReducedWithGPSInfo> municipalityreducedlist,
            List<ReducedWithGPSInfo> districtreducedlist
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

                //Improve Performance this query is very slow!!!!
                var mymuseumquery = QueryFactory
                    .Query("smgpois")
                    .Select("data")
                    .WhereRaw("data->>'CustomId' = $$", museumid.ToLower());

                var mymuseum = await mymuseumquery.GetObjectSingleAsync<ODHActivityPoiLinked>();

                if (mymuseum == null)
                {
                    //Neuen Datensatz
                    mymuseum = new ODHActivityPoiLinked();
                    mymuseum.FirstImport = DateTime.Now;

                    XNamespace ax211 = "http://data.service.kks.siag/xsd";

                    string siagid = mymuseumxml?.Element(ax211 + "museId")?.Value ?? "";
                    string gemeindeid = mymuseumxml?.Element(ax211 + "gemeindeId")?.Value ?? "";

                    mymuseum.Id = "smgpoi" + siagid + "siag";

                    mymuseum.CustomId = siagid;
                    mymuseum.Active = true;
                    mymuseum.SmgActive = true;

                    mymuseum.FirstImport = DateTime.Now;

                    if (mymuseumxml is { })
                        SIAG.Parser.ParseMuseum.ParseMuseumToPG(mymuseum, mymuseumxml, plz);

                    //ADD MAPPING
                    var museummuseId = new Dictionary<string, string>() { { "museId", siagid } };
                    mymuseum.Mapping.TryAddOrUpdate("siag", museummuseId);

                    mymuseum.Shortname = mymuseum.Detail["de"].Title?.Trim();

                    //Suedtirol Type laden
                    var mysmgmaintype = await ODHTagHelper.GeODHTagByID(
                        QueryFactory,
                        "Kultur Sehenswürdigkeiten"
                    );
                    var mysmgsubtype = await ODHTagHelper.GeODHTagByID(QueryFactory, "Museen");
                    var mysmgpoipoitype = new List<SmgTags>();

                    List<string> museumskategorien = new List<string>();
                    var mymuseumscategoriesstrings = mymuseum
                        .PoiProperty["de"]
                        .Where(x => x.Name == "categories")
                        .Select(x => x.Value)
                        .ToList();
                    foreach (var mymuseumscategoriesstring in mymuseumscategoriesstrings)
                    {
                        var splittedlist = mymuseumscategoriesstring?.Split(',').ToList() ?? new();

                        foreach (var splitted in splittedlist)
                        {
                            if (!string.IsNullOrEmpty(splitted))
                            {
                                museumskategorien.Add(splitted.Trim());

                                var mykategoriequery = await ODHTagHelper.GeODHTagByID(
                                    QueryFactory,
                                    "Museen " + splitted.Trim()
                                );
                                if (mykategoriequery is { })
                                    mysmgpoipoitype.Add(mykategoriequery);
                            }
                        }
                    }

                    mymuseum.Type = mysmgmaintype?.Shortname;
                    mymuseum.SubType = mysmgsubtype?.Shortname;

                    List<string> mysmgtags = mymuseum.SmgTags?.ToList() ?? new();

                    if (mysmgmaintype?.Id is { } && !mysmgtags.Contains(mysmgmaintype.Id.ToLower()))
                        mysmgtags.Add(mysmgmaintype.Id.ToLower());

                    if (mysmgsubtype?.Id is { } && !mysmgtags.Contains(mysmgsubtype.Id.ToLower()))
                        mysmgtags.Add(mysmgsubtype.Id.ToLower());

                    if (mysmgpoipoitype.Count > 0)
                    {
                        foreach (var mysmgpoipoitypel in mysmgpoipoitype)
                        {
                            if (
                                mysmgpoipoitypel.Id is { }
                                && !mysmgtags.Contains(mysmgpoipoitypel.Id.ToLower())
                            )
                                mysmgtags.Add(mysmgpoipoitypel.Id.ToLower());
                        }
                    }
                    mymuseum.SmgTags = mysmgtags.ToList();

                    if (mysmgpoipoitype.Count > 0)
                        mymuseum.PoiType = mysmgpoipoitype.FirstOrDefault()?.Shortname;
                    else
                        mymuseum.PoiType = "";

                    List<string> haslanguagelist = new();

                    haslanguagelist.Add("de");
                    haslanguagelist.Add("it");
                    haslanguagelist.Add("en");

                    mymuseum.HasLanguage = haslanguagelist.ToList();

                    foreach (var langcat in languagelistcategories)
                    {
                        AdditionalPoiInfos additional = new AdditionalPoiInfos();
                        additional.Language = langcat;
                        additional.MainType = mysmgmaintype?.TagName[langcat];
                        additional.SubType = mysmgsubtype?.TagName[langcat];
                        additional.PoiType =
                            mysmgpoipoitype.Count > 0
                                ? mysmgpoipoitype.FirstOrDefault()?.TagName[langcat]
                                : "";
                        mymuseum.AdditionalPoiInfos.TryAddOrUpdate(langcat, additional);
                    }

                    //Setting Categorization by Valid Tags
                    var currentcategories = validtagsforcategories.Where(x =>
                        mymuseum.SmgTags.Contains(x.Id.ToLower())
                    );

                    foreach (var smgtagtotranslate in currentcategories)
                    {
                        foreach (string languagecategory in languagelistcategories)
                        {
                            if (mymuseum.AdditionalPoiInfos![languagecategory].Categories == null)
                                mymuseum.AdditionalPoiInfos[languagecategory].Categories =
                                    new List<string>();

                            if (
                                smgtagtotranslate.TagName.ContainsKey(languagecategory)
                                && (
                                    !mymuseum.AdditionalPoiInfos?[
                                        languagecategory
                                    ].Categories?.Contains(
                                        smgtagtotranslate.TagName[languagecategory].Trim()
                                    ) ?? false
                                )
                            )
                                mymuseum.AdditionalPoiInfos![languagecategory]!.Categories!.Add(
                                    smgtagtotranslate.TagName[languagecategory].Trim()
                                );
                        }
                    }

                    //Get Locationinfo by given GPS Points
                    if (mymuseum.GpsInfo != null && mymuseum.GpsInfo.Count > 0)
                    {
                        if (
                            mymuseum.GpsInfo.FirstOrDefault()?.Latitude != 0
                            && mymuseum.GpsInfo.FirstOrDefault()?.Longitude != 0
                        )
                        {
                            var district = await LocationInfoHelper.GetNearestDistrictbyGPS(
                                QueryFactory,
                                mymuseum.GpsInfo.FirstOrDefault()!.Latitude,
                                mymuseum.GpsInfo.FirstOrDefault()!.Longitude,
                                30000
                            );

                            if (district != null)
                            {
                                var locinfo = await LocationInfoHelper.GetTheLocationInfoDistrict(
                                    QueryFactory,
                                    district.Id
                                );

                                mymuseum.LocationInfo = locinfo;
                                mymuseum.TourismorganizationId = locinfo.TvInfo?.Id;
                            }
                        }
                    }

                    //If still no locinfo assigned
                    if (mymuseum.LocationInfo == null)
                    {
                        if (gemeindeid.StartsWith("3"))
                            mymuseum.LocationInfo =
                                await LocationInfoHelper.GetTheLocationInfoMunicipality_Siag(
                                    QueryFactory,
                                    gemeindeid
                                );
                        if (gemeindeid.StartsWith("8"))
                            mymuseum.LocationInfo =
                                await LocationInfoHelper.GetTheLocationInfoDistrict_Siag(
                                    QueryFactory,
                                    gemeindeid
                                );

                        mymuseum.TourismorganizationId = mymuseum.LocationInfo?.TvInfo?.Id;
                    }
                }
                else
                {
                    //mymuseum.CustomId = siagid;
                    mymuseum.Active = true;
                    //mymuseum.SmgActive = true;

                    if (mymuseumxml is { })
                        SIAG.Parser.ParseMuseum.ParseMuseumToPG(mymuseum, mymuseumxml, plz);

                    string subtype = "Museen";
                    if (mymuseum.SubType == "Bergwerke")
                        subtype = "Bergwerke";
                    if (mymuseum.SubType == "Naturparkhäuser")
                        subtype = "Naturparkhäuser";

                    //Load Suedtirol Type
                    var mysmgmaintype = await ODHTagHelper.GeODHTagByID(
                        QueryFactory,
                        "Kultur Sehenswürdigkeiten"
                    );
                    var mysmgsubtype = await ODHTagHelper.GeODHTagByID(QueryFactory, subtype);
                    var mysmgpoipoitype = new List<SmgTags>();

                    List<string> museumskategorien = new List<string>();
                    var mymuseumscategoriesstrings = mymuseum
                        .PoiProperty["de"]
                        .Where(x => x.Name == "categories")
                        .Select(x => x.Value)
                        .ToList();
                    foreach (var mymuseumscategoriesstring in mymuseumscategoriesstrings)
                    {
                        var splittedlist = mymuseumscategoriesstring?.Split(',').ToList() ?? new();

                        foreach (var splitted in splittedlist)
                        {
                            if (!String.IsNullOrEmpty(splitted))
                            {
                                museumskategorien.Add(splitted.Trim());

                                var mykategoriequery = await ODHTagHelper.GeODHTagByID(
                                    QueryFactory,
                                    "Museen " + splitted.Trim()
                                );
                                if (mykategoriequery is { })
                                    mysmgpoipoitype.Add(mykategoriequery);
                            }
                        }
                    }

                    mymuseum.Type = mysmgmaintype?.Shortname;
                    mymuseum.SubType = mysmgsubtype?.Shortname;

                    mymuseum.SmgTags ??= new List<string>();
                    if (
                        mysmgmaintype?.Id is { }
                        && !mymuseum.SmgTags.Contains(mysmgmaintype.Id.ToLower())
                    )
                        mymuseum.SmgTags.Add(mysmgmaintype.Id.ToLower());
                    if (
                        mysmgsubtype?.Id is { }
                        && !mymuseum.SmgTags.Contains(mysmgsubtype.Id.ToLower())
                    )
                        mymuseum.SmgTags.Add(mysmgsubtype.Id.ToLower());
                    if (mysmgpoipoitype.Count > 0)
                    {
                        foreach (var mysmgpoitypel in mysmgpoipoitype)
                        {
                            if (
                                mysmgpoitypel.Id is { }
                                && !mymuseum.SmgTags.Contains(mysmgpoitypel.Id.ToLower())
                            )
                                mymuseum.SmgTags.Add(mysmgpoitypel.Id.ToLower());
                        }
                    }

                    if (mysmgpoipoitype.Count > 0)
                        mymuseum.PoiType = mysmgpoipoitype.FirstOrDefault()?.Shortname;
                    else
                        mymuseum.PoiType = "";

                    foreach (var langcat in languagelistcategories)
                    {
                        AdditionalPoiInfos additional = new AdditionalPoiInfos();
                        additional.Language = langcat;
                        additional.MainType = mysmgmaintype?.TagName[langcat];
                        additional.SubType = mysmgsubtype?.TagName[langcat];
                        additional.PoiType =
                            mysmgpoipoitype.Count > 0
                                ? mysmgpoipoitype.FirstOrDefault()?.TagName[langcat]
                                : "";
                        mymuseum.AdditionalPoiInfos.TryAddOrUpdate(langcat, additional);
                    }

                    //Setting Categorization by Valid Tags
                    var currentcategories = validtagsforcategories.Where(x =>
                        mymuseum.SmgTags.Contains(x.Id.ToLower())
                    );
                    foreach (var smgtagtotranslate in currentcategories)
                    {
                        foreach (var languagecategory in languagelistcategories)
                        {
                            if (mymuseum.AdditionalPoiInfos[languagecategory].Categories == null)
                                mymuseum.AdditionalPoiInfos[languagecategory].Categories =
                                    new List<string>();

                            if (
                                smgtagtotranslate.TagName.ContainsKey(languagecategory)
                                && (
                                    !mymuseum
                                        .AdditionalPoiInfos[languagecategory]
                                        .Categories?.Contains(
                                            smgtagtotranslate.TagName[languagecategory].Trim()
                                        ) ?? false
                                )
                            )
                                mymuseum
                                    .AdditionalPoiInfos[languagecategory]
                                    .Categories?.Add(
                                        smgtagtotranslate.TagName[languagecategory].Trim()
                                    );
                        }
                    }
                }

                //Setting Common Infos
                mymuseum.Source = "siag";
                mymuseum.SyncSourceInterface = "museumdata";
                mymuseum.SyncUpdateMode = "Full";
                mymuseum.LastChange = DateTime.Now;

                //ADD MAPPING
                var mappingid = new Dictionary<string, string>() { { "museId", museumid } };
                mymuseum.Mapping.TryAddOrUpdate("siag", mappingid);

                //Calculate GPS Distance to District and Municipality
                if (mymuseum.LocationInfo != null)
                {
                    if (mymuseum.LocationInfo.DistrictInfo != null)
                    {
                        var districtreduced = districtreducedlist
                            .Where(x => x.Id == mymuseum.LocationInfo.DistrictInfo.Id)
                            .FirstOrDefault();
                        if (districtreduced != null)
                        {
                            mymuseum.ExtendGpsInfoToDistanceCalculationList(
                                "district",
                                districtreduced.Latitude,
                                districtreduced.Longitude
                            );
                        }
                    }
                    if (mymuseum.LocationInfo.MunicipalityInfo != null)
                    {
                        var municipalityreduced = municipalityreducedlist
                            .Where(x => x.Id == mymuseum.LocationInfo.MunicipalityInfo.Id)
                            .FirstOrDefault();
                        if (municipalityreduced != null)
                        {
                            mymuseum.ExtendGpsInfoToDistanceCalculationList(
                                "municipality",
                                municipalityreduced.Latitude,
                                municipalityreduced.Longitude
                            );
                        }
                    }
                }

                //Set Main Type as Poi
                ODHActivityPoiHelper.SetMainCategorizationForODHActivityPoi(mymuseum);

                //Set Tags based on OdhTags
                await GenericTaggingHelper.AddTagsToODHActivityPoi(
                    mymuseum,
                    settings.JsonConfig.Jsondir
                );
                mymuseum.TagIds =
                    mymuseum.Tags != null ? mymuseum.Tags.Select(x => x.Id).ToList() : null;

                if (mymuseumdata?.Root is { })
                {
                    var result = await InsertDataToDB(
                        mymuseum,
                        new KeyValuePair<string, XElement>(museumid, mymuseumdata.Root)
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
                    .SelectRaw("data->>'CustomId'")
                    .Where("gen_syncsourceinterface", "museumdata");

                var mymuseumsonraven = await mymuseumquery.GetAsync<string>();

                var idstodelete = mymuseumsonraven.Where(p => !mymuseumroot.Any(p2 => p2 == p));

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
            KeyValuePair<string, XElement> siagmuseumdata
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
            odhactivitypoi.CreatePublishedOnList();

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

        //Assign ODHTags and preserve old Tags

        //Assign Tags

        //Assign Categorization

        //Add Additional Properties (SiagMuseumDataProperties)

        #endregion
    }
}
