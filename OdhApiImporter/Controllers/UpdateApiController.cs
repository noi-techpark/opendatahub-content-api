// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable disable

using DataModel;
using Helper;
using Helper.Generic;
using LTSAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OdhApiImporter.Helpers;
using OdhApiImporter.Helpers.DSS;
using OdhApiImporter.Helpers.LOOPTEC;
using OdhApiImporter.Helpers.LTSAPI;
using OdhApiImporter.Helpers.SuedtirolWein;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    public class UpdateApiController : Controller
    {
        private readonly ISettings settings;
        private readonly QueryFactory QueryFactory;
        private readonly ILogger<UpdateApiController> logger;
        private readonly IWebHostEnvironment env;
        private IOdhPushNotifier OdhPushnotifier;

        public UpdateApiController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<UpdateApiController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
        {
            this.env = env;
            this.settings = settings;
            this.logger = logger;
            this.QueryFactory = queryFactory;
            this.OdhPushnotifier = odhpushnotifier;
        }

        #region UPDATE FROM RAVEN INSTANCE

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("Raven/{datatype}/Update/{id}")]
        public async Task<IActionResult> UpdateDataFromRaven(
            string id,
            string datatype,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update Raven";
            string updatetype = "single";
            string source = "api";
            string otherinfo = datatype.ToLower();

            try
            {
                RavenImportHelper ravenimporthelper = new RavenImportHelper(
                    settings,
                    QueryFactory,
                    UrlGeneratorStatic("Raven/" + datatype),
                    OdhPushnotifier
                );
                var resulttuple = await ravenimporthelper.GetFromRavenAndTransformToPGObject(
                    id,
                    datatype,
                    cancellationToken
                );
                updatedetail = resulttuple.Item2;

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    resulttuple.Item1,
                    source,
                    operation,
                    updatetype,
                    "Update Raven succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    id,
                    source,
                    operation,
                    updatetype,
                    "Update Raven failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("Raven/{datatype}/Delete/{id}")]
        public async Task<IActionResult> DeleteDataFromRaven(
            string id,
            string datatype,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Delete Raven";
            string updatetype = "single";
            string source = "api";
            string otherinfo = datatype.ToLower();

            try
            {
                RavenImportHelper ravenimporthelper = new RavenImportHelper(
                    settings,
                    QueryFactory,
                    UrlGeneratorStatic("Raven/" + datatype),
                    OdhPushnotifier
                );
                var resulttuple = await ravenimporthelper.DeletePGObject(
                    id,
                    datatype,
                    cancellationToken
                );
                updatedetail = resulttuple.Item2;

                var deleteResult = GenericResultsHelper.GetSuccessUpdateResult(
                    resulttuple.Item1,
                    source,
                    operation,
                    updatetype,
                    "Delete Raven succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(deleteResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    id,
                    source,
                    operation,
                    updatetype,
                    "Delete Raven failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        #endregion
   
        #region REPROCESS PUSH FAILURE QUEUE
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("PushFailureQueue/Retry/{publishedon}")]
        public async Task<IActionResult> ElaborateFailureQueue(
            string publishedon,
            int elementstoprocess = 100,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetailFailureQueue updatedetail = default(UpdateDetailFailureQueue);
            string operation = "Update Failurequeue";
            string updatetype = "multiple";
            string source = "api";
            string otherinfo = "";

            try
            {
                var resulttuple = await OdhPushnotifier.PushFailureQueueToPublishedonService(
                    new List<string>() { publishedon },
                    elementstoprocess
                );

                updatedetail = new UpdateDetailFailureQueue()
                {
                    pushchannels = resulttuple.Keys,
                    pushed = resulttuple,
                };

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    "Elaborate Failurequeue succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    "Elaborate Failurequeue failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        #endregion

        #region CUSTOM PUSH SEND

        /// <summary>
        /// Send custom pushes to a publisher
        /// </summary>
        /// <param name="odhtype">pass the odhtype of the Entity</param>
        /// <param name="publishedon">insert the publisher</param>
        /// <param name="idlist">Pass the Idlist in the Body as json in this format ["",""]</param>
        /// <returns></returns>
        [Authorize(Roles = "DataPush")]
        [HttpPost, Route("CustomDataPush/{odhtype}/{publishedon}")]
        public async Task<IActionResult> CustomDataPush(
            string odhtype,
            string publishedon,
            [FromBody] List<string> idlist
        )
        {
            UpdateDetailFailureQueue updatedetail = default(UpdateDetailFailureQueue);
            string operation = "Update Custom";
            string updatetype = "multiple";
            string source = "api";
            string otherinfo = odhtype;

            try
            {
                var resulttuple = await OdhPushnotifier.PushCustomObjectsToPublishedonService(
                    new List<string>() { publishedon },
                    idlist,
                    odhtype,
                    null
                );

                updatedetail = new UpdateDetailFailureQueue()
                {
                    pushchannels = resulttuple.Keys,
                    pushed = resulttuple,
                };

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    "Custom Update succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    "Custom Update failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        #endregion

        #region EBMS DATA SYNC (EventShort)

        [HttpGet, Route("EBMS/EventShort/UpdateAll")]
        [HttpGet, Route("EBMS/EventShort/Update")]
        public async Task<IActionResult> UpdateAllEBMS(
            bool forceupdate = false,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update EBMS";
            string updatetype = GetUpdateType(null);
            string source = "ebms";

            try
            {
                EbmsEventsImportHelper ebmsimporthelper = new EbmsEventsImportHelper(
                    settings,
                    QueryFactory,
                    "eventeuracnoi",
                    UrlGeneratorStatic("EBMS/EventShort")
                );

                if (forceupdate)
                    ebmsimporthelper.forceupdate = true;

                updatedetail = await ebmsimporthelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "EBMS Eventshorts update succeeded",
                    "",
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "EBMS Eventshorts update failed",
                    "",
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region NINJA DATA SYNC

        //Events Centro Trevi and DRIN
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("NINJA/Events/Update")]
        public async Task<IActionResult> UpdateAllNinjaEvents(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update Ninja Events";
            string updatetype = GetUpdateType(null);
            string source = "mobilityapi";

            try
            {
                MobilityEventsImportHelper ninjaimporthelper = new MobilityEventsImportHelper(
                    settings,
                    QueryFactory,
                    "events",
                    UrlGeneratorStatic("NINJA/Events")
                );
                updatedetail = await ninjaimporthelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Ninja Events update succeeded",
                    "",
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Ninja Events update failed",
                    "",
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(errorResult);
            }
        }

        //Events Centro Trevi and DRIN
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("NINJA/EventFlattened/Update")]
        public async Task<IActionResult> UpdateAllNinjaEventsV2(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update Ninja EventsFlattened";
            string updatetype = GetUpdateType(null);
            string source = "mobilityapi";

            try
            {
                MobilityEventsFlattenedImportHelper ninjaimporthelper = new MobilityEventsFlattenedImportHelper(
                    settings,
                    QueryFactory,
                    "eventsv2",
                    UrlGeneratorStatic("NINJA/EventFlattened")
                );
                updatedetail = await ninjaimporthelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Ninja EventsFlattened update succeeded",
                    "",
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Ninja EventsFlattened update failed",
                    "",
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(errorResult);
            }
        }

        //Events Centro Trevi and DRIN
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("NINJA/VenueFlattened/Update")]
        public async Task<IActionResult> UpdateAllNinjaVenuesFlattened(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update Ninja VenuesFlattened";
            string updatetype = GetUpdateType(null);
            string source = "mobilityapi";

            try
            {
                MobilityVenuesFlattenedImportHelper ninjaimporthelper = new MobilityVenuesFlattenedImportHelper(
                    settings,
                    QueryFactory,
                    "venuesv2",
                    UrlGeneratorStatic("NINJA/VenueFlattened")
                );
                updatedetail = await ninjaimporthelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Ninja VenuesFlattened update succeeded",
                    "",
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Ninja VenuesFlattened update failed",
                    "",
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(errorResult);
            }
        }

        //EchargingStations
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("NINJA/Echarging/Update")]
        public async Task<IActionResult> UpdateAllNinjaEchargingData(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update Ninja Echarging";
            string updatetype = GetUpdateType(null);
            string source = "mobilityapi";

            try
            {
                MobilityEchargingImportHelper ninjaimporthelper = new MobilityEchargingImportHelper(
                    settings,
                    QueryFactory,
                    "odhactivitypoi",
                    UrlGeneratorStatic("NINJA/Echarging")
                );
                updatedetail = await ninjaimporthelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Ninja Echarging update succeeded",
                    "",
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Ninja Echarging update failed",
                    "",
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(errorResult);
            }
        }

        #endregion

        #region SIAG DATA SYNC WEATHER

        [HttpGet, Route("Siag/Weather/Import")]
        public async Task<IActionResult> ImportWeather(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import Weather data";
            string updatetype = GetUpdateType(null);
            string source = "siag";
            string otherinfo = "actual";

            try
            {
                SiagWeatherImportHelper siagimporthelper = new SiagWeatherImportHelper(
                    settings,
                    QueryFactory,
                    "weatherdatahistory",
                    UrlGeneratorStatic("Siag/Weather")
                );
                updatedetail = await siagimporthelper.SaveWeatherToHistoryTable(cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import Weather data succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import Weather data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(errorResult);
            }
        }

        [HttpGet, Route("Siag/Weather/Import/{id}")]
        public async Task<IActionResult> ImportWeatherByID(
            string id,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import Weather data";
            string updatetype = "single";
            string source = "siag";
            string otherinfo = "byid";

            try
            {
                SiagWeatherImportHelper siagimporthelper = new SiagWeatherImportHelper(
                    settings,
                    QueryFactory,
                    "weatherdatahistory",
                    UrlGeneratorStatic("Siag/Weather")
                );
                updatedetail = await siagimporthelper.SaveWeatherToHistoryTable(
                    cancellationToken,
                    id
                );
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    id,
                    source,
                    operation,
                    updatetype,
                    "Import Weather data succeeded id:" + id.ToString(),
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import Weather data failed id:" + id.ToString(),
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region SIAG DATA SYNC MUSEUMS

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("Siag/Museum/Update")]        
        public async Task<IActionResult> ImportSiagMuseum(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import SIAG Museum data";
            string updatetype = GetUpdateType(null);
            string source = "siag";
            string otherinfo = "actual";

            try
            {
                SiagMuseumImportHelper siagimporthelper = new SiagMuseumImportHelper(
                    settings,
                    QueryFactory,
                    "smgpois",
                    UrlGeneratorStatic("Siag/Museum")
                );
                updatedetail = await siagimporthelper.SaveDataToODH(null, null, cancellationToken);

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SIAG Museum data succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SIAG Museum data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("Siag/Museum/Tags/Update")]        
        public async Task<IActionResult> ImportSiagMuseumTags(
           CancellationToken cancellationToken = default
       )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import SIAG Museum Tag data";
            string updatetype = GetUpdateType(null);
            string source = "siag";
            string otherinfo = "actual";

            try
            {
                SiagMuseumTagImportHelper siagimporthelper = new SiagMuseumTagImportHelper(
                    settings,
                    QueryFactory,
                    "tags",
                    UrlGeneratorStatic("Siag/Museum")
                );
                updatedetail = await siagimporthelper.SaveDataToODH(null, null, cancellationToken);

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SIAG Museum Tag data succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SIAG Museum Tag data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region SUEDTIROLWEIN DATA SYNC

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("SuedtirolWein/Company/Update")]
        public async Task<IActionResult> ImportSuedtirolWineCompany(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import SuedtirolWein Company data";
            string updatetype = GetUpdateType(null);
            string source = "suedtirolwein";
            string otherinfo = "actual";

            try
            {
                SuedtirolWeinCompanyImportHelper sweinimporthelper =
                    new SuedtirolWeinCompanyImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        UrlGeneratorStatic("SuedtirolWein/Company"),
                        OdhPushnotifier
                    );
                updatedetail = await sweinimporthelper.SaveDataToODH(null, null, cancellationToken);

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SuedtirolWein Company data succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SuedtirolWein Company data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("SuedtirolWein/WineAward/Update")]
        public async Task<IActionResult> ImportSuedtirolWineAward(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import SuedtirolWein WineAward data";
            string updatetype = GetUpdateType(null);
            string source = "suedtirolwein";
            string otherinfo = "actual";

            try
            {
                SuedtirolWeinAwardImportHelper sweinimporthelper =
                    new SuedtirolWeinAwardImportHelper(
                        settings,
                        QueryFactory,
                        "wines",
                        UrlGeneratorStatic("SuedtirolWein/WineAward")
                    );
                updatedetail = await sweinimporthelper.SaveDataToODH(null, null, cancellationToken);

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SuedtirolWein WineAward data succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SuedtirolWein WineAward data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region COMMON SKIAREA

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("Common/SkiAreaToODHActivityPoi")]
        public async Task<IActionResult> ImportSkiAreaToODHActivityPoi(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import SkiArea To ODHActivityPoi data";
            string updatetype = GetUpdateType(null);
            string source = "common";
            string otherinfo = "actual";

            try
            {
                SkiAreasToODHActivityPoisImportHelper skiareasimporthelper =
                    new SkiAreasToODHActivityPoisImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        UrlGeneratorStatic("Common/Skiarea")
                    );
                updatedetail = await skiareasimporthelper.SaveDataToODH(null, null, cancellationToken);

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SkiArea To ODHActivityPoi data succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import SkiArea To ODHActivityPoi data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region LTS ACCOMMODATION DATA SYNC

        //Adds the Cincode to the Accommodation
        [HttpGet, Route("LTS/Accommodation/Update/CinCode/{id}")]
        public async Task<IActionResult> ImportLTSAccommodationCinCode(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Accommodation Cincode";
            string updatetype = GetUpdateType(new List<string>() { id });
            string source = "lts";
            string otherinfo = "accommodation.cin";

            try
            {
                //LtsApi ltsapi = new LtsApi(settings.LtsCredentials);

                LtsApi ltsapi = new LtsApi(
                    settings.LtsCredentials.serviceurl,
                    settings.LtsCredentials.username,
                    settings.LtsCredentials.password,
                    settings.LtsCredentials.ltsclientid,
                    false
                );
                var qs = new LTSQueryStrings() { page_size = 1, fields = "cinCode" };
                var dict = ltsapi.GetLTSQSDictionary(qs);

                var ltsdata = await ltsapi.AccommodationDetailRequest(id, dict);

                //Todo parse response
                var cincode =
                    ltsdata.FirstOrDefault()["data"] != null
                        ? ltsdata.FirstOrDefault()["data"].Value<string?>("cinCode")
                        : null;

                //Load Accommodation and add Cincode to Mapping
                var query = QueryFactory
                    .Query()
                    .SelectRaw("data")
                    .From("accommodations")
                    .Where("id", id.ToUpper());

                var accommodation = await query.GetObjectSingleAsync<AccommodationLinked>();

                if (accommodation != null)
                {
                    var ltsdict = accommodation.Mapping["lts"];
                    if (ltsdict == null)
                    {
                        ltsdict = new Dictionary<string, string>();
                    }
                    ltsdict.TryAddOrUpdate("cincode", cincode);

                    accommodation.Mapping.TryAddOrUpdate("lts", ltsdict);

                    //Save the Accommodation
                    var result = await QueryFactory.UpsertData<AccommodationLinked>(
                        accommodation,
                        new DataInfo("accommodations", CRUDOperation.Update)
                        {
                            ErrorWhendataIsNew = false,
                        },
                        new EditInfo("lts.push.import", "odh.importer"),
                        new CRUDConstraints(),
                        new CompareConfig(true, false)
                    );

                    //Check if the Object has Changed and Push all infos to the channels
                    if (
                        result.objectchanged != null
                        && result.objectchanged > 0
                        && result.pushchannels != null
                        && result.pushchannels.Count > 0
                    )
                    {
                        var additionalpushinfo = new Dictionary<string, bool>()
                        {
                            { "imageschanged", false },
                        };

                        result.pushed = await OdhPushnotifier.PushToPublishedOnServices(
                            id,
                            "accommodation",
                            "lts.push",
                            additionalpushinfo,
                            false,
                            "api",
                            result.pushchannels.ToList()
                        );
                    }

                    updatedetail = new UpdateDetail()
                    {
                        created = result.created,
                        updated = result.updated,
                        deleted = result.deleted,
                        error = result.error,
                        comparedobjects =
                            result.compareobject != null && result.compareobject.Value ? 1 : 0,
                        objectchanged = result.objectchanged,
                        objectimagechanged = result.objectimagechanged,
                        pushchannels = result.pushchannels,
                        changes = result.changes,
                        pushed = result.pushed,
                    };
                }

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodation CinCode data succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodation CinCode data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region HGV ACCOMMODATION DATA SYNC

        #endregion

        #region LTS MEASURINGPOINTS DATA SYNC

        #endregion

        #region LTS WEBCAM DATA SYNC

        #endregion

        #region STA POI DATA SYNC

        //[Authorize(Roles = "DataWriter,STAPoiImport")]
        [HttpPost, Route("STA/VendingPoints/Update")]
        public async Task<IActionResult> SendVendingPointsFromSTA(
            CancellationToken cancellationToken
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import Vendingpoints";
            string updatetype = GetUpdateType(null);
            string source = "xls";
            string otherinfo = "STA";

            try
            {
                StaVendingpointsImportHelper staimporthelper = new StaVendingpointsImportHelper(
                    settings,
                    QueryFactory,
                    UrlGeneratorStatic("STA/Vendingpoints")
                );

                updatedetail = await staimporthelper.PostVendingPointsFromSTA(
                    Request,
                    cancellationToken
                );

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    "Import Vendingpoints succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    "Import Vendingpoints failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        #endregion

        #region DSS DATA SYNC

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("DSS/{dssentity}/Update")]
        public async Task<IActionResult> UpdateAllDSSData(
            string dssentity,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update DSS " + dssentity;
            string updatetype = GetUpdateType(null);
            string source = "dss";
            string otherinfo = "actual";

            try
            {
                switch (dssentity.ToLower())
                {
                    case "webcam":

                        DSSWebcamImportHelper dsswebcamimporthelper = new DSSWebcamImportHelper(
                            settings,
                            QueryFactory,
                            "webcams",
                            UrlGeneratorStatic("DSS/Webcam")
                        );

                        updatedetail = await dsswebcamimporthelper.SaveDataToODH(
                            null,
                            null,
                            cancellationToken
                        );
                        break;

                    default:
                        DSSImportHelper dssimporthelper = new DSSImportHelper(
                            settings,
                            QueryFactory,
                            "smgpois",
                            UrlGeneratorStatic("DSS/" + dssentity)
                        );
                        dssimporthelper.entitytype = dssentity.ToLower();

                        updatedetail = await dssimporthelper.SaveDataToODH(
                            null,
                            null,
                            cancellationToken
                        );
                        break;
                }

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "DSS " + dssentity + " update succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "DSS " + dssentity + " update failed",
                    "",
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("DSS/SkiArea/Update")]
        public async Task<IActionResult> UpdateDSSSkiAreas(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update DSS skiarea";
            string updatetype = GetUpdateType(null);
            string source = "dss";
            string otherinfo = "actual";

            try
            {
                DSSSkiAreaImportHelper dssimporthelper = new DSSSkiAreaImportHelper(
                    settings,
                    QueryFactory,
                    "skiareas",
                    UrlGeneratorStatic("DSS/SkiArea")
                );

                updatedetail = await dssimporthelper.SaveDataToODH(null, null, cancellationToken);

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "DSS skiarea update succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "DSS skiarea update failed",
                    "",
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        #endregion

        #region EJOBS DATA SYNC

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LOOPTEC/Ejobs/Update")]
        public async Task<IActionResult> UpdateAllLooptecEjobs(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import Looptec Ejobs";
            string updatetype = GetUpdateType(null);
            string source = "looptec";
            string otherinfo = "rawonly";

            try
            {
                LooptecEjobsImportHelper looptecejobsimporthelper = new LooptecEjobsImportHelper(
                    settings,
                    QueryFactory,
                    "",
                    UrlGeneratorStatic("LOOPTEC/Ejobs")
                );

                updatedetail = await looptecejobsimporthelper.SaveDataToODH(
                    null,
                    null,
                    cancellationToken
                );
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import Looptec Ejobs succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import Looptec Ejobs failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region PANOMAX DATA SYNC

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("PANOMAX/Webcam/Update")]
        public async Task<IActionResult> UpdateAllPanomaxWebcams(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import PANOMAX Webcam";
            string updatetype = GetUpdateType(null);
            string source = "panomax";
            string otherinfo = "";

            try
            {
                PanomaxImportHelper panomaximporthelper = new PanomaxImportHelper(
                    settings,
                    QueryFactory,
                    "webcams",
                    UrlGeneratorStatic("PANOMAX/Webcam")
                );

                updatedetail = await panomaximporthelper.SaveDataToODH(
                    null,
                    null,
                    cancellationToken
                );
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import PANOMAX Webcam succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import PANOMAX Webcam failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region PANOCLOUD DATA SYNC

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("PANOCLOUD/Webcam/Update")]
        public async Task<IActionResult> UpdateAllPanocloudWebcams(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import PANOCLOUD Webcam";
            string updatetype = GetUpdateType(null);
            string source = "panocloud";
            string otherinfo = "";

            try
            {
                PanocloudImportHelper panocloudimporthelper = new PanocloudImportHelper(
                    settings,
                    QueryFactory,
                    "webcams",
                    UrlGeneratorStatic("PANOCLOUD/Webcam")
                );

                updatedetail = await panocloudimporthelper.SaveDataToODH(
                    null,
                    null,
                    cancellationToken
                );
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import PANOCLOUD Webcam succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import PANOCLOUD Webcam failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region FERATEL DATA SYNC

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("FERATEL/Webcam/Update")]
        public async Task<IActionResult> UpdateAllFeratelWebcams(
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import FERATEL Wecam";
            string updatetype = GetUpdateType(null);
            string source = "feratel";
            string otherinfo = "";

            try
            {
                FeratelWebcamImportHelper feratelwebcamimporthelper = new FeratelWebcamImportHelper(
                    settings,
                    QueryFactory,
                    "webcams",
                    UrlGeneratorStatic("FERATEL/Wecam")
                );

                updatedetail = await feratelwebcamimporthelper.SaveDataToODH(
                    null,
                    null,
                    cancellationToken
                );
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import FERATEL Wecam succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import FERATEL Wecam failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region A22 DATA SYNC

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("A22/{a22entity}/Update")]
        public async Task<IActionResult> UpdateAllA22Data(
            string a22entity,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import A22 " + a22entity;
            string updatetype = GetUpdateType(null);
            string source = "a22";
            string otherinfo = a22entity;

            try
            {
                IImportHelper a22importhelper;

                switch (a22entity.ToLower())
                {
                    case "webcam":

                        a22importhelper = new A22WebcamImportHelper(
                            settings,
                            QueryFactory,
                            "webcams",
                            UrlGeneratorStatic("A22/Webcam")
                        );
                        updatedetail = await a22importhelper.SaveDataToODH(
                            null,
                            null,
                            cancellationToken
                        );

                        break;
                    case "servicearea":
                        a22importhelper = new A22PoiImportHelper(
                            settings,
                            QueryFactory,
                            "smgpois",
                            UrlGeneratorStatic("A22/ServiceArea"),
                            a22entity.ToLower()
                        );
                        updatedetail = await a22importhelper.SaveDataToODH(
                            null,
                            null,
                            cancellationToken
                        );

                        break;
                    case "tollstation":
                        a22importhelper = new A22PoiImportHelper(
                            settings,
                            QueryFactory,
                            "smgpois",
                            UrlGeneratorStatic("A22/Tollstation"),
                            a22entity.ToLower()
                        );
                        updatedetail = await a22importhelper.SaveDataToODH(
                            null,
                            null,
                            cancellationToken
                        );

                        break;
                }

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import A22 " + a22entity + " succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import A22 " + a22entity + " failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region LTS NEW API SYNC

        //Imports all Guestcards
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/SuedtirolGuestpass/Update/Cardtypes")]
        public async Task<IActionResult> ImportLTSSuedtirolGuestPassCardTypes(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS SuedtirolGuestpass Cardtypes";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "accommodations.suedtirolguestpass.cardtypes";

            try
            {
                LTSApiGuestCardImportHelper importhelper = new LTSApiGuestCardImportHelper(
                    settings,
                    QueryFactory,
                    "tags",
                    UrlGeneratorStatic("LTS/SuedtirolGuestpass/Cardtypes")
                );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS SuedtirolGuestpass Cardtypes succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS SuedtirolGuestpass Cardtypes data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Event Tags
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Event/Update/Tags")]
        public async Task<IActionResult> ImportLTSEventTags(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Events Tags";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "events.tags";

            try
            {
                LTSApiEventTagImportHelper importhelper = new LTSApiEventTagImportHelper(
                    settings,
                    QueryFactory,
                    "tags",
                    UrlGeneratorStatic("LTS/Events/Tags")
                );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Events Tags succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Events Tags data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Event Tags
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Event/Update/Tags/ToODHTags")]
        public async Task<IActionResult> ImportLTSEventTagsToODHTags(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Events Tags To ODH Tags";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "events.tags";

            try
            {
                LTSApiEventTagsToODHTagImportHelper importhelper =
                    new LTSApiEventTagsToODHTagImportHelper(
                        settings,
                        QueryFactory,
                        "smgtags",
                        UrlGeneratorStatic("LTS/Events/Tags"),
                        OdhPushnotifier
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Events Tags succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Events Tags data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Event Categories
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Event/Update/Categories")]
        public async Task<IActionResult> ImportLTSEventCategories(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Events Categories";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "events.categories";

            try
            {
                LTSApiEventCategoriesImportHelper importhelper =
                    new LTSApiEventCategoriesImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Events/Categories")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Events Categories succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Events Categories data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Event Categories
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Event/Update/Classifications")]
        public async Task<IActionResult> ImportLTSEventClassifications(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Events Classifications";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "events.classifications";

            try
            {
                LTSApiEventClassificationsImportHelper importhelper =
                    new LTSApiEventClassificationsImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Events/Classifications")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Events Classifications succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Events Classifications data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Gastronomy Categories
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Gastronomy/Update/Categories")]
        public async Task<IActionResult> ImportLTSGastronomyCategories(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Gastronomies Categories";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "gastronomies.categories";

            try
            {
                LTSApiGastronomyCategoriesImportHelper importhelper =
                    new LTSApiGastronomyCategoriesImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Gastronomies/Categories")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Gastronomies Categories succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Gastronomies Categories data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Gastronomy Facilities
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Gastronomy/Update/Facilities")]
        public async Task<IActionResult> ImportLTSGastronomyFacilities(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Gastronomies Facilities";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "gastronomies.facilities";

            try
            {
                LTSApiGastronomyFacilitiesImportHelper importhelper =
                    new LTSApiGastronomyFacilitiesImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Gastronomies/Facilities")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Gastronomies Facilities succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Gastronomies Facilities data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Gastronomy CeremonyCodes
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Gastronomy/Update/CeremonyCodes")]
        public async Task<IActionResult> ImportLTSGastronomyCeremonyCodes(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Gastronomies CeremonyCodes";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "gastronomies.ceremonycodes";

            try
            {
                LTSApiCeremonyCodesFacilitiesImportHelper importhelper =
                    new LTSApiCeremonyCodesFacilitiesImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Gastronomies/CeremonyCodes")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Gastronomies CeremonyCodes succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Gastronomies CeremonyCodes data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Gastronomy DishCodes
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Gastronomy/Update/DishCodes")]
        public async Task<IActionResult> ImportLTSGastronomyDishCodes(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Gastronomies DishCodes";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "gastronomies.dishcodes";

            try
            {
                LTSApiGastronomyDishCodesImportHelper importhelper =
                    new LTSApiGastronomyDishCodesImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Gastronomies/DishCodes")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Gastronomies DishCodes succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Gastronomies DishCodes data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Accommodation Categories
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Accommodation/Update/Categories")]
        public async Task<IActionResult> ImportLTSAccommodationCategories(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Accommodations Categories";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "accommodations.categories";

            try
            {
                LTSApiAccommodationCategoriesImportHelper importhelper =
                    new LTSApiAccommodationCategoriesImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Accommodations/Categories")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodations Categories succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodations Categories data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Accommodation Mealplans
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Accommodation/Update/Mealplans")]
        public async Task<IActionResult> ImportLTSAccommodationMealplans(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Accommodations Mealplans";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "accommodations.mealplans";

            try
            {
                LTSApiAccommodationMealplansImportHelper importhelper =
                    new LTSApiAccommodationMealplansImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Accommodations/Mealplans")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodations Mealplans succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodations Mealplans data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Accommodation Types
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Accommodation/Update/Types")]
        public async Task<IActionResult> ImportLTSAccommodationTypes(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Accommodation Types";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "accommodation.types";

            try
            {
                LTSApiAccommodationTypesImportHelper importhelper =
                    new LTSApiAccommodationTypesImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Accommodation/Types")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodation Types succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodation Types data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Accommodation Amenities
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Accommodation/Update/Amenities")]
        public async Task<IActionResult> ImportLTSAccommodationAmenities(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Accommodations Amenities";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "accommodations.amenities";

            try
            {
                LTSApiAmenitiesImportHelper importhelper = new LTSApiAmenitiesImportHelper(
                    settings,
                    QueryFactory,
                    "tags",
                    UrlGeneratorStatic("LTS/Accommodations/Amenities")
                );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodations Amenities succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Accommodations Amenities data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Venues Categories
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Venue/Update/Categories")]
        public async Task<IActionResult> ImportLTSVenueCategories(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Venues Categories";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "venues.categories";

            try
            {
                LTSApiVenueCategoriesImportHelper importhelper =
                    new LTSApiVenueCategoriesImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Venues/Categories")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Venues Categories succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Venues Categories data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all Venues HallFeatures
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/Venue/Update/HallFeatures")]
        public async Task<IActionResult> ImportLTSVenueHallFeatures(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS Venues HallFeatures";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "venues.hallfeatures";

            try
            {
                LTSApiVenueHallFeaturesImportHelper importhelper =
                    new LTSApiVenueHallFeaturesImportHelper(
                        settings,
                        QueryFactory,
                        "tags",
                        UrlGeneratorStatic("LTS/Venues/HallFeatures")
                    );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Venues HallFeatures succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS Venues HallFeatures data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all ODHActivityPois Tags
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/ODHActivityPoi/Update/Tags")]
        public async Task<IActionResult> ImportLTSODHActivityPoiTag(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS ODHActivityPois Tags";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "odhactivitypoi.tags";

            try
            {
                LTSApiTagImportHelper importhelper = new LTSApiTagImportHelper(
                    settings,
                    QueryFactory,
                    "tags",
                    UrlGeneratorStatic("LTS/ODHActivityPois/Tags")
                );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS ODHActivityPois Tags succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS ODHActivityPois Tags data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        //Imports all ODHActivityPois TagProperties
        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("LTS/ODHActivityPoi/Update/TagProperties")]
        public async Task<IActionResult> ImportLTSODHActivityPoiTagProperties(
            string id = null,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import LTS ODHActivityPois TagProperties";
            string updatetype = GetUpdateType(null);
            string source = "lts";
            string otherinfo = "odhactivitypoi.tagproperties";

            try
            {
                LTSApiTagPropertyImportHelper importhelper = new LTSApiTagPropertyImportHelper(
                    settings,
                    QueryFactory,
                    "tags",
                    UrlGeneratorStatic("LTS/ODHActivityPois/TagProperties")
                );

                updatedetail = await importhelper.SaveDataToODH(null, null, cancellationToken);
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS ODHActivityPois TagProperties succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import LTS ODHActivityPois TagProperties data failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }       

        #endregion

        #region UPDATE DATA DIRECTLY FROM LTS API

        //Dispatcher (to redirect type odhactivitypoi to the right interface)
        [HttpGet, Route("SyncDataFromLTS/{datatype}/{id}")]
        [Authorize(Roles = "DataPush")]
        public async Task<IActionResult> SyncDataFromLTSDispatcher(
            string id,
            string datatype,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update LTS";
            string updatetype = "single.synctrigger";
            string source = "api";
            string otherinfo = datatype.ToLower();

            try
            {
                string idtopass = id;
                string datatypecalculated = datatype;

                if (datatype.ToLower() == "odhactivitypoi")
                {
                    var data = await QueryFactory
                   .Query("smgpois")
                   .Select("data")
                   .Where("id", id.ToLower())                   
                   .FilterDataByAccessRoles(new List<string>() { "IDM" })
                   .GetObjectSingleAsync<ODHActivityPoiLinked>();

                    idtopass = id.ToLower().Replace("smgpoi", "");

                    if (data == null)
                        throw new Exception("Data not found");
                    else
                    {
                        switch(data.SyncSourceInterface)
                        {
                            case "gastronomicdata": datatypecalculated = "gastronomy";
                                break;
                            case "activitydata":
                                datatypecalculated = "activity";
                                break;
                            case "poidata":
                                datatypecalculated = "poi";
                                break;
                            default: datatypecalculated = datatypecalculated;
                                break;
                        }
                    }
                }

               

                Tuple<string,UpdateDetail> resulttuple = default(Tuple<string,UpdateDetail>);

                if (datatype == "accommodation")
                {
                    RavenImportHelper ravenimporthelper = new RavenImportHelper(
                        settings,
                        QueryFactory,
                        UrlGeneratorStatic("Raven/" + datatype),
                        OdhPushnotifier
                    );
                    resulttuple = await ravenimporthelper.GetFromRavenAndTransformToPGObject(
                        id,
                        datatype,
                        cancellationToken
                    );
                    updatedetail = resulttuple.Item2;
                }
                else
                {
                    LTSAPIImportHelper ltsapiimporthelper = new LTSAPIImportHelper(
                       settings,
                       QueryFactory,
                       UrlGeneratorStatic("LTS/" + datatypecalculated),
                       OdhPushnotifier
                   );

                    resulttuple = await ltsapiimporthelper.UpdateSingleDataFromLTSApi(
                        idtopass,
                        datatypecalculated,
                        cancellationToken
                    );
                    updatedetail = resulttuple.Item2;
                }

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    resulttuple.Item1,
                    source,
                    operation,
                    updatetype,
                    "Update LTS succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    id,
                    source,
                    operation,
                    updatetype,
                    "Update LTS failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }    
        }

        [HttpPost, Route("SyncDataFromLTS/{datatype}/{id}")]
        [Authorize(Roles = "DataPush")]
        public async Task<IActionResult> SyncDataFromLTSDispatcherPost(
            string id,
            string datatype,
            CancellationToken cancellationToken = default
        )
        {
            return await SyncDataFromLTSDispatcher(id, datatype, cancellationToken);
        }


        //Generic Update Single Dataset
        [HttpGet, Route("LTS/{datatype}/Update/{id}")]
        [Authorize(Roles = "DataPush")]
        public async Task<IActionResult> UpdateDataFromLTS(
            string id,
            string datatype,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update LTS";
            string updatetype = "single";
            string source = "api";
            string otherinfo = datatype.ToLower();

            try
            {
                LTSAPIImportHelper ltsapiimporthelper = new LTSAPIImportHelper(
                    settings,
                    QueryFactory,
                    UrlGeneratorStatic("LTS/" + datatype),
                    OdhPushnotifier
                );
                var resulttuple = await ltsapiimporthelper.UpdateSingleDataFromLTSApi(
                    id,
                    datatype,
                    cancellationToken
                );
                updatedetail = resulttuple.Item2;

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    resulttuple.Item1,
                    source,
                    operation,
                    updatetype,
                    "Update LTS succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    id,
                    source,
                    operation,
                    updatetype,
                    "Update LTS failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        /// <summary>
        /// Generic LastChanged Sync
        /// </summary>
        /// <param name="date">Pass a date in format (yyyy-MM-dd hh:mm) or also supported '2h','3d','15m','1w'...</param>
        /// <param name="datatype">Pass a valid LTS Datatype</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet, Route("LTS/{datatype}/UpdateLastChanged/{date}")]
        [Authorize(Roles = "DataPush")]
        public async Task<IActionResult> UpdateLastChangedDataFromLTS(
            string date,
            string datatype,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update LTS";
            string updatetype = "lastchanged";
            string source = "api";
            string otherinfo = datatype.ToLower();

            try
            {
                //supported '2h', '3d', '15m', or '1w'
                if (!DateTime.TryParse(date, out DateTime lastchangeddate))
                    lastchangeddate = DateTimeHelper.SubtractFromNow(date);

                LTSAPIImportHelper ltsapiimporthelper = new LTSAPIImportHelper(
                    settings,
                    QueryFactory,
                    UrlGeneratorStatic("LTS/" + datatype),
                    OdhPushnotifier
                );
                var result = await ltsapiimporthelper.UpdateLastChangedDataFromLTSApi(
                    lastchangeddate,
                    datatype,
                    cancellationToken
                );

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    result.Item1,
                    source,
                    operation,
                    updatetype,
                    "Update LTS succeeded",
                    otherinfo,
                    result.Item2,
                    true);

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    "Update LTS failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        //Generic Deleted Sync        
        [HttpGet, Route("LTS/{datatype}/UpdateDeleted/{date}")]
        [Authorize(Roles = "DataPush")]
        public async Task<IActionResult> UpdateDeletedDataFromLTS(
            string date,
            string datatype,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update LTS";
            string updatetype = "deleted";
            string source = "api";
            string otherinfo = datatype.ToLower();

            try
            {
                //supported '2h', '3d', '15m', or '1w'
                if (!DateTime.TryParse(date, out DateTime lastchangeddate))
                    lastchangeddate = DateTimeHelper.SubtractFromNow(date);
             
                LTSAPIImportHelper ltsapiimporthelper = new LTSAPIImportHelper(
                    settings,
                    QueryFactory,
                    UrlGeneratorStatic("LTS/" + datatype),
                    OdhPushnotifier
                );
                var result = await ltsapiimporthelper.UpdateDeletedDataFromLTSApi(
                    lastchangeddate,
                    datatype,
                    cancellationToken
                );

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    result.Item1,
                    source,
                    operation,
                    updatetype,
                    "Update LTS succeeded",
                    otherinfo,
                    result.Item2,
                    true);

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    "Update LTS failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        //Generic Active/Inactive Sync        
        [HttpGet, Route("LTS/{datatype}/UpdateActiveInactive/{onlyactive}")]
        [Authorize(Roles = "DataPush")]
        public async Task<IActionResult> UpdateActiveInactiveDataFromLTS(
            bool onlyactive,
            string datatype,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Update LTS";
            string updatetype = "activeinactive";
            string source = "api";
            string otherinfo = datatype.ToLower();

            try
            {
               LTSAPIImportHelper ltsapiimporthelper = new LTSAPIImportHelper(
                    settings,
                    QueryFactory,
                    UrlGeneratorStatic("LTS/" + datatype),
                    OdhPushnotifier
                );
                var result = await ltsapiimporthelper.UpdateActiveInactiveDataFromLTSApi(
                    onlyactive,
                    datatype,
                    cancellationToken
                );

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    result.Item1,
                    source,
                    operation,
                    updatetype,
                    "Update LTS succeeded",
                    otherinfo,
                    result.Item2,
                    true);

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    "Update LTS failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        [HttpGet, Route("LTS/{datatype}/Delete/{id}")]
        [Authorize(Roles = "DataPush")]
        public async Task<IActionResult> DeleteDataFromLTS(
           string id,
           string datatype,
           CancellationToken cancellationToken = default
       )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Delete LTS";
            string updatetype = "single";
            string source = "api";
            string otherinfo = datatype.ToLower();

            try
            {
                //Deletes the reduced data, deactivates the full data and sends a push

                LTSAPIImportHelper ltsapiimporthelper = new LTSAPIImportHelper(
                    settings,
                    QueryFactory,
                    UrlGeneratorStatic("LTS/" + datatype),
                    OdhPushnotifier
                );
                var result = await ltsapiimporthelper.DeleteSingleDataFromLTSApi(
                    id,
                    datatype,
                    cancellationToken
                );
                updatedetail = result.Item2;

                var deleteResult = GenericResultsHelper.GetSuccessUpdateResult(
                    result.Item1,
                    source,
                    operation,
                    updatetype,
                    "Delete LTS succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(deleteResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    id,
                    source,
                    operation,
                    updatetype,
                    "Delete LTS failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }


        #endregion

        #region OUTDOORACTIVE DATA SYNC

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("OUTDOORACTIVE/Update/{datatype}")]
        public async Task<IActionResult> UpdateOutdoorActiveData(
            string datatype,
            string date,
            bool syncelevation = false,
            CancellationToken cancellationToken = default
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import Outdooractive " + datatype;
            string updatetype = GetUpdateType(null);
            string source = "outdooractive";
            string otherinfo = "rawonly";

            try
            {
                OutdoorActiveImportHelper outdooractiveimporthelper = new OutdoorActiveImportHelper(
                    settings,
                    QueryFactory,
                    "smgpois",
                    UrlGeneratorStatic("OUTDOORACTIVE/" + datatype)
                );

                //supported '2h', '3d', '15m', or '1w'
                if (!DateTime.TryParse(date, out DateTime lastchangeddate))
                    lastchangeddate = DateTimeHelper.SubtractFromNow(date);

                outdooractiveimporthelper.SetType(datatype);
                outdooractiveimporthelper.updatefrom = lastchangeddate;

                updatedetail = await outdooractiveimporthelper.SaveDataToODH(
                    null,
                    null,
                    cancellationToken
                );
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    $"Import Outdooractive {datatype} succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                     $"Import Outdooractive {datatype} failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region DIGIWAY

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("DIGIWAY/{identifier}/Update")]
        public async Task<IActionResult> UpdateAllDigiwayData(
            string identifier,
            CancellationToken cancellationToken = default
    )
        {
            //cyclewaystyrol
            //mountainbikeroutes
            //hikingtrails
            //intermunicipalcyclingroutes

            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import DIGIWAY " + identifier;
            string updatetype = GetUpdateType(null);
            string source = "digiway";
            string otherinfo = identifier;

            try
            {
                //search from Settings the identifier
                var digiwayconfig = settings.DigiWayConfig[identifier.ToLower()];

                if (digiwayconfig == null)
                    throw new Exception("unknown identifier");
                
                if (digiwayconfig.Source == "civis.geoserver" )
                {
                    DigiWayCivisJson2ODHActivityPoiImportHelper digiwayimporthelper = new DigiWayCivisJson2ODHActivityPoiImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        UrlGeneratorStatic("DIGIWAY/" + identifier.ToLower())
                    );

                    digiwayimporthelper.identifier = identifier.ToLower(); 
                    digiwayimporthelper.source = digiwayconfig.Source;

                    updatedetail = await digiwayimporthelper.SaveDataToODH(
                                            null,
                                            null,
                                            cancellationToken);
                }
                else if (digiwayconfig.Source == "dservices3.arcgis.com" && digiwayconfig.Format == "xml")
                {
                    DigiWayDServices3ArcgisWFSXml2ODHActivityPoiImportHelper digiwayimporthelper = new DigiWayDServices3ArcgisWFSXml2ODHActivityPoiImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        UrlGeneratorStatic("DIGIWAY/" + identifier.ToLower())
                    );

                    digiwayimporthelper.identifier = identifier.ToLower();
                    digiwayimporthelper.source = digiwayconfig.Source;
                    digiwayimporthelper.srid = digiwayconfig.SRid;

                    updatedetail = await digiwayimporthelper.SaveDataToODH(
                                            null,
                                            null,
                                            cancellationToken);
                }
                else if (digiwayconfig.Source == "siat.provincia.tn.it")
                {
                    DigiWaySiatTNShp2OdhActivityPoiImportHelper digiwayimporthelper = new DigiWaySiatTNShp2OdhActivityPoiImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        UrlGeneratorStatic("DIGIWAY/" + identifier.ToLower())
                    );

                    digiwayimporthelper.identifier = identifier.ToLower();
                    digiwayimporthelper.source = digiwayconfig.Source;
                    digiwayimporthelper.srid = digiwayconfig.SRid;

                    updatedetail = await digiwayimporthelper.SaveDataToODH(
                                            null,
                                            null,
                                            cancellationToken);
                }
                else if (digiwayconfig.Source == "euregio")
                {
                    DigiWayEuregioGeoJsonSpatialDataImportHelper digiwayimporthelper = new DigiWayEuregioGeoJsonSpatialDataImportHelper(
                        settings,
                        QueryFactory,
                        "spatialdatas",
                        UrlGeneratorStatic("DIGIWAY/" + identifier.ToLower())
                    );

                    digiwayimporthelper.identifier = identifier.ToLower();
                    digiwayimporthelper.source = digiwayconfig.Source;
                    digiwayimporthelper.srid = digiwayconfig.SRid;

                    updatedetail = await digiwayimporthelper.SaveDataToODH(
                                            null,
                                            null,
                                            cancellationToken);
                }
                else if (digiwayconfig.Source == "dservices3.arcgis.com" && digiwayconfig.Format == "geojson")
                {
                    DigiWayDServices3ArcgisGeoJson2ODHActivityPoiImportHelper digiwayimporthelper = new DigiWayDServices3ArcgisGeoJson2ODHActivityPoiImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        UrlGeneratorStatic("DIGIWAY/" + identifier.ToLower())
                    );

                    digiwayimporthelper.identifier = identifier.ToLower();
                    digiwayimporthelper.source = digiwayconfig.Source;
                    digiwayimporthelper.srid = digiwayconfig.SRid;

                    updatedetail = await digiwayimporthelper.SaveDataToODH(
                                            null,
                                            null,
                                            cancellationToken);
                }

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                        null,
                        source,
                        operation,
                        updatetype,
                        "Import DIGIWAY " + identifier + " succeeded",
                        otherinfo,
                        updatedetail,
                        true
                    );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import DIGIWAY " + identifier + " failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region ZOHO_DIGIWAY

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("Zoho/TrailClosures/Update")]
        public async Task<IActionResult> SaveZohoTrailClosuresDataToAnnouncements(
            CancellationToken cancellationToken
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = $"Import Zoho TrailClosures";
            string updatetype = GetUpdateType(null);
            string source = "json";
            string otherinfo = "digiway.zoho";

            try
            {
                DigiWayZohoGeoJson2AccouncementImportHelper digiwaytrailclosuresimporthelper = new DigiWayZohoGeoJson2AccouncementImportHelper(
                    settings,
                    QueryFactory,
                    "announcements",
                    UrlGeneratorStatic("ZOHO/TrailClosures")
                    );

                updatedetail = await digiwaytrailclosuresimporthelper.SaveDataToODH(
                    null,
                    null,
                    cancellationToken
                );

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                        "",
                        source,
                        operation,
                        updatetype,
                        $"Import Zoho TrailClosures succeeded",
                        otherinfo,
                        updatedetail,
                        true
                    );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    $"Import Zoho TrailClosures failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        #endregion

        #region GTFSAPI

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("GTFSAPI/StaTimeTables/Stops/Update")]
        public async Task<IActionResult> UpdateAllStaTimeTablesStops(
        CancellationToken cancellationToken = default
    )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = "Import GTFSAPI StaTimeTablesStops";
            string updatetype = GetUpdateType(null);
            string source = "gtfsapi";
            string otherinfo = "";

            try
            {
                GtfsApiStaTimeTablesImportHelper gtfsapiimporthelper = new GtfsApiStaTimeTablesImportHelper(
                    settings,
                    QueryFactory,
                    "smgpois",
                    UrlGeneratorStatic("GTFSAPI/StaTimeTablesStops")
                );

                updatedetail = await gtfsapiimporthelper.SaveDataToODH(
                    null,
                    null,
                    cancellationToken
                );
                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import GTFSAPI StaTimeTablesStops succeeded",
                    otherinfo,
                    updatedetail,
                    true
                );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var updateResult = GenericResultsHelper.GetErrorUpdateResult(
                    null,
                    source,
                    operation,
                    updatetype,
                    "Import GTFSAPI StaTimeTablesStops failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );
                return BadRequest(updateResult);
            }
        }

        #endregion

        #region HDS

        [Authorize(Roles = "DataWriter,HDSPoiImport")]
        [HttpPost, Route("HDS/MarketCalendar/Update/{type}")]
        public async Task<IActionResult> SaveHDSDataToPois(string type,
            CancellationToken cancellationToken
        )
        {
            UpdateDetail updatedetail = default(UpdateDetail);
            string operation = $"Import HDS {type}";
            string updatetype = GetUpdateType(null);
            string source = "csv";
            string otherinfo = "hds";

            try
            {
                HdsDataImportHelper hdsimporthelper = new HdsDataImportHelper(
                    settings,
                    QueryFactory,
                    UrlGeneratorStatic("HDS/MarketCalendar"),
                    type
                );
                
                updatedetail = await hdsimporthelper.PostHDSMarketCalendar(
                Request,
                cancellationToken
                );

                var updateResult = GenericResultsHelper.GetSuccessUpdateResult(
                        "",
                        source,
                        operation,
                        updatetype,
                        $"Import HDS {type} succeeded",
                        otherinfo,
                        updatedetail,
                        true
                    );

                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                var errorResult = GenericResultsHelper.GetErrorUpdateResult(
                    "",
                    source,
                    operation,
                    updatetype,
                    $"Import HDS {type} failed",
                    otherinfo,
                    updatedetail,
                    ex,
                    true
                );

                return BadRequest(errorResult);
            }
        }

        #endregion

        protected Func<string, string> UrlGeneratorStatic
        {
            get
            {
                return self =>
                {
                    var location = new Uri($"{Request.Scheme}://{Request.Host}/" + self);
                    return location.AbsoluteUri;
                };
            }
        }

        private static string GetUpdateType(List<string>? idlist, string lastchange = null)
        {
            if (idlist == null && lastchange == null)
                return "all";
            else if (idlist == null && lastchange != null)
                return "lastchanged";
            else if (idlist.Count == 1)
                return "single";
            else
                return "passed_ids";
        }
  
    }

    public static class DateTimeHelper
    {
        public static DateTime SubtractFromNow(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input string cannot be null or empty.");

            var match = Regex.Match(input, @"^(\d+)([smhdw])$", RegexOptions.IgnoreCase);

            if (!match.Success)
                throw new FormatException("Invalid format. Use e.g., '2h', '3d', '15m', or '1w'.");

            int value = int.Parse(match.Groups[1].Value);
            string unit = match.Groups[2].Value.ToLower();

            return unit switch
            {
                "s" => DateTime.Now.AddSeconds(-value),
                "m" => DateTime.Now.AddMinutes(-value),
                "h" => DateTime.Now.AddHours(-value),
                "d" => DateTime.Now.AddDays(-value),
                "w" => DateTime.Now.AddDays(-7 * value),
                _ => throw new NotSupportedException($"Time unit '{unit}' is not supported.")
            };
        }
    }
}
