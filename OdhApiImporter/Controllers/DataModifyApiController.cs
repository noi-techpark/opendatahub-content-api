// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using EBMS;
using Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NINJA;
using NINJA.Parser;
using OdhApiImporter.Helpers;
using RAVEN;
using SqlKata.Execution;

namespace OdhApiImporter.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    public class DataModifyApiController : Controller
    {
        private readonly ISettings settings;
        private readonly QueryFactory QueryFactory;
        private readonly ILogger<DataModifyApiController> logger;
        private readonly IWebHostEnvironment env;

        public DataModifyApiController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<DataModifyApiController> logger,
            QueryFactory queryFactory
        )
        {
            this.env = env;
            this.settings = settings;
            this.logger = logger;
            this.QueryFactory = queryFactory;
        }

        #region EventShort


        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifyEventShort")]
        public async Task<IActionResult> ModifyEventShort(CancellationToken cancellationToken)
        {
            var objectscount = 0;

            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            //objectscount = await customdataoperation.UpdateAllEventShortstActiveTodayField();
            //objectscount = await customdataoperation.UpdateAllEventShortBrokenLinks();

            //objectscount = await customdataoperation.UpdateAllEventShortPublisherInfo();

            //objectscount = await customdataoperation.UpdateAllEventShortstEventDocumentField();

            //objectscount = await customdataoperation.UpdateAllEventShortstActiveFieldToTrue();

            objectscount = await customdataoperation.UpdateAllEventShortstHasLanguage();

            return Ok(
                new UpdateResult
                {
                    operation = "Modify EventShort",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("CleanEventShort")]
        public async Task<IActionResult> CleanEventShort(CancellationToken cancellationToken)
        {
            var objectscount = 0;

            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            //objectscount = await customdataoperation.UpdateAllEventShortstActiveTodayField();
            //objectscount = await customdataoperation.UpdateAllEventShortBrokenLinks();

            //objectscount = await customdataoperation.UpdateAllEventShortPublisherInfo();

            objectscount = await customdataoperation.CleanEventShortstEventDocumentField();

            return Ok(
                new UpdateResult
                {
                    operation = "Clean EventShort",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("FillEventShortTags")]
        public async Task<IActionResult> FillEventShortTags(CancellationToken cancellationToken)
        {
            var objectscount = 0;

            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );

            objectscount = await customdataoperation.FillEventShortTags();

            return Ok(
                new UpdateResult
                {
                    operation = "FillEventShortTags EventShort",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ResaveEventShortWithTags")]
        public async Task<IActionResult> ResaveEventShortWithTags(
            CancellationToken cancellationToken
        )
        {
            var objectscount = 0;

            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );

            objectscount = await customdataoperation.ResaveEventShortWithTags();

            return Ok(
                new UpdateResult
                {
                    operation = "ResaveEventShortWithTags EventShort",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region Accommodations

        [Authorize(Roles = "DataPush")]
        [HttpPost, Route("ModifyAccommodations")]
        public async Task<IActionResult> ModifyAccommodations(
            bool trim,
            [FromBody] List<string> idlist
        )
        {
            var objectscount = 0;

            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );

            objectscount = await customdataoperation.AccommodationModify(idlist, trim);

            return Ok(
                new UpdateResult
                {
                    operation = "Modify Accommodation",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifyAccommodationRooms")]
        public async Task<IActionResult> ModifyAccommodationRooms()
        {
            var objectscount = 0;

            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );

            objectscount = await customdataoperation.AccommodationRoomModify();

            return Ok(
                new UpdateResult
                {
                    operation = "Modify Accommodation Array",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifyAccommodationsToV2")]
        public async Task<IActionResult> ModifyAccommodationsToV2()
        {
            var objectscount = 0;

            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );

            objectscount = await customdataoperation.AccommodationModifyToV2(null);

            return Ok(
                new UpdateResult
                {
                    operation = "Modify Accommodation V2",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region Articles

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("FillDummyNews")]
        public async Task<IActionResult> FillDBWithDummynews(CancellationToken cancellationToken)
        {
            //CustomDataOperation customdataoperation = new CustomDataOperation(settings, QueryFactory);
            //var objectscount = await customdataoperation.FillDBWithDummyNews();

            return Ok(
                new UpdateResult
                {
                    operation = "Modify Articles",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = 0,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("NewsFeedUpdate")]
        public async Task<IActionResult> NewsFeedUpdate(CancellationToken cancellationToken)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.NewsFeedUpdate();

            return Ok(
                new UpdateResult
                {
                    operation = "Modify Articles",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = 0,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region Weather

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifyWeatherHistory")]
        public async Task<IActionResult> ModifyWeatherHistory(CancellationToken cancellationToken)
        {
            //CustomDataOperation customdataoperation = new CustomDataOperation(settings, QueryFactory);
            //var objectscount = await customdataoperation.UpdateAllWeatherHistoryWithMetainfo();

            return Ok(
                new UpdateResult
                {
                    operation = "Modify WeatherHistory",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = 0,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region ODHActivityPoi

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifyODHActivityPoi")]
        public async Task<IActionResult> ModifyODHActivityPoiTags(
            string? id,
            bool? forceupdate,
            int? takethefirst,
            CancellationToken cancellationToken
        )
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.UpdateAllODHActivityPoiTagIds(
                id,
                forceupdate,
                takethefirst
            );
            //var objectscount2 = await customdataoperation.UpdateAllODHActivityPoiOldTags("dss");


            return Ok(
                new UpdateResult
                {
                    operation = "Modify ODHActivityPoi",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifySTAVendingpoint")]
        public async Task<IActionResult> ModifySTAVendingpoint(CancellationToken cancellationToken)
        {
            //CustomDataOperation customdataoperation = new CustomDataOperation(settings, QueryFactory);
            //var objectscount = await customdataoperation.UpdateAllSTAVendingpoints();

            return Ok(
                new UpdateResult
                {
                    operation = "Modify STA Vendingpoint",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = 0,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region MetaData

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("UpdateMetaDataRecordCount")]
        public async Task<IActionResult> UpdateMetaDataRecordCount(
            CancellationToken cancellationToken
        )
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.UpdateMetaDataApiRecordCount();

            return Ok(
                new UpdateResult
                {
                    operation = "Modify Metadata Record Count",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ResaveMetaData")]
        public async Task<IActionResult> ResaveMetaData(bool correcturls = false)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.ResaveMetaData(
                Request.Host.ToString(),
                correcturls
            );

            return Ok(
                new UpdateResult
                {
                    operation = "Resave Metadata",
                    updatetype = "custom",
                    otherinfo = Request.Host.ToString(),
                    message = "Done",
                    recordsmodified = 0,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ResaveTags")]
        public async Task<IActionResult> ResaveTags(bool correcturls = false)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.ResaveTags();

            return Ok(
                new UpdateResult
                {
                    operation = "Resave Tags",
                    updatetype = "custom",
                    otherinfo = Request.Host.ToString(),
                    message = "Done",
                    recordsmodified = 0,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region Generic

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ResaveSourcefield")]
        public async Task<IActionResult> ResaveSource(
            string odhtype,
            string sourcetofilter,
            string sourcetochange
        )
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );

            var objectscount = 0;

            switch (odhtype)
            {
                case "accommodation":
                    objectscount =
                        await customdataoperation.ResaveSourcesOnType<AccommodationLinked>(
                            odhtype,
                            sourcetofilter,
                            sourcetochange
                        );
                    ;
                    break;
                case "accommodationroom":
                    objectscount =
                        await customdataoperation.ResaveSourcesOnType<AccommodationRoomLinked>(
                            odhtype,
                            sourcetofilter,
                            sourcetochange
                        );
                    ;
                    break;
                case "package":
                    objectscount = await customdataoperation.ResaveSourcesOnType<PackageLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "odhactivitypoi":
                    objectscount =
                        await customdataoperation.ResaveSourcesOnType<ODHActivityPoiLinked>(
                            odhtype,
                            sourcetofilter,
                            sourcetochange
                        );
                    ;
                    break;
                case "event":
                    objectscount = await customdataoperation.ResaveSourcesOnType<EventLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "article":
                    objectscount = await customdataoperation.ResaveSourcesOnType<ArticlesLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "ltsactivity":
                    objectscount = await customdataoperation.ResaveSourcesOnType<LTSActivityLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "ltspoi":
                    objectscount = await customdataoperation.ResaveSourcesOnType<LTSPoiLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "ltsgastronomy":
                    objectscount = await customdataoperation.ResaveSourcesOnType<GastronomyLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "webcam":
                    objectscount = await customdataoperation.ResaveSourcesOnType<WebcamInfoLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "wineaward":
                    objectscount = await customdataoperation.ResaveSourcesOnType<WineLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "venue":
                    objectscount = await customdataoperation.ResaveSourcesOnType<VenueLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "eventshort":
                    objectscount = await customdataoperation.ResaveSourcesOnType<EventShortLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
                case "weatherhistory":
                    objectscount =
                        await customdataoperation.ResaveSourcesOnType<WeatherHistoryLinked>(
                            odhtype,
                            sourcetofilter,
                            sourcetochange
                        );
                    ;
                    break;
                case "area":
                    objectscount = await customdataoperation.ResaveSourcesOnType<AreaLinked>(
                        odhtype,
                        sourcetofilter,
                        sourcetochange
                    );
                    ;
                    break;
            }

            return Ok(
                new UpdateResult
                {
                    operation = "Resave Source",
                    updatetype = "custom",
                    otherinfo = odhtype,
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region ODHTag

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifyODHTags")]
        public async Task<IActionResult> ModifyODHTags(CancellationToken cancellationToken)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.UpdateAllODHTags();

            return Ok(
                new UpdateResult
                {
                    operation = "Modify ODHTags",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = 0,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region Haslanguage

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifyWines")]
        public async Task<IActionResult> ModifyWines(CancellationToken cancellationToken)
        {
            var objectscount = 0;

            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            //objectscount = await customdataoperation.UpdateAllEventShortstActiveTodayField();
            //objectscount = await customdataoperation.UpdateAllEventShortBrokenLinks();

            //objectscount = await customdataoperation.UpdateAllEventShortPublisherInfo();

            //objectscount = await customdataoperation.UpdateAllEventShortstEventDocumentField();

            //objectscount = await customdataoperation.UpdateAllEventShortstActiveFieldToTrue();

            objectscount = await customdataoperation.UpdateAllWineHasLanguage();

            return Ok(
                new UpdateResult
                {
                    operation = "Modify wines",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region Tag

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("EventTopicsToTags")]
        public async Task<IActionResult> EventTopicsToTags(CancellationToken cancellationToken)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.EventTopicsToTags();

            return Ok(
                new UpdateResult
                {
                    operation = "EventTopicsToTags",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("EventShortTypesToTags")]
        public async Task<IActionResult> EventShortTypesToTags(CancellationToken cancellationToken)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.EventShortTypesToTags();

            return Ok(
                new UpdateResult
                {
                    operation = "EventShortTypesToTags",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("GastronomyTypesToTags")]
        public async Task<IActionResult> GastronomyTypesToTags(CancellationToken cancellationToken)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.GastronomyTypesToTags();

            return Ok(
                new UpdateResult
                {
                    operation = "GastronomyTypesToTags",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("VenueTypesToTags")]
        public async Task<IActionResult> VenueTypesToTags(CancellationToken cancellationToken)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.VenueTypesToTags();

            return Ok(
                new UpdateResult
                {
                    operation = "VenueTypesToTags",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ArticleTypesToTags")]
        public async Task<IActionResult> ArticleTypesToTags(CancellationToken cancellationToken)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.ArticleTypesToTags();

            return Ok(
                new UpdateResult
                {
                    operation = "ArticleTypesToTags",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("TagSourceFix")]
        public async Task<IActionResult> TagSourceFix(CancellationToken cancellationToken)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.TagSourceFix();

            return Ok(
                new UpdateResult
                {
                    operation = "TagSourceFix",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("TagTypesFix")]
        public async Task<IActionResult> TagTypesFix(CancellationToken cancellationToken)
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.TagTypesFix();

            return Ok(
                new UpdateResult
                {
                    operation = "TagTypesFix",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region Shape

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifyGeoShapes")]
        public async Task<IActionResult> ModifyGeoShapes(          
          CancellationToken cancellationToken
      )
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.UpdateGeoshapeCreateMapping(               
            );
            

            return Ok(
                new UpdateResult
                {
                    operation = "Modify GeoShape",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("ModifyGeoShapesTestMetaInfo")]
        public async Task<IActionResult> ModifyGeoShapeMetadata(
          CancellationToken cancellationToken
      )
        {
            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
            var objectscount = await customdataoperation.UpdateGeoshapeMetaInfo(
            );


            return Ok(
                new UpdateResult
                {
                    operation = "Modify GeoShape",
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }

        #endregion

        #region Reduced

        [Authorize(Roles = "DataPush")]
        [HttpGet, Route("CleanReducedId/{table}")]
        public async Task<IActionResult> CleanReducedId(string table, CancellationToken cancellationToken)
        {
            var objectscount = 0;

            CustomDataOperation customdataoperation = new CustomDataOperation(
                settings,
                QueryFactory
            );
      
            objectscount = await customdataoperation.UpdateAllReducedIdsonData(table);

            return Ok(
                new UpdateResult
                {
                    operation = "Modify Reduced " + table,
                    updatetype = "custom",
                    otherinfo = "",
                    message = "Done",
                    recordsmodified = objectscount,
                    created = 0,
                    deleted = 0,
                    id = "",
                    updated = 0,
                    success = true,
                }
            );
        }


        #endregion
    }
}
