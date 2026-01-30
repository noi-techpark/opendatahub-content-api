// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using A22;
using DataModel;
using Helper;
using Helper.Generic;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class A22PoiImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }
        public A22Poi a22poiinfo { get; set; }

        public A22PoiImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            string entity
        )
            : base(settings, queryfactory, table, importerURL)
        {
            idlistinterface = new List<string>();

            if (entity.ToLower() == "tollstation")
            {
                a22poiinfo = new A22Tollstation();
            }
            if (entity.ToLower() == "servicearea")
            {
                a22poiinfo = new A22ServiceArea();
            }
        }

        public async Task<IEnumerable<UpdateDetail>> SaveDataToODH(
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

            return new List<UpdateDetail>() { updateresult, deleteresult };            
        }

        //Get Data from Source
        private async Task<XDocument> GetData(CancellationToken cancellationToken)
        {
            if (a22poiinfo != null)
                return await a22poiinfo.GetData(settings);
            else
                return null;
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
                CultureInfo myculture = new CultureInfo("en");

                //loop trough a22 items
                foreach (var poi in a22data.Root.Elements(df + a22poiinfo.rootelement))
                {
                    XElement matchedcoordinate = a22poiinfo.GetMatchedGPS(
                        a22data,
                        coordinates,
                        poi
                    );

                    //if(entity == "tollstation")
                    //{
                    //    matchedcoordinate = coordinates.Root.Elements(df + "WSOpenData_CoordinataMappa")
                    //        .Where(x => x.Element(x.GetDefaultNamespace() + "KM").Value == poi.Element(df + "KM").Value).FirstOrDefault();
                    //}
                    //else if(entity == "servicearea")
                    //{
                    //    double distance = Convert.ToDouble(poi.Element(df + "Distanza").Value, myculture);

                    //    matchedcoordinate = coordinates.Root.Elements(df + "WSOpenData_CoordinataMappa")
                    //    .Where(x => Convert.ToDouble(x.Element(x.GetDefaultNamespace() + "KM").Value, myculture) >= distance).FirstOrDefault();
                    //}


                    var importresult = await ImportDataSingle(poi, matchedcoordinate);

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
        public async Task<UpdateDetail> ImportDataSingle(XElement poi, XElement coordinates)
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
                returnid = a22poiinfo.GetReturnid(poi);

                idlistinterface.Add("a22_" + a22poiinfo.entitytype + "_" + returnid);

                //Parse A22 Data
                ODHActivityPoiLinked parsedobject = await ParseA22DataToODHActivityPoi(
                    "a22_" + a22poiinfo.entitytype + "_" + returnid,
                    poi,
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
                    new KeyValuePair<string, XElement>(returnid, poi)
                );

                newcounter = newcounter + pgcrudresult.created ?? 0;
                updatecounter = updatecounter + pgcrudresult.updated ?? 0;

                WriteLog.LogToConsole(
                    parsedobject.Id,
                    "dataimport",
                    "single.a22." + a22poiinfo.entitytype,
                    new ImportLog()
                    {
                        sourceid = parsedobject.Id,
                        sourceinterface = "a22." + a22poiinfo.entitytype,
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
                    "single.a22." + a22poiinfo.entitytype,
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "a22." + a22poiinfo.entitytype,
                        success = false,
                        error = "a22 " + a22poiinfo.entitytype + " could not be parsed",
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
            ODHActivityPoiLinked odhactivitypoi,
            KeyValuePair<string, XElement> a22data
        )
        {
            var rawdataid = await InsertInRawDataDB(a22data);

            odhactivitypoi.Id = odhactivitypoi.Id?.ToLower();

            //Set LicenseInfo
            odhactivitypoi.LicenseInfo =
                Helper.LicenseHelper.GetLicenseInfoobject<ODHActivityPoiLinked>(
                    odhactivitypoi,
                    Helper.LicenseHelper.GetLicenseforOdhActivityPoi
                );

            var pgcrudresult = await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                odhactivitypoi,
                new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                new EditInfo("a22." + a22poiinfo.entitytype + ".import", importerURL),
                new CRUDConstraints(),
                new CompareConfig(true, false),
                rawdataid);

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
                    license = "open",
                    sourceinterface = a22poiinfo.entitytype,
                    sourceurl = settings.A22Config.ServiceUrl,
                    type = "odhactivitypoi." + a22poiinfo.entitytype.ToLower(),
                    sourceid = data.Key,
                    raw = data.Value.ToString(),
                }
            );
        }

        //Parse the a22 interface content
        public async Task<ODHActivityPoiLinked?> ParseA22DataToODHActivityPoi(
            string odhid,
            XElement input,
            XElement coordinates
        )
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var poiindb = await query.GetObjectSingleAsync<ODHActivityPoiLinked>();
            var poi = default(ODHActivityPoiLinked);

            //if (a22poiinfo.entitytype == "tollstation")
            //    poi = ParseA22ToODH.ParseTollStationToODHActivityPoi(poiindb, input, coordinates, odhid);
            //if (a22poiinfo.entitytype == "servicearea")
            //    poi = ParseA22ToODH.ParseServiceAreaToODHActivityPoi(poiindb, input, coordinates, odhid);

            poi = a22poiinfo.ParsePoi(poiindb, input, coordinates, odhid);

            return poi;
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
                var idlistdb = await GetAllDataBySource(
                    new List<string>() { "a22" },
                    new List<string>() { a22poiinfo.entitytype }
                );

                var idstodelete = idlistdb.Where(p => !idlistinterface.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var result = await DeleteOrDisableData<ODHActivityPoiLinked>(idtodelete, false);

                    updateresult = updateresult + result.Item1;
                    deleteresult = deleteresult + result.Item2;
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.a22.odhactivitypoi." + a22poiinfo.entitytype,
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "a22." + a22poiinfo.entitytype,
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

    public abstract class A22Poi
    {
        public string entitytype { get; set; }
        public string rootelement { get; set; }

        public virtual string GetReturnid(XElement poi)
        {
            throw new NotImplementedException();
        }

        public virtual XElement? GetMatchedGPS(
            XDocument a22data,
            XDocument coordinates,
            XElement? poi
        )
        {
            throw new NotImplementedException();
        }

        public virtual async Task<XDocument> GetData(ISettings settings)
        {
            throw new NotImplementedException();
        }

        public virtual ODHActivityPoiLinked ParsePoi(
            ODHActivityPoiLinked? poiindb,
            XElement input,
            XElement coordinate,
            string odhid
        )
        {
            throw new NotImplementedException();
        }
    }

    public class A22Tollstation : A22Poi
    {
        public A22Tollstation()
        {
            entitytype = "tollstation";
            rootelement = "ArchieCasello";
        }

        public override XElement? GetMatchedGPS(
            XDocument a22data,
            XDocument coordinates,
            XElement? poi
        )
        {
            XNamespace df = a22data.Root.Name.Namespace;
            CultureInfo myculture = new CultureInfo("en");

            var matchedcoordinate = coordinates
                .Root.Elements(df + "WSOpenData_CoordinataMappa")
                .Where(x =>
                    x.Element(x.GetDefaultNamespace() + "KM").Value == poi.Element(df + "KM").Value
                )
                .FirstOrDefault();

            return matchedcoordinate;
        }

        public override string GetReturnid(XElement poi)
        {
            return poi.Element(poi.GetDefaultNamespace() + "IDCasello").Value;
        }

        public override async Task<XDocument> GetData(ISettings settings)
        {
            return await GetA22Data.GetTollStations(
                settings.A22Config.ServiceUrl,
                settings.A22Config.User,
                settings.A22Config.Password
            );
        }

        public override ODHActivityPoiLinked ParsePoi(
            ODHActivityPoiLinked? poiindb,
            XElement input,
            XElement coordinate,
            string odhid
        )
        {
            return ParseA22ToODH.ParseTollStationToODHActivityPoi(
                poiindb,
                input,
                coordinate,
                odhid
            );
        }
    }

    public class A22ServiceArea : A22Poi
    {
        public A22ServiceArea()
        {
            entitytype = "servicearea";
            rootelement = "WSOpenData_AreaDiServizio";
        }

        public override XElement? GetMatchedGPS(
            XDocument a22data,
            XDocument coordinates,
            XElement? poi
        )
        {
            XNamespace df = a22data.Root.Name.Namespace;
            CultureInfo myculture = new CultureInfo("en");

            double distance = Convert.ToDouble(poi.Element(df + "Distanza").Value, myculture);

            var matchedcoordinate = coordinates
                .Root.Elements(df + "WSOpenData_CoordinataMappa")
                .Where(x =>
                    Convert.ToDouble(x.Element(x.GetDefaultNamespace() + "KM").Value, myculture)
                    >= distance
                )
                .FirstOrDefault();

            return matchedcoordinate;
        }

        public override string GetReturnid(XElement poi)
        {
            return poi.Element(poi.GetDefaultNamespace() + "ID").Value;
        }

        public override async Task<XDocument> GetData(ISettings settings)
        {
            return await GetA22Data.GetServiceAreas(
                settings.A22Config.ServiceUrl,
                settings.A22Config.User,
                settings.A22Config.Password
            );
        }

        public override ODHActivityPoiLinked ParsePoi(
            ODHActivityPoiLinked? poiindb,
            XElement input,
            XElement coordinate,
            string odhid
        )
        {
            return ParseA22ToODH.ParseServiceAreaToODHActivityPoi(
                poiindb,
                input,
                coordinate,
                odhid
            );
        }
    }
}
