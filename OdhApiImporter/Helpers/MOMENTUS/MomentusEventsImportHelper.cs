// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using EBMS;
using Helper;
using Helper.Extensions;
using Helper.Generic;
using Helper.Tagging;
using MOMENTUS;
using MOMENTUS.Model;
using MOMENTUS.Parser;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers
{
    public class MomentusEventsImportHelper : ImportHelper, IImportHelper
    {
        public bool forceupdate = false; 

        public MomentusEventsImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        #region MOMENTUS Helpers

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            var authtoken = await GetDataFromMomentus.GetAccessTokenAsync(
                settings.MomentusConfig.AuthUrl,
                settings.MomentusConfig.ClientId,
                settings.MomentusConfig.ClientSecret
            );

            var momentusevents = await RequestMomentusEventsWithallRooms(authtoken);
            
            var updateresult = await ImportData(momentusevents, authtoken, cancellationToken);

            //var currenteventshort = await GetAllEventsShort(currentdate);

            //todo check if resulttuple item1 is null
            //var deleteresult = await DeleteDeletedEvents(resulttuple, currenteventshort.ToList());

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult }
            );
        }

        private async Task<IEnumerable<MomentusEvent>> RequestMomentusEventsWithallRooms(MomentusTokenResponse authtoken)
        {
            var momentusrooms = await GetDataFromMomentus.RequestMomentusRooms(
                settings.MomentusConfig.ServiceUrl,
                null,null,null, //Token already present
                authtoken);

            var eventsearchrequest = GetDataFromMomentus.GetEventSearchRequest(
                DateOnly.FromDateTime(DateTime.Now),
                DateOnly.FromDateTime(DateTime.Now.AddYears(1)),
                new List<string> { "venue-1-A" },
                momentusrooms.Select(x => x.Id).ToList(), //Add all rooms
                true);

            return await GetDataFromMomentus.RequestMomentusEvents(
                settings.MomentusConfig.ServiceUrl,
                null,null,null,
                eventsearchrequest,
                authtoken);
        }

        private async Task<IEnumerable<MomentusFunction>> RequestMomentusFunctionByEventId(string eventid, MomentusTokenResponse authtoken)
        {
            return await GetDataFromMomentus.RequestMomentusFunction(
            settings.MomentusConfig.ServiceUrl,
            null, null, null,
            eventid,
            authtoken);
        }

        private async Task<IEnumerable<MomentusBookedSpaceExtended>> RequestMomentusBookedSpacesByEventId(string eventid, MomentusTokenResponse authtoken)
        {
            return await GetDataFromMomentus.RequestMomentusBookedSpaces(
            settings.MomentusConfig.ServiceUrl,
            null, null, null,
            eventid,
            authtoken);
        }

        private async Task<UpdateDetail> ImportData(
            IEnumerable<MomentusEvent> momentuseventlist,
            MomentusTokenResponse authtoken,
            CancellationToken cancellationToken            
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;
            
            foreach (var momentusevent in momentuseventlist.Where(e => e.BookedSpaces != null && e.BookedSpaces.Any(bs => bs.UsageType == "event")))
            {
                var importresult = await ImportDataSingle(momentusevent, authtoken);

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

        private async Task<UpdateDetail> ImportDataSingle(
            MomentusEvent momentusevent,
            MomentusTokenResponse authtoken
        )
        {
            string idtoreturn = "";
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            try
            {
                var query = QueryFactory
                    .Query("events")
                    .Select("data")
                    .Where("id", "urn:events:momentus:" + momentusevent.Id);

                var eventindb = await query.GetObjectSingleAsync<EventLinked>();

                //Get all Functions
                var momentusfunctionlist = await RequestMomentusFunctionByEventId(momentusevent.Id, authtoken);

                //Get detailed BookedRooms
                var momentusbookedroomlist = await RequestMomentusBookedSpacesByEventId(momentusevent.Id, authtoken);

                ///Use the roomid
                var roomid = momentusevent.BookedSpaces.Where(x => x.UsageType == "event").Select(x => x.RoomId).FirstOrDefault();

                //TODO: Get the Venue that contains all rooms here listed!
                //How to identify between Eurac and NOI ? with roomIds ++ Mapping 
                var venuequery = QueryFactory
                    .Query("venues")
                    .Select("data")                    
                    .WhereRaw("data @> $$::jsonb", $"{{\"RoomDetails\":[{{\"Mapping\":{{\"momentus\":{{\"id\":\"{roomid}\"}}}}}}]}}");

                var venue = await venuequery.GetObjectSingleAsync<VenueV2>();


                //TODO Parse the MomentusEvent To Event
                var eventtostore = ParseMomentusData.ParseMomentusEvent(momentusevent, momentusfunctionlist, momentusbookedroomlist, eventindb, venue);

                if (eventtostore == null)
                    return new UpdateDetail() { created = 0, updated = 0, deleted = 0, error = 0 };

                var queryresult = await InsertDataToDB(
                    eventtostore,
                    momentusevent
                );

                newcounter = newcounter + queryresult.created ?? 0;
                updatecounter = updatecounter + queryresult.updated ?? 0;

                WriteLog.LogToConsole(
                    idtoreturn,
                    "dataimport",
                    "single.event",
                    new ImportLog()
                    {
                        sourceid = idtoreturn,
                        sourceinterface = "momentus.event",
                        success = true,
                        error = "",
                    }
                );

            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    idtoreturn,
                    "dataimport",
                    "single.event",
                    new ImportLog()
                    {
                        sourceid = idtoreturn,
                        sourceinterface = "momentus.event",
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

        private async Task<PGCRUDResult> InsertDataToDB(
            EventLinked eventtostore,
            MomentusEvent momentusevent
        )
        {
            try
            {
                //Setting LicenseInfo TO CHECK
                eventtostore.LicenseInfo = LicenseHelper.GetLicenseforEvent(eventtostore, true);
                //Check Languages TO CHECK
                eventtostore.CheckMyInsertedLanguages(new List<string>() { "de","it","en" });

                //PublishedOn is set by the parser logic
                //eventshort.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(momentusevent);

                return await QueryFactory.UpsertData<EventLinked>(
                    eventtostore,
                    new DataInfo("events", Helper.Generic.CRUDOperation.CreateAndUpdate, true),
                    new EditInfo("momentus.event.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(MomentusEvent momentusevent)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "eurac",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(momentusevent),
                    sourceinterface = "momentus",
                    sourceid = momentusevent.Id,
                    sourceurl = "https://api.eu-venueops.com/v1/events/query-by-date-range",
                    type = "event",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private static List<string>? AssignTechnologyfieldsautomatically(
            string companyname,
            List<string>? technologyfields
        )
        {
            if (technologyfields == null)
                technologyfields = new List<string>();

            //Digital, Alpine, Automotive/Automation, Food, Green

            AssignTechnologyFields(companyname, "Digital", "Digital", technologyfields);
            AssignTechnologyFields(companyname, "Alpine", "Alpine", technologyfields);
            AssignTechnologyFields(
                companyname,
                "Automotive",
                "Automotive/Automation",
                technologyfields
            );
            AssignTechnologyFields(companyname, "Food", "Food", technologyfields);
            AssignTechnologyFields(companyname, "Green", "Green", technologyfields);

            if (technologyfields.Count == 0)
                return null;
            else
                return technologyfields;
        }

        private static void AssignTechnologyFields(
            string companyname,
            string tocheck,
            string toassign,
            List<string> automatictechnologyfields
        )
        {
            if (companyname.Contains(tocheck))
                if (!automatictechnologyfields.Contains(toassign))
                    automatictechnologyfields.Add(toassign);
        }

        private async Task<UpdateDetail> DeleteDeletedEvents(
            List<Tuple<EventShortLinked, EBMSEventREST>> resulttuple,
            List<EventShortLinked> eventshortinDB
        )
        {
            var deletecounter = 0;

            if (resulttuple.Select(x => x.Item1).Count() > 0)
            {
                List<EventShortLinked> eventshortfromnow = resulttuple
                    .Select(x => x.Item1)
                    .ToList();

                var idsonListinDB = eventshortinDB.Select(x => x.EventId).ToList();
                var idsonService = eventshortfromnow.Select(x => x.EventId).ToList();

                var idstodelete = idsonListinDB.Where(p => !idsonService.Any(p2 => p2 == p));

                if (idstodelete.Count() > 0)
                {
                    foreach (var idtodelete in idstodelete)
                    {
                        //Set to inactive
                        var eventshorttodeactivate = eventshortinDB
                            .Where(x => x.EventId == idtodelete)
                            .FirstOrDefault();

                        //TODO CHECK IF IT WORKS
                        if (eventshorttodeactivate != null)
                        {
                            //Work With Active instead of deleting....
                            eventshorttodeactivate.Active = false;
                            eventshorttodeactivate.LastChange = DateTime.Now;

                            var updated = await QueryFactory
                                .Query("eventeuracnoi")
                                .Where("id", eventshorttodeactivate.Id?.ToLower())
                                .UpdateAsync(
                                    new JsonBData()
                                    {
                                        id = eventshorttodeactivate.Id?.ToLower() ?? "",
                                        data = new JsonRaw(eventshorttodeactivate),
                                    }
                                );

                            //LOG the Deletion
                            WriteLog.LogToConsole(
                                eventshorttodeactivate.Id,
                                "dataimport",
                                "single.eventeuracnoi.deactivate",
                                new ImportLog()
                                {
                                    sourceid = eventshorttodeactivate.Id,
                                    sourceinterface = "ebms.eventeuracnoi",
                                    success = updated > 0 ? true : false,
                                    error = "",
                                }
                            );

                            deletecounter++;
                        }
                    }
                }
            }

            return new UpdateDetail()
            {
                created = 0,
                updated = 0,
                deleted = deletecounter,
                error = 0,
            };
        }

        private async Task<IEnumerable<EventShortLinked>> GetAllEventsShort(DateTime now)
        {
            var today = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);

            var query = QueryFactory
                .Query("events")
                .Select("data")
                .WhereRaw(
                    "(((to_date(data->> 'EndDate', 'YYYY-MM-DD') >= '"
                        + String.Format("{0:yyyy-MM-dd}", today)
                        + "'))) AND(data#>>'\\{Source\\}' = $$)",
                    "momentus"
                )
                .Where("gen_active", true);

            return await query.GetObjectListAsync<EventShortLinked>();
        }

        #endregion
    }
}
