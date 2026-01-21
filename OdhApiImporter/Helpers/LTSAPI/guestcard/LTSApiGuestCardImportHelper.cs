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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiGuestCardImportHelper : ImportHelper, IImportHelper
    {
        public LTSApiGuestCardImportHelper(
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
            //Import the List
            var guestcards = await GetGuestcardsFromLTSV2();
            //Import Single Data & Deactivate Data
            var result = await SaveSuedtirolGuestPassCardTypesToPG(guestcards);

            return result;
        }

        private async Task<List<JObject>> GetGuestcardsFromLTSV2()
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

                var ltsdata = await ltsapi.SuedtirolGuestPassCardTypesRequest(dict, true);

                return ltsdata;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.suedtirolguestpass.cardtypes",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.suedtirolguestpass.cardtypes",
                        success = false,
                        error = ex.Message,
                    }
                );
                return null;
            }
        }

        private async Task<UpdateDetail> SaveSuedtirolGuestPassCardTypesToPG(List<JObject> ltsdata)
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSGuestcard> guestcardata = new List<LTSGuestcard>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    guestcardata.AddRange(ltsdatasingle["data"].ToObject<IList<LTSGuestcard>>());
                }

                //Add manually the Guestcard Tag from idm
                guestcardata.Add(
                    new LTSGuestcard()
                    {
                        rid = "guestcard",
                        isActive = true,
                        lastUpdate = DateTime.Now,
                        name = new Dictionary<string, string>()
                        {
                            { "de", "Guestcard" },
                            { "it", "Guestcard" },
                            { "en", "Guestcard" },
                        },
                    }
                );

                foreach (var data in guestcardata)
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

                    objecttosave.Source = data.rid == "guestcard" ? "idm" : "lts";
                    objecttosave.TagName = data.name;
                    objecttosave.MainEntity = "accommodation";
                    objecttosave.ValidForEntity = new List<string>() { "accommodation" };
                    objecttosave.Shortname = objecttosave.TagName.ContainsKey("en")
                        ? objecttosave.TagName["en"]
                        : objecttosave.TagName.FirstOrDefault().Value;
                    objecttosave.Types = new List<string>() { "cardtype" };

                    objecttosave.IDMCategoryMapping = null;
                    objecttosave.PublishDataWithTagOn = null;
                    objecttosave.Mapping = new Dictionary<string, IDictionary<string, string>>()
                    {
                        {
                            "lts",
                            new Dictionary<string, string>() { { "rid", data.rid } }
                        },
                    };
                    ;
                    objecttosave.LTSTaggingInfo = null;
                    objecttosave.PublishedOn = null;
                    objecttosave.MappedTagIds = null;

                    var result = await InsertDataToDB(objecttosave, data);

                    newimportcounter = newimportcounter + result.created ?? 0;
                    updateimportcounter = updateimportcounter + result.updated ?? 0;
                    errorimportcounter = errorimportcounter + result.error ?? 0;

                    if (data.rid != "guestcard")
                        idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.suedtirolguestpass.cardtypes",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.suedtirolguestpass.cardtypes",
                            success = true,
                            error = "",
                        }
                    );
                }

                //Deactivate only if there is data updated
                if (idlistlts.Count > 0)
                {
                    //Begin SetDataNotinListToInactive
                    var idlistdb = await GetAllDataBySourceAndType(
                        new List<string>() { "lts" },
                        new List<string>() { "cardtype" }
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
                                "single.suedtirolguestpass.cardtypes.deactivate",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "lts.suedtirolguestpass.cardtypes",
                                    success = true,
                                    error = "",
                                }
                            );
                        else if (deletedisableresult.Item2 > 0)
                            WriteLog.LogToConsole(
                                idtodelete,
                                "dataimport",
                                "single.suedtirolguestpass.cardtypes.delete",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "lts.suedtirolguestpass.cardtypes",
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
            LTSGuestcard guestcard
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

                var rawdataid = await InsertInRawDataDB(guestcard);

                return await QueryFactory.UpsertData<TagLinked>(
                    objecttosave,
                    new DataInfo("tags", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("lts.suedtirolguestpass.cardtypes.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(LTSGuestcard guestcard)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(guestcard),
                    sourceinterface = "suedtirolguestpass",
                    sourceid = guestcard.rid,
                    sourceurl = "https://go.lts.it/api/v1/suedtirolguestpass/cardtypes",
                    type = "suedtirolguestpass.cardtypes",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
    }

    public class LTSGuestcard
    {
        public string rid { get; set; }
        public DateTime lastUpdate { get; set; }
        public IDictionary<string, string> name { get; set; }
        public bool isActive { get; set; }
    }
}
