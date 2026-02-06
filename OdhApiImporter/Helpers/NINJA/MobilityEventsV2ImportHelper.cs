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
using Newtonsoft.Json;
using NINJA;
using NINJA.Parser;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class MobilityEventsFlattenedImportHelper : ImportHelper, IImportHelper
    {
        public MobilityEventsFlattenedImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

        #region NINJA Helpers

        public async Task<IEnumerable<UpdateDetail>> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //Import the data from Mobility Api
            var culturelist = await ImportList(cancellationToken);
            //Parse the data and save it to DB
            var result = await SaveEventsToPG(culturelist.data);

            return new List<UpdateDetail>() { result };
        }

        private async Task<NinjaObject<NinjaEvent>> ImportList(CancellationToken cancellationToken)
        {
            var responseevents = await GetNinjaData.GetNinjaEvent(settings.NinjaConfig.ServiceUrl);

            WriteLog.LogToConsole(
                "",
                "dataimport",
                "list.mobilityculture.events.v2",
                new ImportLog()
                {
                    sourceid = "",
                    sourceinterface = "mobility.culture.events.v2",
                    success = true,
                    error = "",
                }
            );

            return responseevents;
        }

        private async Task<UpdateDetail> SaveEventsToPG(
            ICollection<NinjaData<NinjaEvent>> ninjadataarr
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

            foreach (var ninjadata in ninjadataarr.Select(x => x.tmetadata))
            {
                foreach (KeyValuePair<string, NinjaEvent> kvp in ninjadata)
                {
                    if (!String.IsNullOrEmpty(kvp.Key))
                    {
                        var eventtosave = ParseNinjaData.ParseNinjaEventToODHEventFlattened(
                            kvp.Key.Replace(" ", ""),
                            kvp.Value,
                            kvp.Value.room.Replace(" ", "")
                        );

                        if (eventtosave != null)
                        {
                            eventtosave.Active = true;

                            var idtocheck = kvp.Key;

                            //For Event Ids greater 100 characters
                            if (idtocheck.Length > 100)
                                idtocheck = idtocheck.Substring(0, 99);

                            //Insert Event
                            var result = await InsertDataToDB(eventtosave, kvp);

                            newimportcounter = newimportcounter + result.created ?? 0;
                            updateimportcounter = updateimportcounter + result.updated ?? 0;
                            errorimportcounter = errorimportcounter + result.error ?? 0;

                            idlistspreadsheet.Add(idtocheck.ToUpper());

                            if (!sourcelist.Contains(eventtosave.Source))
                                sourcelist.Add(eventtosave.Source);

                            WriteLog.LogToConsole(
                                idtocheck.ToUpper(),
                                "dataimport",
                                "single.mobilityculture.event",
                                new ImportLog()
                                {
                                    sourceid = idtocheck.ToUpper(),
                                    sourceinterface = "mobility.culture",
                                    success = true,
                                    error = "",
                                }
                            );
                        }
                        else
                        {
                            WriteLog.LogToConsole(
                                kvp.Key,
                                "dataimport",
                                "single.mobilityculture.event",
                                new ImportLog()
                                {
                                    sourceid = kvp.Key,
                                    sourceinterface = "mobility.culture",
                                    success = false,
                                    error = "Event could not be parsed",
                                }
                            );
                        }
                    }
                }
            }

            //Begin SetDataNotinListToInactive
            var idlistdb = await GetAllEventsBySource(sourcelist);

            var idstodelete = idlistdb.Where(p => !idlistspreadsheet.Any(p2 => p2 == p));

            foreach (var idtodelete in idstodelete)
            {
                var deletedisableresult = await DeleteOrDisableData(idtodelete, false);

                if (deletedisableresult.Item1 > 0)
                    WriteLog.LogToConsole(
                        idtodelete,
                        "dataimport",
                        "single.mobilityculture.event.deactivate",
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
                        "single.mobilityculture.event.delete",
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
            EventFlattened eventtosave,
            KeyValuePair<string, NinjaEvent> ninjaevent
        )
        {
            try
            {
                eventtosave.Id = eventtosave.Id?.ToUpper();

                //Set LicenseInfo
                eventtosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                    eventtosave,
                    Helper.LicenseHelper.GetLicenseforEvent
                );

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                eventtosave._Meta = MetadataHelper.GetMetadataobject(eventtosave);

                //Set PublishedOn
                eventtosave.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(ninjaevent);

                return await QueryFactory.UpsertData<EventFlattened>(
                    eventtosave,
                    new DataInfo("eventsv2", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("mobility.eventv2.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, NinjaEvent> ninjaevent)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "centrotrevi-drin",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(ninjaevent.Value),
                    sourceinterface = "culture",
                    sourceid = ninjaevent.Key,
                    sourceurl = "https://mobility.api.opendatahub.com/v2/flat/Culture/",
                    type = "event",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private async Task<Tuple<int, int>> DeleteOrDisableData(string eventid, bool delete)
        {
            var deleteresult = 0;
            var updateresult = 0;

            if (delete)
            {
                deleteresult = await QueryFactory
                    .Query("eventsv2")
                    .Where("id", eventid)
                    .DeleteAsync();
            }
            else
            {
                var query = QueryFactory.Query("eventsv2").Select("data").Where("id", eventid);

                var data = await query.GetObjectSingleAsync<EventFlattened>();

                if (data != null)
                {
                    if (data.Active != false)
                    {
                        data.Active = false;

                        //Publishedon? no push needed here

                        updateresult = await QueryFactory
                            .Query("eventsv2")
                            .Where("id", eventid)
                            .UpdateAsync(
                                new JsonBData() { id = eventid, data = new JsonRaw(data) }
                            );
                    }
                }
            }

            return Tuple.Create(updateresult, deleteresult);
        }

        #endregion

        #region CUSTOM Ninja Import

        private async Task<List<string>> GetAllEventsBySource(List<string> sourcelist)
        {
            var query = QueryFactory
                .Query("eventsv2")
                .Select("id")
                .SourceFilter_GeneratedColumn(sourcelist);

            var eventids = await query.GetAsync<string>();

            return eventids.ToList();
        }

        private async Task SetLocationInfo(EventLinked myevent)
        {
            var district = await LocationInfoHelper.GetNearestDistrictbyGPS(
                QueryFactory,
                myevent.Latitude,
                myevent.Longitude,
                30000
            );

            if (district == null)
                return;

            myevent.DistrictId = district.Id;
            myevent.DistrictIds = new List<string>() { district.Id };

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

                myevent.LocationInfo = locinfolinked;
            }
        }

        #endregion
    }
}
