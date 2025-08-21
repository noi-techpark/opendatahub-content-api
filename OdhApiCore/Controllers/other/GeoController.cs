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
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Responses;
using OdhNotifier;
using Schema.NET;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// <a href="https://github.com/noi-techpark/odh-docs/wiki/Geoshapes-Api-and-Geo-Converter#geoshapes-api" target="_blank">Wiki GeoShapes Api</a>
    /// </summary>
    [EnableCors("CorsPolicy")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    [NullStringParameterActionFilter]
    public class GeoController : OdhController
    {
        public GeoController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<GeoController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET GeoShapes List
        /// </summary>
        /// <param name="srid">Spatial Reference Identifier, Coordinate System of the geojson, available formats(epsg:4362,epsg:32632,epsg:3857)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>        
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of GeoShapes Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(IEnumerable<GeoShapeJson>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator))]
        [HttpGet, Route("GeoShapes")]
        [HttpGet, Route("GeoShape")]
        //[Authorize(Roles = "DataReader,CommonReader,AccoReader,ActivityReader,PoiReader,ODHPoiReader,PackageReader,GastroReader,EventReader,ArticleReader")]
        public async Task<IActionResult> GetGeoShapesAsync(
            uint? pagenumber = 1,
            PageSize pagesize = null!,
            string? srid = "epsg:4362",
            string source = null,
            string type = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await Get(
                pagenumber,
                pagesize,
                srid,
                source,
                type,
                fields: fields ?? Array.Empty<string>(),
                searchfilter,
                rawfilter,
                rawsort,
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET GeoShape Single
        /// </summary>
        /// <param name="id">ID of the Tag</param>
        /// <param name="format">Spatial Reference Identifier, Coordinate System of the geojson, available formats(epsg:4362,epsg:32632,epsg:3857)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>GeoShapes Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(GeoShapeJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("GeoShapes/{id}", Name = "SingleGeoShapes")]
        [HttpGet, Route("GeoShape/{id}", Name = "SingleGeoShape")]
        //[Authorize(Roles = "DataReader,CommonReader,AccoReader,ActivityReader,PoiReader,ODHPoiReader,PackageReader,GastroReader,EventReader,ArticleReader")]
        public async Task<IActionResult> GetGeoShapeSingle(
            string id,
            string? srid = "epsg:4362",
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetSingle(
                id,
                srid,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        #endregion

        #region GETTER

        private Task<IActionResult> Get(
            uint? pagenumber,
            int? pagesize,
            string srid,
            string source,
            string type,
            string[] fields,
            string? searchfilter,
            string? rawfilter,
            string? rawsort,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                string columntoretrieve = "data";
                if (srid.Contains("32632"))
                    columntoretrieve = "data32632";
                if (srid.Contains("4326") || srid.ToLower().Contains("wgs84"))
                    columntoretrieve = "data";
                if (srid.Contains("3857") || srid.ToLower().Contains("mercator"))
                    columntoretrieve = "data3857";

                //TODO Add searchfilter

                var query = QueryFactory
                    .Query()
                    .SelectRaw(columntoretrieve + " as data")
                    .From("geoshapes")
                    .When(!String.IsNullOrEmpty(source), x => x.Where("source", source))
                    .When(!String.IsNullOrEmpty(type), x => x.Where("type", type))
                    .SearchFilter(new List<string>() { "Name" }.ToArray(), searchfilter)
                    .ApplyRawFilter(rawfilter)
                    .ApplyOrdering(
                        new PGGeoSearchResult() { geosearch = false },
                        rawsort,
                        "data #>>'\\{Type\\}', data#>>'\\{Name\\}'"
                    );

                // Get paginated data
                var data = await query.PaginateAsync<JsonRaw>(
                    page: (int)pagenumber,
                    perPage: pagesize ?? 10
                );

                var dataTransformed = data.List.Select(raw =>
                    raw.TransformRawData(
                        null,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );

                uint totalpages = (uint)data.TotalPages;
                uint totalcount = (uint)data.Count;

                return ResponseHelpers.GetResult(
                    (uint)pagenumber,
                    totalpages,
                    totalcount,
                    null,
                    dataTransformed,
                    Url
                );
            });
        }

        private Task<IActionResult> GetSingle(
            string id,
            string srid,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);
                
                string columntoretrieve = "data";
                if (srid.Contains("32632"))
                    columntoretrieve = "data32632";
                if (srid.Contains("4326") || srid.ToLower().Contains("wgs84"))
                    columntoretrieve = "data";
                if (srid.Contains("3857") || srid.ToLower().Contains("mercator"))
                    columntoretrieve = "data3857";

                var data = await QueryFactory
                    .Query("geoshapes")
                    .SelectRaw(columntoretrieve + " as data")
                    .Where("id", id)
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    //.FilterDataByAccessRoles(UserRolesToFilter)
                    .FirstOrDefaultAsync<JsonRaw>();

                return data?.TransformRawData(
                    null,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );

            });
        }

        #endregion
    }
}
