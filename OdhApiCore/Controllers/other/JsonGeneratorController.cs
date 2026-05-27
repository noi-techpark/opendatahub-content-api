// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.CacheOutput;
using DataModel;
using Helper;
using Helper.S3;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.GenericHelpers;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.other
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    public class JsonGeneratorController : OdhController
    {
        private readonly ISettings settings;

        public JsonGeneratorController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<JsonGeneratorController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            this.settings = settings;
        }

        #region Accommodation

        [HttpGet, Route("ODH/AccommodationBooklist")]
        public async Task<IActionResult> ProduceAccoBooklistJson(
            CancellationToken cancellationToken
        )
        {
            try
            {
                await JsonGeneratorHelper.GenerateJSONAccommodationsForBooklist(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    true,
                    "AccosBookable"
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "AccommodationBooklist",
                    "Generate Json AccommodationBooklist succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "AccommodationBooklist",
                    "Generate Json AccommodationBooklist succeeded",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        [HttpGet, Route("ODH/AccommodationFulllist")]
        public async Task<IActionResult> ProduceAccoFulllistJson(
            CancellationToken cancellationToken
        )
        {
            try
            {
                await JsonGeneratorHelper.GenerateJSONAccommodationsForBooklist(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    null,
                    "AccosAll"
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "AccommodationFullist",
                    "Generate Json AccommodationFullist succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "AccommodationFullist",
                    "Generate Json AccommodationBooklist failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        #endregion

        #region Tags

        [HttpGet, Route("ODH/Taglist")]
        public async Task<IActionResult> ProduceTagJson(CancellationToken cancellationToken)
        {
            try
            {
                await JsonGeneratorHelper.GenerateJSONTaglist(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "GenericTags"
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "Taglist",
                    "Generate Json Taglist succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "Taglist",
                    "Generate Json Taglist failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        [HttpGet, Route("ODH/OdhTagAutoPublishlist")]
        public async Task<IActionResult> ProduceOdhTagAutoPublishListJson(
            CancellationToken cancellationToken
        )
        {
            try
            {
                await JsonGeneratorHelper.GenerateJSONODHTagAutoPublishlist(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "AutoPublishTags"
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "ODHTagAutopublishlist",
                    "Generate Json ODHTagAutopublishlist succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "ODHTagAutopublishlist",
                    "Generate Json ODHTagAutopublishlist failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        [HttpGet, Route("ODH/OdhTagCategorieslist")]
        public async Task<IActionResult> ProduceOdhTagCategoriesListJson(
            CancellationToken cancellationToken
        )
        {
            try
            {
                await JsonGeneratorHelper.GenerateJSONODHTagCategoriesList(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "TagsForCategories"
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "ODHTagCategoriesList",
                    "Generate Json ODHTagCategoriesList succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "ODHTagCategoriesList",
                    "Generate Json ODHTagCategoriesList failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        #endregion

        #region STA

        [InvalidateCacheOutput(
            typeof(OdhApiCore.Controllers.sta.STAController),
            nameof(OdhApiCore.Controllers.sta.STAController.GetODHActivityPoiListSTA)
        )] // this will invalidate Get in a different controller
        [HttpGet, Route("STA/JsonPoi")]
        public async Task<IActionResult> ProducePoiSTAJson(CancellationToken cancellationToken)
        {
            try
            {
                await STARequestHelper.GenerateJSONODHActivityPoiForSTA(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    settings.XmlConfig.Xmldir
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "ODHActivityPoiSTA",
                    "Generate Json ODHActivityPoi for STA succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "ODHActivityPoiSTA",
                    "Generate Json ODHActivityPoi for STA failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        [InvalidateCacheOutput(
            typeof(OdhApiCore.Controllers.sta.STAController),
            nameof(OdhApiCore.Controllers.sta.STAController.GetAccommodationsSTA)
        )] // this will invalidate Get in a different controller
        [HttpGet, Route("STA/JsonAccommodation")]
        public async Task<IActionResult> ProduceAccoSTAJson(CancellationToken cancellationToken)
        {
            try
            {
                await STARequestHelper.GenerateJSONAccommodationsForSTA(
                    QueryFactory,
                    settings.JsonConfig.Jsondir
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "AccommodationSTA",
                    "Generate Json Accommodation for STA succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "AccommodationSTA",
                    "Generate Json Accommodation for STA failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        #endregion

        #region Weather

        [HttpGet, Route("ODH/WeatherForecast")]
        public async Task<IActionResult> DownloadWeatherForecastJsonFromS3(
            CancellationToken cancellationToken
        )
        {
            try
            {
                if (!settings.S3Config.ContainsKey("dc-meteorology-province-forecast"))
                    throw new Exception("No weatherforecast file found");

                await GetDataFromS3.GetFileFromS3(
                    "dc-meteorology-province-forecast",
                    settings.S3Config["dc-meteorology-province-forecast"].AccessKey,
                    settings.S3Config["dc-meteorology-province-forecast"].AccessSecretKey,
                    settings.S3Config["dc-meteorology-province-forecast"].Filename,
                    settings.JsonConfig.Jsondir
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "Weatherforecast",
                    "Download Json Weatherforecast succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "Weatherforecast",
                    "Download Json Weatherforecast failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        #endregion
    }
}
