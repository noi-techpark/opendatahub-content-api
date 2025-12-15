// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using LOOPTEC;
using Newtonsoft.Json;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.LOOPTEC
{
    public class LooptecEjobsImportHelper : ImportHelper, IImportHelper
    {
        public LooptecEjobsImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        public async Task<Tuple<int, int>> DeleteOrDisableData(string id, bool delete)
        {
            throw new NotImplementedException();
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //GET Data and Deserialize to Json
            var data = await GetEJobsData.GetEjobsDataAsync(
                "",
                "",
                settings.LoopTecConfig.ServiceUrl
            );

            var newcounter = 0;

            if (data != null)
            {
                //Save to RAWTABLE
                foreach (var ejob in data.jobs)
                {
                    var rawdataid = await InsertInRawDataDB(ejob);
                    newcounter++;

                    //Because a dynamic is passed to the method a dynamic is returned also if int is defined!!! strange behavior of c#
                    string rawdataidstr = rawdataid.ToString();

                    WriteLog.LogToConsole(
                        rawdataidstr,
                        "dataimport",
                        "single.ejob",
                        new ImportLog()
                        {
                            sourceid = rawdataidstr,
                            sourceinterface = "looptec.ejob",
                            success = true,
                            error = "",
                        }
                    );
                }
            }

            return new UpdateDetail()
            {
                created = newcounter,
                updated = 0,
                deleted = 0,
                error = 0,
            };
        }

        private async Task<int> InsertInRawDataDB(dynamic ejob)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "looptec",
                    rawformat = "json",
                    importdate = DateTime.Now,
                    license = "open",
                    sourceinterface = "ejobs-onboard",
                    sourceurl = settings.LoopTecConfig.ServiceUrl,
                    type = "ejob",
                    sourceid = ejob.identifier,
                    raw = JsonConvert.SerializeObject(ejob),
                }
            );
        }
    }
}
