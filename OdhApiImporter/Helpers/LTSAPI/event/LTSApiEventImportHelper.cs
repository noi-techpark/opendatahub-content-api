// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Extensions;
using Helper.Generic;
using Helper.Location;
using Helper.Tagging;
using LTSAPI;
using LTSAPI.Parser;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhApiImporter.Helpers.RAVEN;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiEventImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiEventImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        //Not implemented here
        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();            
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,            
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }


        public async Task<UpdateDetail> SaveSingleDataToODH(
            string id,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            opendata = reduced;

            //Import the List
            var eventlts = await GetEventsFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (eventlts != null && eventlts.FirstOrDefault().ContainsKey("success") && (Boolean)eventlts.FirstOrDefault()["success"]) //&& eventlts.FirstOrDefault()["Success"] == true
            {     
                //Import Single Data & Deactivate Data
                return await SaveEventsToPG(eventlts);                
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (eventlts != null && eventlts.FirstOrDefault().ContainsKey("status") && ((int)eventlts.FirstOrDefault()["status"] == 403 || (int)eventlts.FirstOrDefault()["status"] == 404))
            {
                var resulttoreturn = default(UpdateDetail);

                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    resulttoreturn = await DeleteOrDisableEventData(id, false, false);
                    if (eventlts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(eventlts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + eventlts.FirstOrDefault()["message"].ToString() + "|";
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    resulttoreturn = await DeleteOrDisableEventData(id, true, true);
                    if (eventlts.FirstOrDefault().ContainsKey("message") && !String.IsNullOrEmpty(eventlts.FirstOrDefault()["message"].ToString()))
                        resulttoreturn.exception = resulttoreturn.exception + "opendata:" + eventlts.FirstOrDefault()["message"].ToString() + "|";
                }

                return resulttoreturn;
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

        public async Task<List<string>> GetLastChangedData(
            DateTime lastchanged,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            //Import the List
            var lastchangedlts = await GetEventsFromLTSV2(null, lastchanged, null, null);
            List<string> lastchangedlist = new List<string>();

            if (lastchangedlts != null && lastchangedlts.FirstOrDefault().ContainsKey("success") && (Boolean)lastchangedlts.FirstOrDefault()["success"])
            {
                var lastchangedrids = lastchangedlts.FirstOrDefault()["data"].ToObject<List<LtsRidList>>();

                lastchangedlist = lastchangedrids.Select(x => x.rid).ToList();
            }
            else
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "lastchanged.events",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.events",
                        success = false,
                        error = "Could not fetch last changed List",
                    }
                );
            }

            return lastchangedlist;
        }

        public async Task<List<string>> GetLastDeletedData(
            DateTime deletedfrom,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            //Import the List
            var deletedlts = await GetEventsFromLTSV2(null, null, deletedfrom, null);
            List<string> lastdeletedlist = new List<string>();

            if (deletedlts != null && deletedlts.FirstOrDefault().ContainsKey("success") && (Boolean)deletedlts.FirstOrDefault()["success"])
            {
                var lastchangedrids = deletedlts.FirstOrDefault()["data"].ToObject<List<LtsRidList>>();

                lastdeletedlist = lastchangedrids.Select(x => x.rid).ToList();
            }
            else
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deleted.events",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.events",
                        success = false,
                        error = "Could not fetch deleted List",
                    }
                );
            }

            return lastdeletedlist;
        }

        public async Task<List<string>> GetActiveList(
            bool active,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            //Import the List
            var activelistlts = await GetEventsFromLTSV2(null, null, null, active);
            List<string> activeList = new List<string>();

            if (activelistlts != null && activelistlts.FirstOrDefault().ContainsKey("success") && (Boolean)activelistlts.FirstOrDefault()["success"])
            {
                var activerids = activelistlts.FirstOrDefault()["data"].ToObject<List<LtsRidList>>();

                activeList = activerids.Select(x => x.rid).ToList();
            }
            else
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "active.events",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.events",
                        success = false,
                        error = "Could not fetch active List",
                    }
                );
            }

            return activeList;
        }
        private async Task<List<JObject>> GetEventsFromLTSV2(string? eventid, DateTime? lastchanged, DateTime? deletedfrom, bool? activelist)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi(opendata);

                //When 1 ID is passed retrieve only Detail
                if (eventid != null)
                {
                    var qs = new LTSQueryStrings() { page_size = 1, filter_endDate = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"), filter_startDate = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd") };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.EventDetailRequest(eventid, dict);
                }
                else if (lastchanged != null)
                {
                    var qs = new LTSQueryStrings() { fields = "rid", filter_endDate = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"), filter_startDate = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd") };

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.EventListRequest(dict, true);
                }
                else if (deletedfrom != null)
                {
                    var qs = new LTSQueryStrings() { fields = "rid", filter_endDate = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"), filter_startDate = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd") };

                    if (deletedfrom != null)
                        qs.filter_lastUpdate = deletedfrom;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.EventDeletedRequest(dict, true);
                }
                else if (activelist != null)
                {
                    var qs = new LTSQueryStrings() { fields = "rid", filter_endDate = DateTime.MaxValue.ToString("yyyy-MM-dd"), filter_startDate = DateTime.MinValue.ToString("yyyy-MM-dd") };

                    if (activelist != null)
                        qs.filter_onlyActive = activelist;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.EventListRequest(dict, true);
                }
                else
                    return null;
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
                LtsApi ltsapi = GetLTSApi(opendata);

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

        private async Task<UpdateDetail> SaveEventsToPG(List<JObject> ltsdata)
        {
            //var newimportcounter = 0;
            //var updateimportcounter = 0;
            //var errorimportcounter = 0;
            //var deleteimportcounter = 0;

            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

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

                    //Add manual assigned Tags to TagIds TO check if this should be activated
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

                        //Add Event Tag of type eventtag to ODHTags Compatibility
                        await AddEventTagsToODHTags(eventparsed);
                    }

                    //When requested with opendata Interface does not return isActive field
                    //All data returned by opendata interface are active by default
                    if (opendata)
                    {
                        eventparsed.Active = true;
                        eventparsed.SmgActive = true;
                    }

                    //Compatibility create Topic Object
                    await GenerateTopicObject(eventparsed);


                    var result = await InsertDataToDB(eventparsed, data.data);

                    //newimportcounter = newimportcounter + result.created ?? 0;
                    //updateimportcounter = updateimportcounter + result.updated ?? 0;
                    //errorimportcounter = errorimportcounter + result.error ?? 0;

                    updatedetails.Add(new UpdateDetail()
                    {
                        created = result.created,
                        updated = result.updated,
                        deleted = result.deleted,
                        error = result.error,
                        objectchanged = result.objectchanged,
                        objectimagechanged = result.objectimagechanged,
                        comparedobjects =
                        result.compareobject != null && result.compareobject.Value ? 1 : 0,
                        pushchannels = result.pushchannels,
                        changes = result.changes,
                    });

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
            else
            {
                updatedetails.Add(new UpdateDetail()
                {
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    comparedobjects = 0,
                    pushchannels = null,
                    changes = null                    
                });
            }


            //To check, this works only for single updates             
            //return new UpdateDetail()
            //{
            //    updated = updateimportcounter,
            //    created = newimportcounter,
            //    deleted = deleteimportcounter,
            //    error = errorimportcounter,
            //};

            return updatedetails.FirstOrDefault();
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            EventLinked objecttosave,
            LTSEventData eventlts            
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = LicenseHelper.GetLicenseforEvent(objecttosave, opendata);

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave, opendata);

                //Set PublishedOn (only full data)
                if(!opendata)
                    objecttosave.CreatePublishedOnList();
                else
                    objecttosave.PublishedOn = new List<string>();

                var rawdataid = await InsertInRawDataDB(eventlts);

                return await QueryFactory.UpsertData<EventLinked>(
                    objecttosave,
                    new DataInfo("events", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
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

        public async Task<UpdateDetail> DeleteOrDisableEventData(string id, bool delete, bool reduced)
        {
            UpdateDetail deletedisableresult = default(UpdateDetail);

            PGCRUDResult result = default(PGCRUDResult);

            if (delete)
            {
                result =  await QueryFactory.DeleteData<EventLinked>(
                    id,
                    new DataInfo("events", CRUDOperation.Delete),
                    new CRUDConstraints(),
                    reduced
                );

                if (result.errorreason != "Data Not Found")
                {
                    deletedisableresult = new UpdateDetail()
                    {
                        created = result.created,
                        updated = result.updated,
                        deleted = result.deleted,
                        error = result.error,
                        objectchanged = result.objectchanged,
                        objectimagechanged = result.objectimagechanged,
                        comparedobjects =
                        result.compareobject != null && result.compareobject.Value ? 1 : 0,
                        pushchannels = result.pushchannels,
                        changes = result.changes,
                    };
                }
            }
            else
            {
                var query = QueryFactory.Query(table).Select("data").Where("id", id);

                var data = await query.GetObjectSingleAsync<EventLinked>();

                if (data != null)
                {
                    if (
                        data.Active != false
                        || (data is ISmgActive && ((ISmgActive)data).SmgActive != false)
                    )
                    {
                        data.Active = false;
                        if (data is ISmgActive)
                            ((ISmgActive)data).SmgActive = false;

                        //updateresult = await QueryFactory
                        //    .Query(table)
                        //    .Where("id", id)
                        //    .UpdateAsync(new JsonBData() { id = id, data = new JsonRaw(data) });

                        result = await QueryFactory.UpsertData<EventLinked>(
                               data,
                               new DataInfo("events", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                               new EditInfo("lts.events.import.deactivate", importerURL),
                               new CRUDConstraints(),
                               new CompareConfig(true, false)
                        );

                        deletedisableresult = new UpdateDetail()
                        {
                            created = result.created,
                            updated = result.updated,
                            deleted = result.deleted,
                            error = result.error,
                            objectchanged = result.objectchanged,
                            objectimagechanged = result.objectimagechanged,
                            comparedobjects =
                        result.compareobject != null && result.compareobject.Value ? 1 : 0,
                            pushchannels = result.pushchannels,
                            changes = result.changes,
                        };
                    }
                }
            }

            return deletedisableresult;
        }

        private async Task MergeEventDates(EventLinked eventNew, EventLinked eventOld, int monthstogoback = 12)
        {

            if (eventOld != null && eventOld.EventDate != null)
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
            if(eventNew.EventDate.Count > 0)
                eventNew.DateBegin = eventNew.EventDate.Select(x => x.From).Min();

            //Set Enddate to the last possible date
            if(eventNew.EventDate.Count > 0)
                eventNew.DateEnd = eventNew.EventDate.Select(x => x.To).Max();
        }

        private async Task MergeEventTags(EventLinked eventNew, EventLinked eventOld)
        {
            if (eventOld != null)
            {
                eventNew.SmgTags = eventOld.SmgTags;
                //Remove all assigned EventTags first (we copied EventTags to ODHTags)
                if (eventNew.SmgTags != null && eventNew.SmgTags.Count > 0)
                {
                    //GET all Tags of Type "eventtag" ID only
                    var eventtagidlist = await QueryFactory.Query().From("tags").TagTypesFilter(new List<string>() { "eventtag" }).Select("id").GetAsync<string>();

                    if(eventtagidlist != null && eventtagidlist.Count() > 0)
                        eventNew.SmgTags = ListExtensions.RemoveItemsPresentInOtherList(eventNew.SmgTags.ToList(), eventtagidlist.ToList());
                }

                //Readd all Redactional Tags
                //var redactionalassignedTags = eventOld.Tags != null ? eventOld.Tags.Where(x => x.Source != "lts").ToList() : null;
                var redactionalassignedTags = eventOld.Tags != null ? eventOld.Tags.Where(x => x.Source != "lts" && (x.Source == "idm" && x.Type != "odhcategory")).ToList() : null;

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

        //Compatibility resons add the Event Tag to ODHTag
        private async Task AddEventTagsToODHTags(EventLinked eventNew)
        {
            if (eventNew != null && eventNew.Tags != null && eventNew.Tags.Count > 0)
            {               
                foreach (var eventtag in eventNew.Tags.Where(x => x.Type == "eventtag"))
                {
                    if(eventNew.SmgTags == null)
                        eventNew.SmgTags = new List<string>();

                    if(!eventNew.SmgTags.Contains(eventtag.Id.ToLower()))
                        eventNew.SmgTags.Add(eventtag.Id.ToLower());
                }
            }
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
    }
}
