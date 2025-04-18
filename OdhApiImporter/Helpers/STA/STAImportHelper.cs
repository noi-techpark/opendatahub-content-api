// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class StaVendingpointsImportHelper //: ImportHelper, IImportHelper
    {
        private readonly QueryFactory QueryFactory;
        private readonly ISettings settings;
        private string importerURL;

        public StaVendingpointsImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string importerURL
        )
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
            this.importerURL = importerURL;
        }

        private static async Task<string> ReadStringDataManual(HttpRequest request)
        {
            //CSV has to be encoded in UTF8
            using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
            //using (StreamReader reader = new StreamReader(request.Body, Encoding.Default))
            //{
            //    return await reader.ReadToEndAsync();
            //}
        }

        public async Task<UpdateDetail> PostVendingPointsFromSTA(
            HttpRequest request,
            CancellationToken cancellationToken
        )
        {
            string jsonContent = await ReadStringDataManual(request);

            if (!string.IsNullOrEmpty(jsonContent))
            {
                return await ImportVendingPointsFromCSV(jsonContent, cancellationToken);
            }
            else
                throw new Exception("no Content");
        }

        private async Task<UpdateDetail> ImportVendingPointsFromCSV(
            string csvcontent,
            CancellationToken cancellationToken
        )
        {
            var vendingpoints = await STA.GetDataFromSTA.ImportCSVFromSTA(csvcontent);

            if (vendingpoints.Success)
            {
                var updatecounter = 0;
                var newcounter = 0;
                var deletecounter = 0;
                var errorcounter = 0;

                List<string> idlistspreadsheet = new List<string>();

                //Import Each STA Vendingpoi to ODH
                foreach (var vendingpoint in vendingpoints.records)
                {
                    if (!String.IsNullOrEmpty(vendingpoint.STA_ID))
                    {
                        //Parse to ODHActivityPoi
                        var odhactivitypoi = STA.ParseSTAPois.ParseSTAVendingPointToODHActivityPoi(
                            vendingpoint
                        );

                        if (odhactivitypoi != null)
                        {
                            //MetaData
                            //odhactivitypoi._Meta = MetadataHelper.GetMetadataobject<ODHActivityPoiLinked>(odhactivitypoi, MetadataHelper.GetMetadataforOdhActivityPoi); //GetMetadata(data.Id, "odhactivitypoi", sourcemeta, data.LastChange);
                            //LicenseInfo                                                                                                                                    //License
                            odhactivitypoi.LicenseInfo = LicenseHelper.GetLicenseforOdhActivityPoi(
                                odhactivitypoi
                            );

                            if (odhactivitypoi.GpsPoints.ContainsKey("position"))
                            {
                                //Get Nearest District
                                var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                                    odhactivitypoi.GpsPoints["position"].Latitude,
                                    odhactivitypoi.GpsPoints["position"].Longitude,
                                    10000
                                );
                                var nearestdistrict = await LocationInfoHelper.GetNearestDistrict(
                                    QueryFactory,
                                    geosearchresult,
                                    1
                                );

                                if (nearestdistrict != null && nearestdistrict.Count() > 0)
                                {
                                    //Get LocationInfo Object
                                    var locationinfo =
                                        await LocationInfoHelper.GetTheLocationInfoDistrict(
                                            QueryFactory,
                                            nearestdistrict.FirstOrDefault()?.Id
                                        );

                                    if (locationinfo != null)
                                        odhactivitypoi.LocationInfo = locationinfo;
                                }
                            }

                            //Adding TypeInfo Additional
                            odhactivitypoi.AdditionalPoiInfos =
                                await GetAdditionalTypeInfo.GetAdditionalTypeInfoForPoi(
                                    QueryFactory,
                                    odhactivitypoi?.SubType,
                                    new List<string>() { "de", "it", "en" }
                                );

                            if (odhactivitypoi is { })
                            {
                                ODHTagHelper.SetMainCategorizationForODHActivityPoi(odhactivitypoi);

                                //Special get all Taglist and traduce it on import
                                await GenericTaggingHelper.AddTagsToODHActivityPoi(
                                    odhactivitypoi,
                                    settings.JsonConfig.Jsondir
                                );
                                odhactivitypoi.TagIds =
                                    odhactivitypoi.Tags != null
                                        ? odhactivitypoi.Tags.Select(x => x.Id).ToList()
                                        : null;

                                //Save to Rawdatatable
                                var rawdataid = await InsertInRawDataDB(vendingpoint);

                                //PublishedOn Info
                                if (odhactivitypoi.PublishedOn == null)
                                    odhactivitypoi.PublishedOn = new List<string>() { "sta" };
                                else
                                {
                                    if (!odhactivitypoi.PublishedOn.Contains("sta")) 
                                        odhactivitypoi.PublishedOn.Add("sta");
                                }
                                    
                                //Save to PG
                                //Check if data exists
                                var result = await QueryFactory.UpsertData(
                                    odhactivitypoi,
                                    new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate),
                                    new EditInfo("sta.vendingpoint.import", importerURL),
                                    new CRUDConstraints(),
                                    new CompareConfig(true, false),
                                    rawdataid
                                );

                                idlistspreadsheet.Add(odhactivitypoi.Id);

                                if (result.updated != null)
                                    updatecounter = updatecounter + result.updated.Value;
                                if (result.created != null)
                                    newcounter = newcounter + result.created.Value;
                                if (result.deleted != null)
                                    deletecounter = deletecounter + result.deleted.Value;
                            }
                        }
                    }
                }

                //Set all Deleted Vendingpoints to inactive
                var idlistdb = await GetAllVendingpoints();
                var idstodelete = idlistdb.Where(p => !idlistspreadsheet.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var deletedisableresult = await DeleteOrDisableData(idtodelete, false);

                    if (deletedisableresult.Item1 > 0)
                        WriteLog.LogToConsole(
                            idtodelete,
                            "dataimport",
                            "sta.vendingpoint.import.deactivate",
                            new ImportLog()
                            {
                                sourceid = idtodelete,
                                sourceinterface = "sta.vendingpoint",
                                success = true,
                                error = "",
                            }
                        );
                    else if (deletedisableresult.Item2 > 0)
                        WriteLog.LogToConsole(
                            idtodelete,
                            "dataimport",
                            "sta.vendingpoint.import.delete",
                            new ImportLog()
                            {
                                sourceid = idtodelete,
                                sourceinterface = "sta.vendingpoint",
                                success = true,
                                error = "",
                            }
                        );

                    deletecounter =
                        deletecounter + deletedisableresult.Item1 + deletedisableresult.Item2;
                }


                return new UpdateDetail()
                {
                    created = newcounter,
                    updated = updatecounter,
                    deleted = deletecounter,
                    error = errorcounter,
                };
            }
            else if (vendingpoints.Error)
                throw new Exception(vendingpoints.ErrorMessage);
            else
                throw new Exception("no data to import");
        }

        private async Task<int> InsertInRawDataDB(STA.STAVendingPoint stavendingpoint)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "sta",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(stavendingpoint),
                    sourceinterface = "csv",
                    sourceid = stavendingpoint?.STA_ID ?? "",
                    sourceurl = "csvfile",
                    type = "odhactivitypoi.vendingpoint",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private async Task<List<string>> GetAllVendingpoints()
        {
            var query = QueryFactory
                .Query("smgpois")
                .Select("id")
                .SourceFilter_GeneratedColumn(new List<string>() { "sta" })
                .WhereLike("id", "salespoint_sta%");

            var eventids = await query.GetAsync<string>();

            return eventids.ToList();
        }

        private async Task<Tuple<int, int>> DeleteOrDisableData(string id, bool delete)
        {
            var deleteresult = 0;
            var updateresult = 0;

            if (delete)
            {
                deleteresult = await QueryFactory
                    .Query("smgpois")
                    .Where("id", id)
                    .DeleteAsync();
            }
            else
            {
                var query = QueryFactory.Query("smgpois").Select("data").Where("id", id);

                var data = await query.GetObjectSingleAsync<ODHActivityPoiLinked>();

                if (data != null)
                {
                    if (data.Active != false)
                    {
                        data.Active = false;
                        data.SmgActive = false;

                        updateresult = await QueryFactory
                            .Query("smgpois")
                            .Where("id", id)
                            .UpdateAsync(
                                new JsonBData() { id = id, data = new JsonRaw(data) }
                            );
                    }
                }
            }

            return Tuple.Create(updateresult, deleteresult);
        }


    }
}
