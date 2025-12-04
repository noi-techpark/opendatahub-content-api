// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Amazon.Util.Internal;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Tagging;
using LTSAPI;
using LTSAPI.Parser;
using NetTopologySuite.GeometriesGraph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiAccommodationImportHelper : ImportHelper, IImportHelper
    {
        public enum RequestType
        {
            detail,
            list,
            listdetail,
        }

        public RequestType requestType = RequestType.listdetail;

        public LTSApiAccommodationImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

        private async Task<List<JObject>> GetAccommodationListFromLTSV2(
            DateTime? lastchanged,
            List<string>? idlist,
            RequestType requestTypeToUse
        )
        {
            try
            {
                if (requestTypeToUse == RequestType.listdetail)
                {
                    LtsApi ltsapi = new LtsApi(
                        settings.LtsCredentials.serviceurl,
                        settings.LtsCredentials.username,
                        settings.LtsCredentials.password,
                        settings.LtsCredentials.ltsclientid,
                        false
                    );
                    var qs = new LTSQueryStrings()
                    {
                        page_size = 20,
                        filter_marketingGroupRids = "9E72B78AC5B14A9DB6BED6C2592483BF",
                    };

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    if (idlist != null && idlist.Count > 0)
                        qs.filter_rids = String.Join(",", idlist);

                    var dict = ltsapi.GetLTSQSDictionary(qs);
                    var ltsdata = await ltsapi.AccommodationListRequest(dict, true);

                    return ltsdata;
                }
                else if (requestTypeToUse == RequestType.list)
                {
                    LtsApi ltsapi = new LtsApi(
                        settings.LtsCredentials.serviceurl,
                        settings.LtsCredentials.username,
                        settings.LtsCredentials.password,
                        settings.LtsCredentials.ltsclientid,
                        false
                    );
                    var qs = new LTSQueryStrings()
                    {
                        page_size = 100,
                        filter_marketingGroupRids = "9E72B78AC5B14A9DB6BED6C2592483BF",
                        fields = "rid",
                    };

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    if (idlist != null && idlist.Count > 0)
                        qs.filter_rids = String.Join(",", idlist);

                    var dict = ltsapi.GetLTSQSDictionary(qs);
                    var ltsdata = await ltsapi.AccommodationListRequest(dict, true);

                    return ltsdata;
                }
                else if (requestTypeToUse == RequestType.detail)
                {
                    LtsApi ltsapi = new LtsApi(
                        settings.LtsCredentials.serviceurl,
                        settings.LtsCredentials.username,
                        settings.LtsCredentials.password,
                        settings.LtsCredentials.ltsclientid,
                        false
                    );
                    var qs = new LTSQueryStrings() { page_size = 1 };

                    var dict = ltsapi.GetLTSQSDictionary(qs);
                    var ltsdata = await ltsapi.AccommodationDetailRequest(
                        idlist.FirstOrDefault(),
                        dict
                    );

                    return ltsdata;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    requestType.ToString() + ".accommodations",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.accommodations",
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
            //Import the List & Data
            var accommodationids = await GetAccommodationListFromLTSV2(
                lastchanged,
                idlist,
                requestType
            );

            //Import Single Data & Deactivate Data
            return await SaveAccommodationsToPG(accommodationids);
        }

        private async Task<UpdateDetail> SaveAccommodationsToPG(List<JObject> ltsdata)
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();
                List<LTSAccoData> ltsaccos = new List<LTSAccoData>();

                var xmlfiles = ImportUtils.LoadXmlFiles(
                    Path.Combine(".\\xml\\"),
                    new List<string>()
                    {
                        "AccoCategories",
                        "AccoTypes",
                        "Alpine",
                        "Boards",
                        "City",
                        "Dolomites",
                        "Mediterranean",
                        "NearSkiArea",
                        "RoomAmenities",
                        "Vinum",
                        "Wine",
                    }
                );

                var jsonfiles = await ImportUtils.LoadJsonFiles(
                    Path.Combine(".\\json\\"),
                    new List<string>() { "Features" }
                );

                foreach (var ltsdatapage in ltsdata)
                {
                    //If we have a single detail request add directly
                    if (requestType == RequestType.detail)
                    {
                        ltsaccos.Add(ltsdatapage["data"].ToObject<LTSAccoData>());
                    }
                    else
                    {
                        //Else add it sequentially
                        foreach (var ltsdatasingle in ltsdatapage["data"].ToArray())
                        {
                            //To check if this works also for the paginated
                            ltsaccos.Add(ltsdatasingle.ToObject<LTSAccoData>());
                        }
                    }
                }

                foreach (var data in ltsaccos)
                {
                    string id = data.rid;
                    var accodetail = data;

                    //If requesttype detail get the data first
                    if (requestType == RequestType.list)
                    {
                        var accodetailresult = await GetAccommodationListFromLTSV2(
                            null,
                            new List<string>() { id },
                            RequestType.detail
                        );

                        accodetail = accodetailresult
                            .FirstOrDefault()["data"]
                            .ToObject<LTSAccoData>();
                    }

                    //See if data exists
                    var query = QueryFactory.Query("accommodations").Select("data").Where("id", id);

                    var objecttosave = await query.GetObjectSingleAsync<AccommodationV2>();

                    //Parse Accommodation TOCHECK!
                    AccommodationV2 accommodationV2 = AccommodationParser.ParseLTSAccommodation(
                        accodetail,
                        false,
                        xmlfiles,
                        jsonfiles
                    );

                    //TODO Take everything from Loaded Accommodation that should remain as it is
                    if (objecttosave != null) { }

                    //TODO Update All ROOMS

                    //TODO Update HGV INFO

                    //TODO Update HGV ROOMS

                    //FINALLY UPDATE ACCOMMODATION ROOT OBJECT

                    var result = await InsertDataToDB(accommodationV2, accodetail);

                    newimportcounter = newimportcounter + result.created ?? 0;
                    updateimportcounter = updateimportcounter + result.updated ?? 0;
                    errorimportcounter = errorimportcounter + result.error ?? 0;

                    idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.accommodations",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.accommodations",
                            success = true,
                            error = "",
                        }
                    );
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
            AccommodationV2 objecttosave,
            LTSAccoData data
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                    objecttosave,
                    Helper.LicenseHelper.GetLicenseforAccommodation
                );

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave);

                //Set PublishedOn
                objecttosave.CreatePublishedOnList();

                //Populate Tags (Id/Source/Type)
                await objecttosave.UpdateTagsExtension(QueryFactory);

                var rawdataid = await InsertInRawDataDB(data);

                return await QueryFactory.UpsertData<AccommodationV2>(
                    objecttosave,
                    new DataInfo("accommodations", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("lts.accommodations.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(LTSAccoData data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(data),
                    sourceinterface = "accommodations",
                    sourceid = data.rid,
                    sourceurl = "https://go.lts.it/api/v1/accommodations",
                    type = "accommodations",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
    }

    public class GenericRidList
    {
        public string rid { get; set; }
    }
}
