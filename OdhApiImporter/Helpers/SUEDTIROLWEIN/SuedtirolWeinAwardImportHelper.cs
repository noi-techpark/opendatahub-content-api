// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;
using Helper;
using Helper.Generic;
using SqlKata.Execution;
using SuedtirolWein;
using SuedtirolWein.Parser;

namespace OdhApiImporter.Helpers.SuedtirolWein
{
    public class SuedtirolWeinAwardImportHelper : ImportHelper, IImportHelper
    {
        public SuedtirolWeinAwardImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            var winegastrolist = await ImportList(cancellationToken);

            var updateresult = default(UpdateDetail);  //await ImportData(winegastrolist, cancellationToken);

            var deleteresult = await SetDataNotinListToInactive(winegastrolist, cancellationToken);

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }

        public async Task<IDictionary<string, XDocument>> ImportList(
            CancellationToken cancellationToken = default
        )
        {
            IDictionary<string, XDocument> mywinedata = new Dictionary<string, XDocument>();

            List<string> languagestoretrieve = new List<string>() { "de", "it", "en" };

            foreach (var language in languagestoretrieve)
            {
                mywinedata.Add(language, await GetSuedtirolWeinData.GetSuedtirolWineAwardsAsync(
                settings.SuedtirolWeinConfig.ServiceUrl,
                language
            ));
            }

            return mywinedata;
        }

        private async Task<UpdateDetail> ImportData(
            IDictionary<string, XDocument> wineddatalist,
            CancellationToken cancellationToken = default
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            foreach (
                var winedata in wineddatalist["de"].Root?.Elements("item")
                    ?? Enumerable.Empty<XElement>()
            )
            {
                var importresult = await ImportDataSingle(winedata, wineddatalist);

                newcounter = newcounter + importresult.created ?? newcounter;
                updatecounter = updatecounter + importresult.updated ?? updatecounter;
                errorcounter = errorcounter + importresult.error ?? errorcounter;
            }

            return new UpdateDetail()
            {
                created = newcounter,
                updated = updatecounter,
                deleted = 0,
                error = errorcounter,
            };
        }

        public async Task<UpdateDetail> ImportDataSingle(
            XElement winedata,
            IDictionary<string, XDocument> winedatalist
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            string dataid = winedata.Element("id").Value;

            try
            {
                var winedatait = winedatalist["it"]
                    .Root.Elements("item")
                    .Where(x => x.Element("id").Value == dataid)
                    .FirstOrDefault();
                var winedataen = winedatalist["en"]
                    .Root.Elements("item")
                    .Where(x => x.Element("id").Value == dataid)
                    .FirstOrDefault();

                List<string> haslanguage = new List<string>() { "de" };
                if (winedatait != null)
                    haslanguage.Add("it");
                if (winedataen != null)
                    haslanguage.Add("en");

                //Get Wein Award                
                var weinaward = await LoadDataFromDB<WineLinked>(dataid.ToUpper(), IDStyle.lowercase);
                
                if (weinaward == null)
                {
                    weinaward = new WineLinked();
                    weinaward.FirstImport = DateTime.Now;
                }

                weinaward = ParseAwardData.ParsetheAwardData(
                    weinaward,
                    winedata,
                    winedatait,
                    winedataen,
                    haslanguage
                );

                //Add nullchecks field was removed
                if (winedata.Element("active") != null)
                {
                    weinaward.Active = Convert.ToBoolean(winedata.Element("active").Value);
                    weinaward.SmgActive = Convert.ToBoolean(winedata.Element("active").Value);
                }

                weinaward.HasLanguage = haslanguage;

                //Setting Common Infos
                weinaward.Source = "suedtirolwein";                
                weinaward.LastChange = DateTime.Now;

                //ADD MAPPING
                var suedtirolweinid = new Dictionary<string, string>() { { "id", dataid } };
                weinaward.Mapping.TryAddOrUpdate("suedtirolwein", suedtirolweinid);

                var result = await InsertDataToDB(
                    weinaward,
                    new KeyValuePair<string, XElement>(dataid, winedata)
                );
                newcounter = newcounter + result.created ?? 0;
                updatecounter = updatecounter + result.updated ?? 0;

                WriteLog.LogToConsole(
                    dataid,
                    "dataimport",
                    "single.suedtirolweinaward",
                    new ImportLog()
                    {
                        sourceid = dataid,
                        sourceinterface = "suedtirolwein.award",
                        success = true,
                        error = "",
                    }
                );
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    dataid,
                    "dataimport",
                    "single.suedtirolweinaward",
                    new ImportLog()
                    {
                        sourceid = dataid,
                        sourceinterface = "suedtirolwein.award",
                        success = false,
                        error = ex.Message,
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

        private async Task<UpdateDetail> SetDataNotinListToInactive(
            IDictionary<string, XDocument> wineawarddatalist,
            CancellationToken cancellationToken
        )
        {
            int updateresult = 0;
            int deleteresult = 0;
            int errorresult = 0;

            try
            {
                //The service returns always the same ids in each language
                List<string?> wineidlistonsource =
                    wineawarddatalist["de"]
                        .Root?.Elements("item")
                        .Select(x => x.Element("id")?.Value)
                        .ToList() ?? new();

                var myquery = QueryFactory.Query("wines").Select("id");
                var winesondb = await myquery.GetAsync<string>();

                var idstodelete = winesondb.Where(p => !wineidlistonsource.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var result = await DeleteOrDisableData<WineLinked>(idtodelete, false, true);

                    updateresult = updateresult + result.Item1;
                    deleteresult = deleteresult + result.Item2;
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.suedtirolweinaward",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "suedtirolwein.award",
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

        private async Task<PGCRUDResult> InsertDataToDB(
            WineLinked wineaward,
            KeyValuePair<string, XElement> suedtirolweindata
        )
        {
            wineaward.Id = wineaward.Id?.ToUpper();

            //Set LicenseInfo
            wineaward.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<WineLinked>(
                wineaward,
                Helper.LicenseHelper.GetLicenseforWineAward
            );

            //Set Publishedon List
            wineaward.CreatePublishedOnList<WineLinked>();

            var rawdataid = await InsertInRawDataDB(suedtirolweindata);

            //TODO Add column rawdataid
            return await QueryFactory.UpsertData<WineLinked>(
                wineaward,
                new DataInfo("wines", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("suedtirolwein.weinaward.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid
            );
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, XElement> suedtirolweindata)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "suedtirolwein",
                    importdate = DateTime.Now,
                    raw = suedtirolweindata.Value.ToString(),
                    sourceinterface = "suedtirolwein-award",
                    sourceid = suedtirolweindata.Key,
                    sourceurl = "https://suedtirolwein.secure.consisto.net/awards.ashx",
                    type = "wineaward",
                    license = "open",
                    rawformat = "xml",
                }
            );
        }
    }
}
