// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;
using FERATEL;
using Helper;
using Helper.Generic;
using Newtonsoft.Json;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class FeratelWebcamImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }

        public FeratelWebcamImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL)
        {
            idlistinterface = new List<string>();
        }

        public async Task<IEnumerable<UpdateDetail>> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //GET Data
            var data = await GetData(cancellationToken);

            //UPDATE all data
            var updateresult = await ImportData(data, cancellationToken);

            //Disable Data not in feratel list
            var deleteresult = await SetDataNotinListToInactive(cancellationToken);

            return new List<UpdateDetail>() { updateresult, deleteresult };
        }

        //Get Data from Source
        private async Task<XDocument> GetData(CancellationToken cancellationToken)
        {
            return await GetFeratelData.GetWebcams(settings.FeratelConfig.ServiceUrl);
        }

        //Import the Data
        public async Task<UpdateDetail> ImportData(
            XDocument ferateldata,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (
                ferateldata != null
                && ferateldata.Root.Element("content") != null
                && ferateldata.Root.Element("content").Element("portal") != null
                && ferateldata.Root.Element("content").Element("portal").Element("links") != null
                && ferateldata
                    .Root.Element("content")
                    .Element("portal")
                    .Element("links")
                    .Elements("link")
                    .Count() > 0
            )
            {
                //loop trough feratel items
                foreach (
                    var link in ferateldata
                        .Root.Element("content")
                        .Element("portal")
                        .Element("links")
                        .Elements("link")
                )
                {
                    //Special case feratel has more cams on a link
                    foreach (var webcam in link.Element("cams").Elements("cam"))
                    {
                        var importresult = await ImportDataSingle(webcam, link);

                        newcounter = newcounter + importresult.created ?? newcounter;
                        updatecounter = updatecounter + importresult.updated ?? updatecounter;
                        errorcounter = errorcounter + importresult.error ?? errorcounter;
                    }
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
        public async Task<UpdateDetail> ImportDataSingle(XElement webcam, XElement link)
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            //id
            string returnid = "";

            try
            {
                //id generating by link id and panid from the cam
                returnid = link.Attribute("id").Value + "_" + webcam.Attribute("panid").Value;

                idlistinterface.Add("FERATEL_" + returnid);

                //Parse Feratel Webcam Data
                WebcamInfoLinked parsedobject = await ParseFeratelDataToWebcam(
                    "FERATEL_" + returnid,
                    webcam,
                    link
                );
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
                    new KeyValuePair<string, XElement>(returnid, webcam)
                );

                newcounter = newcounter + pgcrudresult.created ?? 0;
                updatecounter = updatecounter + pgcrudresult.updated ?? 0;

                WriteLog.LogToConsole(
                    parsedobject.Id,
                    "dataimport",
                    "single.feratel",
                    new ImportLog()
                    {
                        sourceid = parsedobject.Id,
                        sourceinterface = "feratel.webcam",
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
                    "single.feratel",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "feratel.webcam",
                        success = false,
                        error = "feratel webcam could not be parsed",
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
            KeyValuePair<string, XElement> ferateldata
        )
        {
            var rawdataid = await InsertInRawDataDB(ferateldata);

            webcam.Id = webcam.Id?.ToUpper();

            //Set LicenseInfo
            webcam.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<WebcamInfoLinked>(
                webcam,
                Helper.LicenseHelper.GetLicenseforWebcam
            );

            //PublishedOnInfo?

            var pgcrudresult = await QueryFactory.UpsertData<WebcamInfoLinked>(
                webcam,
                new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                new EditInfo("feratel.webcam.import", importerURL),
                new CRUDConstraints(),
                new CompareConfig(true, false),
                rawdataid
            );

            return pgcrudresult;
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, XElement> data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "feratel",
                    rawformat = "xml",
                    importdate = DateTime.Now,
                    license = "open",
                    sourceinterface = "webcams",
                    sourceurl = settings.FeratelConfig.ServiceUrl,
                    type = "webcam",
                    sourceid = data.Key,
                    raw = data.Value.ToString(),
                }
            );
        }

        //Parse the feratel interface content
        public async Task<WebcamInfoLinked?> ParseFeratelDataToWebcam(
            string odhid,
            XElement input,
            XElement link
        )
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var webcamindb = await query.GetObjectSingleAsync<WebcamInfoLinked>();
            var webcam = default(WebcamInfoLinked);

            webcam = ParseFeratelToODH.ParseWebcamToWebcamInfo(webcamindb, input, link, odhid);

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
                var idlistdb = await GetAllDataBySource(new List<string>() { "feratel" });

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
                    "deactivate.feratel",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "feratel.webcam",
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
