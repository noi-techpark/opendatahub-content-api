// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlKata.Execution;

namespace OdhApiImporter.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    public class JsonGeneratorController : Controller
    {
        private readonly ISettings settings;
        private readonly QueryFactory QueryFactory;
        private readonly ILogger<JsonGeneratorController> logger;
        private readonly IWebHostEnvironment env;

        public JsonGeneratorController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<JsonGeneratorController> logger,
            QueryFactory queryFactory
        )
        {
            this.env = env;
            this.settings = settings;
            this.logger = logger;
            this.QueryFactory = queryFactory;
        }

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

        [HttpGet, Route("ODH/OdhTagGeneratedlist")]
        public async Task<IActionResult> ProduceOdhTagGeneratedListJson(
            CancellationToken cancellationToken
        )
        {
            try
            {
                await JsonGeneratorHelper.GenerateJSONODHTagSourceIDMLTSList(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "ODHTagsSourceIDMLTS"
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "ODHTagGeneratedList",
                    "Generate Json ODHTagGeneratedList succeeded",
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

        [HttpGet, Route("ODH/GastronomyCategorieslist")]
        public async Task<IActionResult> ProduceGastronomyCategoriesListJson(
            CancellationToken cancellationToken
        )
        {
            try
            {
                await JsonGeneratorHelper.GenerateJSONGastronomyTagCategoriesList(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "CategoryCodes",
                     new List<string>() { "gastronomycategory" }
                );
                await JsonGeneratorHelper.GenerateJSONGastronomyTagCategoriesList(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "DishRates",
                     new List<string>() { "gastronomydishcodes" }
                );
                await JsonGeneratorHelper.GenerateJSONGastronomyTagCategoriesList(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "Facilities",
                     new List<string>() { "gastronomyfacilities" }
                );
                await JsonGeneratorHelper.GenerateJSONGastronomyTagCategoriesList(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "CapacityCeremonies",
                     new List<string>() { "gastronomyceremonycodes" }
                );
                //Maybe Duplicate
                await JsonGeneratorHelper.GenerateJSONODHTagsDisplayAsCategoryList(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "GastronomyDisplayAsCategory",
                     new List<string>() { "essen trinken" }
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "GastronomyCategorieslist",
                    "Generate Json GastronomyCategorieslist succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "GastronomyCategorieslist",
                    "Generate Json GastronomyCategorieslist failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        [HttpGet, Route("ODH/ActivityDatalist")]
        public async Task<IActionResult> ProduceActivityDataListJson(
            CancellationToken cancellationToken
        )
        {
            try
            {                
                await JsonGeneratorHelper.GenerateJSONLTSTagsList(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "CapacityCeremonies",
                     new List<string>() { "gastronomyceremonycodes" }
                );                
                await JsonGeneratorHelper.GenerateJSONODHTagsDisplayAsCategoryList(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "ActivityDisplayAsCategory",
                     new List<string>() { "activity" }
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "ActivityDatalist",
                    "Generate Json ActivityDatalist succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "GastronomyCategorieslist",
                    "Generate Json GastronomyCategorieslist failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }


        #endregion

        #region LocationInfo

        [HttpGet, Route("ODH/LocationList")]
        public async Task<IActionResult> ProduceLocationListJson(
            CancellationToken cancellationToken
        )
        {
            try
            {
                await JsonGeneratorHelper.GenerateJSONLocationlist(
                    QueryFactory,
                    settings.JsonConfig.Jsondir,
                    "LocationList"
                );

                var result = GenericResultsHelper.GetSuccessJsonGenerateResult(
                    "Json Generation",
                    "LocationList",
                    "Generate Json LocationList succeeded",
                    true
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = GenericResultsHelper.GetErrorJsonGenerateResult(
                    "Json Generation",
                    "LocationList",
                    "Generate Json LocationList failed",
                    ex,
                    true
                );

                return BadRequest(result);
            }
        }

        #endregion
    }
}
