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
        public string? source { get; set; }

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
            if (identifier == null || source == null)
                throw new Exception("no identifier|source defined");
            
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
                returnid = digiwaydata.id.ToLower();

                idlistinterface.Add(returnid);

                //Parse  Data
                var parsedobject = await ParseDigiWayDataToODHActivityPoi(
                    returnid, 
                    digiwaydata
                );
                if (parsedobject.Item1 == null || parsedobject.Item2 == null)
                    throw new Exception();

                //var pgcrudshaperesult = await InsertDataInShapesDB(parsedobject.Item2);
                var pgcrudshaperesult = await GeoShapeInsertHelper.InsertDataInShapesDB(QueryFactory, parsedobject.Item2, source, "32632");


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
        private async Task<int> InsertInRawDataDB(KeyValuePair<string, IGeoServerCivisData> data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "digiway",
                    rawformat = "json",
                    importdate = DateTime.Now,
                    license = "open",
                    sourceinterface = source + "." + identifier,
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

            var result = ParseGeoServerDataToODHActivityPoi.ParseToODHActivityPoi(dataindb, input, identifier, source);

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
                    //since the id is not the same delete all old 
                    if (source == "civis.geoserver")
                    {
                        //Delete Data
                        var result = await DeleteOrDisableData<ODHActivityPoiLinked>(idtodelete, true);
                        //Delete Gps Data
                        var result2 = await GeoShapeInsertHelper.DeleteFromShapesDB(QueryFactory, idtodelete);

                        deleteresult = deleteresult + result.Item2 + result2;
                    }
                    //else simply deactivate
                    else
                    {
                        var result = await DeleteOrDisableData<ODHActivityPoiLinked>(idtodelete, false);

                        updateresult = updateresult + result.Item1;
                        deleteresult = deleteresult + result.Item2;
                    }
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
