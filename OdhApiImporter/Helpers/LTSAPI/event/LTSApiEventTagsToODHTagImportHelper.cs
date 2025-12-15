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
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiEventTagsToODHTagImportHelper : ImportHelper, IImportHelper
    {        
        public LTSApiEventTagsToODHTagImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier)
        {
            this.OdhPushnotifier = odhpushnotifier;
        }

        private async Task<List<JObject>> GetEventTagsFromLTSV2()
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

                var ltsdata = await ltsapi.EventTagRequest(dict, true);

                return ltsdata;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.events.tags",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.events.tags",
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
            var eventtags = await GetEventTagsFromLTSV2();
            //Import Single Data & Deactivate Data
            var result = await SaveEventTagsToPG(eventtags);

            return result;
        }

        private async Task<UpdateDetail> SaveEventTagsToPG(List<JObject> ltsdata)
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSEventTag> eventtagdata = new List<LTSEventTag>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    eventtagdata.AddRange(ltsdatasingle["data"].ToObject<IList<LTSEventTag>>());
                }

                foreach (var data in eventtagdata)
                {
                    string id = data.rid;

                    //See if data exists
                    var query = QueryFactory.Query("smgtags").Select("data").Where("id", id);

                    var objecttosave = await query.GetObjectSingleAsync<ODHTagLinked>();

                    if (objecttosave == null)
                        objecttosave = new ODHTagLinked();

                    objecttosave.Id = data.rid;
                    objecttosave.DisplayAsCategory = false;
                    objecttosave.FirstImport =
                        objecttosave.FirstImport == null ? DateTime.Now : objecttosave.FirstImport;
                    objecttosave.LastChange = data.lastUpdate;

                    objecttosave.Source = new List<string>() { "LTSTag" };
                    objecttosave.TagName = data.name;

                    objecttosave.MainEntity = "event";
                    objecttosave.ValidForEntity = new List<string>() { "event" };
                    objecttosave.Shortname = objecttosave.TagName.ContainsKey("en")
                        ? objecttosave.TagName["en"]
                        : objecttosave.TagName.FirstOrDefault().Value;

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
                            }
                        },
                    };
                    objecttosave.LTSTaggingInfo = null;
                    objecttosave.PublishedOn = null;
                    objecttosave.MappedTagIds = null;

                    var result = await InsertDataToDB(objecttosave, data);

                    //Push Data if changed
                    //push modified data to all published Channels
                    //TODO adding the push status to the response
                    result.pushed = await ImportUtils.CheckIfObjectChangedAndPush(
                        OdhPushnotifier,
                        result,
                        result.id,
                        result.odhtype
                    );

                    newimportcounter = newimportcounter + result.created ?? 0;
                    updateimportcounter = updateimportcounter + result.updated ?? 0;
                    errorimportcounter = errorimportcounter + result.error ?? 0;

                    idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.events.tags",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.events.tags",
                            success = true,
                            error = "",
                        }
                    );
                }

                if (idlistlts.Count > 0)
                {
                    // var query =
                    //     QueryFactory.Query("smgtags")
                    //.Select("id")
                    //.ODHTagSourcesFilter_GeneratedColumn(new List<string>() { "LTSTag" });

                    // var idlistdb = await query.GetAsync<string>();

                    // var idstodelete = idlistdb.Where(p => !idlistlts.Any(p2 => p2 == p));

                    // foreach (var idtodelete in idstodelete)
                    // {
                    //     var deletedisableresult = await DeleteOrDisableData<ODHTagLinked>(idtodelete, false);

                    //     if (deletedisableresult.Item1 > 0)
                    //         WriteLog.LogToConsole(idtodelete, "dataimport", "single.events.tags.deactivate", new ImportLog() { sourceid = idtodelete, sourceinterface = "lts.events.tags", success = true, error = "" });
                    //     else if (deletedisableresult.Item2 > 0)
                    //         WriteLog.LogToConsole(idtodelete, "dataimport", "single.events.tags.delete", new ImportLog() { sourceid = idtodelete, sourceinterface = "lts.events.tags", success = true, error = "" });


                    //     deleteimportcounter = deleteimportcounter + deletedisableresult.Item1 + deletedisableresult.Item2;
                    // }
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
            ODHTagLinked objecttosave,
            LTSEventTag eventtag
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                    objecttosave,
                    Helper.LicenseHelper.GetLicenseforODHTag
                );

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave);

                //Set PublishedOn
                objecttosave.PublishedOn = new List<string>() { "idm-marketplace" };

                return await QueryFactory.UpsertData<ODHTagLinked>(
                    objecttosave,
                    new DataInfo("smgtags", CRUDOperation.CreateAndUpdate),
                    new EditInfo("importer", "lts"),
                    new CRUDConstraints(),
                    new CompareConfig(true, false)
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
