// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Helper.Generic;
using LTSAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINJA.Parser;
using ServiceReferenceLCS;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiGastronomyFacilitiesImportHelper : ImportHelper, IImportHelper
    {
        public LTSApiGastronomyFacilitiesImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

        private async Task<List<JObject>> GetDataFromLTSV2()
        {
            try
            {
                LtsApi ltsapi = new LtsApi(
                    settings.LtsCredentials.serviceurl,
                    settings.LtsCredentials.username,
                    settings.LtsCredentials.password,
                    settings.LtsCredentials.ltsclientid,
                    false
                );
                var qs = new LTSQueryStrings() { page_size = 100 };
                var dict = ltsapi.GetLTSQSDictionary(qs);

                var ltsdata = await ltsapi.GastronomyFacilityRequest(dict, true);

                return ltsdata;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.gastronomies.facilities",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.gastronomies.facilities",
                        success = false,
                        error = ex.Message,
                    }
                );

                return null;
            }
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //Import the List
            var eventtags = await GetDataFromLTSV2();
            //Import Single Data & Deactivate Data
            var result = await SaveDataToPG(eventtags);

            return result;
        }

        private async Task<UpdateDetail> SaveDataToPG(List<JObject> ltsdata)
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSGastronomyFacilities> tagdata = new List<LTSGastronomyFacilities>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    tagdata.AddRange(
                        ltsdatasingle["data"].ToObject<IList<LTSGastronomyFacilities>>()
                    );
                }

                foreach (var data in tagdata)
                {
                    string id = data.rid;

                    //See if data exists
                    var query = QueryFactory.Query("tags").Select("data").Where("id", id);

                    var objecttosave = await query.GetObjectSingleAsync<TagLinked>();

                    if (objecttosave == null)
                        objecttosave = new TagLinked();

                    objecttosave.Id = data.rid;
                    objecttosave.Active = true;
                    objecttosave.DisplayAsCategory = false;
                    objecttosave.FirstImport =
                        objecttosave.FirstImport == null ? DateTime.Now : objecttosave.FirstImport;
                    objecttosave.LastChange = data.lastUpdate;

                    objecttosave.Source = "lts";
                    objecttosave.TagName = data.name;
                    objecttosave.Description = data.description;

                    objecttosave.MainEntity = "odhactivitypoi";
                    objecttosave.ValidForEntity = new List<string>()
                    {
                        "odhactivitypoi",
                        "gastronomy",
                    };
                    objecttosave.Shortname = objecttosave.TagName.ContainsKey("en")
                        ? objecttosave.TagName["en"]
                        : objecttosave.TagName.FirstOrDefault().Value;

                    string matchedtype = GetTypeFromFacilityGroup(data.group);

                    objecttosave.Types = new List<string>() { matchedtype, "gastronomyfacilities" };

                    objecttosave.IDMCategoryMapping = null;
                    objecttosave.PublishDataWithTagOn = null;
                    objecttosave.Mapping = new Dictionary<string, IDictionary<string, string>>()
                    {
                        {
                            "lts",
                            new Dictionary<string, string>()
                            {
                                { "rid", data.rid },
                                { "code", data.code },
                                { "group", data.group },
                            }
                        },
                    };
                    objecttosave.LTSTaggingInfo = null;
                    objecttosave.PublishedOn = null;

                    //Do not set this because we have mapped tag ids assigned
                    objecttosave.MappedTagIds = null;

                    var result = await InsertDataToDB(objecttosave, data);

                    newimportcounter = newimportcounter + result.created ?? 0;
                    updateimportcounter = updateimportcounter + result.updated ?? 0;
                    errorimportcounter = errorimportcounter + result.error ?? 0;

                    idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.gastronomies.facilities",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.gastronomies.facilities",
                            success = true,
                            error = "",
                        }
                    );
                }

                if (idlistlts.Count > 0)
                {
                    //Begin SetDataNotinListToInactive
                    var idlistdb = await GetAllDataBySourceAndType(
                        new List<string>() { "lts" },
                        new List<string>()
                        {
                            "gastronomyfacilities",
                            "facilitycodes_cuisinecodes",
                            "facilitycodes_creditcard",
                            "facilitycodes_qualityseals",
                            "facilitycodes_equipment",
                        }
                    );

                    var idstodelete = idlistdb.Where(p => !idlistlts.Any(p2 => p2 == p));

                    foreach (var idtodelete in idstodelete)
                    {
                        var deletedisableresult = await DeleteOrDisableData<TagLinked>(
                            idtodelete,
                            false
                        );

                        if (deletedisableresult.Item1 > 0)
                            WriteLog.LogToConsole(
                                idtodelete,
                                "dataimport",
                                "single.gastronomies.facilities.deactivate",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "lts.gastronomies.facilities",
                                    success = true,
                                    error = "",
                                }
                            );
                        else if (deletedisableresult.Item2 > 0)
                            WriteLog.LogToConsole(
                                idtodelete,
                                "dataimport",
                                "single.gastronomies.facilities.delete",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "lts.gastronomies.facilities",
                                    success = true,
                                    error = "",
                                }
                            );

                        deleteimportcounter =
                            deleteimportcounter
                            + deletedisableresult.Item1
                            + deletedisableresult.Item2;
                    }
                }
            }
            else
                errorimportcounter = 1;

            return new UpdateDetail()
            {
                updated = updateimportcounter,
                created = newimportcounter,
                deleted = deleteimportcounter,
                error = errorimportcounter,
            };
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            TagLinked objecttosave,
            LTSGastronomyFacilities data
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                    objecttosave,
                    Helper.LicenseHelper.GetLicenseforTag
                );

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave);

                //Set PublishedOn
                objecttosave.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(data);

                return await QueryFactory.UpsertData<TagLinked>(
                    objecttosave,
                    new DataInfo("tags", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("lts.gastronomies.facilities.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(LTSGastronomyFacilities data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(data),
                    sourceinterface = "gastronomies",
                    sourceid = data.rid,
                    sourceurl = "https://go.lts.it/api/v1/gastronomies/facilities",
                    type = "gastronomies.facilities",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        public static string GetTypeFromFacilityGroup(string group)
        {
            return group switch
            {
                "RS_Cuisine" => "facilitycodes_cuisinecodes",
                "RS_Equipment" => "facilitycodes_equipment",
                "RS_QualitySeals" => "facilitycodes_qualityseals",
                "RS_CreditCard" => "facilitycodes_creditcard",
                _ => "gastronomyfacilities",
            };
        }
    }

    public class LTSGastronomyFacilities
    {
        public string rid { get; set; }
        public DateTime lastUpdate { get; set; }
        public IDictionary<string, string> name { get; set; }
        public IDictionary<string, string> description { get; set; }

        //public bool isActive { get; set; }
        public string code { get; set; }
        public string group { get; set; }
    }
}
