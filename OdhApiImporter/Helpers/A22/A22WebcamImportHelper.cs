// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using A22;
using DataModel;
using Helper;
using Helper.Generic;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers
{
    public class A22WebcamImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }

        public A22WebcamImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier)
        {
            idlistinterface = new List<string>();
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //GET Data
            var data = await GetData(cancellationToken);
            var gpsdata = await GetGpsData(cancellationToken);

            //UPDATE all data
            var updateresult = await ImportData(data, gpsdata, cancellationToken);

            //Disable Data not in list
            var deleteresult = await SetDataNotinListToInactive(cancellationToken);

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }

        //Get Data from Source
        private async Task<XDocument> GetData(CancellationToken cancellationToken)
        {
            return await GetA22Data.GetWebcams(
                settings.A22Config.ServiceUrl,
                settings.A22Config.User,
                settings.A22Config.Password
            );
        }

        //Get Data from Source
        private async Task<XDocument> GetGpsData(CancellationToken cancellationToken)
        {
            return await GetA22Data.GetCoordinates(
                settings.A22Config.ServiceUrl,
                settings.A22Config.User,
                settings.A22Config.Password
            );
        }

        //Import the Data
        public async Task<UpdateDetail> ImportData(
            XDocument a22data,
            XDocument coordinates,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (a22data != null && a22data.Root != null)
            {
                XNamespace df = a22data.Root.Name.Namespace;

                //loop trough a22 webcam items
                foreach (var webcam in a22data.Root.Elements(df + "WSOpenData_WebCam"))
                {
                    var matchedcoordinate = coordinates
                        .Root.Elements(df + "WSOpenData_CoordinataMappa")
                        .Where(x =>
                            x.Element(x.GetDefaultNamespace() + "KM").Value
                            == webcam.Element(df + "KM").Value
                        )
                        .FirstOrDefault();

                    var importresult = await ImportDataSingle(webcam, matchedcoordinate);

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
        public async Task<UpdateDetail> ImportDataSingle(XElement webcam, XElement coordinates)
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
                returnid = webcam.Element(webcam.GetDefaultNamespace() + "KM").Value;

                idlistinterface.Add("A22_" + returnid);

                //Parse A22 Webcam Data
                WebcamInfoLinked parsedobject = await ParseA22DataToWebcam(
                    "A22_" + returnid,
                    webcam,
                    coordinates
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
                    "single.a22",
                    new ImportLog()
                    {
                        sourceid = parsedobject.Id,
                        sourceinterface = "a22.webcam",
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
                    "single.a22",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "a22.webcam",
                        success = false,
                        error = "a22 webcam could not be parsed",
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
        private async Task<PGCRUDResult> InsertDataToDB(
            WebcamInfoLinked webcam,
            KeyValuePair<string, XElement> a22data
        )
        {
            var rawdataid = await InsertInRawDataDB(a22data);

            webcam.Id = webcam.Id?.ToUpper();

            //Set LicenseInfo
            webcam.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<WebcamInfoLinked>(
                webcam,
                Helper.LicenseHelper.GetLicenseforWebcam
            );

            var pgcrudresult = await QueryFactory.UpsertData<WebcamInfoLinked>(
                webcam,
                new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                new EditInfo("a22.webcam.import", importerURL),
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
                    datasource = "a22",
                    rawformat = "xml",
                    importdate = DateTime.Now,
                    license = "closed",
                    sourceinterface = "webcams",
                    sourceurl = settings.A22Config.ServiceUrl,
                    type = "webcam",
                    sourceid = data.Key,
                    raw = data.Value.ToString(),
                }
            );
        }

        //Parse the a22 interface content
        public async Task<WebcamInfoLinked?> ParseA22DataToWebcam(
            string odhid,
            XElement input,
            XElement coordinates
        )
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var webcamindb = await query.GetObjectSingleAsync<WebcamInfoLinked>();
            var webcam = default(WebcamInfoLinked);

            webcam = ParseA22ToODH.ParseWebcamToWebcamInfo(webcamindb, input, coordinates, odhid);

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
                var idlistdb = await GetAllDataBySource(new List<string>() { "a22" });

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
                    "deactivate.a22.webcam",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "a22.webcam",
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
