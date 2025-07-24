// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using AspNetCore.CacheOutput;
using DataModel;
using Geo.Measure;
using Helper;
using Helper.Generic;
using Helper.Identity;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Utilities;
using NuGet.Protocol.Core.Types;
using OdhApiCore.Controllers.api;
using OdhApiCore.Responses;
using OdhNotifier;
using Schema.NET;
using ServiceReferenceLCS;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// Gastronomy Api (data provided by LTS GastronomicData) Deprecated! Please use Endpoint ODHActivityPoi?type=32
    /// </summary>
    [Obsolete("Please use ODHActivityPoi Endpoint")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]     
    public class GastronomyController : OdhController
    {
        public GastronomyController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<GastronomyController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Gastronomy List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Gastronomy IDs), (default:'null')</param>
        /// <param name="locfilter">Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a></param>
        /// <param name="dishcodefilter">Dish Code Filter (BITMASK values: 1 = (Speisen), 2 = (Vorspeise), 4 = (Hauptspeise), 8 = (Nachspeise), 16 = (Tagesgericht), 32 = (Menü), 64 = (Degustationsmenü), 128 = (Kindermenüs), 256 = (Mittagsmenüs)</param>
        /// <param name="ceremonycodefilter">Ceremony Code Filter (BITMASK  values: 1 = (Familienfeiern), 2 = (Hochzeiten), 4 = (Geburtstagsfeiern), 8 = (Firmenessen), 16 = (Weihnachtsessen), 32 = (Sylvestermenü), 64 = (Seminare / Tagungen), 128 = (Versammlungen)</param>
        /// <param name="categorycodefilter">Category Code Filter (BITMASK  values: 1 = (Restaurant), 2 = (Bar / Café / Bistro), 4 = (Pub / Disco), 8 = (Apres Ski), 16 = (Jausenstation), 32 = (Pizzeria), 64 = (Bäuerlicher Schankbetrieb), 128 = (Buschenschank), 256 = (Hofschank), 512 = (Törggele Lokale), 1024 = (Schnellimbiss), 2048 = (Mensa), 4096 = (Vinothek /Weinhaus / Taverne), 8192 = (Eisdiele), 16348 = (Gasthaus), 32768 = (Gasthof), 65536 = (Braugarten), 131072 = (Schutzhütte), 262144 = (Alm), 524288 = (Skihütte)</param>
        /// <param name="facilitycodefilter">Facility Code Filter (BITMASK  values: 1 = (American Express), 2 = (Diners Club), 4 = (Eurocard / Mastercard), 8 = (Visa), 16 = (Hunde erlaubt), 32 = (Geeignet für Busse), 64 = (Garten), 128 = (Garagen), 256 = (Bierbar), 512 = (Kinderspielplatz), 1024 = (Spielzimmer), 2048 = (Spielplatz), 4096 = (Parkplätze), 8192 = (Raucherräume), 16348 = (Terrasse), 32768 = (Behindertengerecht), 65536 = (Biergarten), 131072 = (Aussichtsterrasse), 262144 = (Wintergarten), 524288 = (Gault Millau Südtirol), 1048576 = (Guida Espresso), 2097152 = (Gambero Rosso), 4194304 = (Feinschmecker), 8388608 = (Aral Schlemmer Atlas), 16777216 = (Varta Führer), 33554432 = (Bertelsmann), 67108864 = (Preis für Südtiroler Weinkultur), 134217728 = (Michelin), 268435456 = (Roter Hahn), 536870912 = (Tafelspitz))</param>
        /// <param name="cuisinecodefilter">Cuisine Code Filter (BITMASK  values: 1 = (Vegetarische Küche), 2 = (Glutenfreie Küche), 4 = (Laktosefreie Kost), 8 = (Warme Küche), 16 = (Südtiroler Spezialitäten), 32 = (Gourmet Küche), 64 = (Italienische Küche), 128 = (Internationale Küche), 256 = (Pizza), 512 = (Fischspezialitäten), 1024 = (Asiatische Küche), 2048 = (Wildspezialitäten), 4096 = (Produkte eigener Erzeugung), 8192 = (Diätküche), 16348 = (Grillspezialitäten), 32768 = (Ladinische Küche), 65536 = (Kleine Karte), 131072 = (Fischwochen), 262144 = (Spargelwochen), 524288 = (Lammwochen), 1048576 = (Wildwochen), 2097152 = (Vorspeisewochen), 4194304 = (Nudelwochen), 8388608 = (Kräuterwochen), 16777216 = (Kindermenüs), 33554432 = (Mittagsmenüs))</param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=gastronomy'), (default:'null')</param>
        /// <param name="active">Active Gastronomies Filter (possible Values: 'true' only Active Gastronomies, 'false' only Disabled Gastronomies), (default:'null'</param>
        /// <param name="odhactive">ODH Active (Published) Gastronomies Filter (Refers to field OdhActive) (possible Values: 'true' only published Gastronomies, 'false' only not published Gastronomies), (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of Gastronomy Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        //[ProducesResponseType(typeof(JsonResult<GastronomyLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(JsonResult<ODHActivityPoiLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        [HttpGet, Route("Gastronomy")]
        public async Task<IActionResult> GetGastronomyList(
            string? language = null,
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? idlist = null,
            string? locfilter = null,
            string? dishcodefilter = null,
            string? ceremonycodefilter = null,
            string? categorycodefilter = null,
            string? facilitycodefilter = null,
            string? cuisinecodefilter = null,
            string? odhtagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? updatefrom = null,
            string? seed = null,
            string? langfilter = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            //Use the ODHActivityPoi Endpoint
            return this.RedirectToRoute("GetODHActivityPoiList",
                new { 
                    tagfilter = "Gastronomy",
                    seed = seed,
                    latitude = latitude,
                    longitude = longitude,
                    radius = radius,
                    polygon = polygon,
                    searchfilter = searchfilter,
                    rawfilter = rawfilter,
                    rawsort = rawsort,
                    removenullvalues=removenullvalues,
                    getasidarray = getasidarray,
                    cancellationToken = cancellationToken
                });          
        }

        /// <summary>
        /// GET Gastronomy Single
        /// </summary>
        /// <param name="id">ID of the Gastronomy</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Gastronomy Object</returns>
        [ProducesResponseType(typeof(ODHActivityPoiLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Gastronomy/{id}", Name = "SingleGastronomy")]
        public async Task<IActionResult> GetGastronomySingle(
            string id,
            string? language,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            //Hack Replace GASTRO with SMGPOI in Id
            if (id.ToLower().StartsWith("gastro"))
                id = id.ToLower().Replace("gastro", "smgpoi");

            //Use the ODHActivityPoi Endpoint
            return this.RedirectToRoute("SingleODHActivityPoi",
                new
                {
                    id = id,
                    language = language,
                    fields = fields,
                    removenullvalues = removenullvalues,
                    cancellationToken = cancellationToken
                });
        }

        /// <summary>
        /// GET Gastronomy Types List
        /// </summary>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of GastronomyTypes Object</returns>
        [ProducesResponseType(typeof(IEnumerable<GastronomyTypes>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,GastroReader")]
        [HttpGet, Route("GastronomyTypes")]
        public async Task<IActionResult> GetAllGastronomyTypesList(
            string? language,
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? seed = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return StatusCode(410, new
            {
                message = "This endpoint is no longer available.",
                replacement = "/v1/Tag?source=lts&validforentity=gastronomy",
                note = "Please refer to the API docs for migration."
            });
        }

        /// <summary>
        /// GET Gastronomy Types Single
        /// </summary>
        /// <param name="id">ID of the Gastronomy Type</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>GastronomyTypes Object</returns>
        [ProducesResponseType(typeof(GastronomyTypes), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,GastroReader")]
        [HttpGet, Route("GastronomyTypes/{id}", Name = "SingleGastronomyTypes")]
        public async Task<IActionResult> GetAllGastronomyTypesList(
            string id,
            string? language,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            //Use the ODHActivityPoi Endpoint
            return this.RedirectToRoute("SingleTag",
                new
                {
                    id = id,
                    language = language,
                    fields = fields,
                    removenullvalues = removenullvalues,
                    cancellationToken = cancellationToken
                });
        }

        #endregion
    }
}
