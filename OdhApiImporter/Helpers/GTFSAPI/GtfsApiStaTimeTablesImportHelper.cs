// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;
using DIGIWAY;
using GTFSAPI;
using Helper;
using Helper.Generic;
using LTSAPI.Parser;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using SqlKata;
using SqlKata.Execution;
using SqlKata.Extensions;
using Helper.Tagging;
using Helper.Location;

namespace OdhApiImporter.Helpers
{
    public class GtfsApiStaTimeTablesImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistfrominterface { get; set; }

        public GtfsApiStaTimeTablesImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL)
        {
            idlistfrominterface = new List<string>();
        }

        public async Task<IEnumerable<UpdateDetail>> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            var data = await GetData(cancellationToken);

            ////UPDATE all data
            var updateresult = await ImportData(data, cancellationToken);

            //Disable Data not in list
            var deleteresult = await SetDataNotinListToInactive(cancellationToken);

            return new List<UpdateDetail>() { updateresult, deleteresult };
        }

        //Get Data from Source
        private async Task<List<StaTimeTableStopsCsv>> GetData(CancellationToken cancellationToken)
        {            
            return await GetTimeTablesData.GetTimeTablesDataAsync("", "", settings.GTFSApiConfig["StaTimetables"].ServiceUrl);
        }

        //Import the Data
        public async Task<UpdateDetail> ImportData(
            List<StaTimeTableStopsCsv> datalist,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (
                datalist != null               
            )
            {
                //loop trough items
                foreach (
                    var stadata in datalist
                )
                {                    
                        var importresult = await ImportDataSingle(stadata);

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
        public async Task<UpdateDetail> ImportDataSingle(StaTimeTableStopsCsv data)
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            //id
            string returnid = "";

            try
            {
                returnid = data.stop_id.ToLower();

                idlistfrominterface.Add(returnid);

                //Parse  Data
                var parsedobject = await ParseStaTimeTableStopsDataToODHActivityPoi(
                    returnid, 
                    data
                );
                               
                //Save parsedobject to DB + Save Rawdata to DB
                var pgcrudresult = await InsertDataToDB(
                    parsedobject,
                    new KeyValuePair<string, StaTimeTableStopsCsv>(returnid, data)
                );

                newcounter = newcounter + pgcrudresult.created ?? 0;
                updatecounter = updatecounter + pgcrudresult.updated ?? 0;

                WriteLog.LogToConsole(
                    returnid,
                    "dataimport",
                    "single.gtfsapi",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "gtfsapi.statimetablesstops",
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
                    "single.gtfsapi",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "gtfsapi.statimetablesstops",
                        success = false,
                        error = "gtfsapi statimetablesstops could not be parsed",
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
            ODHActivityPoiLinked data,
            KeyValuePair<string, StaTimeTableStopsCsv> stadata
        )
        {
            var rawdataid = await InsertInRawDataDB(stadata);

            data.Id = data.Id?.ToLower();

            //Set LicenseInfo
            data.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<ODHActivityPoiLinked>(
                data,
                Helper.LicenseHelper.GetLicenseforOdhActivityPoi
            );

            data.TagIds = new List<string>();
            data.Tags = new List<Tags>();

            //search Tag and add its ID
            await data.AddTagsByNameExtension("Public Tansport Stops", 
                null, 
                new List<string>() { "sta" }, 
                QueryFactory);

            //TODO Add the LTS Tags for Mobility / Bus Stops etc..? 


            //Create Tags not needed because of extension used
            //await data.UpdateTagsExtension(QueryFactory);

            //DistanceCalculation only if not present
            if(data.DistanceInfo == null)
                await data.UpdateDistanceCalculation(QueryFactory);
            
            //PublishedOn Info set to sta
            data.PublishedOn = new List<string>() { "sta" };

            var pgcrudresult = await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                data,
                new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                new EditInfo("gtfsapi.statimetablesstops.import", importerURL),
                new CRUDConstraints(),
                new CompareConfig(true, false),
                rawdataid
            );

            return pgcrudresult;
        }
    
        private async Task<int> InsertInRawDataDB(KeyValuePair<string, StaTimeTableStopsCsv> data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "gtfsapi",
                    rawformat = "json",
                    importdate = DateTime.Now,
                    license = "open",
                    sourceinterface = "statimetablesstops",
                    sourceurl = settings.GTFSApiConfig["StaTimetables"].ServiceUrl,
                    type = "odhactivitypoi",
                    sourceid = data.Key,
                    raw = data.Value.ToString(),
                }
            );
        }

        //Parse the interface content
        public async Task<ODHActivityPoiLinked?> ParseStaTimeTableStopsDataToODHActivityPoi(
            string odhid,
            StaTimeTableStopsCsv input
        )
        {
            //Get the ODH Item
            var query = QueryFactory.Query(table).Select("data").Where("id", odhid);

            var dataindb = await query.GetObjectSingleAsync<ODHActivityPoiLinked>();

            var result = ParseGtfsApi.ParseStaTimeTableStopsToODHActivityPoi(dataindb, input);

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
                var idlistdb = await GetAllDataBySource(new List<string>() { "sta" }, new List<string>() { "gtfsapi" });

                var idstodelete = idlistdb.Where(p => !idlistfrominterface.Any(p2 => p2 == p));

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
                    "deactivate.gtfsapi",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "gtfsapi.statimetablesstops",
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
