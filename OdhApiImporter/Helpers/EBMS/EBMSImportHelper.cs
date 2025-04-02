// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using EBMS;
using Helper;
using Helper.Extensions;
using Helper.Tagging;
using Helper.Generic;
using Newtonsoft.Json;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public class EbmsEventsImportHelper : ImportHelper, IImportHelper
    {
        public bool forceupdate = false; 

        public EbmsEventsImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }

        #region EBMS Helpers

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            var resulttuple = GetEBMSData.GetEbmsEvents(
                settings.EbmsConfig.ServiceUrl,
                settings.EbmsConfig.User,
                settings.EbmsConfig.Password
            );

            //To check we have to use here not DateTime.Now but DateTime now from our timezone
            var currentdate = TimeZoneInfo.ConvertTime(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome")
            );
            var currenteventshort = await GetAllEventsShort(currentdate);

            var updateresult = await ImportData(resulttuple, cancellationToken);

            //todo check if resulttuple item1 is null
            var deleteresult = await DeleteDeletedEvents(resulttuple, currenteventshort.ToList());

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }

        private async Task<UpdateDetail> ImportData(
            List<Tuple<EventShortLinked, EBMSEventREST>> resulttuple,
            CancellationToken cancellationToken            
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            var resulttuplesorted = resulttuple.OrderBy(x => x.Item1.StartDate);

            foreach (var (eventshort, eventebms) in resulttuplesorted)
            {
                var importresult = await ImportDataSingle(eventshort, eventebms);

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
            EventShortLinked eventshort,
            EBMSEventREST eventebms
        )
        {
            string idtoreturn = "";
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            try
            {
                var query = QueryFactory
                    .Query("eventeuracnoi")
                    .Select("data")
                    .Where("id", eventshort.Id);

                var eventindb = await query.GetObjectSingleAsync<EventShortLinked>();

                //currenteventshort.Where(x => x.EventId == eventshort.EventId).FirstOrDefault();

                var changedonDB = DateTime.Now;

                //Fields to not overwrite
                var imagegallery = new List<ImageGallery>();
                var eventTextDE = "";
                var eventTextIT = "";
                var eventTextEN = "";
                var videourl = "";
                Nullable<bool> activeweb = null;
                Nullable<bool> activecommunity = null;
                List<string>? technologyfields = null;
                List<string>? customtagging = null;
                var webadress = "";
                //List<DocumentPDF>? eventdocument = new List<DocumentPDF>();
                IDictionary<string, List<Document>?> document =
                    new Dictionary<string, List<Document>?>();

                bool? soldout = false;
                bool? externalorganizer = false;
                IDictionary<string, string> eventText = new Dictionary<string, string>();
                ICollection<string>? publishedon = new List<string>();

                ICollection<string>? tagids = null;

                if (eventindb == null)
                {
                    eventshort.FirstImport = DateTime.Now;
                }

                if (eventindb != null)
                {
                    changedonDB = eventindb.ChangedOn;
                    imagegallery =
                        eventindb.ImageGallery != null ? eventindb.ImageGallery.ToList() : null;
                    eventTextDE = eventindb.EventTextDE;
                    eventTextIT = eventindb.EventTextIT;
                    eventTextEN = eventindb.EventTextEN;

                    eventText = eventindb.EventText;

                    activeweb = eventindb.ActiveWeb;
                    activecommunity = eventindb.ActiveCommunityApp;

                    videourl = eventindb.VideoUrl;
                    technologyfields = eventindb.TechnologyFields;
                    customtagging = eventindb.CustomTagging;
                    webadress = eventindb.WebAddress;
                    externalorganizer = eventindb.ExternalOrganizer;

                    //eventdocument = eventindb.EventDocument;
                    document = eventindb.Documents;

                    soldout = eventindb.SoldOut;

                    publishedon = eventindb.PublishedOn;

                    tagids = eventindb.TagIds;
                }

                if (changedonDB != eventshort.ChangedOn || forceupdate)
                {
                    eventshort.ImageGallery = imagegallery;
                    //eventshort.EventTextDE = eventTextDE;
                    //eventshort.EventTextIT = eventTextIT;
                    //eventshort.EventTextEN = eventTextEN;

                    eventshort.EventText = eventText;

                    //eventshort.ActiveWeb = activeweb;
                    //eventshort.ActiveCommunityApp = activecommunity;

                    eventshort.PublishedOn = publishedon;

                    eventshort.VideoUrl = videourl;
                    //eventshort.TechnologyFields = technologyfields;
                    //eventshort.CustomTagging = customtagging;

                    eventshort.TagIds = tagids;

                    if (!String.IsNullOrEmpty(webadress))
                        eventshort.WebAddress = webadress;

                    eventshort.SoldOut = soldout;

                    //eventshort.EventDocument = eventdocument;
                    eventshort.Documents = document;

                    eventshort.ExternalOrganizer = externalorganizer;

                    //New If CompanyName is Noi - blablabla assign TechnologyField automatically and Write to Display5 if not empty "NOI"
                    if (
                        !String.IsNullOrEmpty(eventshort.CompanyName)
                        && eventshort.CompanyName.StartsWith("NOI - ")
                    )
                    {
                        if (String.IsNullOrEmpty(eventshort.Display5))
                            eventshort.Display5 = "NOI";

                        //MODIFIED
                        eventshort.TagIds = AssignTechnologyfieldsautomatically(
                            eventshort.CompanyName,
                            eventshort.TechnologyFields
                        );
                        //eventshort.TechnologyFields = AssignTechnologyfieldsautomatically(eventshort.CompanyName, eventshort.TechnologyFields);
                    }

                    //Set Publishers in base of Displays
                    //Eurac Videowall
                    if (eventshort.Display1 == "Y")
                        publishedon.TryAddOrUpdateOnList("eurac-videowall");
                    if (eventshort.Display1 == "N")
                        publishedon.TryRemoveOnList("eurac-videowall");
                    //Eurac Videowall
                    if (eventshort.Display2 == "Y")
                        publishedon.TryAddOrUpdateOnList("eurac-seminarroom");
                    if (eventshort.Display2 == "N")
                        publishedon.TryRemoveOnList("eurac-seminarroom");
                    //today.noi.bz.it
                    if (eventshort.Display3 == "Y")
                        publishedon.TryAddOrUpdateOnList("today.noi.bz.it");
                    if (eventshort.Display3 == "N")
                        publishedon.TryRemoveOnList("today.noi.bz.it");
                    //Eurac Videowall
                    if (eventshort.Display4 == "Y")
                        publishedon.TryAddOrUpdateOnList("noi-totem");
                    if (eventshort.Display4 == "N")
                        publishedon.TryRemoveOnList("noi-totem");

                    //Fix when TagIds are set lets update the Tags Object
                    if (eventshort.TagIds != null && eventshort.TagIds.Count > 0)
                    {
                        //Populate Tags (Id/Source/Type)
                        await eventshort.UpdateTagsExtension(QueryFactory);
                    }

                    var queryresult = await InsertDataToDB(
                        eventshort,
                        new KeyValuePair<string, EBMSEventREST>(
                            eventebms.EventId.ToString(),
                            eventebms
                        )
                    );

                    newcounter = newcounter + queryresult.created ?? 0;
                    updatecounter = updatecounter + queryresult.updated ?? 0;

                    WriteLog.LogToConsole(
                        idtoreturn,
                        "dataimport",
                        "single.eventeuracnoi",
                        new ImportLog()
                        {
                            sourceid = idtoreturn,
                            sourceinterface = "ebms.eventeuracnoi",
                            success = true,
                            error = "",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    idtoreturn,
                    "dataimport",
                    "single.eventeuracnoi",
                    new ImportLog()
                    {
                        sourceid = idtoreturn,
                        sourceinterface = "ebms.eventeuracnoi",
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
            EventShortLinked eventshort,
            KeyValuePair<string, EBMSEventREST> ebmsevent
        )
        {
            try
            {
                //Setting LicenseInfo
                eventshort.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<EventShort>(
                    eventshort,
                    Helper.LicenseHelper.GetLicenseforEventShort
                );
                //Check Languages
                eventshort.CheckMyInsertedLanguages();

                //Remove Set PublishedOn not set automatically
                //eventshort.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(ebmsevent);

                return await QueryFactory.UpsertData<EventShortLinked>(
                    eventshort,
                    new DataInfo("eventeuracnoi", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("ebms.eventshort.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, EBMSEventREST> eventebms)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "eurac",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(eventebms.Value),
                    sourceinterface = "ebms",
                    sourceid = eventebms.Key,
                    sourceurl = "https://emea-interface.ungerboeck.com",
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
                .Query("eventeuracnoi")
                .Select("data")
                .WhereRaw(
                    "(((to_date(data->> 'EndDate', 'YYYY-MM-DD') >= '"
                        + String.Format("{0:yyyy-MM-dd}", today)
                        + "'))) AND(data#>>'\\{Source\\}' = $$)",
                    "ebms"
                )
                .Where("gen_active", true);

            return await query.GetObjectListAsync<EventShortLinked>();
        }

        #endregion
    }
}
