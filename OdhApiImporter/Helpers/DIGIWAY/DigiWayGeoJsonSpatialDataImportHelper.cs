// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DIGIWAY;
using DIGIWAY.Model;
using DIGIWAY.Model.GeoJsonReadModel;
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
    public class DigiWayGeoJsonSpatialDataImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }
        public string? identifier { get; set; }
        public string? source { get; set; }

        public string? srid { get; set; }

        public DigiWayGeoJsonSpatialDataImportHelper(
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
            if (identifier == null || source == null || srid == null)
                throw new Exception("no identifier|source|srid defined");

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
        private async Task<ICollection<GeoJsonFeature>> GetData(CancellationToken cancellationToken)
        {
            return await GetDigiwayData.GetDigiWayGeoJsonDataFromFileAsync("", "", settings.DigiWayConfig[identifier].ServiceUrl, false);
        }

        //Import the Data
        public async Task<UpdateDetail> ImportData(
            ICollection<GeoJsonFeature> featurelist,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (featurelist != null)
            {                
                foreach (var digiwaydata in featurelist)
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
                //Parse  Data
                var parsedobject = await ParseDigiWayDataToSpatialData(
                    returnid,
                    digiwaydata
                );
                if (parsedobject == null)
                    throw new Exception();

                returnid = parsedobject.Id;
                idlistinterface.Add(returnid);
                
                //Create Tags
                //await parsedobject.UpdateTagsExtension(QueryFactory);

                //Save parsedobject to DB + Save Rawdata to DB
                var pgcrudresult = await InsertDataToDB(
                    parsedobject,
                    new KeyValuePair<string, GeoJsonFeature>(returnid, digiwaydata)
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
            SpatialData data,
            KeyValuePair<string, GeoJsonFeature> digiwaydata
        )
        {
            //Deactivate Rawdatainsert for now
            //var rawdataid = await InsertInRawDataDB(digiwaydata);

            data.Id = data.Id?.ToLower();
            data._Meta = new Metadata() { Id = data.Id, Reduced = false, Source = data.Source, LastUpdate = DateTime.Now, Type = "spatialdata" };

            //Set LicenseInfo
            data.LicenseInfo = new LicenseInfo() { ClosedData = false, License = "CC0" };
            //    Helper.LicenseHelper.GetLicenseInfoobject<SpatialData>(
            //    data,
            //    Helper.LicenseHelper.GetLicenseforSpatialData
            //);

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
  
        //private async Task<int> InsertInRawDataDB(KeyValuePair<string, GeoJsonFeature> data)
        //{
        //    return await QueryFactory.InsertInRawtableAndGetIdAsync(
        //        new RawDataStore()
        //        {
        //            datasource = "digiway",
        //            rawformat = settings.DigiWayConfig[identifier].Format,
        //            importdate = DateTime.Now,
        //            license = "open",
        //            sourceinterface = identifier,
        //            sourceurl = settings.DigiWayConfig[identifier].ServiceUrl,
        //            type = "odhactivitypoi",
        //            sourceid = data.Key,
        //            raw = data.Value.ToString(),
        //        }
        //    );
        //}

        //Parse the interface content
        public async Task<SpatialData?> ParseDigiWayDataToSpatialData(
            string odhid,
            GeoJsonFeature input
        )
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var dataindb = await query.GetObjectSingleAsync<SpatialData>();

            var result = ParseGeoJsonDataToSpatialData.ParseToSpatialData(dataindb, input, identifier, source, srid);

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
