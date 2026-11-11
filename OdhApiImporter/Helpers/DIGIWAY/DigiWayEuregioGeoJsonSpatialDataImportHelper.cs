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
using OdhNotifier;
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
    public class DigiWayEuregioGeoJsonSpatialDataImportHelper : ImportHelper, IImportHelper
    {
        public HashSet<string> idlistinterface { get; set; }
        //id -> signature (geometry + all attributes) of the features imported in this run
        private readonly Dictionary<string, string> importedfeatures = new Dictionary<string, string>();
        public string? identifier { get; set; }
        public string? source { get; set; }
        public string? srid { get; set; }

        public DigiWayEuregioGeoJsonSpatialDataImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier)
        {
            idlistinterface = new HashSet<string>();
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
            var importresult = await ImportData(data, cancellationToken);
            resultlist.Add(importresult);

            //Deactivate data no longer delivered by the source (only after a complete import without errors,
            //otherwise a partial or failed download would deactivate valid records)
            if (data != null && data.Count > 0 && importresult.error == 0)
                resultlist.Add(await SetDataNotinListToInactive(cancellationToken));

            return GenericResultsHelper.MergeUpdateDetail(
                resultlist
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
                featurelist = RemoveOlderReleaseDuplicates(featurelist);

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

        /// <summary>
        /// The source delivers some data (e.g. Trentino) in several releases at once, distinguished only by DATA_STATUS.
        /// Features with identical geometry and identical attributes (apart from DATA_STATUS) are older-release copies:
        /// only the one with the newest DATA_STATUS is kept. Features without a parseable DATA_STATUS are never skipped.
        /// </summary>
        public static ICollection<GeoJsonFeature> RemoveOlderReleaseDuplicates(ICollection<GeoJsonFeature> featurelist)
        {
            const string datastatuskey = "DATA_STATUS";

            static DateTime? GetDataStatus(GeoJsonFeature feature) =>
                feature.Attributes.TryGetValue(datastatuskey, out var value) &&
                value != null &&
                DateTime.TryParse(value.ToString(), out var datastatus)
                    ? datastatus
                    : null;

            static string GetReleaseIndependentKey(GeoJsonFeature feature) =>
                feature.Geometry?.AsText() + "|" + string.Join("|",
                    feature.Attributes
                        .Where(a => a.Key != datastatuskey)
                        .OrderBy(a => a.Key, StringComparer.Ordinal)
                        .Select(a => a.Key + "=" + a.Value?.ToString()));

            var newestperkey = featurelist
                .Select(feature => (feature, datastatus: GetDataStatus(feature)))
                .Where(f => f.datastatus != null)
                .GroupBy(f => GetReleaseIndependentKey(f.feature))
                .Where(g => g.Select(f => f.datastatus).Distinct().Count() > 1)
                .ToDictionary(g => g.Key, g => g.Max(f => f.datastatus));

            if (newestperkey.Count == 0)
                return featurelist;

            var result = featurelist
                .Where(feature =>
                {
                    var datastatus = GetDataStatus(feature);
                    return datastatus == null ||
                        !newestperkey.TryGetValue(GetReleaseIndependentKey(feature), out var newest) ||
                        datastatus == newest;
                })
                .ToList();

            WriteLog.LogToConsole(
                "",
                "dataimport",
                "skipolderrelease.digiway",
                new ImportLog()
                {
                    sourceid = "",
                    sourceinterface = "digiway.euregio",
                    success = true,
                    error = $"skipped {featurelist.Count - result.Count} features already present in a newer {datastatuskey}",
                }
            );

            return result;
        }

        /// <summary>
        /// The stable id is a hash of geometry and identifying attributes. Returns null if an identical feature
        /// (geometry + all attributes) was already imported in this run, otherwise the id, with a counter appended
        /// if a different feature produced the same id.
        /// </summary>
        private string? GetUniqueId(string id, GeoJsonFeature digiwaydata)
        {
            var signature = digiwaydata.Geometry?.AsText() + "|" + string.Join("|",
                digiwaydata.Attributes
                    .OrderBy(a => a.Key, StringComparer.Ordinal)
                    .Select(a => a.Key + "=" + a.Value?.ToString()));

            var uniqueid = id;
            var counter = 1;

            while (importedfeatures.TryGetValue(uniqueid, out var existingsignature))
            {
                if (existingsignature == signature)
                    return null;

                counter++;
                uniqueid = id + "-" + counter;
            }

            importedfeatures.Add(uniqueid, signature);

            return uniqueid;
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

                var uniqueid = GetUniqueId(parsedobject.Id, digiwaydata);
                if (uniqueid == null)
                {
                    //Exact copy of a feature already imported in this run
                    return new UpdateDetail() { created = 0, updated = 0, deleted = 0, error = 0 };
                }

                parsedobject.Id = uniqueid;
                returnid = uniqueid;
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
        private async Task<UpdateDetail> InsertDataToDB(
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
            //Get the ODH Item - Disable this since the Id is not easy to assign
            //var query = QueryFactory.Query(table).Select("data").Where("id", odhid);
            //var dataindb = await query.GetObjectSingleAsync<SpatialData>();

            var result = ParseEuregioGeoJsonDataToSpatialData.ParseToSpatialData(null, input, identifier, source, srid);

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
                //Active records of this source and dataset (TagIds contain the identifier, e.g. euregio.routes)
                var idlistdb = await QueryFactory
                    .Query(table)
                    .Select("id")
                    .SourceFilter_GeneratedColumn(new List<string>() { source })
                    .WhereArrayInListOr(new List<string>() { identifier }, "gen_tags")
                    .WhereRaw("data->>'Active' = 'true'")
                    .GetAsync<string>();

                var idstodelete = idlistdb.Where(id => !idlistinterface.Contains(id)).ToList();

                foreach (var idtodelete in idstodelete)
                {
                    var result = await DeleteOrDisableData<SpatialData>(idtodelete, false);

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
