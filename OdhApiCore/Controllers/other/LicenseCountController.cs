// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using Helper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Filters;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.other
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class LicenseCountController : OdhController
    {
        public LicenseCountController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<LicenseCountController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        /// <summary>
        /// GET LicenseCount
        /// </summary>
        /// <returns>LicenseCountResult Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(LicenseCountResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 36000)]
        [HttpGet, Route("LicenseCount")]
        public async Task<IActionResult> GetLicenseCount(
            CancellationToken cancellationToken = default
        )
        {
            return await GetAllLicenseCount(cancellationToken);
        }

        private async Task<IActionResult> GetAllLicenseCount(CancellationToken cancellationToken)
        {
            LicenseCountResult result = new LicenseCountResult();

            result.AccoTotaldataopen =
                await Helper.PostgresLicenseCountHelper.GetTotalCountOpendata(
                    QueryFactory,
                    "accommodations"
                );
            result.GastroTotaldataopen =
                await Helper.PostgresLicenseCountHelper.GetTotalCountOpendata(
                    QueryFactory,
                    "gastronomies"
                );
            result.EventTotaldataopen =
                await Helper.PostgresLicenseCountHelper.GetTotalCountOpendata(
                    QueryFactory,
                    "events"
                );
            result.PoiTotaldataopen = await Helper.PostgresLicenseCountHelper.GetTotalCountOpendata(
                QueryFactory,
                "pois"
            );
            result.ActivityTotaldataopen =
                await Helper.PostgresLicenseCountHelper.GetTotalCountOpendata(
                    QueryFactory,
                    "activities"
                );
            result.RegionTotaldataopen =
                await Helper.PostgresLicenseCountHelper.GetTotalCountOpendata(
                    QueryFactory,
                    "regions"
                );
            result.TVTotaldataopen = await Helper.PostgresLicenseCountHelper.GetTotalCountOpendata(
                QueryFactory,
                "tvs"
            );
            result.SkiAreaTotaldataopen =
                await Helper.PostgresLicenseCountHelper.GetTotalCountOpendata(
                    QueryFactory,
                    "skiareas"
                );
            result.ODHActivityPoiTotaldataopen =
                await Helper.PostgresLicenseCountHelper.GetTotalCountOpendata(
                    QueryFactory,
                    "smgpois"
                );

            result.AccoTotaldata = await Helper.PostgresLicenseCountHelper.GetTotalCount(
                QueryFactory,
                "accommodations"
            );
            result.GastroTotaldata = await Helper.PostgresLicenseCountHelper.GetTotalCount(
                QueryFactory,
                "gastronomies"
            );
            result.EventTotaldata = await Helper.PostgresLicenseCountHelper.GetTotalCount(
                QueryFactory,
                "events"
            );
            result.PoiTotaldata = await Helper.PostgresLicenseCountHelper.GetTotalCount(
                QueryFactory,
                "pois"
            );
            result.ActivityTotaldata = await Helper.PostgresLicenseCountHelper.GetTotalCount(
                QueryFactory,
                "activities"
            );
            result.RegionTotaldata = await Helper.PostgresLicenseCountHelper.GetTotalCount(
                QueryFactory,
                "regions"
            );
            result.TVTotaldata = await Helper.PostgresLicenseCountHelper.GetTotalCount(
                QueryFactory,
                "tvs"
            );
            result.SkiAreaTotaldata = await Helper.PostgresLicenseCountHelper.GetTotalCount(
                QueryFactory,
                "skiareas"
            );
            result.ODHActivityPoiTotaldata = await Helper.PostgresLicenseCountHelper.GetTotalCount(
                QueryFactory,
                "smgpois"
            );

            result.AccoWithCC0Image =
                await Helper.PostgresLicenseCountHelper.GetAllDataWithCC0Image(
                    QueryFactory,
                    "accommodations"
                );
            result.GastroWithCC0Image =
                await Helper.PostgresLicenseCountHelper.GetAllDataWithCC0Image(
                    QueryFactory,
                    "gastronomies"
                );
            result.EventWithCC0Image =
                await Helper.PostgresLicenseCountHelper.GetAllDataWithCC0Image(
                    QueryFactory,
                    "events"
                );
            result.PoiWithCC0Image = await Helper.PostgresLicenseCountHelper.GetAllDataWithCC0Image(
                QueryFactory,
                "pois"
            );
            result.ActivityWithCC0Image =
                await Helper.PostgresLicenseCountHelper.GetAllDataWithCC0Image(
                    QueryFactory,
                    "activities"
                );
            result.RegionWithCC0Image =
                await Helper.PostgresLicenseCountHelper.GetAllDataWithCC0Image(
                    QueryFactory,
                    "regions"
                );
            result.TVWithCC0Image = await Helper.PostgresLicenseCountHelper.GetAllDataWithCC0Image(
                QueryFactory,
                "tvs"
            );
            result.SkiAreaWithCC0Image =
                await Helper.PostgresLicenseCountHelper.GetAllDataWithCC0Image(
                    QueryFactory,
                    "skiareas"
                );
            result.ODHActivityPoiWithCC0Image =
                await Helper.PostgresLicenseCountHelper.GetAllDataWithCC0Image(
                    QueryFactory,
                    "smgpois"
                );

            result.AccoCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithCC0License(
                    QueryFactory,
                    "accommodations"
                );
            result.GastroCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithCC0License(
                    QueryFactory,
                    "gastronomies"
                );
            result.EventCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithCC0License(
                    QueryFactory,
                    "events"
                );
            result.PoiCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithCC0License(
                    QueryFactory,
                    "pois"
                );
            result.ActivityCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithCC0License(
                    QueryFactory,
                    "activities"
                );
            result.RegionCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithCC0License(
                    QueryFactory,
                    "regions"
                );
            result.TVCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithCC0License(
                    QueryFactory,
                    "tvs"
                );
            result.SkiAreaCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithCC0License(
                    QueryFactory,
                    "skiareas"
                );
            result.ODHActivityPoiCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithCC0License(
                    QueryFactory,
                    "smgpois"
                );

            result.AccoNONCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithNONCC0License(
                    QueryFactory,
                    "accommodations"
                );
            result.GastroNONCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithNONCC0License(
                    QueryFactory,
                    "gastronomies"
                );
            result.EventNONCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithNONCC0License(
                    QueryFactory,
                    "events"
                );
            result.PoiNONCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithNONCC0License(
                    QueryFactory,
                    "pois"
                );
            result.ActivityNONCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithNONCC0License(
                    QueryFactory,
                    "activities"
                );
            result.RegionNONCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithNONCC0License(
                    QueryFactory,
                    "regions"
                );
            result.TVNONCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithNONCC0License(
                    QueryFactory,
                    "tvs"
                );
            result.SkiAreaNONCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithNONCC0License(
                    QueryFactory,
                    "skiareas"
                );
            result.ODHActivityPoiNONCC0ImagesCount =
                await Helper.PostgresLicenseCountHelper.GetAllImagesWithNONCC0License(
                    QueryFactory,
                    "smgpois"
                );

            return Ok(result);
        }
    }

    public class LicenseCountResult
    {
        public long AccoTotaldata { get; set; }
        public long GastroTotaldata { get; set; }
        public long EventTotaldata { get; set; }
        public long PoiTotaldata { get; set; }
        public long ActivityTotaldata { get; set; }
        public long RegionTotaldata { get; set; }
        public long TVTotaldata { get; set; }
        public long SkiAreaTotaldata { get; set; }
        public long ODHActivityPoiTotaldata { get; set; }

        public long AccoTotaldataopen { get; set; }
        public long GastroTotaldataopen { get; set; }
        public long EventTotaldataopen { get; set; }
        public long PoiTotaldataopen { get; set; }
        public long ActivityTotaldataopen { get; set; }
        public long RegionTotaldataopen { get; set; }
        public long TVTotaldataopen { get; set; }
        public long SkiAreaTotaldataopen { get; set; }
        public long ODHActivityPoiTotaldataopen { get; set; }

        public long AccoWithCC0Image { get; set; }
        public long GastroWithCC0Image { get; set; }
        public long EventWithCC0Image { get; set; }
        public long PoiWithCC0Image { get; set; }
        public long ActivityWithCC0Image { get; set; }
        public long RegionWithCC0Image { get; set; }
        public long TVWithCC0Image { get; set; }
        public long SkiAreaWithCC0Image { get; set; }
        public long ODHActivityPoiWithCC0Image { get; set; }

        public long AccoCC0ImagesCount { get; set; }
        public long GastroCC0ImagesCount { get; set; }
        public long EventCC0ImagesCount { get; set; }
        public long PoiCC0ImagesCount { get; set; }
        public long ActivityCC0ImagesCount { get; set; }
        public long RegionCC0ImagesCount { get; set; }
        public long TVCC0ImagesCount { get; set; }
        public long SkiAreaCC0ImagesCount { get; set; }
        public long ODHActivityPoiCC0ImagesCount { get; set; }

        public long AccoNONCC0ImagesCount { get; set; }
        public long GastroNONCC0ImagesCount { get; set; }
        public long EventNONCC0ImagesCount { get; set; }
        public long PoiNONCC0ImagesCount { get; set; }
        public long ActivityNONCC0ImagesCount { get; set; }
        public long RegionNONCC0ImagesCount { get; set; }
        public long TVNONCC0ImagesCount { get; set; }
        public long SkiAreaNONCC0ImagesCount { get; set; }
        public long ODHActivityPoiNONCC0ImagesCount { get; set; }
    }
}
