// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DIGIWAY;
using Helper;
using Helper.Generic;
using Helper.Tagging;
using Newtonsoft.Json;
using SqlKata;
using SqlKata.Execution;
using SqlKata.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers
{
    public class DigiWayImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }
        public string? identifier { get; set; }

        public DigiWayImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL)
        {
            idlistinterface = new List<string>();
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            if (identifier == null)
                throw new Exception("no identifier defined");

            var data = await GetData(cancellationToken);

            ////UPDATE all data
            var updateresult = await ImportData(data, cancellationToken);

            //Disable Data not in list
            var deleteresult = await SetDataNotinListToInactive(cancellationToken);

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }

        //Get Data from Source
        private async Task<IGeoserverCivisResult> GetData(CancellationToken cancellationToken)
        {
            return await GetDigiwayData.GetDigiWayDataAsync("", "", settings.DigiWayConfig[identifier].ServiceUrl, identifier);
        }

        //Import the Data
        public async Task<UpdateDetail> ImportData(
            IGeoserverCivisResult digiwaydatalist,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (digiwaydatalist != null && digiwaydatalist.features != null)
            {                
                foreach (var digiwaydata in digiwaydatalist.features)
                {
                    var importresult = await ImportDataSingle(digiwaydata);

                    newcounter = newcounter + importresult.created ?? newcounter;
                    updatecounter = updatecounter + importresult.updated ?? updatecounter;
                    errorcounter = errorcounter + importresult.error ?? errorcounter;
                }
                

                //if (identifier == "cyclewaystyrol")
                //{
                //    if ((digiwaydatalist as GeoserverCivisResultCycleWay).features != null)
                //    {
                //        foreach (var digiwaydata in (digiwaydatalist as GeoserverCivisResultCycleWay).features)
                //        {
                //            var importresult = await ImportDataSingle(digiwaydata);

                //            newcounter = newcounter + importresult.created ?? newcounter;
                //            updatecounter = updatecounter + importresult.updated ?? updatecounter;
                //            errorcounter = errorcounter + importresult.error ?? errorcounter;
                //        }
                //    }
                //}
                //else if (identifier == "mountainbikeroutes")
                //{
                //    if ((digiwaydatalist as GeoserverCivisResultMountainbike).features != null)
                //    {
                //        foreach (var digiwaydata in (digiwaydatalist as GeoserverCivisResultMountainbike).features)
                //        {
                //            var importresult = await ImportDataSingle(digiwaydata);

                //            newcounter = newcounter + importresult.created ?? newcounter;
                //            updatecounter = updatecounter + importresult.updated ?? updatecounter;
                //            errorcounter = errorcounter + importresult.error ?? errorcounter;
                //        }
                //    }
                //}
                //else if (identifier == "hikingtrails")
                //{
                //if ((digiwaydatalist as GeoserverCivisResultHikingTrail).features != null)
                //{
                //    foreach (var digiwaydata in (digiwaydatalist as GeoserverCivisResultHikingTrail).features)
                //    {
                //        var importresult = await ImportDataSingle(digiwaydata);

                //        newcounter = newcounter + importresult.created ?? newcounter;
                //        updatecounter = updatecounter + importresult.updated ?? updatecounter;
                //        errorcounter = errorcounter + importresult.error ?? errorcounter;
                //    }
                //}
                //}
                //else if (identifier == "intermunicipalcyclingroutes")
                //{
                //    if ((digiwaydatalist as GeoserverCivisResultIntermunicipalPaths).features != null)
                //    {
                //        foreach (var digiwaydata in (digiwaydatalist as GeoserverCivisResultIntermunicipalPaths).features)
                //        {
                //            var importresult = await ImportDataSingle(digiwaydata);

                //            newcounter = newcounter + importresult.created ?? newcounter;
                //            updatecounter = updatecounter + importresult.updated ?? updatecounter;
                //            errorcounter = errorcounter + importresult.error ?? errorcounter;
                //        }
                //    }
                //}
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
        public async Task<UpdateDetail> ImportDataSingle(IGeoServerCivisData digiwaydata)
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            //id
            string returnid = "";

            try
            {
                returnid = digiwaydata.id;

                idlistinterface.Add(returnid);

                //Parse  Data
                var parsedobject = await ParseDigiWayDataToODHActivityPoi(
                    returnid, 
                    digiwaydata
                );
                if (parsedobject.Item1 == null || parsedobject.Item2 == null)
                    throw new Exception();

                var pgcrudshaperesult = await InsertDataInShapesDB(parsedobject.Item2);

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
                    new KeyValuePair<string, IGeoServerCivisData>(returnid, digiwaydata)
                );

                newcounter = newcounter + pgcrudresult.created ?? 0;
                updatecounter = updatecounter + pgcrudresult.updated ?? 0;

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
            KeyValuePair<string, IGeoServerCivisData> digiwaydata
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

        private async Task<PGCRUDResult> InsertDataInShapesDB(
          GeoShapeJson data
      )
        {
            try
            {                
                //Set LicenseInfo
                data.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<GeoShapeJson>(
                    data,
                    Helper.LicenseHelper.GetLicenseforGeoShape
                );

                //Set Meta
                data._Meta = MetadataHelper.GetMetadataobject<GeoShapeJson>(data);

                //Check if data is there by Name
                var shapeid = await QueryFactory.Query("geoshapes").Select("id").Where("id", data.Id.ToLower()).FirstOrDefaultAsync<string>();

                int insert = 0;
                int update = 0;

                PGCRUDResult result = default(PGCRUDResult);
                if (String.IsNullOrEmpty(shapeid))
                {                                                            
                    insert = await QueryFactory
                   .Query("geoshapes")
                   .InsertAsync(new GeoShapeDB<UnsafeLiteral>()
                   {
                       id = data.Id.ToLower(),
                       licenseinfo = new JsonRaw(data.LicenseInfo),
                       meta = new JsonRaw(data._Meta),
                       mapping = new JsonRaw(data.Mapping),
                       name = data.Name,
                       country = data.Country,
                       type = data.Type,
                       source = "civis.geoserver",
                       srid = "32632",                       
                       //geom = new PGGeometryRaw("ST_GeometryFromText('" + data.Geometry + "', 32632)"),                       
                       geometry = new UnsafeLiteral("ST_GeometryFromText('" + data.Geometry.ToString() + "', 32632)", false),
                       //geojson = new UnsafeLiteral("ST_AsGeoJSON(ST_Transform(ST_GeometryFromText('" + data.Geometry.ToString() + "', 32632),4326))", false),
                   });
                }
                else
                {
                    update = await QueryFactory
                   .Query("geoshapes")
                   .Where("id", data.Id.ToLower())
                   .UpdateAsync(new GeoShapeDB<UnsafeLiteral>()
                   {
                       id = data.Id.ToLower(),
                       licenseinfo = new JsonRaw(data.LicenseInfo),
                       meta = new JsonRaw(data._Meta),
                       mapping = new JsonRaw(data.Mapping),
                       name = data.Name,
                       country = data.Country,
                       type = data.Type,
                       source = "civis.geoserver",
                       srid = "32632",
                       //geom = new PGGeometryRaw("ST_GeometryFromText('" + data.Geometry + "', 32632)"),                       
                       geometry = new UnsafeLiteral("ST_GeometryFromText('" + data.Geometry.ToString() + "', 32632)", false),
                       //geojson = new UnsafeLiteral("ST_AsGeoJSON(ST_Transform(ST_GeometryFromText('" + data.Geometry.ToString() + "', 32632),4326))", false),
                   });
                }

                return new PGCRUDResult()
                {
                    id = data.Id,
                    odhtype = data._Meta.Type,
                    created = insert,
                    updated = update,
                    deleted = 0,
                    error = 0,
                    errorreason = null,
                    operation = "insert shape",
                    changes = null,
                    compareobject = false,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    pushchannels = null,
                };
            }
            catch (Exception ex)
            {
                return new PGCRUDResult()
                {
                    id = "",
                    odhtype = data._Meta.Type,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    errorreason = ex.Message,
                    operation = "insert shape",
                    changes = null,
                    compareobject = false,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    pushchannels = null,
                };
            }
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, IGeoServerCivisData> data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "digiway",
                    rawformat = "xml",
                    importdate = DateTime.Now,
                    license = "open",
                    sourceinterface = identifier,
                    sourceurl = settings.DigiWayConfig[identifier].ServiceUrl,
                    type = "odhactivitypoi",
                    sourceid = data.Key,
                    raw = data.Value.ToString(),
                }
            );
        }

        //Parse the interface content
        public async Task<(ODHActivityPoiLinked?, GeoShapeJson?)> ParseDigiWayDataToODHActivityPoi(
            string odhid,
             IGeoServerCivisData input
        )
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var dataindb = await query.GetObjectSingleAsync<ODHActivityPoiLinked>();

            var result = ParseGeoServerDataToODHActivityPoi.ParseToODHActivityPoi(dataindb, input, identifier);

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
                var idlistdb = await GetAllDataBySource(new List<string>() { "digiway", "" });

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
