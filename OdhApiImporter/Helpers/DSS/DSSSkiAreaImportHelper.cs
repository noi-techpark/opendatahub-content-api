// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;
using DSS;
using Helper;
using Helper.Generic;
using Newtonsoft.Json;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers.DSS
{
    public class DSSSkiAreaImportHelper : ImportHelper, IImportHelper
    {
        public DSSSkiAreaImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

        public async Task<IEnumerable<UpdateDetail>> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //GET DATA
            var dssdata = await GetData(cancellationToken);

            //Import Data to rawtable and odh TODO ADD THE Other types
            var updateresult = await ImportData(dssdata, cancellationToken);

            return new List<UpdateDetail>() { updateresult };
        }

        //Imports DSS Data
        private async Task<List<dynamic>> GetData(CancellationToken cancellationToken)
        {
            List<dynamic> dssdata = new List<dynamic>();

            //Get DSS data
            dssdata.Add(
                await GetDSSData.GetDSSDataAsync(
                    DSSRequestType.skiresorts,
                    settings.DSSConfig.User,
                    settings.DSSConfig.Password,
                    settings.DSSConfig.ServiceUrl
                )
            );

            return dssdata;
        }

        public async Task<UpdateDetail> ImportData(
            List<dynamic> dssinput,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (dssinput != null && dssinput.Count > 0)
            {
                //loop trough dss items
                foreach (var item in dssinput[0].items)
                {
                    var importresult = await ImportDataSingle(item);

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

        public async Task<UpdateDetail> ImportDataSingle(dynamic item)
        {
            int updatecounter = 0;
            int errorcounter = 0;

            string skiarearid = "";

            try
            {
                skiarearid = item.rid;

                //Check if we have one or more skiareas with this rid
                //Get the ODH Item
                var mydssquery = QueryFactory
                    .Query(table)
                    .Select("data")
                    .WhereRaw("data#>>'\\{Mapping,dss,rid\\}' like $$", skiarearid);

                var skiareas = await mydssquery.GetObjectListAsync<SkiAreaLinked>();

                foreach (var skiarea in skiareas)
                {
                    //Update the Openingtimes
                    if (item["season-winter"] != null)
                    {
                        string seasonwinterstart = item["season-winter"]["start"];
                        string seasonwinterend = item["season-winter"]["end"];

                        double.TryParse(seasonwinterstart, out double seasonwinterstartdb);
                        double.TryParse(seasonwinterend, out double seasonwinterenddb);

                        OperationSchedule operationSchedule = new OperationSchedule();
                        operationSchedule.Start = Helper.DateTimeHelper.UnixTimeStampToDateTime(
                            seasonwinterstartdb
                        );
                        operationSchedule.Stop = Helper.DateTimeHelper.UnixTimeStampToDateTime(
                            seasonwinterenddb
                        );
                        operationSchedule.OperationscheduleName = new Dictionary<string, string>()
                        {
                            { "de", "Wintersaison" },
                            { "it", "stagione invernale" },
                            { "en", "winter season" },
                        };
                        operationSchedule.OperationScheduleTime = new List<OperationScheduleTime>();
                        operationSchedule.Type = "1";

                        skiarea.OperationSchedule = new List<OperationSchedule>()
                        {
                            operationSchedule,
                        };

                        //Save parsedobject to DB + Save Rawdata to DB
                        var pgcrudresult = await InsertDataToDB(
                            skiarea,
                            new KeyValuePair<string, dynamic>(skiarearid, item)
                        );

                        updatecounter = updatecounter + pgcrudresult.updated ?? 0;
                    }
                    else
                        errorcounter = errorcounter + 1;
                }

                WriteLog.LogToConsole(
                    skiarearid,
                    "dataimport",
                    "single.dss.skiarea",
                    new ImportLog()
                    {
                        sourceid = skiarearid,
                        sourceinterface = "dss.skiarea",
                        success = true,
                        error = "",
                    }
                );
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    skiarearid,
                    "dataimport",
                    "single.dss.skiarea",
                    new ImportLog()
                    {
                        sourceid = skiarearid,
                        sourceinterface = "dss.skiarea",
                        success = false,
                        error = "skiarea could not be parsed",
                    }
                );

                errorcounter = errorcounter + 1;
            }

            return new UpdateDetail()
            {
                created = 0,
                updated = updatecounter,
                deleted = 0,
                error = errorcounter,
            };
        }

        private async Task<UpdateDetail> InsertDataToDB(
            SkiAreaLinked skiarea,
            KeyValuePair<string, dynamic> dssdata
        )
        {
            //var rawdataid = await InsertInRawDataDB(dssdata);

            return await QueryFactory.UpsertData<SkiAreaLinked>(
                skiarea,
                new DataInfo(table, CRUDOperation.Update),
                new EditInfo("dss.skiarea.import", importerURL),
                new CRUDConstraints(),
                new CompareConfig(true, false)
            );
        }

        //private async Task<int> InsertInRawDataDB(KeyValuePair<string, dynamic> dssdata)
        //{
        //    return await QueryFactory.InsertInRawtableAndGetIdAsync(
        //                new RawDataStore()
        //                {
        //                    datasource = "dss",
        //                    importdate = DateTime.Now,
        //                    raw = JsonConvert.SerializeObject(dssdata.Value),
        //                    sourceinterface = "webcam",
        //                    sourceid = dssdata.Key,
        //                    sourceurl = settings.DSSConfig.ServiceUrl,
        //                    type = "webcam",
        //                    license = "open",
        //                    rawformat = "json"
        //                });
        //}
    }
}
