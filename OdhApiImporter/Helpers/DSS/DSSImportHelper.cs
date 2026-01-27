// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DSS;
using DSS.Parser;
using Helper;
using Helper.Generic;
using Helper.Location;
using Helper.Tagging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlKata.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers.DSS
{
    public class DSSImportHelper : ImportHelper, IImportHelper
    {
        public DSSImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL)
        {
            requesttypelist = new();
            rawonly = true;
            idlistdssinterface = new();
            entitytype = "";
        }

        public List<DSSRequestType> requesttypelist { get; set; }
        public string entitytype { get; set; }
        public bool rawonly { get; set; }

        public List<string> idlistdssinterface { get; set; }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //GET DATA
            var dssdata = await GetData(cancellationToken);

            //Import Data to rawtable and odh TODO ADD THE Other types
            var updateresult = await ImportData(dssdata, cancellationToken);

            //Disable Data no
            var deleteresult = await SetDataNotinListToInactive(cancellationToken);

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }

        //Imports DSS Data
        private async Task<List<dynamic>> GetData(CancellationToken cancellationToken)
        {
            List<dynamic> dssdata = new List<dynamic>();

            var requesttype = DSSImportUtil.GetRequestTypeList(entitytype);
            rawonly = requesttype.Item2;

            //Get DSS data
            dssdata.Add(
                await GetDSSData.GetDSSDataAsync(
                    requesttype.Item1,
                    settings.DSSConfig.User,
                    settings.DSSConfig.Password,
                    settings.DSSConfig.ServiceUrl
                )
            );

            return dssdata;
        }

        public async Task<UpdateDetail> ImportData(
            List<dynamic> dssinput,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            List<string> idlistdssinterface = new List<string>();

            if (dssinput != null && dssinput.Count > 0)
            {
                string lastupdatestr = dssinput[0].lastUpdate;
                //interface lastupdate
                DateTime.TryParseExact(
                    lastupdatestr,
                    "dd.MM.yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime lastupdate
                );

                var areaquery = QueryFactory.Query().SelectRaw("data").From("areas");

                // Get all Areas
                var arealist = await areaquery.GetObjectListAsync<AreaLinked>();

                var validforentity = new List<string>();

                if (entitytype.ToLower() == "lift")
                {
                    validforentity.Add("anderes");
                }
                else if (entitytype.ToLower() == "slope")
                {
                    validforentity.Add("winter");
                }

                var validcategories = new List<ODHTagLinked>();

                // Get all valid categories

                var subcategories = await QueryFactory
                    .Query()
                    .SelectRaw("data")
                    .From("smgtags")
                    .ODHTagValidForEntityFilter(validforentity)
                    .ODHTagMainEntityFilter(new List<string>() { "smgpoi" })
                    .GetObjectListAsync<ODHTagLinked>();

                //Temporary get winter/anderes tag and add it to validcategories
                var maincategories = await QueryFactory
                    .Query()
                    .SelectRaw("data")
                    .From("smgtags")
                    .IdIlikeFilter(new List<string>() { "winter", "anderes" })
                    .GetObjectListAsync<ODHTagLinked>();

                validcategories.AddRange(maincategories);
                validcategories.AddRange(subcategories);

                var jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                settings.JsonConfig.Jsondir,
                new List<string>()
                    {
                        "GenericTags",
                    }
                );

                //loop trough dss items
                foreach (var item in dssinput[0].items)
                {
                    var importresult = await ImportDataSingle(item, validcategories, arealist, jsondata);

                    newcounter = newcounter + importresult.created ?? newcounter;
                    updatecounter = updatecounter + importresult.updated ?? updatecounter;
                    errorcounter = errorcounter + importresult.error ?? errorcounter;
                }
            }

            return new UpdateDetail()
            {
                created = newcounter,
                updated = updatecounter,
                deleted = deletecounter,
                error = errorcounter,
            };
        }

        public async Task<UpdateDetail> ImportDataSingle(
            dynamic item,
            List<ODHTagLinked>? validcategories,
            IEnumerable<AreaLinked> arealist,
            IDictionary<string, JArray>? jsonfiles
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            //id
            string returnid = "";

            try
            {
                ODHActivityPoiLinked parsedobject = default(ODHActivityPoiLinked);

                returnid = item.pid;

                if (!rawonly)
                {
                    //Parse DSS Data
                    parsedobject = await ParseDSSDataToODHActivityPoi(item);
                    if (parsedobject == null)
                        throw new Exception();

                    //Add to list
                    idlistdssinterface.Add(parsedobject.Id);

                    var getlocationfromarea = true;

                    //Add the LocationInfo
                    //TODO if Area can be mapped return locationinfo
                    if (parsedobject.GpsInfo != null && parsedobject.GpsInfo.Count > 0)
                    {
                        if (
                            parsedobject.GpsInfo.FirstOrDefault()?.Latitude != 0
                            && parsedobject.GpsInfo.FirstOrDefault()?.Longitude != 0
                        )
                        {
                            var district = await LocationInfoHelper.GetNearestDistrictbyGPS(
                                QueryFactory,
                                parsedobject.GpsInfo.FirstOrDefault()!.Latitude.GetValueOrDefault(0),
                                parsedobject.GpsInfo.FirstOrDefault()!.Longitude.GetValueOrDefault(0),
                                10000
                            );

                            if (district != null)
                            {
                                var locinfo = await LocationInfoHelper.GetTheLocationInfoDistrict(
                                    QueryFactory,
                                    district.Id
                                );

                                parsedobject.LocationInfo = locinfo;
                                parsedobject.TourismorganizationId = locinfo.TvInfo?.Id;

                                getlocationfromarea = false;
                            }
                            else
                            {
                                parsedobject.LocationInfo = new LocationInfoLinked();
                            }
                        }
                    }

                    //Add AreaInfo from DSS skiarea
                    var dssskiarearid = (int?)item["skiresort"]["pid"];
                    if (dssskiarearid != null)
                    {
                        //TODO Select Area which has the mapping to dss/rid and fill AreaId Array and LocationInfo.Area
                        var area = arealist
                            .Where(x =>
                                x.Mapping.ContainsKey("dss")
                                && x.Mapping["dss"].ContainsKey("pid")
                                && x.Mapping["dss"]["pid"] == dssskiarearid.ToString()
                            )
                            .FirstOrDefault();

                        if (area?.Id != null)
                        {
                            parsedobject.AreaId = new HashSet<string>() { area.Id };
                            if (parsedobject.LocationInfo == null)
                                parsedobject.LocationInfo = new LocationInfoLinked();

                            Dictionary<string, string> areanames = new Dictionary<string, string>();
                            if (area.Shortname is { })
                            {
                                areanames.Add("de", area.Shortname);
                                areanames.Add("it", area.Shortname);
                                areanames.Add("en", area.Shortname);
                                areanames.Add("nl", area.Shortname);
                                areanames.Add("cs", area.Shortname);
                                areanames.Add("pl", area.Shortname);
                                areanames.Add("fr", area.Shortname);
                                areanames.Add("ru", area.Shortname);
                            }

                            parsedobject.LocationInfo.AreaInfo = new AreaInfoLinked()
                            {
                                Id = area.Id,
                                Name = areanames,
                            };

                            //Use RegionId, TVId from Area
                            if (parsedobject.LocationInfo.RegionInfo == null || getlocationfromarea)
                                if (!String.IsNullOrEmpty(area.RegionId))
                                {
                                    var region = await QueryFactory
                                        .Query("regions")
                                        .Select("data")
                                        .Where("id", area.RegionId.ToUpper())
                                        .GetObjectSingleAsync<Region>();

                                    if (region != null)
                                    {
                                        parsedobject.LocationInfo.RegionInfo =
                                            new RegionInfoLinked()
                                            {
                                                Id = region.Id,
                                                Name = (
                                                    from x in region?.Detail
                                                    select x
                                                ).ToDictionary(x => x.Key, x => x.Value.Title),
                                            };
                                    }
                                }

                            if (parsedobject.LocationInfo.TvInfo == null || getlocationfromarea)
                                if (!String.IsNullOrEmpty(area.TourismvereinId))
                                {
                                    var tv = await QueryFactory
                                        .Query("tvs")
                                        .Select("data")
                                        .Where("id", area.TourismvereinId.ToUpper())
                                        .GetObjectSingleAsync<Tourismverein>();

                                    if (tv != null)
                                    {
                                        parsedobject.LocationInfo.TvInfo = new TvInfoLinked()
                                        {
                                            Id = tv.Id,
                                            Name = (from x in tv?.Detail select x).ToDictionary(
                                                x => x.Key,
                                                x => x.Value.Title
                                            ),
                                        };
                                        parsedobject.TourismorganizationId = area.TourismvereinId;
                                    }
                                }

                            if (
                                parsedobject.LocationInfo.MunicipalityInfo == null
                                || getlocationfromarea
                            )
                                if (!String.IsNullOrEmpty(area.MunicipalityId))
                                {
                                    var mun = await QueryFactory
                                        .Query("municipality")
                                        .Select("data")
                                        .Where("id", area.MunicipalityId.ToUpper())
                                        .GetObjectSingleAsync<Municipality>();

                                    if (mun != null)
                                    {
                                        parsedobject.LocationInfo.MunicipalityInfo =
                                            new MunicipalityInfoLinked()
                                            {
                                                Id = mun.Id,
                                                Name = (
                                                    from x in mun?.Detail
                                                    select x
                                                ).ToDictionary(x => x.Key, x => x.Value.Title),
                                            };
                                    }
                                }
                        }
                    }

                    //Setting Categorization by Valid Tags
                    var currentcategories = validcategories.Where(x =>
                        parsedobject.SmgTags?.Select(y => y.ToLower()).Contains(x.Id.ToLower())
                        ?? false
                    );

                    foreach (var languagecategory in parsedobject.HasLanguage ?? new List<string>())
                    {
                        if (parsedobject.AdditionalPoiInfos == null)
                            parsedobject.AdditionalPoiInfos =
                                new Dictionary<string, AdditionalPoiInfos>();

                        //Set MainType, SubType, PoiType
                        var additionalpoiinfo = new AdditionalPoiInfos();
                        additionalpoiinfo.Language = languagecategory;

                        //var maintypeobj = validcategories
                        //    .Where(x => x.Id == parsedobject.Type?.ToLower())
                        //    .FirstOrDefault();
                        //var subtypeobj = validcategories
                        //    .Where(x => x.Id == parsedobject.SubType?.ToLower())
                        //    .FirstOrDefault();

                        additionalpoiinfo.MainType = null;
                        //maintypeobj != null
                        //&& maintypeobj.TagName != null
                        //&& maintypeobj.TagName.ContainsKey(languagecategory)
                        //    ? maintypeobj.TagName[languagecategory]
                        //    : "";
                        additionalpoiinfo.SubType = null;
                            //subtypeobj != null
                            //&& subtypeobj.TagName != null
                            //&& subtypeobj.TagName.ContainsKey(languagecategory)
                            //    ? subtypeobj.TagName[languagecategory]
                            //    : "";

                        //Add the AdditionalPoi Info (include Novelty)
                        if (entitytype.ToLower() == "lift")
                            additionalpoiinfo.Novelty = (string)item["info-text"][languagecategory];
                        if (entitytype.ToLower() == "slope")
                            additionalpoiinfo.Novelty = (string)
                                item["info-text-winter"][languagecategory];

                        foreach (
                            var smgtagtotranslate in currentcategories.Where(x =>
                                x.DisplayAsCategory == true
                            )
                        )
                        {
                            if (additionalpoiinfo.Categories == null)
                                additionalpoiinfo.Categories = new List<string>();

                            if (
                                smgtagtotranslate.TagName.ContainsKey(languagecategory)
                                && !additionalpoiinfo.Categories.Contains(
                                    smgtagtotranslate.TagName[languagecategory].Trim()
                                )
                            )
                                additionalpoiinfo.Categories.Add(
                                    smgtagtotranslate.TagName[languagecategory].Trim()
                                );
                        }

                        parsedobject.AdditionalPoiInfos.TryAddOrUpdate(
                            languagecategory,
                            additionalpoiinfo
                        );
                    }

                    //Set shortname
                    if (!String.IsNullOrEmpty(parsedobject.Detail["de"].Title))
                        parsedobject.Shortname = parsedobject.Detail["de"].Title;
                    else if (!String.IsNullOrEmpty(parsedobject.Detail["it"].Title))
                        parsedobject.Shortname = parsedobject.Detail["it"].Title;
                    else if (!String.IsNullOrEmpty(parsedobject.Detail["en"].Title))
                        parsedobject.Shortname = parsedobject.Detail["en"].Title;

                    ODHTagHelper.SetMainCategorizationForODHActivityPoi(parsedobject);

                    //Traduce Assigned TagIds

                    //await GenericTaggingHelper.AddTagsToODHActivityPoi(
                    //    parsedobject,
                    //    settings.JsonConfig.Jsondir
                    //);

                    //Traduce all Tags with Source IDM to english tags, CONSIDER TagId "poi" is added here
                    await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                        parsedobject,
                        jsonfiles != null && jsonfiles["GenericTags"] != null ? jsonfiles["GenericTags"].ToObject<List<TagLinked>>() : null
                    );

                    //Create Tag Object
                    //Create Tags and preserve the old TagEntries
                    await parsedobject.UpdateTagsExtension(QueryFactory);
                }

                var sourceid = (string)DSSImportUtil.GetSourceId(item, entitytype);

                //TODO GET ID based on item type

                //IF no id is provided timestamp generated WRONG i need a unique identifier to group on!
                if (String.IsNullOrEmpty(sourceid))
                    sourceid = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                //Save parsedobject to DB + Save Rawdata to DB
                var pgcrudresult = await InsertDataToDB(
                    parsedobject,
                    new KeyValuePair<string, dynamic>(sourceid, item)
                );

                newcounter = newcounter + pgcrudresult.created ?? 0;
                updatecounter = updatecounter + pgcrudresult.updated ?? 0;

                WriteLog.LogToConsole(
                    parsedobject?.Id ?? returnid,
                    "dataimport",
                    "single.dss" + entitytype,
                    new ImportLog()
                    {
                        sourceid = parsedobject?.Id ?? returnid,
                        sourceinterface = "dss." + entitytype,
                        success = true,
                        error = "",
                    }
                );
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    returnid,
                    "dataimport",
                    "single.dss." + entitytype,
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "dss." + entitytype,
                        success = false,
                        error = entitytype + " could not be parsed: " + ex.Message,
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
            CancellationToken cancellationToken
        )
        {
            int updateresult = 0;
            int deleteresult = 0;
            int errorresult = 0;

            try
            {
                //Begin SetDataNotinListToInactive
                var idlistdb = await GetAllDSSDataByInterface(
                    new List<string>() { "dss" + entitytype.ToLower() + "base" }
                );

                var idstodelete = idlistdb.Where(p => !idlistdssinterface.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var deletedisableresult = await DeleteOrDisableData<ODHActivityPoiLinked>(
                        idtodelete,
                        false
                    );

                    if (deletedisableresult.Item1 > 0)
                        WriteLog.LogToConsole(
                            idtodelete,
                            "dataimport",
                            "deactivate.dss" + entitytype,
                            new ImportLog()
                            {
                                sourceid = idtodelete,
                                sourceinterface = "dss." + entitytype,
                                success = true,
                                error = "",
                            }
                        );
                    else if (deletedisableresult.Item2 > 0)
                        WriteLog.LogToConsole(
                            idtodelete,
                            "dataimport",
                            "deactivate.dss" + entitytype,
                            new ImportLog()
                            {
                                sourceid = idtodelete,
                                sourceinterface = "dss." + entitytype,
                                success = true,
                                error = "",
                            }
                        );

                    deleteresult =
                        deleteresult + deletedisableresult.Item1 + deletedisableresult.Item2;
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.dss." + entitytype,
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "dss." + entitytype,
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

        //Parse the dss interface content
        public async Task<ODHActivityPoiLinked?> ParseDSSDataToODHActivityPoi(dynamic dssinput)
        {
            //id
            string odhdssid = "dss_" + dssinput.pid;

            //Get the ODH Item
            var mydssquery = QueryFactory.Query(table).Select("data").Where("id", odhdssid);

            var odhactivitypoiindb = await mydssquery.GetObjectSingleAsync<ODHActivityPoiLinked>();
            var odhactivitypoi = default(ODHActivityPoiLinked);

            if (entitytype.ToLower() == "lift")
            {
                odhactivitypoi = ParseDSSToODH.ParseDSSLiftDataToODHActivityPoi(
                    odhactivitypoiindb,
                    dssinput
                );
            }
            else if (entitytype.ToLower() == "slope")
            {
                odhactivitypoi = ParseDSSToODH.ParseDSSSlopeDataToODHActivityPoi(
                    odhactivitypoiindb,
                    dssinput
                );
            }

            //TODOS all of this stuff, Tags, Categories etc....

            return odhactivitypoi;
        }

        private async Task<UpdateDetail> InsertDataToDB(
            ODHActivityPoiLinked odhactivitypoi,
            KeyValuePair<string, dynamic> dssdata
        )
        {
            var rawdataid = await InsertInRawDataDB(dssdata);

            if (!rawonly)
            {
                odhactivitypoi.Id = odhactivitypoi.Id?.ToLower();

                //Set LicenseInfo
                odhactivitypoi.LicenseInfo =
                    Helper.LicenseHelper.GetLicenseInfoobject<ODHActivityPoi>(
                        odhactivitypoi,
                        Helper.LicenseHelper.GetLicenseforOdhActivityPoi
                    );

                var pgcrudresult = await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                    odhactivitypoi,
                    new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("dss." + entitytype + ".import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid
                );

                //Publishedon Helper?


                //Hack insert also in Activity table
                //await InsertInLegacyActivityTable(odhactivitypoi);

                return pgcrudresult;
            }
            else
                return new UpdateDetail
                {
                    created = 1,
                    deleted = 0,
                    updated = 0,
                    error = 0,
                    operation = "RAW_INSERT",
                    id = rawdataid.ToString(),
                };
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, dynamic> dssdata)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "dss",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(dssdata.Value),
                    sourceinterface = entitytype,
                    sourceid = dssdata.Key,
                    sourceurl = settings.DSSConfig.ServiceUrl,
                    type = "odhactivitypoi." + entitytype.ToLower(),
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private async Task<List<string>> GetAllDSSDataByInterface(
            List<string> syncsourceinterfacelist
        )
        {
            var query = QueryFactory
                .Query(table)
                .Select("id")
                .SyncSourceInterfaceFilter_GeneratedColumn(syncsourceinterfacelist);

            var idlist = await query.GetAsync<string>();

            return idlist.ToList();
        }

        #region ActivityHackDestinationdata

        ////TODO before each import clear activity table data

        //private async Task<PGCRUDResult> InsertInLegacyActivityTable(
        //    ODHActivityPoiLinked odhactivitypoi
        //)
        //{
        //    //Transform to LTSActivityLinked
        //    var activity = TransformODHActivityPoiToActivity(odhactivitypoi);

        //    //Insert in Table
        //    var pgcrudresult = await QueryFactory.UpsertData<LTSActivityLinked>(
        //        activity,
        //        new DataInfo("activities", CRUDOperation.Update),
        //        new EditInfo("dss." + entitytype + ".import", importerURL),
        //        new CRUDConstraints(),
        //        new CompareConfig(false, false)
        //    );

        //    return pgcrudresult;
        //}

        //private LTSActivityLinked TransformODHActivityPoiToActivity(
        //    ODHActivityPoiLinked odhactivitypoi
        //)
        //{
        //    //TODO Transform class
        //    LTSActivityLinked myactivity = CastODHActivityTOLTSActivity(odhactivitypoi);

        //    //Update Categorization
        //    if (odhactivitypoi.SyncSourceInterface == "dssliftbase")
        //    {
        //        myactivity.Type = "Aufstiegsanlagen";

        //        var types = GetSubTypeAndPoiTypeFromFlagDescription(
        //            myactivity.SmgTags?.ToList() ?? new()
        //        );

        //        myactivity.SubType = types.Item1;
        //        myactivity.PoiType = types.Item2;

        //        myactivity.AdditionalPoiInfos.TryAddOrUpdate(
        //            "de",
        //            new AdditionalPoiInfos()
        //            {
        //                Language = "de",
        //                MainType = "Aufstiegsanlagen",
        //                SubType = "",
        //            }
        //        );
        //        myactivity.AdditionalPoiInfos.TryAddOrUpdate(
        //            "it",
        //            new AdditionalPoiInfos()
        //            {
        //                Language = "it",
        //                MainType = "lifts",
        //                SubType = "",
        //            }
        //        );
        //        myactivity.AdditionalPoiInfos.TryAddOrUpdate(
        //            "en",
        //            new AdditionalPoiInfos()
        //            {
        //                Language = "en",
        //                MainType = "ascensioni",
        //                SubType = "",
        //            }
        //        );
        //    }
        //    else if (odhactivitypoi.SyncSourceInterface == "dssslopebase")
        //    {
        //        myactivity.Type = "Piste";
        //        myactivity.SubType = "Ski Alpin";

        //        if (myactivity.Difficulty == "2")
        //            myactivity.PoiType = "blau";
        //        if (myactivity.Difficulty == "4")
        //            myactivity.PoiType = "rot";
        //        if (myactivity.Difficulty == "6")
        //            myactivity.PoiType = "schwarz";

        //        myactivity.AdditionalPoiInfos.TryAddOrUpdate(
        //            "de",
        //            new AdditionalPoiInfos()
        //            {
        //                Language = "de",
        //                MainType = "Ski alpin",
        //                SubType = "Piste",
        //            }
        //        );
        //        myactivity.AdditionalPoiInfos.TryAddOrUpdate(
        //            "it",
        //            new AdditionalPoiInfos()
        //            {
        //                Language = "it",
        //                MainType = "Sci alpino",
        //                SubType = "piste",
        //            }
        //        );
        //        myactivity.AdditionalPoiInfos.TryAddOrUpdate(
        //            "en",
        //            new AdditionalPoiInfos()
        //            {
        //                Language = "en",
        //                MainType = "Ski alpin",
        //                SubType = "slopes",
        //            }
        //        );
        //    }

        //    //Type to Tag
        //    if (
        //        !String.IsNullOrEmpty(myactivity.Type)
        //        && (!myactivity.SmgTags?.Contains(myactivity.Type.ToLower()) ?? false)
        //    )
        //        myactivity.SmgTags?.Add(myactivity.Type.ToLower());
        //    if (
        //        !String.IsNullOrEmpty(myactivity.SubType)
        //        && (!myactivity.SmgTags?.Contains(myactivity.SubType.ToLower()) ?? false)
        //    )
        //        myactivity.SmgTags?.Add(myactivity.SubType.ToLower());
        //    if (
        //        !String.IsNullOrEmpty(myactivity.PoiType)
        //        && (!myactivity.SmgTags?.Contains(myactivity.PoiType.ToLower()) ?? false)
        //    )
        //        myactivity.SmgTags?.Add(myactivity.PoiType.ToLower());

        //    if (myactivity.SmgTags?.Contains("anderes") ?? false)
        //        myactivity.SmgTags?.Remove("anderes");
        //    if (myactivity.SmgTags?.Contains("winter") ?? false)
        //        myactivity.SmgTags?.Remove("winter");
        //    if (myactivity.SmgTags?.Contains("skirundtouren pisten") ?? false)
        //        myactivity.SmgTags?.Remove("skirundtouren pisten");
        //    if (myactivity.SmgTags?.Contains("activity") ?? false)
        //        myactivity.SmgTags?.Remove("activity");
        //    if (myactivity.SmgTags?.Contains("poi") ?? false)
        //        myactivity.SmgTags?.Remove("poi");

        //    //Update GPS points position/valleystation/mountainstation
        //    if (odhactivitypoi.GpsInfo != null)
        //    {
        //        foreach (var gpsinfo in odhactivitypoi.GpsInfo)
        //        {
        //            var gpsresult = ReturnGpsInfoActivityKey(gpsinfo);

        //            myactivity.GpsInfo?.Add(gpsresult.Item2);
        //            myactivity.GpsPoints.TryAddOrUpdate(gpsresult.Item1, gpsresult.Item2);
        //        }
        //    }

        //    return myactivity;
        //}

        //private Tuple<string?, string?> GetSubTypeAndPoiTypeFromFlagDescription(
        //    List<string> odhtags
        //)
        //{
        //    List<string> validtags = new List<string>();

        //    List<string> dsslifttypes = new List<string>()
        //    {
        //        "Seilbahn",
        //        "Kabinenbahn",
        //        "Unterirdische Bahn",
        //        "Sessellift",
        //        "Sessellift",
        //        "Skilift",
        //        "Schrägaufzug",
        //        "Klein-Skilift",
        //        "Telemix",
        //        "Standseilbahn/Zahnradbahn",
        //        "Skibus",
        //        "Zug",
        //        "Sessellift",
        //        "Sessellift",
        //        "Sessellift",
        //        "Förderband",
        //        "4er Sessellift kuppelbar",
        //        "6er Sessellift kuppelbar",
        //        "8er Sessellift kuppelbar",
        //    };

        //    foreach (var odhtag in odhtags)
        //    {
        //        if (
        //            odhtag != "activity"
        //            && odhtag != "poi"
        //            && odhtag != "anderes"
        //            && odhtag != "aufstiegsanlagen"
        //            && odhtag != "weitere aufstiegsanlagen"
        //            && odhtag != "winter"
        //            && odhtag != "skirundtouren pisten"
        //            && odhtag != "pisten"
        //            && odhtag != "ski alpin"
        //            && odhtag != "piste"
        //            && odhtag != "weitere pisten"
        //        )
        //        {
        //            validtags.Add(odhtag);
        //        }
        //    }

        //    string? subtype = null;
        //    string? poitype = null;

        //    if (validtags.Count > 0)
        //    {
        //        subtype = dsslifttypes.Where(x => x.ToLower() == validtags[0]).FirstOrDefault();

        //        if (validtags.Count > 1)
        //        {
        //            poitype = dsslifttypes.Where(x => x.ToLower() == validtags[1]).FirstOrDefault();
        //        }
        //    }

        //    return Tuple.Create(subtype, poitype);
        //}

        //private LTSActivityLinked CastODHActivityTOLTSActivity(ODHActivityPoiLinked odhactivitypoi)
        //{
        //    var myactivity = new LTSActivityLinked();

        //    myactivity.Active = odhactivitypoi.Active;
        //    //myactivity.AdditionalPoiInfos = odhactivitypoi.AdditionalPoiInfos;
        //    myactivity.AltitudeDifference = odhactivitypoi.AltitudeDifference;
        //    myactivity.AltitudeHighestPoint = odhactivitypoi.AltitudeHighestPoint;
        //    myactivity.AltitudeLowestPoint = odhactivitypoi.AltitudeLowestPoint;
        //    myactivity.AltitudeSumDown = odhactivitypoi.AltitudeSumDown;
        //    myactivity.AltitudeSumUp = odhactivitypoi.AltitudeSumUp;
        //    myactivity.AreaId = odhactivitypoi.AreaId;
        //    myactivity.ContactInfos = odhactivitypoi.ContactInfos;
        //    myactivity.CopyrightChecked = odhactivitypoi.CopyrightChecked;
        //    myactivity.BikeTransport = odhactivitypoi.BikeTransport;
        //    myactivity.ChildPoiIds = odhactivitypoi.ChildPoiIds;
        //    myactivity.Detail = odhactivitypoi.Detail;
        //    myactivity.Difficulty = odhactivitypoi.Difficulty;
        //    myactivity.DistanceDuration = odhactivitypoi.DistanceDuration;
        //    myactivity.DistanceInfo = odhactivitypoi.DistanceInfo;
        //    myactivity.DistanceLength = odhactivitypoi.DistanceLength;
        //    myactivity.Exposition = odhactivitypoi.Exposition;
        //    myactivity.FeetClimb = odhactivitypoi.FeetClimb;
        //    myactivity.FirstImport = odhactivitypoi.FirstImport;
        //    myactivity.GpsInfo = new List<GpsInfo>();
        //    //myactivity.GpsPoints = odhactivitypoi.GpsPoints;
        //    myactivity.GpsTrack = odhactivitypoi.GpsTrack;
        //    myactivity.HasFreeEntrance = odhactivitypoi.HasFreeEntrance;
        //    myactivity.HasLanguage = odhactivitypoi.HasLanguage;
        //    myactivity.HasRentals = odhactivitypoi.HasRentals;
        //    myactivity.Highlight = odhactivitypoi.Highlight;
        //    myactivity.Id = odhactivitypoi.Id?.ToUpper();
        //    myactivity.ImageGallery = odhactivitypoi.ImageGallery;
        //    myactivity.IsOpen = odhactivitypoi.IsOpen;
        //    myactivity.IsPrepared = odhactivitypoi.IsPrepared;
        //    myactivity.IsWithLigth = odhactivitypoi.IsWithLigth;
        //    myactivity.LastChange = odhactivitypoi.LastChange;
        //    myactivity.LiftAvailable = odhactivitypoi.LiftAvailable;
        //    myactivity.LicenseInfo = odhactivitypoi.LicenseInfo;
        //    myactivity.LocationInfo = odhactivitypoi.LocationInfo;
        //    myactivity.LTSTags = odhactivitypoi.LTSTags;
        //    myactivity.Mapping = odhactivitypoi.Mapping;
        //    myactivity.MasterPoiIds = odhactivitypoi.MasterPoiIds;
        //    myactivity.Number = odhactivitypoi.Number;
        //    myactivity.OperationSchedule = odhactivitypoi.OperationSchedule;
        //    myactivity.OutdooractiveElevationID = odhactivitypoi.OutdooractiveElevationID;
        //    myactivity.OutdooractiveID = odhactivitypoi.OutdooractiveID;
        //    myactivity.OwnerRid = odhactivitypoi.OwnerRid;
        //    myactivity.PublishedOn = odhactivitypoi.PublishedOn;
        //    myactivity.Ratings = odhactivitypoi.Ratings;
        //    myactivity.RunToValley = odhactivitypoi.RunToValley;
        //    myactivity.Shortname = odhactivitypoi.Shortname;
        //    myactivity.SmgActive = odhactivitypoi.SmgActive;
        //    myactivity.SmgId = odhactivitypoi.SmgId;
        //    myactivity.SmgTags = odhactivitypoi.SmgTags;
        //    myactivity.Source = odhactivitypoi.Source;
        //    myactivity.TourismorganizationId = odhactivitypoi.TourismorganizationId;
        //    myactivity.WayNumber = odhactivitypoi.WayNumber;

        //    return myactivity;
        //}

        //private Tuple<string, GpsInfo> ReturnGpsInfoActivityKey(GpsInfo gpsinfo)
        //{
        //    string activitygpstypevalue = "";
        //    string gpstypekey = "";

        //    switch (gpsinfo.Gpstype)
        //    {
        //        case "valleystationpoint":
        //            activitygpstypevalue = "Talstation";
        //            gpstypekey = "position";
        //            break;
        //        case "middlestationpoint":
        //            activitygpstypevalue = "Mittelstation";
        //            gpstypekey = "middleposition";
        //            break;
        //        case "mountainstationpoint":
        //            activitygpstypevalue = "Bergstation";
        //            gpstypekey = "endposition";
        //            break;
        //        case "position":
        //            activitygpstypevalue = "Startpunkt";
        //            gpstypekey = "position";
        //            break;
        //    }
        //    ;

        //    GpsInfo gpstoreturn = new GpsInfo()
        //    {
        //        Gpstype = activitygpstypevalue,
        //        Altitude = gpsinfo.Altitude,
        //        AltitudeUnitofMeasure = gpsinfo.AltitudeUnitofMeasure,
        //        Latitude = gpsinfo.Latitude,
        //        Longitude = gpsinfo.Longitude,
        //    };

        //    return Tuple.Create(gpstypekey, gpstoreturn);
        //}

        #endregion
    }
}
