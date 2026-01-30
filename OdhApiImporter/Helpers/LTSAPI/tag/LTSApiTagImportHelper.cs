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
using SqlKata.Execution;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiTagImportHelper : ImportHelper, IImportHelper
    {
        public LTSApiTagImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

        private async Task<List<JObject>> GetTagsFromLTSV2()
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

                var ltsdata = await ltsapi.TagListRequest(dict, true);

                return ltsdata;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.tags",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.tags",
                        success = false,
                        error = ex.Message,
                    }
                );

                return null;
            }
        }

        public async Task<IEnumerable<UpdateDetail>> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //Import the List
            var eventtags = await GetTagsFromLTSV2();
            //Import Single Data & Deactivate Data
            var result = await SaveTagsToPG(eventtags);

            return new List<UpdateDetail>() { result };
        }

        private async Task<UpdateDetail> SaveTagsToPG(List<JObject> ltsdata)
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();
                List<string> typelistlts = new List<string>();

                List<LTSTags> tagdata = new List<LTSTags>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    tagdata.AddRange(ltsdatasingle["data"].ToObject<IList<LTSTags>>());
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
                    objecttosave.Active = data.isActive;
                    objecttosave.DisplayAsCategory = false;
                    objecttosave.FirstImport =
                        objecttosave.FirstImport == null ? DateTime.Now : objecttosave.FirstImport;
                    objecttosave.LastChange = data.lastUpdate;

                    objecttosave.Source = "lts";
                    objecttosave.TagName = data.name;
                    objecttosave.Description = data.description;

                    objecttosave.MainEntity = "odhactivitypoi";
                    objecttosave.ValidForEntity = new List<string>() { "odhactivitypoi" };
                    objecttosave.Shortname = objecttosave.TagName.ContainsKey("en")
                        ? objecttosave.TagName["en"]
                        : objecttosave.TagName.FirstOrDefault().Value;
                    objecttosave.Types = new List<string>() { "tags" + data.entityType.ToLower() };

                    if (!typelistlts.Contains("ltstags"))
                        typelistlts.Add("ltstags");

                    //objecttosave.IDMCategoryMapping = null;
                    objecttosave.PublishDataWithTagOn = null;
                    objecttosave.Mapping = new Dictionary<string, IDictionary<string, string>>()
                    {
                        {
                            "lts",
                            new Dictionary<string, string>()
                            {
                                { "rid", data.rid },
                                { "code", data.code },
                                { "entityType", data.entityType },
                                { "level", data.level.ToString() },
                                { "mainTagRid", data.mainTagRid },
                                { "parentTagRid", data.parentTagRid },
                                { "isSelectable", data.isSelectable.ToString() },
                                { "properties", data.properties != null ? String.Join(",", data.properties.Select(x => x.rid).ToList()) : string.Empty },
                            }
                        },
                    };
                    objecttosave.LTSTaggingInfo = new LTSTaggingInfo()
                    {
                        LTSRID = data.rid,
                        ParentLTSRID = data.parentTagRid,
                    };
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
                        "single.tags",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.tags",
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
                                "single.tags.deactivate",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "lts.tags",
                                    success = true,
                                    error = "",
                                }
                            );
                        else if (deletedisableresult.Item2 > 0)
                            WriteLog.LogToConsole(
                                idtodelete,
                                "dataimport",
                                "single.tags.delete",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "lts.tags",
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

        private async Task<UpdateDetail> InsertDataToDB(TagLinked objecttosave, LTSTags data)
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
                    new EditInfo("lts.tags.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(LTSTags data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(data),
                    sourceinterface = "tags",
                    sourceid = data.rid,
                    sourceurl = "https://go.lts.it/api/v1/tags",
                    type = "tags",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
    }

    public class LTSTags
    {
        public string rid { get; set; }
        public DateTime lastUpdate { get; set; }

        public IDictionary<string, string> name { get; set; }
        public IDictionary<string, string> description { get; set; }

        public string code { get; set; }
        public string entityType { get; set; }
        public int level { get; set; }

        public string mainTagRid { get; set; }

        public string parentTagRid { get; set; }

        public bool isActive { get; set; }

        public bool isSelectable { get; set; }

        public ICollection<LTSTagRidObject> properties { get; set; }
    }

    public class LTSTagRidObject
    {
        public string rid { get; set; }
    }
}
