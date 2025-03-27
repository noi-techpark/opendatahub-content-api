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
using Helper.Tagging;
using LTSAPI;
using LTSAPI.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlKata.Execution;


namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiEventImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiEventImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

        private LtsApi GetLTSApi()
        {
            if (!opendata)
            {
                return new LtsApi(
                   settings.LtsCredentials.serviceurl,
                   settings.LtsCredentials.username,
                   settings.LtsCredentials.password,
                   settings.LtsCredentials.ltsclientid,
                   false
               );
            }
            else
            {
                return new LtsApi(
                settings.LtsCredentialsOpen.serviceurl,
                settings.LtsCredentialsOpen.username,
                settings.LtsCredentialsOpen.password,
                settings.LtsCredentialsOpen.ltsclientid,
                true
            );
            }
        }

        private async Task<List<JObject>> GetEventsFromLTSV2(List<string> eventids, DateTime? lastchanged)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi();
                
                if(eventids.Count == 1)
                {
                    var qs = new LTSQueryStrings() { page_size = 1, filter_endDate = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"), filter_startDate = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd") };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.EventDetailRequest(eventids.FirstOrDefault(), dict);
                }
                else
                {
                    var qs = new LTSQueryStrings() { page_size = 100, filter_endDate = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"), filter_startDate = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd") };

                    if (eventids != null && eventids.Count > 0)
                        qs.filter_rids = String.Join(",", eventids);
                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.EventListRequest(dict, true);
                }                
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.events",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.events",
                        success = false,
                        error = ex.Message,
                    }
                );
                return null;
            }
        }

        private async Task<List<JObject>> GetOrganizerFromLTSV2(string organizerid)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi();

                var qs = new LTSQueryStrings() { page_size = 1 };
                var dict = ltsapi.GetLTSQSDictionary(qs);

                return await ltsapi.EventOrganizerDetailRequest(organizerid, dict);
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "single.events.organizers",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.events.organizers",
                        success = false,
                        error = ex.Message,
                    }
                );
                return null;
            }
        }



        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            opendata = reduced;

            //Import the List
            var eventlts = await GetEventsFromLTSV2(idlist, lastchanged);

            //Check if Data is accessible on LTS
            if (eventlts != null && eventlts.FirstOrDefault().ContainsKey("success") && (Boolean)eventlts.FirstOrDefault()["success"]) //&& eventlts.FirstOrDefault()["Success"] == true
            {     //Import Single Data & Deactivate Data
                var result = await SaveEventsToPG(eventlts);
                return result;
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if(eventlts != null && eventlts.FirstOrDefault().ContainsKey("status") && (int)eventlts.FirstOrDefault()["status"] == 403)
            {
                var result = await DeleteOrDisableData<EventLinked>(idlist.FirstOrDefault(), false);
                var resultopen = await DeleteOrDisableData<EventLinked>(idlist.FirstOrDefault() + "_REDUCED", true);

                return new UpdateDetail()
                {
                    updated = result.Item1,
                    created = 0,
                    deleted = resultopen.Item2,
                    error = 0,
                };
            }
            else
            {
                return new UpdateDetail()
                {
                    updated = 0,
                    created = 0,
                    deleted = 0,
                    error = 1,
                };
            }            
        }

        private async Task<UpdateDetail> SaveEventsToPG(List<JObject> ltsdata)
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSEvent> eventdata = new List<LTSEvent>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    eventdata.Add(
                        ltsdatasingle.ToObject<LTSEvent>()
                    );
                }

                foreach (var data in eventdata)
                {
                    string id = data.data.rid;

                    var eventparsed = EventParser.ParseLTSEventV1(data.data, false);

                    //TODO Add the Code Here for POST Processing Data

                    //POPULATE LocationInfo
                    eventparsed.LocationInfo = await eventparsed.UpdateLocationInfoExtension(
                        QueryFactory
                    );

                    //DistanceCalculation
                    await eventparsed.UpdateDistanceCalculation(QueryFactory);
            
                    //GET OLD Event
                    var eventindb = await LoadDataFromDB<EventLinked>(id);

                    //Do not delete Old Dates from Event
                    await MergeEventDates(eventparsed, eventindb, 6);

                    //Add manual assigned Tags to TagIds
                    await MergeEventTags(eventparsed, eventindb);

                    //Create Tags
                    await eventparsed.UpdateTagsExtension(QueryFactory);
                                                      
                    if (!opendata)
                    {
                        //GET Organizer Data and add to Event
                        await AddOrganizerData(eventparsed);

                        //Add the MetaTitle for IDM
                        await AddMetaTitle(eventparsed);

                        //Resort the publisher
                        await ResortPublisher(eventparsed);
                    }                        

                    //Compatibility create Topic Object
                    await GenerateTopicObject(eventparsed);


                    var result = await InsertDataToDB(eventparsed, data.data);

                    newimportcounter = newimportcounter + result.created ?? 0;
                    updateimportcounter = updateimportcounter + result.updated ?? 0;
                    errorimportcounter = errorimportcounter + result.error ?? 0;

                    idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.events",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.events",
                            success = true,
                            error = "",
                        }
                    );
                }

                //Deactivate this in the meantime
                //if (idlistlts.Count > 0)
                //{
                //    //Begin SetDataNotinListToInactive
                //    var idlistdb = await GetAllDataBySourceAndType(
                //        new List<string>() { "lts" },
                //        new List<string>() { "eventcategory" }
                //    );

                //    var idstodelete = idlistdb.Where(p => !idlistlts.Any(p2 => p2 == p));

                //    foreach (var idtodelete in idstodelete)
                //    {
                //        var deletedisableresult = await DeleteOrDisableData<TagLinked>(
                //            idtodelete,
                //            false
                //        );

                //        if (deletedisableresult.Item1 > 0)
                //            WriteLog.LogToConsole(
                //                idtodelete,
                //                "dataimport",
                //                "single.events.categories.deactivate",
                //                new ImportLog()
                //                {
                //                    sourceid = idtodelete,
                //                    sourceinterface = "lts.events.categories",
                //                    success = true,
                //                    error = "",
                //                }
                //            );
                //        else if (deletedisableresult.Item2 > 0)
                //            WriteLog.LogToConsole(
                //                idtodelete,
                //                "dataimport",
                //                "single.events.categories.delete",
                //                new ImportLog()
                //                {
                //                    sourceid = idtodelete,
                //                    sourceinterface = "lts.events.categories",
                //                    success = true,
                //                    error = "",
                //                }
                //            );

                //        deleteimportcounter =
                //            deleteimportcounter
                //            + deletedisableresult.Item1
                //            + deletedisableresult.Item2;
                //    }
                //}
            }
            else if(ltsdata == null && opendata == true)
            {
                //Delete the Opendata if it is no more returned
            }
            else
                errorimportcounter = 1;

            return new UpdateDetail()
            {
                updated = updateimportcounter,
                created = newimportcounter,
                deleted = deleteimportcounter,
                error = errorimportcounter,
            };
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            EventLinked objecttosave,
            LTSEventData eventlts            
        )
        {
            try
            {
                //Set LicenseInfo
                //objecttosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                //    objecttosave,
                //    Helper.LicenseHelper.GetLicenseforEvent(
                //);
                objecttosave.LicenseInfo = LicenseHelper.GetLicenseforEvent(objecttosave, opendata);

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave, opendata);

                //Set PublishedOn
                objecttosave.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(eventlts);

                return await QueryFactory.UpsertData<EventLinked>(
                    objecttosave,
                    new DataInfo("events", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("lts.events.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid,
                    opendata
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(LTSEventData eventlts)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(eventlts),
                    sourceinterface = "events",
                    sourceid = eventlts.rid,
                    sourceurl = "https://go.lts.it/api/v1/events",
                    type = "events",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private async Task MergeEventDates(EventLinked eventNew, EventLinked eventOld, int monthstogoback = 12)
        {

            if (eventOld != null)
            {
                //EventDates not delete
                //Event Start Begindate Logic    
                List<EventDate> eventDatesBeforeToday = new List<EventDate>();
                foreach (var eventdate in eventOld.EventDate.Where(x => x.From.Date < DateTime.Now.Date))
                {
                    //How many event dates we have to store?
                    if(eventdate.From.Date > DateTime.Now.Date.AddMonths(-1 * monthstogoback))
                        eventDatesBeforeToday.Add(eventdate);
                }

                foreach (var eventdatebefore in eventDatesBeforeToday)
                {
                    if (eventNew.EventDate.Where(x => x.DayRID == eventdatebefore.DayRID).Count() == 0)
                        eventNew.EventDate.Add(eventdatebefore);
                }
            }

            //Reorder Event Dates
            eventNew.EventDate = eventNew.EventDate.OrderBy(x => x.From).ToList();

            //Set Begindate to the first possible date
            eventNew.DateBegin = eventNew.EventDate.Select(x => x.From).Min();

            //Set Enddate to the last possible date            
            eventNew.DateEnd = eventNew.EventDate.Select(x => x.To).Max();
        }

        private async Task MergeEventTags(EventLinked eventNew, EventLinked eventOld)
        {
            if (eventOld != null)
            {
                eventNew.SmgTags = eventOld.SmgTags;

                //Readd all Redactional Tags
                var redactionalassignedTags = eventOld.Tags != null ? eventOld.Tags.Where(x => x.Source != "lts").ToList() : null;
                if (redactionalassignedTags != null)
                {
                    foreach (var tag in redactionalassignedTags)
                    {
                        eventNew.TagIds.Add(tag.Id);
                    }
                }
            }
            //TODO import the Redactional Tags from Events into Tags?
        }

        //Compatibility reasons recreate this Topic Object but without description
        private async Task GenerateTopicObject(EventLinked eventNew)
        {
            if (eventNew != null && eventNew.Tags != null && eventNew.Tags.Count > 0)
            {
                eventNew.Topics = new List<TopicLinked>();

                foreach (var topicrid in eventNew.Tags.Where(x => x.Type == "eventcategory"))
                {
                    eventNew.Topics.Add(new TopicLinked() { TopicRID = topicrid.Id, TopicInfo = topicrid.Name  });
                }
            }            
        }

        //Metadata assignment detailde.MetaTitle = detailde.Title + " | suedtirol.info";
        private async Task AddMetaTitle(EventLinked eventNew)
        {
            if (eventNew != null && eventNew.Detail != null)
            {                
                foreach (var detail in eventNew.Detail)
                {
                    detail.Value.MetaTitle = detail.Value.Title + " | suedtirol.info";
                }
            }
        }

        //Compatibility make sure publisher C9475CF585664B2887DE543481182A2D if available is on first position
        private async Task ResortPublisher(EventLinked eventNew)
        {
            if(eventNew.EventPublisher != null)
            {
                if (eventNew.EventPublisher.Where(x => x.PublisherRID == "C9475CF585664B2887DE543481182A2D").Count() > 0)
                {
                    var toppublisher = eventNew.EventPublisher.Where(x => x.PublisherRID == "C9475CF585664B2887DE543481182A2D").FirstOrDefault();
                    if (toppublisher != null)
                    {                        
                        eventNew.EventPublisher.Remove(toppublisher);
                        eventNew.EventPublisher = eventNew.EventPublisher.Prepend(toppublisher).ToList();
                    }
                }
                //seems not working
                //eventNew.EventPublisher = eventNew.EventPublisher.OrderBy(x => x.PublisherRID == "C9475CF585664B2887DE543481182A2D").ToList();
            }
        }

        private async Task AddOrganizerData(EventLinked eventNew)
        {
            if(!String.IsNullOrEmpty(eventNew.OrgRID))
            {
                //Get the Organizer from LTS
                //To check add organizer only in languages the event is available?
                //To chek VAT missing

                var organizer = await GetOrganizerFromLTSV2(eventNew.OrgRID);

                if (organizer != null)
                {
                    var organizerinfo = EventOrganizerParser.ParseLTSEventOrganizer(organizer.FirstOrDefault());
                    eventNew.OrganizerInfos = organizerinfo;
                }
            }
        }

        public Task<UpdateDetail> SaveDataToODH(DateTime? lastchanged = null, List<string>? idlist = null, CancellationToken cancellationToken = default)
        {
            return SaveDataToODH(lastchanged, idlist, false, cancellationToken);
        }
    }
}
