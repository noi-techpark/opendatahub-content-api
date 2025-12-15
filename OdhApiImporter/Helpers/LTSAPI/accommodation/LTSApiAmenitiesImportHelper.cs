// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using LTSAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiAmenitiesImportHelper : ImportHelper, IImportHelper
    {
        public LTSApiAmenitiesImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        private async Task<List<JObject>> GetAccommodationAmenitiesFromLTSV2()
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

                var ltsdata = await ltsapi.AmenityListRequest(dict, true);

                return ltsdata;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.accommodations.amenities",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.accommodations.amenities",
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
            var eventtags = await GetAccommodationAmenitiesFromLTSV2();
            //Import Single Data & Deactivate Data
            var result = await SaveAccommodationAmenitiesToPG(eventtags);

            return result;
        }

        private async Task<UpdateDetail> SaveAccommodationAmenitiesToPG(List<JObject> ltsdata)
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();
                List<string> typelistlts = new List<string>();

                List<LTSAmenity> tagdata = new List<LTSAmenity>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    tagdata.AddRange(ltsdatasingle["data"].ToObject<IList<LTSAmenity>>());
                }

                //Saving the data into the folder
                ImportUtils.SaveDataAsJson<List<LTSAmenity>>(tagdata, "Features", "json/");

                foreach (var data in tagdata)
                {
                    string id = data.rid;

                    //See if data exists
                    var query = QueryFactory.Query("tags").Select("data").Where("id", id);

                    var objecttosave = await query.GetObjectSingleAsync<TagLinked>();

                    if (objecttosave == null)
                        objecttosave = new TagLinked();
                    else
                        Console.WriteLine("Amenities here");

                    //TODO Some Accommodation Amenities are also Gastronomy facilitycodes_equipment facilitycodes_cuisinecodes
                    //TODO this on all tag imports

                    objecttosave.Id = data.rid;
                    objecttosave.Active = true;
                    objecttosave.DisplayAsCategory = false;
                    objecttosave.FirstImport =
                        objecttosave.FirstImport == null ? DateTime.Now : objecttosave.FirstImport;
                    objecttosave.LastChange = data.lastUpdate;

                    objecttosave.Source = "lts";
                    objecttosave.TagName = data.name;
                    objecttosave.Description = data.description;

                    objecttosave.MainEntity = "accommodation";

                    if (objecttosave.ValidForEntity == null)
                        objecttosave.ValidForEntity = new List<string>() { "accommodation" };
                    else
                    {
                        if (!objecttosave.ValidForEntity.Contains("accommodation"))
                            objecttosave.ValidForEntity.Add("accommodation");
                    }

                    objecttosave.Shortname = objecttosave.TagName.ContainsKey("en")
                        ? objecttosave.TagName["en"]
                        : objecttosave.TagName.FirstOrDefault().Value;

                    if (objecttosave.Types == null)
                        objecttosave.Types = new List<string>() { "accommodation" + data.type };
                    else
                    {
                        if (!objecttosave.Types.Contains("accommodation" + data.type))
                            objecttosave.Types.Add("accommodation" + data.type);
                    }

                    if (!typelistlts.Contains("accommodation" + data.type))
                        typelistlts.Add("accommodation" + data.type);

                    //objecttosave.IDMCategoryMapping = null;
                    objecttosave.PublishDataWithTagOn = null;
                    if (objecttosave.Mapping == null || !objecttosave.Mapping.ContainsKey("lts"))
                        objecttosave.Mapping = new Dictionary<string, IDictionary<string, string>>()
                        {
                            {
                                "lts",
                                new Dictionary<string, string>()
                                {
                                    { "rid", data.rid },
                                    { "code", data.code },
                                    { "type", data.type },
                                }
                            },
                        };
                    else
                    {
                        objecttosave.Mapping["lts"].TryAddOrUpdate("rid", data.rid);
                        objecttosave.Mapping["lts"].TryAddOrUpdate("code", data.code);
                        objecttosave.Mapping["lts"].TryAddOrUpdate("type", data.type);
                    }

                    objecttosave.LTSTaggingInfo = null;
                    objecttosave.PublishedOn = null;

                    //Do not set this because we have mapped tag ids assigned
                    //objecttosave.MappedTagIds = null;


                    var result = await InsertDataToDB(objecttosave, data);

                    newimportcounter = newimportcounter + result.created ?? 0;
                    updateimportcounter = updateimportcounter + result.updated ?? 0;
                    errorimportcounter = errorimportcounter + result.error ?? 0;

                    idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.accommodations.amenities",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.accommodations.amenities",
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
                        typelistlts
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
                                "single.accommodations.amenities.deactivate",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "lts.accommodations.amenities",
                                    success = true,
                                    error = "",
                                }
                            );
                        else if (deletedisableresult.Item2 > 0)
                            WriteLog.LogToConsole(
                                idtodelete,
                                "dataimport",
                                "single.accommodations.amenities.delete",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "lts.accommodations.amenities",
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

        private async Task<PGCRUDResult> InsertDataToDB(TagLinked objecttosave, LTSAmenity data)
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
                    new EditInfo("lts.accommodations.amenities.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(LTSAmenity data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(data),
                    sourceinterface = "amenities",
                    sourceid = data.rid,
                    sourceurl = "https://go.lts.it/api/v1/amenities",
                    type = "accommodations.amenities",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
    }

    public class LTSAmenity
    {
        public string rid { get; set; }
        public DateTime lastUpdate { get; set; }

        public IDictionary<string, string> name { get; set; }
        public IDictionary<string, string> description { get; set; }

        //public bool isActive { get; set; }
        public string code { get; set; }
        public string type { get; set; }
    }
}
