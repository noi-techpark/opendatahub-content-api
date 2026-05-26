// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http;
using System.Threading.Tasks;
using GeoConverter;
using Helper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OdhApiCore.Swagger;
using Microsoft.Extensions.Logging;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers.api
{
    /// <summary>
    /// <a href="https://github.com/noi-techpark/odh-docs/wiki/Geoshapes-Api-and-Geo-Converter#geoconverter-api" target="_blank">Wiki GeoConverter Api</a>
    /// </summary>
    [SwaggerTags("Z6 GeoConverter")]
    public class GeoConverterController : OdhController
    {
        private readonly IHttpClientFactory _clientFactory;

        public GeoConverterController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<GeoConverterController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier,
            IHttpClientFactory clientFactory
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            _clientFactory = clientFactory;
        }

        /// <summary>
        /// Converts the KML file from the supplied URL to GeoJSON.
        /// </summary>
        /// <param name="url">The URL to the KML file.</param>
        /// <returns>The generated GeoJSON file.</returns>
        [HttpGet]
        [Route("GeoConverter/KmlToGeoJson")]
        [Produces("application/geo+json")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetKmlToGeoJson([FromQuery] string url)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound($"URL {url} not found.");
            }
            var body = await response.Content.ReadAsStringAsync();
            return PostKmlToGeoJson(body);
        }

        /// <summary>
        /// Converts the KML provided as the body to GeoJSON.
        /// </summary>
        /// <param name="body">The KML file content.</param>
        /// <returns>application/geo+json</returns>
        /// <returns>The generated GeoJSON file.</returns>
        [HttpPost]
        [Route("GeoConverter/KmlToGeoJson")]
        [Produces("application/geo+json")]
        [ProducesResponseType(200)]
        public ActionResult PostKmlToGeoJson(string body)
        {
            return Content(GeoJsonConverter.ConvertFromKml(body), "application/geo+json");
        }

        /// <summary>
        /// Converts the GPX file from the supplied URL to GeoJSON.
        /// </summary>
        /// <param name="url">The URL to the GPX file.</param>
        /// <returns>The generated GeoJSON file.</returns>
        [HttpGet]
        [Route("GeoConverter/GpxToGeoJson")]
        [Produces("application/geo+json")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetGpxToGeoJson([FromQuery] string url)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound($"URL {url} not found.");
            }
            var body = await response.Content.ReadAsStringAsync();
            return PostGpxToGeoJson(body);
        }

        /// <summary>
        /// Converts the GPX provided as the body to GeoJSON.
        /// </summary>
        /// <param name="body">The GPX file content.</param>
        /// <returns>application/geo+json</returns>
        /// <returns>The generated GeoJSON file.</returns>
        [HttpPost]
        [Route("GeoConverter/GpxToGeoJson")]
        [Produces("application/geo+json")]
        [ProducesResponseType(200)]
        public ActionResult PostGpxToGeoJson(string body)
        {
            return Content(GeoJsonConverter.ConvertFromGpx(body), "application/geo+json");
        }
    }
}
