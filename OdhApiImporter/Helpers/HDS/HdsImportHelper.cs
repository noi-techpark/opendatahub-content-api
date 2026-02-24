// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using Microsoft.AspNetCore.Http;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using Helper.Tagging;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HDS;


namespace OdhApiImporter.Helpers
{
    public class HdsDataImportHelper //: ImportHelper, IImportHelper
    {
        private readonly QueryFactory QueryFactory;
        private readonly ISettings settings;
        private string importerURL;

        /// <summary>
        /// Type (yearmarket, market: currently available)
        /// </summary>
        private string type;

        public HdsDataImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string importerURL,
            string type
        )
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
            this.importerURL = importerURL;
            this.type = type;
        }

        private static async Task<string> ReadStringDataManual(HttpRequest request)
        {
            //CSV has to be encoded in UTF8
            using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public async Task<UpdateDetail> PostHDSMarketCalendar(
            HttpRequest request,
            CancellationToken cancellationToken
        )
        {
            string jsonContent = await ReadStringDataManual(request);

            if (!string.IsNullOrEmpty(jsonContent))
            {
                if(type == "market")                
                    return await ImportMarketCalendarFromCSV(jsonContent, cancellationToken);
                else if(type == "yearmarket")
                    return await ImportYearMarketCalendarFromCSV(jsonContent, cancellationToken);
                else if (type == "municipality")
                    return await ImportMunicipalityFromCSV(jsonContent, cancellationToken);
                else
                    throw new Exception("type invalid");
            }
            else
                throw new Exception("no Content");
        }

        private async Task<UpdateDetail> ImportMarketCalendarFromCSV(
            string csvcontent,
            CancellationToken cancellationToken
        )
        {
            var dataparsed = await HDS.GetDataFromHDS.ImportCSVDataFromHDS<HDSMarket>(csvcontent);

            if (dataparsed.Success)
            {
                var updatecounter = 0;
                var newcounter = 0;
                var deletecounter = 0;
                var errorcounter = 0;

                List<string> idlistspreadsheet = new List<string>();

                var jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                settings.JsonConfig.Jsondir,
                new List<string>()
                    {
                        "GenericTags",
                    }
                );

                //Import Each HDS data to ODH
                foreach (var market in dataparsed.records)
                {
                    //Avoid importing markets without name
                    if (market != null && !String.IsNullOrEmpty(market.Municipality))
                    {
                        //Parse to ODHActivityPoi
                        var odhactivitypoi = HDS.ParseHDSPois.ParseHDSMarketToODHActivityPoi(
                            market
                        );

                        if (odhactivitypoi != null)
                        {
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

                            if (odhactivitypoi is { })
                            {
                                ODHTagHelper.SetMainCategorizationForODHActivityPoi(odhactivitypoi);

                                //Traduce all Tags with Source IDM to english tags, CONSIDER TagId "poi" is added here
                                //await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                                //    odhactivitypoi,
                                //    jsondata != null && jsondata["GenericTags"] != null ? jsondata["GenericTags"].ToObject<List<TagLinked>>() : null
                                //);

                                //Create Tag Object
                                //Create Tags and preserve the old TagEntries
                                await odhactivitypoi.UpdateTagsExtension(QueryFactory);

                                //Save to Rawdatatable
                                var rawdataid = await InsertInRawDataDB(market);

                                //PublishedOn Info
                                if (odhactivitypoi.PublishedOn == null)
                                    odhactivitypoi.PublishedOn = new List<string>() { "hds" };
                                else
                                {
                                    if (!odhactivitypoi.PublishedOn.Contains("hds"))
                                        odhactivitypoi.PublishedOn.Add("hds");
                                }

                                //Save to PG
                                //Check if data exists
                                var result = await QueryFactory.UpsertData(
                                    odhactivitypoi,
                                    new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate),
                                    new EditInfo("hds.market.import", importerURL),
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
                var idlistdb = await GetAllHDSData(type);
                var idstodelete = idlistdb.Where(p => !idlistspreadsheet.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var deletedisableresult = await DeleteOrDisableData(idtodelete, false);

                    if (deletedisableresult.Item1 > 0)
                        WriteLog.LogToConsole(
                            idtodelete,
                            "dataimport",
                            $"hds.{type}.import.deactivate",
                            new ImportLog()
                            {
                                sourceid = idtodelete,
                                sourceinterface = $"hds.{type}",
                                success = true,
                                error = "",
                            }
                        );
                    else if (deletedisableresult.Item2 > 0)
                        WriteLog.LogToConsole(
                            idtodelete,
                            "dataimport",
                            $"hds.{type}.import.delete",
                            new ImportLog()
                            {
                                sourceid = idtodelete,
                                sourceinterface = $"hds.{type}",
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
            else if (dataparsed.Error)
                throw new Exception(dataparsed.ErrorMessage);
            else
                throw new Exception("no data to import");
        }

        private async Task<UpdateDetail> ImportYearMarketCalendarFromCSV(
            string csvcontent,
            CancellationToken cancellationToken
        )
        {
            var dataparsed = await HDS.GetDataFromHDS.ImportCSVDataFromHDS<HDSYearMarket>(csvcontent);

            if (dataparsed.Success)
            {
                var updatecounter = 0;
                var newcounter = 0;
                var deletecounter = 0;
                var errorcounter = 0;

                List<string> idlistspreadsheet = new List<string>();

                var jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                settings.JsonConfig.Jsondir,
                new List<string>()
                    {
                        "GenericTags",
                    }
                );

                //Import Each STA Vendingpoi to ODH
                foreach (var yearmarket in dataparsed.records)
                {
                    if (yearmarket != null && !String.IsNullOrEmpty(yearmarket.Municipality))
                    {
                        //Parse to ODHActivityPoi
                        var odhactivitypoi = HDS.ParseHDSPois.ParseHDSYearMarketToODHActivityPoi(
                            yearmarket
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

                            if (odhactivitypoi is { })
                            {
                                ODHTagHelper.SetMainCategorizationForODHActivityPoi(odhactivitypoi);
                                
                                //Traduce all Tags with Source IDM to english tags, CONSIDER TagId "poi" is added here
                                //await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                                //    odhactivitypoi,
                                //    jsondata != null && jsondata["GenericTags"] != null ? jsondata["GenericTags"].ToObject<List<TagLinked>>() : null
                                //);

                                //Create Tag Object
                                //Create Tags and preserve the old TagEntries
                                await odhactivitypoi.UpdateTagsExtension(QueryFactory);

                                //odhactivitypoi.TagIds =
                                //    odhactivitypoi.Tags != null
                                //        ? odhactivitypoi.Tags.Select(x => x.Id).ToList()
                                //        : null;

                                //Save to Rawdatatable
                                var rawdataid = await InsertInRawDataDB(yearmarket);

                                //PublishedOn Info
                                if (odhactivitypoi.PublishedOn == null)
                                    odhactivitypoi.PublishedOn = new List<string>() { "hds" };
                                else
                                {
                                    if (!odhactivitypoi.PublishedOn.Contains("hds"))
                                        odhactivitypoi.PublishedOn.Add("hds");
                                }

                                //Save to PG
                                //Check if data exists
                                var result = await QueryFactory.UpsertData(
                                    odhactivitypoi,
                                    new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate),
                                    new EditInfo("hds.market.import", importerURL),
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
                var idlistdb = await GetAllHDSData(type);
                var idstodelete = idlistdb.Where(p => !idlistspreadsheet.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var deletedisableresult = await DeleteOrDisableData(idtodelete, false);

                    if (deletedisableresult.Item1 > 0)
                        WriteLog.LogToConsole(
                            idtodelete,
                            "dataimport",
                            $"hds.{type}.import.deactivate",
                            new ImportLog()
                            {
                                sourceid = idtodelete,
                                sourceinterface = $"hds.{type}",
                                success = true,
                                error = "",
                            }
                        );
                    else if (deletedisableresult.Item2 > 0)
                        WriteLog.LogToConsole(
                            idtodelete,
                            "dataimport",
                            $"hds.{type}.import.delete",
                            new ImportLog()
                            {
                                sourceid = idtodelete,
                                sourceinterface = $"hds.{type}",
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
            else if (dataparsed.Error)
                throw new Exception(dataparsed.ErrorMessage);
            else
                throw new Exception("no data to import");
        }

        private async Task<UpdateDetail> ImportMunicipalityFromCSV(
            string csvcontent,
            CancellationToken cancellationToken
        )
        {
            var dataparsed = await HDS.GetDataFromHDS.ImportCSVDataFromHDS<HDSComune>(csvcontent);

            if (dataparsed.Success)
            {
                var updatecounter = 0;
                var newcounter = 0;
                var deletecounter = 0;
                var errorcounter = 0;

                //Import Each Municipality
                foreach (var municipality in dataparsed.records)
                {
                    if (municipality != null)
                    {
                        var name = municipality.Municipality.Split("-");

                        //Search municipality
                        var municipalityodhquery = QueryFactory.Query("municipality").Select("data")
                            .SearchFilterWithGenId(PostgresSQLWhereBuilder.TitleFieldsToSearchFor("de"), name[0]);

                        var municipalityodh = await municipalityodhquery.GetObjectSingleAsync<MunicipalityLinked>();

                        if(municipalityodh != null)
                        {
                            //TODO Add the ContactInfo
                            ContactInfos contactinfode = new ContactInfos();
                            contactinfode.Address = municipality.Address;
                            contactinfode.CountryCode = "IT";
                            contactinfode.Phonenumber = municipality.Telephone;
                            contactinfode.Email = municipality.Pec;
                            contactinfode.ZipCode = municipality.PlzCap;
                            contactinfode.LogoUrl = municipality.Logo;
                            contactinfode.Givenname = municipality.Municipality;

                            if (municipalityodh.ContactInfos == null)
                                municipalityodh.ContactInfos = new Dictionary<string, ContactInfos>();

                            municipalityodh.ContactInfos.TryAddOrUpdate("de", contactinfode);

                            municipalityodh.Mapping.TryAddOrUpdate("hds", new Dictionary<string, string>() { { "name", municipality.Municipality } });

                            //Save to Rawdatatable
                            var rawdataid = await InsertInRawDataDB(municipality);

                            //Save to PG
                            //Check if data exists
                            var result = await QueryFactory.UpsertData(
                                municipalityodh,
                                new DataInfo("municipalities", Helper.Generic.CRUDOperation.CreateAndUpdate),
                                new EditInfo("hds.municipality.import", importerURL),
                                new CRUDConstraints(),
                                new CompareConfig(true, false),
                                rawdataid
                            );

                            if (result.updated != null)
                                updatecounter = updatecounter + result.updated.Value;
                            if (result.created != null)
                                newcounter = newcounter + result.created.Value;
                            if (result.deleted != null)
                                deletecounter = deletecounter + result.deleted.Value;
                        }
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
            else if (dataparsed.Error)
                throw new Exception(dataparsed.ErrorMessage);
            else
                throw new Exception("no data to import");
        }

        private async Task<int> InsertInRawDataDB(HDSMarket hdsmarket)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "hds",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(hdsmarket),
                    sourceinterface = "csv",
                    sourceid = "",
                    sourceurl = "csvfile",
                    type = "odhactivitypoi.market",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private async Task<int> InsertInRawDataDB(HDSYearMarket hdsmarket)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "hds",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(hdsmarket),
                    sourceinterface = "csv",
                    sourceid = "",
                    sourceurl = "csvfile",
                    type = "odhactivitypoi.yearmarket",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private async Task<int> InsertInRawDataDB(HDSComune hdsmunicipality)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "hds",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(hdsmunicipality),
                    sourceinterface = "csv",
                    sourceid = "",
                    sourceurl = "csvfile",
                    type = "odhactivitypoi.municipality",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private async Task<List<string>> GetAllHDSData(string syncsourcedatabase)
        {
            var query = QueryFactory
                .Query("smgpois")
                .Select("id")
                .SyncSourceInterfaceFilter_GeneratedColumn(new List<string>() { "hds." + syncsourcedatabase })
                .SourceFilter_GeneratedColumn(new List<string>() { "hds" })
                .WhereLike("id", $"hds:{type}%");

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
