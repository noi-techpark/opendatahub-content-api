// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using Newtonsoft.Json;
using NINJA;
using NINJA.Parser;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class MobilityEchargingImportHelper : ImportHelper, IImportHelper
    {
        public MobilityEchargingImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

        #region NINJA Helpers

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //Import the data from Mobility Api
            var echarginglist = await ImportList(cancellationToken);
            //Parse the data and save it to DB
            var result = await SaveEchargingstationsToPG(echarginglist);

            return result;
        }

        private async Task<
            NinjaObjectWithParent<NinjaEchargingPlug, NinjaEchargingStation>
        > ImportList(CancellationToken cancellationToken)
        {
            var response = await GetNinjaData.GetNinjaEchargingPlugs(
                settings.NinjaConfig.ServiceUrl
            );

            WriteLog.LogToConsole(
                "",
                "dataimport",
                "list.echarging",
                new ImportLog()
                {
                    sourceid = "",
                    sourceinterface = "mobility.echarging",
                    success = true,
                    error = "",
                }
            );

            return response;
        }

        private async Task<UpdateDetail> SaveEchargingstationsToPG(
            NinjaObjectWithParent<NinjaEchargingPlug, NinjaEchargingStation> ninjadata
        )
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            List<string> idlistspreadsheet = new List<string>();

            //Get all sources
            var sourcelist = GetAndParseProviderList(ninjadata);

            var tagquery = QueryFactory
                .Query("smgtags")
                .Select("data")
                .Where("id", "e-auto ladestation");
            var echargingtag = await tagquery.GetObjectSingleAsync<ODHTagLinked>();

            foreach (var data in ninjadata.data)
            {
                string id = "echarging_" + data.scode.Replace("/", "").ToLower();

                //See if data exists
                var query = QueryFactory.Query("smgpois").Select("data").Where("id", id);

                var objecttosave = await query.GetObjectSingleAsync<ODHActivityPoiLinked>();

                objecttosave = ParseNinjaData.ParseNinjaEchargingToODHActivityPoi(
                    id,
                    data,
                    objecttosave,
                    echargingtag
                );

                if (objecttosave != null)
                {
                    //Setting Location Info
                    if (objecttosave.GpsInfo != null)
                    {
                        await SetLocationInfo(objecttosave);
                    }

                    objecttosave.Active = true;
                    //objecttosave.SmgActive = true;

                    //Set TagIds based on OdhTags
                    //await GenericTaggingHelper.AddTagsToODHActivityPoi(
                    //    objecttosave,
                    //    settings.JsonConfig.Jsondir
                    //);
                    //Create Tag Objects
                    objecttosave.TagIds =
                        objecttosave.Tags != null
                            ? objecttosave.Tags.Select(x => x.Id).ToList()
                            : null;

                    var result = await InsertDataToDB(
                        objecttosave,
                        new KeyValuePair<
                            string,
                            NinjaDataWithParent<NinjaEchargingPlug, NinjaEchargingStation>
                        >(id, data)
                    );

                    newimportcounter = newimportcounter + result.created ?? 0;
                    updateimportcounter = updateimportcounter + result.updated ?? 0;
                    errorimportcounter = errorimportcounter + result.error ?? 0;

                    idlistspreadsheet.Add(id);

                    //if (!sourcelist.Contains(objecttosave.Source))
                    //    sourcelist.Add(objecttosave.Source);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.echarging",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "mobility.echarging",
                            success = true,
                            error = "",
                        }
                    );
                }
                else
                {
                    errorimportcounter = errorimportcounter + 1;

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.echarging",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "mobility.echarging",
                            success = false,
                            error = "echarging could not be parsed",
                        }
                    );
                }
            }

            //Begin SetDataNotinListToInactive
            var idlistdb = await GetAllDataBySource(
                sourcelist.Select(x => x.Item1).Distinct().ToList()
            );

            var idstodelete = idlistdb.Where(p => !idlistspreadsheet.Any(p2 => p2 == p));

            foreach (var idtodelete in idstodelete)
            {
                var deletedisableresult = await DeleteOrDisableData(idtodelete, false);

                if (deletedisableresult.Item1 > 0)
                    WriteLog.LogToConsole(
                        idtodelete,
                        "dataimport",
                        "single.echarging.deactivate",
                        new ImportLog()
                        {
                            sourceid = idtodelete,
                            sourceinterface = "mobility.echarging",
                            success = true,
                            error = "",
                        }
                    );
                else if (deletedisableresult.Item2 > 0)
                    WriteLog.LogToConsole(
                        idtodelete,
                        "dataimport",
                        "single.echarging.delete",
                        new ImportLog()
                        {
                            sourceid = idtodelete,
                            sourceinterface = "mobility.echarging",
                            success = true,
                            error = "",
                        }
                    );

                deleteimportcounter =
                    deleteimportcounter + deletedisableresult.Item1 + deletedisableresult.Item2;
            }

            return new UpdateDetail()
            {
                updated = updateimportcounter,
                created = newimportcounter,
                deleted = deleteimportcounter,
                error = errorimportcounter,
            };
        }

        private async Task<UpdateDetail> InsertDataToDB(
            ODHActivityPoiLinked objecttosave,
            KeyValuePair<
                string,
                NinjaDataWithParent<NinjaEchargingPlug, NinjaEchargingStation>
            > ninjadata
        )
        {
            try
            {
                objecttosave.Id = objecttosave.Id?.ToLower();

                //Set LicenseInfo
                objecttosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                    objecttosave,
                    Helper.LicenseHelper.GetLicenseforOdhActivityPoi
                );

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave);

                //Set PublishedOn
                objecttosave.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(ninjadata);

                return await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                    objecttosave,
                    new DataInfo("smgpois", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("mobility.echarging.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(
            KeyValuePair<
                string,
                NinjaDataWithParent<NinjaEchargingPlug, NinjaEchargingStation>
            > ninjadata
        )
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = ninjadata.Value.porigin,
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(ninjadata.Value),
                    sourceinterface = "echarging",
                    sourceid = ninjadata.Key,
                    sourceurl = "https://mobility.api.opendatahub.com/v2/flat/EChargingPlug/",
                    type = "echarging",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private async Task<Tuple<int, int>> DeleteOrDisableData(string id, bool delete)
        {
            var deleteresult = 0;
            var updateresult = 0;

            if (delete)
            {
                deleteresult = await QueryFactory.Query("smgpois").Where("id", id).DeleteAsync();
            }
            else
            {
                var query = QueryFactory.Query("smgpois").Select("data").Where("id", id);

                var data = await query.GetObjectSingleAsync<ODHActivityPoiLinked>();

                if (data != null)
                {
                    if (data.Active != false || data.SmgActive != false)
                    {
                        data.Active = false;
                        data.SmgActive = false;
                        data.LastChange = DateTime.Now;
                        data._Meta.LastUpdate = DateTime.Now;

                        updateresult = await QueryFactory
                            .Query("smgpois")
                            .Where("id", id)
                            .UpdateAsync(new JsonBData() { id = id, data = new JsonRaw(data) });
                    }
                }
            }

            return Tuple.Create(updateresult, deleteresult);
        }

        #endregion

        #region CUSTOM Ninja Import

        private async Task<List<string>> GetAllDataBySource(List<string> sourcelist)
        {
            var query = QueryFactory
                .Query("smgpois")
                .Select("id")
                .SourceFilter_GeneratedColumn(sourcelist);

            var eventids = await query.GetAsync<string>();

            return eventids.ToList();
        }

        private async Task SetLocationInfo(ODHActivityPoiLinked odhactivitypoi)
        {
            var gpspoint = odhactivitypoi
                .GpsInfo.Where(x => x.Gpstype == "position")
                .FirstOrDefault();

            if (gpspoint != null)
            {
                var district = await LocationInfoHelper.GetNearestDistrictbyGPS(
                    QueryFactory,
                    gpspoint.Latitude.GetValueOrDefault(0),
                    gpspoint.Longitude.GetValueOrDefault(0),
                    30000
                );
                if (district == null)
                    return;

                var locinfo = await LocationInfoHelper.GetTheLocationInfoDistrict(
                    QueryFactory,
                    district.Id
                );
                if (locinfo != null)
                {
                    LocationInfoLinked locinfolinked = new LocationInfoLinked
                    {
                        DistrictInfo = new DistrictInfoLinked
                        {
                            Id = locinfo.DistrictInfo?.Id,
                            Name = locinfo.DistrictInfo?.Name,
                        },
                        MunicipalityInfo = new MunicipalityInfoLinked
                        {
                            Id = locinfo.MunicipalityInfo?.Id,
                            Name = locinfo.MunicipalityInfo?.Name,
                        },
                        TvInfo = new TvInfoLinked
                        {
                            Id = locinfo.TvInfo?.Id,
                            Name = locinfo.TvInfo?.Name,
                        },
                        RegionInfo = new RegionInfoLinked
                        {
                            Id = locinfo.RegionInfo?.Id,
                            Name = locinfo.RegionInfo?.Name,
                        },
                    };

                    odhactivitypoi.LocationInfo = locinfolinked;
                    odhactivitypoi.TourismorganizationId = locinfo.TvInfo?.Id;
                }
            }
        }

        #endregion


        #region Speficif Helpers

        private static List<Tuple<string, string>> GetDataProviderlist(
            NinjaObjectWithParent<NinjaEchargingPlug, NinjaEchargingStation> ninjadata
        )
        {
            ////to test show all state, all capacity, all accessInfo, all accessType, all reserveable,
            //Console.WriteLine(String.Join(",", ninjadata.data.Select(x => x.pmetadata.provider).Distinct().ToList()));
            //Console.WriteLine(String.Join(",", ninjadata.data.Select(x => x.pmetadata.state).Distinct().ToList()));
            //Console.WriteLine(String.Join(",", ninjadata.data.Select(x => x.pmetadata.capacity).Distinct().ToList()));
            //Console.WriteLine(String.Join(",", ninjadata.data.Select(x => x.pmetadata.accessType).Distinct().ToList()));
            //Console.WriteLine(String.Join(",", ninjadata.data.Select(x => x.pmetadata.accessInfo).Distinct().ToList()));

            //Console.WriteLine(String.Join(",", ninjadata.data.SelectMany(x => x.smetadata.outlets.Select(y => y.outletTypeCode)).Distinct().ToList()));



            //Get all sources
            return ninjadata
                .data.Select(x =>
                    Tuple.Create(
                        x.porigin.ToLower(),
                        x.pmetadata.provider != null ? x.pmetadata.provider.ToLower() : ""
                    )
                )
                .Distinct()
                .ToList();
        }

        private static List<Tuple<string, string>> GetAndParseProviderList(
            NinjaObjectWithParent<NinjaEchargingPlug, NinjaEchargingStation> ninjadata
        )
        {
            var list = GetDataProviderlist(ninjadata);
            var listtoreturn = new List<Tuple<string, string>>();

            foreach (var data in list)
            {
                listtoreturn.Add(
                    Tuple.Create(
                        data.Item1 switch
                        {
                            "1uccqzavgmvyrpeq-lipffalqawcg4lfpakc2mjt79fy" =>
                                "echargingspreadsheet",
                            _ => data.Item1,
                        },
                        data.Item2
                    )
                );
            }
            //Hack for alperia stations
            listtoreturn.Add(Tuple.Create("alperia", "alperia"));

            return listtoreturn;
        }

        #endregion
    }
}
