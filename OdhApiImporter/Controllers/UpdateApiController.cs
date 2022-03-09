﻿#nullable disable

using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SqlKata.Execution;
using EBMS;
using NINJA;
using NINJA.Parser;
using System.Net.Http;
using RAVEN;
using Microsoft.Extensions.Hosting;
using OdhApiImporter.Helpers;
using OdhApiImporter.Helpers.DSS;

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

        public UpdateApiController(IWebHostEnvironment env, ISettings settings, ILogger<UpdateApiController> logger, QueryFactory queryFactory)
        {
            this.env = env;
            this.settings = settings;
            this.logger = logger;
            this.QueryFactory = queryFactory;
        }        

        #region UPDATE FROM RAVEN INSTANCE

        [HttpGet, Route("Raven/{datatype}/Update/{id}")]
        //[Authorize(Roles = "DataWriter,DataCreate,DataUpdate")]
        public async Task<IActionResult> UpdateFromRaven(string id, string datatype, CancellationToken cancellationToken = default)
        {
            try
            {
                RAVENImportHelper ravenimporthelper = new RAVENImportHelper(settings, QueryFactory);
                var result = await ravenimporthelper.GetFromRavenAndTransformToPGObject(id, datatype, cancellationToken);

                return Ok(new UpdateResult
                {
                    operation = "Update Raven",
                    updatetype = "single",
                    otherinfo = datatype,
                    message = "",
                    id = id,
                    recordsmodified = (result.created + result.updated + result.deleted),
                    created = result.created,
                    updated = result.updated,
                    deleted = result.deleted,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new UpdateResult
                {
                    operation = "Update Raven",
                    updatetype = "all",
                    otherinfo = datatype,
                    id = id,
                    message = "Update Raven failed: " + ex.Message,
                    recordsmodified = 0,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    success = false
                });
            }
        }

        #endregion

        #region EBMS DATA SYNC (EventShort)

        [HttpGet, Route("EBMS/EventShort/UpdateAll")]
        public async Task<IActionResult> UpdateAllEBMS(CancellationToken cancellationToken = default)
        {
            try
            {
                EBMSImportHelper ebmsimporthelper = new EBMSImportHelper(settings, QueryFactory, "eventeuracnoi");

                var result = await ebmsimporthelper.SaveDataToODH(null, cancellationToken);

                return Ok(new UpdateResult
                {
                    operation = "Update EBMS",
                    updatetype = "all",
                    otherinfo = "",
                    message = "EBMS Eventshorts update succeeded",
                    recordsmodified = (result.created + result.updated + result.deleted),
                    created = result.created,
                    updated = result.updated,
                    deleted = result.deleted,
                    success = true
                });
            }
            catch(Exception ex)
            {
                return BadRequest(new UpdateResult
                {
                    operation = "Update EBMS",
                    updatetype = "all",
                    otherinfo = "",
                    message = "EBMS Eventshorts update failed: " + ex.Message,
                    recordsmodified = 0,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    success = false
                });
            }
        }

        [HttpGet, Route("EBMS/EventShort/UpdateSingle/{id}")]
        public IActionResult UpdateSingleEBMS(string id, CancellationToken cancellationToken = default)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new { error = "Not Implemented" });

            //try
            //{               
            //    return Ok(new UpdateResult
            //    {
            //        operation = "Update EBMS",
            //        id = id,
            //        updatetype = "single",
            //        otherinfo = "",
            //        message = "EBMS Eventshorts update succeeded",
            //        recordsmodified = 1,
            //        created = 0,
            //        updated = 0,
            //        deleted = 0,
            //        success = true
            //    });
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new UpdateResult
            //    {
            //        operation = "Update EBMS",
            //        updatetype = "all",
            //        otherinfo = "",
            //        message = "EBMS Eventshorts update failed: " + ex.Message,
            //        recordsmodified = 0,
            //        created = 0,
            //        updated = 0,
            //        deleted = 0,
            //        success = false
            //    });
            //}
        }

        #endregion

        #region NINJA DATA SYNC (Events Centro Trevi and DRIN)

        [HttpGet, Route("NINJA/Events/UpdateAll")]
        public async Task<IActionResult> UpdateAllNinjaEvents(CancellationToken cancellationToken = default)
        {
            try
            {              
                NINJAImportHelper ninjaimporthelper = new NINJAImportHelper(settings, QueryFactory);
                var result = await ninjaimporthelper.SaveDataToODH(null, cancellationToken);

                return Ok(new UpdateResult
                {
                    operation = "Update Ninja Events",
                    updatetype = "all",
                    otherinfo = "",
                    message = "Ninja Events update succeeded",
                    recordsmodified = (result.created + result.updated + result.deleted),
                    created = result.created,
                    updated = result.updated,
                    deleted = result.deleted,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new UpdateResult
                {
                    operation = "Update Ninja Events",
                    updatetype = "all",
                    otherinfo = "",
                    message = "Update Ninja Events failed: " + ex.Message,
                    recordsmodified = 0,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    success = false
                });
            }
        }

        [HttpGet, Route("NINJA/Events/UpdateSingle/{id}")]
        public async Task<IActionResult> UpdateSingleNinjaEvents(string id, CancellationToken cancellationToken = default)
        {           
            return StatusCode(StatusCodes.Status501NotImplemented, new { error = "Not Implemented" });
        }

        #endregion        

        #region SIAG DATA SYNC WEATHER 

        [HttpGet, Route("Siag/Weather/Import")]
        public async Task<IActionResult> ImportWeather(CancellationToken cancellationToken = default)
        {
            try
            {
                SIAGImportHelper siagimporthelper = new SIAGImportHelper(settings, QueryFactory, "weatherdatahistory");
                var result = await siagimporthelper.SaveWeatherToHistoryTable(cancellationToken);

                return Ok(new UpdateResult
                {
                    operation = "Import Weather data",
                    updatetype = "single",
                    otherinfo = "actual",
                    message = "Import Weather data succeeded",
                    recordsmodified = result.created + result.updated + result.deleted,                    
                    created = result.created,
                    updated = result.updated,
                    deleted = result.deleted,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new UpdateResult
                {
                    operation = "Import Weather data",
                    updatetype = "single",
                    otherinfo = "actual",
                    message = "Import Weather data failed: " + ex.Message,
                    recordsmodified = 0,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    success = false
                });
            }
        }

        [HttpGet, Route("Siag/Weather/Import/{id}")]
        public async Task<IActionResult> ImportWeatherByID(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                SIAGImportHelper siagimporthelper = new SIAGImportHelper(settings, QueryFactory, "weatherdatahistory");
                var result = await siagimporthelper.SaveWeatherToHistoryTable(cancellationToken, id);

                return Ok(new UpdateResult
                {
                    operation = "Import Weather data",
                    updatetype = "single",
                    otherinfo = "byid",
                    id = id,
                    message = "Import Weather data succeeded id:" + id.ToString(),
                    recordsmodified = (result.created + result.updated + result.deleted),
                    created = result.created,
                    updated = result.updated,
                    deleted = result.deleted,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new UpdateResult
                {
                    operation = "Import Weather data",
                    updatetype = "single",
                    otherinfo = "byid",
                    message = "Import Weather data failed: id:" + id.ToString() + " error:" + ex.Message,
                    recordsmodified = 0,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    success = false
                });
            }
        }

        #endregion

        #region SIAG DATA SYNC MUSEUMS

        [HttpGet, Route("Siag/Museum/UpdateAll")]
        public async Task<IActionResult> ImportMuseum(CancellationToken cancellationToken = default)
        {
            try
            {
                SIAGImportHelper siagimporthelper = new SIAGImportHelper(settings, QueryFactory, "smgpois");
                var result = await siagimporthelper.SaveDataToODH(null, cancellationToken);

                return Ok(new UpdateResult
                {
                    operation = "Import SIAG Museum data",
                    updatetype = "all",
                    otherinfo = "",
                    message = "Import SIAG Museum data succeeded",
                    recordsmodified = result.created + result.updated + result.deleted,
                    created = result.created,
                    updated = result.updated,
                    deleted = result.deleted,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new UpdateResult
                {
                    operation = "Import Weather data",
                    updatetype = "single",
                    otherinfo = "actual",
                    message = "Import Weather data failed: " + ex.Message,
                    recordsmodified = 0,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    success = false
                });
            }
        }

        #endregion

        #region SUEDTIROLWEIN DATA SYNC

        #endregion

        #region LTS ACTIVITYDATA SYNC

        #endregion

        #region LTS POIDATA SYNC

        #endregion

        #region LTS EVENT DATA SYNC

        #endregion

        #region LTS GASTRONOMIC DATA SYNC

        #endregion

        #region LTS ACCOMMODATION DATA SYNC

        #endregion

        #region HGV ACCOMMODATION DATA SYNC

        #endregion

        #region LTS MEASURINGPOINTS DATA SYNC

        #endregion

        #region LTS WEBCAM DATA SYNC

        #endregion

        #region STA POI DATA SYNC

        [Authorize(Roles = "DataWriter,STAPoiImport")]
        [HttpPost, Route("STA/VendingPoints/UpdateAll")]
        public async Task<IActionResult> SendVendingPointsFromSTA(CancellationToken cancellationToken)
        {
            try
            {
                STAImportHelper staimporthelper = new STAImportHelper(settings, QueryFactory);

                var result = await staimporthelper.PostVendingPointsFromSTA(Request, cancellationToken);

                return Ok(new
                {
                    operation = "Import Vendingpoints",
                    updatetype = "all",
                    otherinfo = "STA",
                    id = "",
                    message = "Import Vendingpoints succeeded",
                    recordsmodified = (result.created + result.updated + result.deleted),
                    created = result.created,
                    updated = result.updated,
                    deleted = result.deleted,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new UpdateResult
                {
                    operation = "Import Vendingpoints",
                    updatetype = "all",
                    otherinfo = "STA",
                    id = "",
                    message = "Import Vendingpoints failed: " + ex.Message,
                    recordsmodified = 0,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    success = false
                });
            }
        }


        #endregion

        #region EBMS DATA SYNC (EventShort)

        [HttpGet, Route("DSS/{dssentity}/UpdateAll")]
        public async Task<IActionResult> UpdateAllDSSLifts(string dssentity, CancellationToken cancellationToken = default)
        {
            try
            {
                DSSImportHelper dssimporthelper = new DSSImportHelper(settings, QueryFactory, "odhactivitypoi");
                dssimporthelper.requesttype = DSS.DSSRequestType.liftbase;

                var result = await dssimporthelper.SaveDataToODH(null, cancellationToken);

                return Ok(new UpdateResult
                {
                    operation = "Update DSS",
                    updatetype = "all",
                    otherinfo = dssentity,
                    message = "DSS " + dssentity + " update succeeded",
                    recordsmodified = (result.created + result.updated + result.deleted),
                    created = result.created,
                    updated = result.updated,
                    deleted = result.deleted,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new UpdateResult
                {
                    operation = "Update EBMS",
                    updatetype = "all",
                    otherinfo = "",
                    message = "EBMS Eventshorts update failed: " + ex.Message,
                    recordsmodified = 0,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    success = false
                });
            }
        }

        [HttpGet, Route("DSS/{dssentity}/UpdateSingle/{id}")]
        public IActionResult UpdateSingleDSS(string dssentity, string id, CancellationToken cancellationToken = default)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new { error = "Not Implemented" });

            //try
            //{               
            //    return Ok(new UpdateResult
            //    {
            //        operation = "Update EBMS",
            //        id = id,
            //        updatetype = "single",
            //        otherinfo = "",
            //        message = "EBMS Eventshorts update succeeded",
            //        recordsmodified = 1,
            //        created = 0,
            //        updated = 0,
            //        deleted = 0,
            //        success = true
            //    });
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(new UpdateResult
            //    {
            //        operation = "Update EBMS",
            //        updatetype = "all",
            //        otherinfo = "",
            //        message = "EBMS Eventshorts update failed: " + ex.Message,
            //        recordsmodified = 0,
            //        created = 0,
            //        updated = 0,
            //        deleted = 0,
            //        success = false
            //    });
            //}
        }

        #endregion

    }
}
