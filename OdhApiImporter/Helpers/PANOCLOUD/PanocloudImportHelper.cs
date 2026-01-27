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
using Newtonsoft.Json;
using PANOCLOUD;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class PanocloudImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }

        //Constructor
        public PanocloudImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL)
        {
            idlistinterface = new List<string>();
        }

        //Main Method, Gets the data (Webcams) from source, Imports and Parses the data, Sets Data no more on the interface to inactive
        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //GET Data and Deserialize to Json
            var data = await GetData(cancellationToken);

            //UPDATE all data
            var updateresult = await ImportData(data, cancellationToken);

            //Disable Data not in panomax list
            var deleteresult = await SetDataNotinListToInactive(cancellationToken);

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }

        //Get Data from Source
        private async Task<dynamic?> GetData(CancellationToken cancellationToken)
        {
            //Get Panomax webcam data
            return await GetPanocloudData.GetWebcams(settings.PanocloudConfig.ServiceUrl);
        }

        //Import the Data
        public async Task<UpdateDetail> ImportData(
            dynamic? panomaxinput,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (panomaxinput != null)
            {
                //loop trough dss items
                foreach (var webcam in panomaxinput.LiveCam)
                {
                    var importresult = await ImportDataSingle(webcam);

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

        //Parsing the Data
        public async Task<UpdateDetail> ImportDataSingle(dynamic webcam)
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            //id
            string returnid = "";

            try
            {
                //id
                returnid =
                    "PANOCLOUD_"
                    + (string)webcam["@attributes"]["locationId"]
                    + "_"
                    + (string)webcam["@attributes"]["geoAlt"];

                idlistinterface.Add(returnid);

                //Parse Panomax Webcam Data
                WebcamInfoLinked parsedobject = await ParsePanocloudDataToWebcam(returnid, webcam);
                if (parsedobject == null)
                    throw new Exception();

                //Get Areas to Assign (Areas is a LTS only concept and will be removed in future)

                //Set Shortname
                parsedobject.Shortname = parsedobject
                    .Detail.Select(x => x.Value.Title)
                    .FirstOrDefault();

                //Save parsedobject to DB + Save Rawdata to DB
                var pgcrudresult = await InsertDataToDB(
                    parsedobject,
                    new KeyValuePair<string, dynamic>(returnid, webcam)
                );

                newcounter = newcounter + pgcrudresult.created ?? 0;
                updatecounter = updatecounter + pgcrudresult.updated ?? 0;

                WriteLog.LogToConsole(
                    parsedobject.Id,
                    "dataimport",
                    "single.panocloud",
                    new ImportLog()
                    {
                        sourceid = parsedobject.Id,
                        sourceinterface = "panocloud.webcam",
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
                    "single.panocloud",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "panocloud.webcam",
                        success = false,
                        error = "panocloud webcam could not be parsed",
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

        //Inserting into DB
        private async Task<UpdateDetail> InsertDataToDB(
            WebcamInfoLinked webcam,
            KeyValuePair<string, dynamic> panomaxdata
        )
        {
            var rawdataid = await InsertInRawDataDB(panomaxdata);

            webcam.Id = webcam.Id?.ToUpper();

            //Set LicenseInfo
            webcam.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<WebcamInfoLinked>(
                webcam,
                Helper.LicenseHelper.GetLicenseforWebcam
            );

            //PublishedOn Info??

            var pgcrudresult = await QueryFactory.UpsertData<WebcamInfoLinked>(
                webcam,
                new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("panocloud.webcam.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid                
            );

            return pgcrudresult;
        }

        //Inserting into RawDB
        private async Task<int> InsertInRawDataDB(KeyValuePair<string, dynamic> data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "panocloud",
                    rawformat = "json",
                    importdate = DateTime.Now,
                    license = "open",
                    sourceinterface = "webcams",
                    sourceurl = settings.PanocloudConfig.ServiceUrl,
                    type = "webcam",
                    sourceid = data.Key,
                    raw = JsonConvert.SerializeObject(data.Value),
                }
            );
        }

        //Parse the panocloud interface content
        public async Task<WebcamInfoLinked?> ParsePanocloudDataToWebcam(string odhid, dynamic input)
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var webcamindb = await query.GetObjectSingleAsync<WebcamInfoLinked>();
            var webcam = default(WebcamInfoLinked);

            webcam = ParsePanocloudToODH.ParseWebcamToWebcamInfo(webcamindb, input, odhid);

            return webcam;
        }

        //Deactivates all data that is no more on the interface
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
                var idlistdb = await GetAllDataBySource(new List<string>() { "panocloud" });

                var idstodelete = idlistdb.Where(p => !idlistinterface.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var result = await DeleteOrDisableData<WebcamInfoLinked>(idtodelete, false);

                    updateresult = updateresult + result.Item1;
                    deleteresult = deleteresult + result.Item2;
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.panocloud",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "panocloud.webcam",
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
    }
}
