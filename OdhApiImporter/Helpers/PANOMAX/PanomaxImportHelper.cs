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
using PANOMAX;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class PanomaxImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }

        //Constructor
        public PanomaxImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL)
        {
            idlistinterface = new List<string>();
        }

        //Main Method, Gets the data list (Webcams and Videos) from source, Imports and Parses the data, Sets Data no more on the interface to inactive
        public async Task<IEnumerable<UpdateDetail>> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //GET Data List Webcams and Videos
            var data = await GetData(cancellationToken);

            //UPDATE all data
            var updateresult = await ImportData(data, cancellationToken);

            //Disable Data not in panomax list
            var deleteresult = await SetDataNotinListToInactive(cancellationToken);

            return new List<UpdateDetail>() { updateresult, deleteresult };
        }

        //Get Data from Source
        private async Task<List<dynamic>> GetData(CancellationToken cancellationToken)
        {
            List<dynamic> panomaxdata = new List<dynamic>();

            //Get Panomax webcam data
            panomaxdata.Add(await GetPanomaxData.GetWebcams(settings.PanomaxConfig.ServiceUrl));
            //Get Panomax video data
            panomaxdata.Add(await GetPanomaxData.GetVideos(settings.PanomaxConfig.ServiceUrl));

            return panomaxdata;
        }

        //Import the Data
        public async Task<UpdateDetail> ImportData(
            List<dynamic> panomaxinput,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (panomaxinput != null && panomaxinput.Count > 0)
            {
                //loop trough panomax items
                foreach (var webcam in panomaxinput[0])
                {
                    var importresult = await ImportDataSingle(webcam, panomaxinput[1]);

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
        public async Task<UpdateDetail> ImportDataSingle(dynamic webcam, dynamic videolist)
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
                returnid = webcam.id;

                idlistinterface.Add("PANOMAX_" + returnid);

                //Parse Panomax Webcam Data
                WebcamInfoLinked parsedobject = await ParsePanomaxDataToWebcam(
                    "PANOMAX_" + returnid,
                    webcam
                );
                if (parsedobject == null)
                    throw new Exception();

                //Get Areas to Assign (Areas is a LTS only concept and will be removed in future)

                //Set Shortname
                parsedobject.Shortname = parsedobject
                    .Detail.Select(x => x.Value.Title)
                    .FirstOrDefault();

                //assign Videos

                var videostoassign = ((IEnumerable<dynamic>)videolist.cams)
                    .Where(x => x.id == webcam.camId)
                    .FirstOrDefault();

                parsedobject.VideoItems = ParsePanomaxToODH.ParseVideosToVideoItems(
                    parsedobject.VideoItems,
                    videostoassign
                );

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
                    "single.panomax",
                    new ImportLog()
                    {
                        sourceid = parsedobject.Id,
                        sourceinterface = "panomax.webcam",
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
                    "single.panomax",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "panomax.webcam",
                        success = false,
                        error = "panomax webcam could not be parsed",
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
                    new EditInfo("panomax.webcam.import", importerURL),
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
                    datasource = "panomax",
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

        //Parse the panomax interface content
        public async Task<WebcamInfoLinked?> ParsePanomaxDataToWebcam(string odhid, dynamic input)
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var webcamindb = await query.GetObjectSingleAsync<WebcamInfoLinked>();
            var webcam = default(WebcamInfoLinked);

            webcam = ParsePanomaxToODH.ParseWebcamToWebcamInfo(webcamindb, input, odhid);

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
                var idlistdb = await GetAllDataBySource(new List<string>() { "panomax" });

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
                    "deactivate.panomax",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "panomax.webcam",
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
