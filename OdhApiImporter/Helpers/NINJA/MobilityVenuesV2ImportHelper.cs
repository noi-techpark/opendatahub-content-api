// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using MSS;
using Newtonsoft.Json;
using NINJA;
using NINJA.Parser;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class MobilityVenuesFlattenedImportHelper : ImportHelper, IImportHelper
    {
        public MobilityVenuesFlattenedImportHelper(
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
            var culturedata = await ImportList(cancellationToken);
            //Parse the data and save it to DB
            var result = await SaveVenuesToPG(culturedata.data);

            return result;
        }

        private async Task<NinjaObject<NinjaPlaceRoom>> ImportList(
            CancellationToken cancellationToken
        )
        {
            var responseplaces = await GetNinjaData.GetNinjaPlaces(settings.NinjaConfig.ServiceUrl);

            WriteLog.LogToConsole(
                "",
                "dataimport",
                "list.mobilityculture.venues.flattened",
                new ImportLog()
                {
                    sourceid = "",
                    sourceinterface = "mobility.culture.venues.flattened",
                    success = true,
                    error = "",
                }
            );

            return responseplaces;
        }

        private async Task<UpdateDetail> SaveVenuesToPG(
            ICollection<NinjaData<NinjaPlaceRoom>> ninjaplaceroomarr
        )
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            List<string> idlistspreadsheet = new List<string>();
            List<string> sourcelist = new List<string>();
            sourcelist.Add("drin");
            sourcelist.Add("trevilab");

            foreach (var ninjadata in ninjaplaceroomarr)
            {
                var venuetosave = default(VenueFlattened);

                //From sheet places
                if (ninjadata.smetadata.sheetName == "Places")
                {
                    venuetosave = ParseNinjaData.ParseNinjaEventPlaceToVenueFlattened(
                        ninjadata.sname.Replace(" ", ""),
                        ninjadata,
                        null
                    );
                }
                //From sheet rooms
                else
                {
                    venuetosave = ParseNinjaData.ParseNinjaEventPlaceToVenueFlattened(
                        ninjadata.sname.Replace(" ", ""),
                        ninjadata,
                        ninjadata.smetadata.place.Replace(" ", "")
                    );
                }

                //Save only once
                if (venuetosave != null)
                {
                    //Setting Location Info
                    await SetLocationInfo(venuetosave);

                    venuetosave.Active = true;

                    //Insert Event
                    var result = await InsertDataToDB(venuetosave, ninjadata);

                    newimportcounter = newimportcounter + result.created ?? 0;
                    updateimportcounter = updateimportcounter + result.updated ?? 0;
                    errorimportcounter = errorimportcounter + result.error ?? 0;

                    idlistspreadsheet.Add(venuetosave.Id);

                    if (!sourcelist.Contains(venuetosave.Source))
                        sourcelist.Add(venuetosave.Source);

                    WriteLog.LogToConsole(
                        venuetosave.Id,
                        "dataimport",
                        "single.mobilityculture",
                        new ImportLog()
                        {
                            sourceid = venuetosave.Id,
                            sourceinterface = "mobility.culture.venue",
                            success = true,
                            error = "",
                        }
                    );
                }
                else
                {
                    WriteLog.LogToConsole(
                        venuetosave.Id,
                        "dataimport",
                        "single.mobilityculture",
                        new ImportLog()
                        {
                            sourceid = ninjadata.smetadata.id,
                            sourceinterface = "mobility.culture.venue",
                            success = false,
                            error = "Venue could not be parsed",
                        }
                    );
                }
            }

            //Begin SetDataNotinListToInactive
            var idlistvenuedb = await GetAllVenuesBySource(sourcelist);

            var idstodeletevenue = idlistvenuedb.Where(p => !idlistspreadsheet.Any(p2 => p2 == p));

            foreach (var idtodelete in idstodeletevenue)
            {
                var deletedisableresult = await DeleteOrDisableData(idtodelete, false);

                if (deletedisableresult.Item1 > 0)
                    WriteLog.LogToConsole(
                        idtodelete,
                        "dataimport",
                        "single.mobilityculture.venue.deactivate",
                        new ImportLog()
                        {
                            sourceid = idtodelete,
                            sourceinterface = "mobility.culture",
                            success = true,
                            error = "",
                        }
                    );
                else if (deletedisableresult.Item2 > 0)
                    WriteLog.LogToConsole(
                        idtodelete,
                        "dataimport",
                        "single.mobilityculture.venue.delete",
                        new ImportLog()
                        {
                            sourceid = idtodelete,
                            sourceinterface = "mobility.culture",
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
            VenueFlattened venuetosave,
            NinjaData<NinjaPlaceRoom> ninjavenue
        )
        {
            try
            {
                venuetosave.Id = venuetosave.Id?.ToUpper();

                //Set LicenseInfo
                venuetosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                    venuetosave,
                    Helper.LicenseHelper.GetLicenseforVenue
                );

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                venuetosave._Meta = MetadataHelper.GetMetadataobject(venuetosave);

                //Set PublishedOn
                venuetosave.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(ninjavenue);

                return await QueryFactory.UpsertData<VenueFlattened>(
                    venuetosave,
                    new DataInfo("venuesv2", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("mobility.venuev2.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(NinjaData<NinjaPlaceRoom> ninjaevent)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "centrotrevi-drin",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(ninjaevent),
                    sourceinterface = "culture",
                    sourceid = ninjaevent.scode,
                    sourceurl = "https://mobility.api.opendatahub.com/v2/flat/Culture/",
                    type = "venue",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private async Task<Tuple<int, int>> DeleteOrDisableData(string venueid, bool delete)
        {
            var deleteresult = 0;
            var updateresult = 0;

            if (delete)
            {
                deleteresult = await QueryFactory
                    .Query("venuesv2")
                    .Where("id", venueid)
                    .DeleteAsync();
            }
            else
            {
                var query = QueryFactory.Query("venuesv2").Select("data").Where("id", venueid);

                var data = await query.GetObjectSingleAsync<VenueFlattened>();

                if (data != null)
                {
                    if (data.Active != false)
                    {
                        data.Active = false;

                        //Publishedon? no push needed here

                        updateresult = await QueryFactory
                            .Query("events")
                            .Where("id", venueid)
                            .UpdateAsync(
                                new JsonBData() { id = venueid, data = new JsonRaw(data) }
                            );
                    }
                }
            }

            return Tuple.Create(updateresult, deleteresult);
        }

        #endregion

        #region CUSTOM Ninja Import

        private async Task<List<string>> GetAllVenuesBySource(List<string> sourcelist)
        {
            var query = QueryFactory
                .Query("venuesv2")
                .Select("id")
                .SourceFilter_GeneratedColumn(sourcelist);

            var venueids = await query.GetAsync<string>();

            return venueids.ToList();
        }

        private async Task SetLocationInfo(VenueFlattened venue)
        {
            if (
                venue.GpsPoints != null
                && venue.GpsPoints.ContainsKey("position")
                && venue.GpsPoints["position"].Latitude > 0
                && venue.GpsPoints["position"].Longitude > 0
            )
            {
                var district = await LocationInfoHelper.GetNearestDistrictbyGPS(
                    QueryFactory,
                    venue.GpsPoints["position"].Latitude.GetValueOrDefault(0),
                    venue.GpsPoints["position"].Longitude.GetValueOrDefault(0),
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

                    venue.LocationInfo = locinfolinked;
                }
            }
        }

        #endregion
    }
}
