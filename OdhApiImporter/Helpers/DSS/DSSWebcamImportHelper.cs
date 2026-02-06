// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using DSS;
using DSS.Parser;
using Helper;
using Helper.Generic;
using Newtonsoft.Json;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers.DSS
{
    public class DSSWebcamImportHelper : ImportHelper, IImportHelper
    {
        public DSSWebcamImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL)
        {
            entitytype = "webcam";
            idlistdssinterface = new();
        }

        string entitytype;
        public List<string> idlistdssinterface { get; set; }

        public async Task<IEnumerable<UpdateDetail>> SaveDataToODH(
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

            return new List<UpdateDetail>() { updateresult, deleteresult };
        }

        //Imports DSS Data
        private async Task<List<dynamic>> GetData(CancellationToken cancellationToken)
        {
            List<dynamic> dssdata = new List<dynamic>();

            //Get DSS data
            dssdata.Add(
                await GetDSSData.GetDSSDataAsync(
                    DSSRequestType.webcams,
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

                //loop trough dss items
                foreach (var item in dssinput[0].items)
                {
                    var importresult = await ImportDataSingle(item);

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

        public async Task<UpdateDetail> ImportDataSingle(dynamic item)
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            //id
            string returnid = "";

            try
            {
                WebcamInfoLinked parsedobject = default(WebcamInfoLinked);

                returnid = item.pid;

                //Parse DSS Data
                parsedobject = await ParseDSSDataToWebcam(item);
                if (parsedobject == null)
                    throw new Exception();

                //Get Areas to Assign

                //Set Shortname
                if (
                    parsedobject.Webcamname.ContainsKey("de")
                    && !String.IsNullOrEmpty(parsedobject.Webcamname["de"])
                )
                    parsedobject.Shortname = parsedobject.Webcamname["de"];
                else if (
                    parsedobject.Webcamname.ContainsKey("it")
                    && !String.IsNullOrEmpty(parsedobject.Webcamname["it"])
                )
                    parsedobject.Shortname = parsedobject.Webcamname["it"];
                else if (
                    parsedobject.Webcamname.ContainsKey("en")
                    && !String.IsNullOrEmpty(parsedobject.Webcamname["en"])
                )
                    parsedobject.Shortname = parsedobject.Webcamname["en"];

                var sourceid = (string)DSSImportUtil.GetSourceId(item, entitytype);

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
                    "single.dss" + entitytype,
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "dss." + entitytype,
                        success = false,
                        error = entitytype + " could not be parsed",
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
                var idlistdb = await GetAllDSSDataByInterface(new List<string>() { "dsswebcam" });

                var idstodelete = idlistdb.Where(p => !idlistdssinterface.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var deletedisableresult = await DeleteOrDisableData<WebcamInfoLinked>(
                        idtodelete,
                        false
                    );

                    if (deletedisableresult.Item1 > 0)
                        WriteLog.LogToConsole(
                            idtodelete,
                            "dataimport",
                            "single.dss" + entitytype,
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
                            "single.dss" + entitytype,
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
                    "deactivate.dss" + entitytype,
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
        public async Task<WebcamInfoLinked?> ParseDSSDataToWebcam(dynamic dssinput)
        {
            //id
            string odhdssid = "DSS_" + dssinput.pid;

            //Get the ODH Item
            var mydssquery = QueryFactory.Query(table).Select("data").Where("id", odhdssid);

            var webcamindb = await mydssquery.GetObjectSingleAsync<WebcamInfoLinked>();
            var webcam = default(WebcamInfoLinked);

            webcam = ParseDSSToODH.ParseDSSWebcamToWebcamInfoLinked(webcamindb, dssinput);

            idlistdssinterface.Add(odhdssid);

            return webcam;
        }

        private async Task<UpdateDetail> InsertDataToDB(
            WebcamInfoLinked webcam,
            KeyValuePair<string, dynamic> dssdata
        )
        {
            var rawdataid = await InsertInRawDataDB(dssdata);

            webcam.Id = webcam.Id?.ToUpper();

            //Set LicenseInfo
            webcam.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<WebcamInfoLinked>(
                webcam,
                Helper.LicenseHelper.GetLicenseforWebcam
            );

            var pgcrudresult = await QueryFactory.UpsertData<WebcamInfoLinked>(
                webcam,
                new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                new EditInfo("dss.webcam.import", importerURL),
                new CRUDConstraints(),
                new CompareConfig(true, false),
                rawdataid
            );

            return pgcrudresult;
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, dynamic> dssdata)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "dss",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(dssdata.Value),
                    sourceinterface = "webcam",
                    sourceid = dssdata.Key,
                    sourceurl = settings.DSSConfig.ServiceUrl,
                    type = "webcam",
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
                .SourceFilter_GeneratedColumn(syncsourceinterfacelist);

            var idlist = await query.GetAsync<string>();

            return idlist.ToList();
        }
    }
}
