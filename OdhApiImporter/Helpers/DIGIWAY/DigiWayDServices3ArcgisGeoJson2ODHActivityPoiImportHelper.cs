// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DIGIWAY;
using DIGIWAY.Model.GeoJsonReadModel;
using Helper;
using Helper.Generic;
using Helper.Tagging;
using OdhApiImporter.Helpers.DIGIWAY;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers
{
    public class DigiWayDServices3ArcgisGeoJson2ODHActivityPoiImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }
        public string? identifier { get; set; }
        public string? source { get; set; }

        public string? srid { get; set; }

        public bool importtospatialdata { get; set; }

        public DigiWayDServices3ArcgisGeoJson2ODHActivityPoiImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier)
        {
            idlistinterface = new List<string>();
            importtospatialdata = false;
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            if (identifier == null || source == null || srid == null)
                throw new Exception("no identifier|source|srid defined");

            List<UpdateDetail> resultlist = new List<UpdateDetail>();

            var data = await GetData(cancellationToken);

            ////UPDATE all data
            resultlist.Add(await ImportData(data, cancellationToken));

            //Disable Data not in list
            if (!importtospatialdata)
                resultlist.Add(await SetDataNotinListToInactive(cancellationToken));

            return GenericResultsHelper.MergeUpdateDetail(
                resultlist
            );
        }

        //Get Data from Source
        private async Task<ICollection<GeoJsonFeature>> GetData(CancellationToken cancellationToken)
        {
            return await GetDigiwayData.GetDigiWayGeoJsonDataFromUrlAsync("", "", settings.DigiWayConfig[identifier].ServiceUrl);
        }

        //Import the Data
        public async Task<UpdateDetail> ImportData(
            ICollection<GeoJsonFeature> datacollection,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (datacollection != null)
            {                
                foreach (var digiwaydata in datacollection)
                {
                    var importresult = await ImportDataSingle(digiwaydata);

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
        public async Task<UpdateDetail> ImportDataSingle(GeoJsonFeature digiwaydata)
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            //id
            string returnid = "";

            try
            {
                returnid = "urn:digiway:dservices3arcgiscom:" + identifier + ":" + digiwaydata.Attributes["OBJECTID"].ToString().ToLower();
                if (importtospatialdata)
                    returnid = ("urn:" + source + ":" + identifier + ":" + digiwaydata.Attributes["OBJECTID"].ToString().ToLower());

                idlistinterface.Add(returnid);

                if (importtospatialdata)
                {
                    //Transform Geometry to 4326 not needed already in this format
                    //digiwaydata.Geometry = await DigiWayConverter.ConvertGeometryWithPostGIS(QueryFactory, digiwaydata.Geometry, srid, "4326");

                    //Parse  Data
                    var parsedobject = await ParseDigiWayDataToSpatialData(
                        returnid,
                        digiwaydata
                    );

                    if (parsedobject == null)
                        throw new Exception();

                    //Save parsedobject to DB + Save Rawdata to DB
                    var pgcrudresult = await InsertDataToSpatialDataDB(
                        parsedobject
                    );

                    newcounter = newcounter + pgcrudresult.created ?? 0;
                    updatecounter = updatecounter + pgcrudresult.updated ?? 0;
                }
                else
                {
                    //Parse  Data
                    var parsedobject = await ParseDigiWayDataToODHActivityPoi(
                        returnid,
                        digiwaydata
                    );
                    if (parsedobject.Item1 == null || parsedobject.Item2 == null)
                        throw new Exception();

                    var pgcrudshaperesult = await GeoShapeInsertHelper.InsertDataInShapesDB(QueryFactory, parsedobject.Item2, source, srid);

                    //Create GPX Info
                    GpsTrack gpstrack = new GpsTrack()
                    {
                        Format = "geojson",
                        GpxTrackUrl = "GeoShape/" + pgcrudshaperesult.id.ToLower(),
                        Id = pgcrudshaperesult.id.ToLower(),
                        Type = "Track",
                        GpxTrackDesc = null
                    };

                    if (parsedobject.Item1.GpsTrack == null)
                        parsedobject.Item1.GpsTrack = new List<GpsTrack>();

                    parsedobject.Item1.GpsTrack.Add(gpstrack);

                    //Create Tags
                    await parsedobject.Item1.UpdateTagsExtension(QueryFactory);

                    //Save parsedobject to DB + Save Rawdata to DB
                    var pgcrudresult = await InsertDataToDB(
                        parsedobject.Item1,
                        new KeyValuePair<string, GeoJsonFeature>(returnid, digiwaydata)
                    );

                    newcounter = newcounter + pgcrudresult.created ?? 0;
                    updatecounter = updatecounter + pgcrudresult.updated ?? 0;
                }

                WriteLog.LogToConsole(
                    returnid,
                    "dataimport",
                    "single.digiway",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "digiway." + identifier,
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
                    "single.digiway",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "digiway." + identifier,
                        success = false,
                        error = "digiway " + identifier + " could not be parsed",
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
            ODHActivityPoiLinked data,
            KeyValuePair<string, GeoJsonFeature> digiwaydata
        )
        {
            var rawdataid = await InsertInRawDataDB(digiwaydata);

            data.Id = data.Id?.ToLower();

            //Set LicenseInfo
            data.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<ODHActivityPoiLinked>(
                data,
                Helper.LicenseHelper.GetLicenseforOdhActivityPoi
            );

            //PublishedOnInfo?

            var pgcrudresult = await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                data,
                new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                new EditInfo("digiway." + identifier + ".import", importerURL),
                new CRUDConstraints(),
                new CompareConfig(true, false),
                rawdataid
            );

            return pgcrudresult;
        }

        //Inserting into DB
        private async Task<PGCRUDResult> InsertDataToSpatialDataDB(
            SpatialData data
        )
        {
            data.Id = data.Id?.ToLower();

            //Set LicenseInfo
            data.LicenseInfo = new LicenseInfo() { ClosedData = false, License = "CC0" };

            //PublishedOnInfo?

            var pgcrudresult = await QueryFactory.UpsertData<SpatialData>(
                 new UpsertableSpatialData(data),
                 new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                 new EditInfo("digiway." + identifier + ".import", importerURL),
                 new CRUDConstraints(),
                 new CompareConfig(true, false)
             );

            return pgcrudresult;
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, GeoJsonFeature> data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "digiway",
                    rawformat = settings.DigiWayConfig[identifier].Format,
                    importdate = DateTime.Now,
                    license = "open",
                    sourceinterface = identifier,
                    sourceurl = settings.DigiWayConfig[identifier].ServiceUrl,
                    type = "odhactivitypoi",
                    sourceid = data.Key,
                    raw = data.Value.ToString(), //JsonConvert.SerializeObject(data.Value) Causes Exception self reference looping
                }
            );
        }

        //Parse the interface content
        public async Task<(ODHActivityPoiLinked?, GeoShapeJson?)> ParseDigiWayDataToODHActivityPoi(
            string odhid,
            GeoJsonFeature input
        )
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var dataindb = await query.GetObjectSingleAsync<ODHActivityPoiLinked>();

            var result = ParseDServices3ArcgisGeoJsonDataToODHActivityPoi.ParseToODHActivityPoi(dataindb, input, identifier, source,srid);

            return result;
        }

        //Parse the interface content
        public async Task<SpatialData> ParseDigiWayDataToSpatialData(
            string odhid,
            GeoJsonFeature input
        )
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var dataindb = await query.GetObjectSingleAsync<SpatialData>();

            var result = ParseDServices3ArcgisGeoJsonDataToSpatialData.ParseToSpatialData(dataindb, input, identifier, source, srid);

            return result;
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
                var idlistdb = await GetAllDataBySource(new List<string>() { source }, new List<string>() { source + "." + identifier.ToLower() });

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
                    "deactivate.digiway",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "digiway." + identifier,
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
