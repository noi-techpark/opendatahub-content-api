// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AspNetCore.CacheOutput;
using CDB;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Identity;
using Helper.Location;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Responses;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// Accommodation Api (data provided by LTS / Availability Requests provided by HGV/LTS) SOME DATA Available as OPENDATA
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class AccommodationController : OdhController
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ISettings settings;

        public AccommodationController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<AccommodationController> logger,
            QueryFactory queryFactory,
            IHttpClientFactory httpClientFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier)
        {
            this.httpClientFactory = httpClientFactory;
            this.settings = settings;
        }

        //Duplicate on AVailabilitySearchInterceptorAttribute
        private bool CheckAvailabilitySearch(System.Security.Claims.ClaimsPrincipal User)
        {
            List<string> roles = new List<string>() { "DataReader", "AccoReader" };

            foreach (var role in roles)
            {
                if (User.IsInRole(role))
                    return true;
            }

            return false;
        }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Accommodation List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page (If availabilitycheck set, pagesize has no effect all Accommodations are returned), (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="categoryfilter">Categoryfilter BITMASK values: 1 = (not categorized), 2 = (1star), 4 = (1flower), 8 = (1sun), 14 = (1star/1flower/1sun), 16 = (2stars), 32 = (2flowers), 64 = (2suns), 112 = (2stars/2flowers/2suns), 128 = (3stars), 256 = (3flowers), 512 = (3suns), 1024 = (3sstars), 1920 = (3stars/3flowers/3suns/3sstars), 2048 = (4stars), 4096 = (4flowers), 8192 = (4suns), 16384 = (4sstars), 30720 = (4stars/4flowers/4suns/4sstars), 32768 = (5stars), 65536 = (5flowers), 131072 = (5suns), 229376 = (5stars/5flowers/5suns), 'null' = (No Filter), (default:'null')</param>
        /// <param name="typefilter">Typefilter BITMASK values: 1 = (HotelPension), 2 = (BedBreakfast), 4 = (Farm), 8 = (Camping), 16 = (Youth), 32 = (Mountain), 64 = (Apartment), 128 = (Not defined),'null' = (No Filter), (default:'null')</param>
        /// <param name="boardfilter">Boardfilter BITMASK values: 0 = (all boards), 1 = (without board), 2 = (breakfast), 4 = (half board), 8 = (full board), 16 = (All inclusive), 'null' = (No Filter), (default:'null')</param>
        /// <param name="featurefilter">FeatureFilter BITMASK values: 1 = (Group-friendly), 2 = (Meeting rooms), 4 = (Swimming pool), 8 = (Sauna), 16 = (Garage), 32 = (Pick-up service), 64 = (WLAN), 128 = (Barrier-free), 256 = (Special menus for allergy sufferers), 512 = (Pets welcome), 'null' = (No Filter), (default:'null')</param>
        /// <param name="featureidfilter">Feature Id Filter, LIST filter over ALL Features available. Separator ',' List of Feature IDs, 'null' = (No Filter), (default:'null')</param>
        /// <param name="themefilter">Themefilter BITMASK values: 1 = (Gourmet), 2 = (At altitude), 4 = (Regional wellness offerings), 8 = (on the wheels), 16 = (With family), 32 = (Hiking), 64 = (In the vineyards), 128 = (Urban vibe), 256 = (At the ski resort), 512 = (Mediterranean), 1024 = (In the Dolomites), 2048 = (Alpine), 4096 = (Small and charming), 8192 = (Huts and mountain inns), 16384 = (Rural way of life), 32768 = (Balance), 65536 = (Christmas markets), 131072 = (Sustainability), 'null' = (No Filter), (default:'null')</param>
        /// <param name="badgefilter">BadgeFilter BITMASK values: 1 = (Belvita Wellness Hotel), 2 = (Familyhotel), 4 = (Bikehotel), 8 = (Red Rooster Farm), 16 = (Barrier free certificated), 32 = (Vitalpina Hiking Hotel), 64 = (Private Rooms in South Tyrol), 128 = (Vinum Hotels), 'null' = (No Filter), (default:'null')</param>
        /// <param name="idfilter">IDFilter LIST Separator ',' List of Accommodation IDs, 'null' = (No Filter), (default:'null')</param>
        /// <param name="locfilter">Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a></param>
        /// <param name="odhtagfilter">ODHTag Filter LIST (refers to Array SmgTags) (String, Separator ',' more ODHTags possible, 'null' = No Filter, available ODHTags reference to 'v1/ODHTag?validforentity=accommodation'), (default:'null')</param>
        /// <param name="odhactive">ODHActive Filter BOOLEAN (refers to field SmgActive) (possible Values: 'null' Displays all Accommodations, 'true' only ODH Active Accommodations, 'false' only ODH Disabled Accommodations), (default:'null')</param>
        /// <param name="active">TIC Active Filter BOOLEAN (possible Values: 'null' Displays all Accommodations, 'true' only TIC Active Accommodations, 'false' only TIC Disabled Accommodations), (default:'null')</param>
        /// <param name="altitudefilter">Altitude Range Filter SPECIAL (Separator ',' example Value: 500,1000 Altitude from 500 up to 1000 metres), (default:'null')</param>
        /// <param name="availabilitycheck">Availability Check BOOLEAN (possible Values: 'true', 'false), (default Value: 'false') NOT AVAILABLE AS OPEN DATA, IF Availabilty Check is true certain filters are Required</param>
        /// <param name="arrival">Arrival DATE (yyyy-MM-dd) REQUIRED ON Availabilitycheck = true, (default:'Today's date')</param>
        /// <param name="departure">Departure DATE (yyyy-MM-dd) REQUIRED ON Availabilitycheck = true, (default:'Tomorrow's date')</param>
        /// <param name="roominfo">Roominfo Filter REQUIRED ON Availabilitycheck = true (Splitter for Rooms '|' Splitter for Persons Ages ',') (Room Types: 0=notprovided, 1=room, 2=apartment, 4=pitch/tent(onlyLTS), 8=dorm(onlyLTS)) possible Values Example 1-18,10|1-18 = 2 Rooms, Room 1 for 2 person Age 18 and Age 10, Room 2 for 1 Person Age 18), (default:'1-18,18')</param>
        /// <param name="bokfilter">Booking Channels Filter REQUIRED ON Availabilitycheck = true (Separator ',' possible values: hgv = (Booking Südtirol), htl = (Hotel.de), exp = (Expedia), bok = (Booking.com), lts = (LTS Availability check)), (default:'hgv')</param>
        /// <param name="msssource">Source for MSS availability check, (default:'sinfo')</param>
        /// <param name="availabilitychecklanguage">Language of the Availability Response (possible values: 'de','it','en')</param>
        /// <param name="detail">Detail of the Availablity check (string, 1 = full Details, 0 = basic Details (default))</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter' target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of Accommodation Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<AccommodationV2>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [TypeFilter(typeof(Filters.AvailabilitySearchInterceptorAttribute))]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        [HttpGet, Route("Accommodation", Name = "AccommodationList")]
        public async Task<IActionResult> GetAccommodations(
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? seed = null,
            string? categoryfilter = null,
            string? typefilter = null,
            string? boardfilter = null,
            string? featurefilter = null,
            string? featureidfilter = null,
            string? themefilter = null,
            string? badgefilter = null,
            string? idfilter = null,
            string? locfilter = null,
            string? altitudefilter = null,
            string? odhtagfilter = null,
            string? source = null,
            LegacyBool odhactive = null!,
            LegacyBool active = null!,
            LegacyBool bookablefilter = null!,
            string? arrival = null,
            string? departure = null,
            string? roominfo = "1-18,18",
            string? bokfilter = "hgv",
            string? msssource = "sinfo",
            string? availabilitychecklanguage = "en",
            string? detail = "0",
            LegacyBool availabilitycheck = null!,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            string? publishedon = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            //if availabilitysearch requested and User not logged
            if (availabilitycheck?.Value == true && !CheckAvailabilitySearch(User))
            {
                return Unauthorized("User not allowed for availabilitysearch");
            }

            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            List<string> bokfilterlist = bokfilter?.Split(',').ToList() ?? new List<string>();

            if (availabilitycheck?.Value != true)
            {
                return await GetFiltered(
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    pagenumber: pagenumber,
                    pagesize: pagesize,
                    idfilter: idfilter,
                    idlist: new List<string>(),
                    locfilter: locfilter,
                    categoryfilter: categoryfilter,
                    typefilter: typefilter,
                    boardfilter: boardfilter,
                    featurefilter: featurefilter,
                    featureidfilter: featureidfilter,
                    themefilter: themefilter,
                    badgefilter: badgefilter,
                    altitudefilter: altitudefilter,
                    active: active,
                    smgactive: odhactive,
                    bookablefilter: bookablefilter,
                    smgtagfilter: odhtagfilter,
                    sourcefilter: source,
                    publishedon: publishedon,
                    seed: seed,
                    updatefrom: updatefrom,
                    langfilter: langfilter,
                    searchfilter: searchfilter,
                    polygonsearchresult,
                    geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                ); ;
            }
            else if (availabilitycheck?.Value == true)
            {
                var accobooklist = Request.HttpContext.Items["accobooklist"];
                var accoavailabilitymss = Request.HttpContext.Items["mssavailablity"];
                var accoavailabilitylcs = Request.HttpContext.Items["lcsavailablity"];

                var availableonlineaccos = new List<string>();
                if (accoavailabilitymss != null)
                    availableonlineaccos.AddRange(
                        ((MssResult?)accoavailabilitymss)
                            ?.MssResponseShort?.Select(x => x.A0RID?.ToUpper() ?? "")
                            .Distinct()
                            .ToList() ?? new List<string>()
                    );
                if (accoavailabilitylcs != null)
                    availableonlineaccos.AddRange(
                        ((MssResult?)accoavailabilitylcs)
                            ?.MssResponseShort?.Select(x => x.A0RID?.ToUpper() ?? "")
                            .Distinct()
                            .ToList() ?? new List<string>()
                    );

                //TODO SORT ORDER???

                return await GetFiltered(
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    pagenumber: pagenumber,
                    pagesize: pagesize,
                    idfilter: idfilter,
                    idlist: availableonlineaccos,
                    locfilter: locfilter,
                    categoryfilter: categoryfilter,
                    typefilter: typefilter,
                    boardfilter: boardfilter,
                    featurefilter: featurefilter,
                    featureidfilter: featureidfilter,
                    themefilter: themefilter,
                    badgefilter: badgefilter,
                    altitudefilter: altitudefilter,
                    active: active,
                    smgactive: odhactive,
                    bookablefilter: bookablefilter,
                    smgtagfilter: odhtagfilter,
                    sourcefilter: source,
                    publishedon: publishedon,
                    seed: seed,
                    updatefrom: updatefrom,
                    langfilter: langfilter,
                    searchfilter: searchfilter,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return BadRequest("not supported!");
            }
        }

        /// <summary>
        /// GET Accommodation Single
        /// </summary>
        /// <param name="id">ID of the Accommodation</param>
        /// <param name="idsource">ID Source Filter (possible values:'lts','hgv','a0r_id'), (default:'lts')</param>
        /// <param name="boardfilter">Boardfilter BITMASK values: 0 = (all boards), 1 = (without board), 2 = (breakfast), 4 = (half board), 8 = (full board), 16 = (All inclusive), 'null' = (No Filter), (default:'null')</param>
        /// <param name="availabilitycheck">Availability Check enabled/disabled (possible Values: 'true', 'false), (default Value: 'false') NOT AVAILABLE AS OPEN DATA</param>
        /// <param name="arrival">Arrival Date (yyyy-MM-dd) REQUIRED, (default:'Today')</param>
        /// <param name="departure">Departure Date (yyyy-MM-dd) REQUIRED, (default:'Tomorrow')</param>
        /// <param name="roominfo">Roominfo Filter REQUIRED (Splitter for Rooms '|' Splitter for Persons Ages ',') (Room Types: 0=notprovided, 1=room, 2=apartment, 4=pitch/tent(onlyLTS), 8=dorm(onlyLTS)) possible Values Example 1-18,10|1-18 = 2 Rooms, Room 1 for 2 person Age 18 and Age 10, Room 2 for 1 Person Age 18), (default:'1-18,18')</param>/// <param name="source">Source for MSS availability check, (default:'sinfo')</param>
        /// <param name="bokfilter">Booking Channels Filter REQUIRED (Separator ',' possible values: hgv = (Booking Südtirol), htl = (Hotel.de), exp = (Expedia), bok = (Booking.com), lts = (LTS Availability check)), (default:'hgv')</param>
        /// <param name="availabilitychecklanguage">Language of the Availability Response (possible values: 'de','it','en')</param>
        /// <param name="detail">Detail of the Availablity check (string, 1 = full Details, 0 = basic Details (default))</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Accommodation Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(AccommodationV2), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Accommodation/{id}", Name = "SingleAccommodation")]
        [TypeFilter(typeof(Filters.AvailabilitySearchInterceptorAttribute))]
        public async Task<IActionResult> GetAccommodation(
            string id,
            string? idsource = "lts",
            string? availabilitychecklanguage = "en",
            string? boardfilter = null,
            string? arrival = null,
            string? departure = null,
            string? roominfo = "1-18,18",
            string? bokfilter = "hgv",
            string? msssource = "sinfo",
            LegacyBool availabilitycheck = null!,
            string? detail = "0",
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            //if availabilitysearch requested and User not logged
            if (availabilitycheck?.Value == true && !CheckAvailabilitySearch(User))
            {
                return Unauthorized("User not allowed for availabilitysearch");
            }

            if (idsource == "hgv")
                return await GetSingleByHgvId(
                    id,
                    language,
                    fields: fields ?? Array.Empty<string>(),
                    removenullvalues,
                    cancellationToken
                );
            else if (idsource == "a0r_id")
                return await GetSingleBya0r_id(
                    id,
                    language,
                    fields: fields ?? Array.Empty<string>(),
                    removenullvalues,
                    cancellationToken
                );
            else
                return await GetSingle(
                    id,
                    language,
                    fields: fields ?? Array.Empty<string>(),
                    removenullvalues,
                    cancellationToken
                );
        }

        //ACCO TYPES

        /// <summary>
        /// GET Accommodation Types List
        /// </summary>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)</param>
        /// <param name="type">Type to filter for ('Board','Type','Theme','Category','Badge','SpecialFeature')</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter' target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of AccommodationType Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(IEnumerable<AccoTypes>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("AccommodationTypes")]
        public async Task<IActionResult> GetAllAccommodationTypesList(
            string? language,
            string? type = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetAccoTypeList(
                language,
                type,
                fields: fields ?? Array.Empty<string>(),
                searchfilter,
                rawfilter,
                rawsort,
                removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Accommodation Types Single
        /// </summary>
        /// <param name="id">ID of the AccommodationType</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>AccommodationType Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(AccoTypes), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,AccoReader")]
        [HttpGet, Route("AccommodationTypes/{id}", Name = "SingleAccommodationTypes")]
        public async Task<IActionResult> GetAllAccommodationTypessingle(
            string id,
            string? language,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetAccoTypeSingle(
                id,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Accommodation Feature List (LTS Features)
        /// </summary>
        /// <param name="ltst0idfilter">Filtering by LTS T0ID, filter behaviour is "startswith" so it is possible to send only one character, (default: blank)</param>
        /// <param name="source">IF source = "lts" the Features list is returned in XML Format directly from LTS, (default: blank)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter' target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of AccoFeatures Object / XML LTS</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(IEnumerable<AccoFeature>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,AccoReader")]
        [HttpGet, Route("AccommodationFeatures")]
        public async Task<IActionResult> GetAllAccommodationFeaturesList(
            string? language,
            string? ltst0idfilter = null,
            string? source = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            if (!String.IsNullOrEmpty(source) && source == "lts")
                return GetFeatureListXML(cancellationToken);
            else
                return await GetAccoFeatureList(
                    language,
                    ltst0idfilter,
                    fields: fields ?? Array.Empty<string>(),
                    searchfilter,
                    rawfilter,
                    rawsort,
                    removenullvalues,
                    cancellationToken
                );
        }

        /// <summary>
        /// GET Accommodation Feature Single (LTS Features)
        /// </summary>
        /// <param name="id">ID of the AccommodationFeature</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language, possible values: 'de|it|en|nl|cs|pl|fr|ru' only one language supported (default:'null' all languages are displayed)</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>AccoFeatures Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(AccoFeature), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,AccoReader")]
        [HttpGet, Route("AccommodationFeatures/{id}", Name = "SingleAccommodationFeatures")]
        public async Task<IActionResult> GetAllAccommodationFeaturesSingle(
            string id,
            string? language,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetAccoFeatureSingle(
                id,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues,
                cancellationToken
            );
        }

        // ACCO ROOMS

        /// <summary>
        /// GET Accommodation Room Info by Accommodation
        /// </summary>
        /// <param name="accoid">Accommodation ID</param>
        /// <param name="idsource">HGV ID or LTS ID of the Accommodation (possible values:'lts','hgv','a0r_id'), (default:'lts')</param>
        /// <param name="source">Source Filter (possible values:'lts','hgv'), (default:null)</param>
        /// <param name="getall">Get Rooms from all sources (If an accommodation is bookable on Booking Southtyrol, rooms from this source are returned, setting getall to true returns also LTS Rooms), (default:false)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter' target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href='https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter' target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of AccoRoom Objects</returns>
        [ProducesResponseType(
            typeof(IEnumerable<AccommodationRoomLinked>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,AccoReader")]
        [HttpGet, Route("AccommodationRoom", Name = "AccommodationRoomList")]
        public async Task<IActionResult> GetAccoRoomInfos(
            string accoid,
            string? idsource = "lts",
            string? source = null,
            bool getall = false,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            string idtocheck = accoid;

            if (idsource == "hgv")
            {
                idtocheck = await GetAccoIdByHgvId(accoid, cancellationToken);
            }
            else if (idsource == "a0r_id")
            {
                idtocheck = await GetAccoIdByHgvId(accoid, cancellationToken);
            }

            return await GetAccommodationRooms(
                idtocheck,
                fields: fields ?? Array.Empty<string>(),
                language,
                getall,
                source,
                updatefrom,
                langfilter,
                searchfilter,
                rawfilter,
                rawsort,
                removenullvalues,
                cancellationToken
            );
        }

        // ACCO ROOMS

        /// <summary>
        /// GET Accommodation Room Info Single
        /// </summary>
        /// <param name="id">AccommodationRoom ID</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>AccommodationRoom Object</returns>
        [ProducesResponseType(typeof(AccommodationRoomLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,AccoReader")]
        [HttpGet, Route("AccommodationRoom/{id}", Name = "SingleAccommodationRoom")]
        public async Task<IActionResult> GetAccoRoomInfosById(
            string id,
            string? language = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetSingleAccommodationRoom(
                id,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues,
                cancellationToken
            );
        }

        //SPECIAL GETTER

        /// <summary>
        /// POST Pass Accommodation Ids and get Accommodations with Availability Information / Availability Information Only <a href="https://github.com/noi-techpark/odh-docs/wiki/Accommodation-Workflow#availability-search" target="_blank">Wiki Availability Search</a>
        /// </summary>
        /// <param name="availabilitychecklanguage">Language of the Availability Response</param>
        /// <param name="boardfilter">Boardfilter (BITMASK values: 0 = (all boards), 1 = (without board), 2 = (breakfast), 4 = (half board), 8 = (full board), 16 = (All inclusive), 'null' = No Filter)</param>
        /// <param name="arrival">Arrival Date (yyyy-MM-dd) REQUIRED</param>
        /// <param name="departure">Departure Date (yyyy-MM-dd) REQUIRED</param>
        /// <param name="roominfo">Roominfo Filter REQUIRED (Splitter for Rooms '|' Splitter for Persons Ages ',') (Room Types: 0=notprovided, 1=room, 2=apartment, 4=pitch/tent(onlyLTS), 8=dorm(onlyLTS)) possible Values Example 1-18,10|1-18 = 2 Rooms, Room 1 for 2 person Age 18 and Age 10, Room 2 for 1 Person Age 18), (default:'1-18,18')</param>/// <param name="bokfilter">Booking Channels Filter (Separator ',' possible values: hgv = (Booking Südtirol), htl = (Hotel.de), exp = (Expedia), bok = (Booking.com), lts = (LTS Availability check), (default:hgv)) REQUIRED</param>
        /// <param name="detail">Include Offer Details (String, 1 = full Details)</param>
        /// <param name="msssource">Source of the Requester (possible value: 'sinfo' = Suedtirol.info, 'sbalance' = Südtirol Balance) REQUIRED</param>
        /// <param name="availabilityonly">Get only availability information without Accommodation information</param>
        /// <param name="locfilter">Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a></param>
        /// <param name="usemsscache">Use the MSS Cache to Request Data. No Ids have to be passed, the whole MSS Result of whole South Tyrol is used, available options (null/false/true) default (null), false = pass always the Ids to MSS omit its caching mechanism, true = do not pass ids to MSS get availability result and filter the resultset of MSS, null = let opendatahub decide when to use caching and when not.</param>
        /// <param name="uselcscache">Currently not used (planned to be active in 2025)</param>
        /// <param name="removeduplicatesfrom">Remove all duplicate offers from the requested booking channel possible values: ('lts','hgv'), default(NULL)</param>
        /// <param name="idfilter">Posted Accommodation IDs (Separated by , must be specified in the POST Body as raw)</param>
        /// <returns>Result Object with Collection of Accommodation Objects</returns>
        [ProducesResponseType(
            typeof(JsonResultWithBookingInfo<AccommodationV2>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,AccoReader,PackageReader")]
        [TypeFilter(typeof(Filters.AvailabilitySearchInterceptorAttribute))]
        [HttpPost, Route("AccommodationAvailable")]
        //[HttpPost, Route("AvailabilityCheck")]
        public async Task<IActionResult> PostAvailableAccommodations(
            [FromBody(
                EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow
            )]
            string? idfilter = null,
            string? availabilitychecklanguage = "en",
            string? boardfilter = null,
            string? arrival = null,
            string? departure = null,
            string? roominfo = "1-18,18",
            string? bokfilter = "hgv",
            string? msssource = "sinfo",
            string? detail = "0",
            string? locfilter = null,
            string? publishedon = null, //setting idm-marketplace as default publishedon
            bool availabilityonly = false,
            bool usemsscache = false,
            bool uselcscache = true,
            string removeduplicatesfrom = null, //only valid for availability only?
            string? ltsapiversion = "v1",
            CancellationToken cancellationToken = default
        )
        {
            bokfilter ??= "hgv";

            //if availabilitysearch requested and User not logged
            if (!CheckAvailabilitySearch(User))
            {
                return Unauthorized("User not allowed for availabilitysearch");
            }

            //For Compatiblity if Route equals AvailabilityCheck return only Availability Response
            var usedroute = ControllerContext.ActionDescriptor.AttributeRouteInfo?.Template;

            if (usedroute == "v1/AvailabilityCheck")
                availabilityonly = true;

            //TODO CHECK THIS WITHOUTIDS Stuff (not needed if no id is passed)
            ////If no ids in the post body, makes sure withoutids is checked (make use of cached MSS)
            //if((idfilter == null || String.IsNullOrEmpty(idfilter)))
            //    return BadRequest("No Ids in the POST Body, Availability Search over all Accommodations only with withoutids set to true");

            var accobooklist = Request.HttpContext.Items["accobooklist"];
            var accoavailabilitymss = ((MssResult?)Request.HttpContext.Items["mssavailablity"]);
            var accoavailabilitylcs = ((MssResult?)Request.HttpContext.Items["lcsavailablity"]);

            //Remove Availabilities from
            if (removeduplicatesfrom != null)
                RemoveDuplicatesFrom(
                    removeduplicatesfrom,
                    accoavailabilitymss,
                    accoavailabilitylcs
                );

            var resultid = accoavailabilitymss?.ResultId ?? "";

            //Counts
            var requestedtotal = accobooklist != null ? ((List<string>)accobooklist).Count : 0;

            var availableonline =
                accoavailabilitymss
                    ?.MssResponseShort?.Select(x => x.A0RID?.ToUpper() ?? "")
                    .Distinct()
                    .Count() ?? 0;
            var availableonrequest =
                accoavailabilitylcs
                    ?.MssResponseShort?.Select(x => x.A0RID?.ToUpper() ?? "")
                    .Distinct()
                    .Count() ?? 0;

            if (availabilityonly)
            {
                var toreturn = new List<MssResponseShort>();

                if (bokfilter.Contains("hgv") && accoavailabilitymss != null)
                    toreturn.AddRange(accoavailabilitymss?.MssResponseShort?.ToList() ?? new());
                if (bokfilter.Contains("lts") && accoavailabilitylcs != null)
                    toreturn.AddRange(accoavailabilitylcs?.MssResponseShort?.ToList() ?? new());

                //return immediately the mss response
                var result = ResponseHelpers.GetResult(
                    1,
                    1,
                    (uint)requestedtotal,
                    requestedtotal,
                    availableonline,
                    availableonrequest,
                    resultid,
                    "",
                    toreturn,
                    Url
                );

                return (Ok(result));
            }
            else
            {
                var accosonmss =
                    accoavailabilitymss
                        ?.MssResponseShort?.Select(x => x.A0RID?.ToUpper() ?? "")
                        .Distinct()
                        .ToList() ?? new List<string>();
                var accosonlcs =
                    accoavailabilitylcs
                        ?.MssResponseShort?.Select(x => x.A0RID?.ToUpper() ?? "")
                        .Distinct()
                        .ToList() ?? new List<string>();

                var availableonlineaccos = new List<string>();
                if (accoavailabilitymss != null)
                    availableonlineaccos.AddRange(accosonmss);
                if (accoavailabilitylcs != null)
                    availableonlineaccos.AddRange(accosonlcs);

                return await GetFiltered(
                    fields: Array.Empty<string>(),
                    language: null,
                    pagenumber: 1,
                    pagesize: int.MaxValue,
                    idfilter: idfilter,
                    idlist: availableonlineaccos,
                    locfilter: locfilter,
                    categoryfilter: null,
                    typefilter: null,
                    boardfilter: boardfilter,
                    featurefilter: null,
                    featureidfilter: null,
                    themefilter: null,
                    badgefilter: null,
                    altitudefilter: null,
                    active: null,
                    smgactive: null,
                    bookablefilter: null,
                    smgtagfilter: null,
                    sourcefilter: null,
                    publishedon: publishedon,
                    seed: null,
                    updatefrom: null,
                    langfilter: null,
                    searchfilter: null,
                    null,
                    new PGGeoSearchResult()
                    {
                        geosearch = false,
                        latitude = 0,
                        longitude = 0,
                        radius = 0,
                    },
                    rawfilter: null,
                    rawsort: null,
                    removenullvalues: false,
                    getasidarray: false,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// POST Pass Accommodation Ids and get Accommodations with Availability Information / Availability Information Only
        /// </summary>
        /// <param name="availabilitychecklanguage">Language of the Availability Response</param>
        /// <param name="boardfilter">Boardfilter (BITMASK values: 0 = (all boards), 1 = (without board), 2 = (breakfast), 4 = (half board), 8 = (full board), 16 = (All inclusive), 'null' = No Filter)</param>
        /// <param name="arrival">Arrival Date (yyyy-MM-dd) REQUIRED</param>
        /// <param name="departure">Departure Date (yyyy-MM-dd) REQUIRED</param>
        /// <param name="roominfo">Roominfo Filter REQUIRED (Splitter for Rooms '|' Splitter for Persons Ages ',') (Room Types: 0=notprovided, 1=room, 2=apartment, 4=pitch/tent(onlyLTS), 8=dorm(onlyLTS)) possible Values Example 1-18,10|1-18 = 2 Rooms, Room 1 for 2 person Age 18 and Age 10, Room 2 for 1 Person Age 18), (default:'1-18,18')</param>
        /// <param name="bokfilter">Booking Channels Filter (Separator ',' possible values: hgv = (Booking Südtirol), htl = (Hotel.de), exp = (Expedia), bok = (Booking.com), lts = (LTS Availability check), (default:hgv)) REQUIRED</param>
        /// <param name="detail">Include Offer Details (String, 1 = full Details)</param>
        /// <param name="msssource">Source of the Requester (possible value: 'sinfo' = Suedtirol.info, 'sbalance' = Südtirol Balance) REQUIRED</param>
        /// <param name="locfilter">Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a></param>
        /// <param name="usemsscache">Use the MSS Cache to Request Data. No Ids have to be passed, the whole MSS Result of whole South Tyrol is used, default(false)</param>
        /// <param name="uselcscache">Currently not used (planned to be active in 2025)</param>
        /// <param name="removeduplicatesfrom">Remove all duplicate offers from the requested booking channel possible values: ('lts','hgv'), default(NULL)</param>
        /// <param name="idfilter">Posted Accommodation IDs (Separated by , must be specified in the POST Body as raw)</param>
        /// <returns>Result Object with Collection of Accommodation Objects</returns>
        [ProducesResponseType(
            typeof(JsonResultWithBookingInfo<MssResult>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,AccoReader,PackageReader")]
        [TypeFilter(typeof(Filters.AvailabilitySearchInterceptorAttribute))]
        [HttpPost, Route("AvailabilityCheck")]
        public async Task<IActionResult> PostAvailableAccommodationsOnlyMssResult(
            [FromBody(
                EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow
            )]
            string? idfilter = null,
            string? availabilitychecklanguage = "en",
            string? boardfilter = null,
            string? arrival = null,
            string? departure = null,
            string? roominfo = "1-18,18",
            string? bokfilter = "hgv",
            string? msssource = "sinfo",
            string? detail = "0",
            string? locfilter = null,
            string? publishedon = "idm-marketplace",
            bool usemsscache = false,
            bool uselcscache = false,
            string? removeduplicatesfrom = null,
            string? ltsapiversion = "v1",
            CancellationToken cancellationToken = default
        )
        {
            return await PostAvailableAccommodations(
                idfilter,
                availabilitychecklanguage,
                boardfilter,
                arrival,
                departure,
                roominfo,
                bokfilter,
                msssource,
                detail,
                locfilter,
                publishedon,
                true,                
                usemsscache,
                uselcscache,
                removeduplicatesfrom,
                ltsapiversion,
                cancellationToken
            );
        }

        #endregion

        #region GETTER

        private Task<IActionResult> GetFiltered(
            string[] fields,
            string? language,
            uint pagenumber,
            int? pagesize,
            string? idfilter,
            List<string> idlist,
            string? locfilter,
            string? categoryfilter,
            string? typefilter,
            string? boardfilter,
            string? featurefilter,
            string? featureidfilter,
            string? themefilter,
            string? badgefilter,
            string? altitudefilter,
            bool? active,
            bool? smgactive,
            bool? bookablefilter,
            string? smgtagfilter,
            string? sourcefilter,
            string? publishedon,
            string? seed,
            string? updatefrom,
            string? langfilter,
            string? searchfilter,
            GeoPolygonSearchResult? polygonsearchresult,
            PGGeoSearchResult geosearchresult,
            string? rawfilter,
            string? rawsort,
            bool removenullvalues,
            bool getasidarray,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                //Hack Bookable Filter
                bool? customisbookablefilter = bookablefilter;
                if (idfilter != null && idfilter.Count() > 0)
                    customisbookablefilter = null;

                AccommodationHelper myhelper = await AccommodationHelper.CreateAsync(
                    QueryFactory,
                    idfilter: idfilter,
                    locfilter: locfilter,
                    boardfilter: boardfilter,
                    categoryfilter: categoryfilter,
                    typefilter: typefilter,
                    featurefilter: featurefilter,
                    featureidfilter: featureidfilter,
                    badgefilter: badgefilter,
                    themefilter: themefilter,
                    altitudefilter: altitudefilter,
                    smgtags: smgtagfilter,
                    activefilter: active,
                    smgactivefilter: smgactive,
                    bookablefilter: customisbookablefilter,
                    sourcefilter: sourcefilter,
                    lastchange: updatefrom,
                    langfilter: langfilter,
                    publishedonfilter: publishedon,
                    cancellationToken
                );

                //Fix if idlist from availabilitysearch is added use this instead of idfilter
                if (idlist.Count > 0)
                    myhelper.idlist = idlist;

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x =>x.SelectRaw("data"))
                    .From("accommodations")
                    .AccommodationWhereExpression(
                        idlist: myhelper.idlist,
                        accotypelist: myhelper.accotypelist,
                        categorylist: myhelper.categorylist,
                        featurelist: myhelper.featurelist,
                        featureidlist: myhelper.featureidlist,
                        badgelist: myhelper.badgelist,
                        themelist: myhelper.themelist,
                        boardlist: myhelper.boardlist,
                        smgtaglist: myhelper.smgtaglist,
                        districtlist: myhelper.districtlist,
                        municipalitylist: myhelper.municipalitylist,
                        tourismvereinlist: myhelper.tourismvereinlist,
                        regionlist: myhelper.regionlist,
                        apartmentfilter: myhelper.apartment,
                        bookable: myhelper.bookable,
                        altitude: myhelper.altitude,
                        altitudemin: myhelper.altitudemin,
                        altitudemax: myhelper.altitudemax,
                        activefilter: myhelper.active,
                        smgactivefilter: myhelper.smgactive,
                        publishedonlist: myhelper.publishedonlist,
                        sourcelist: myhelper.sourcelist,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: myhelper.lastchange,
                        languagelist: myhelper.languagelist,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilter
                    )
                    .ApplyRawFilter(rawfilter)
                    .When(
                        polygonsearchresult != null,
                        x =>
                            x.WhereRaw(
                                PostgresSQLHelper.GetGeoWhereInPolygon_GeneratedColumns(
                                    polygonsearchresult.wktstring,
                                    polygonsearchresult.polygon,
                                    polygonsearchresult.srid,
                                    polygonsearchresult.operation,
                                    polygonsearchresult.reduceprecision
                                )
                            )
                    )
                    .ApplyOrdering_GeneratedColumns(ref seed, geosearchresult, rawsort);

                //IF getasidarray set simply return array of ids
                if(getasidarray)
                {
                    return await query.GetAsync<string>();
                }

                // Get paginated data
                var data = await query.PaginateAsync<JsonRaw>(
                    page: (int)pagenumber,
                    perPage: pagesize ?? 25
                );

                var dataTransformed = data.List.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );

                uint totalpages = (uint)data.TotalPages;
                uint totalcount = (uint)data.Count;

                var availableonline =
                    Request.HttpContext.Items["mssavailablity"] != null
                        ? ((MssResult?)Request.HttpContext.Items["mssavailablity"])!
                            .MssResponseShort
                            .Count
                        : 0;
                var availableonrequest =
                    Request.HttpContext.Items["lcsavailablity"] != null
                        ? ((MssResult?)Request.HttpContext.Items["lcsavailablity"])!
                            .MssResponseShort
                            .Count
                        : 0;
                var accobooklist = Request.HttpContext.Items["accobooklist"];
                var accosrequested = accobooklist != null ? ((List<string>)accobooklist).Count : 0;
                var resultid =
                    ((MssResult?)Request.HttpContext.Items["mssavailablity"])?.ResultId ?? "";

                if (availableonline > 0 || availableonrequest > 0)
                {
                    return ResponseHelpers.GetResult(
                        pagenumber,
                        totalpages,
                        (uint)accosrequested,
                        accosrequested,
                        availableonline,
                        availableonrequest,
                        resultid,
                        seed,
                        dataTransformed,
                        Url
                    );
                }
                else
                {
                    return ResponseHelpers.GetResult(
                        pagenumber,
                        totalpages,
                        totalcount,
                        seed,
                        dataTransformed,
                        Url
                    );
                }
            });
        }

        private Task<IActionResult> GetSingle(
            string id,
            string? language,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query("accommodations")
                    .Select("data")
                    .Where("id", id.ToUpper())
                    .FilterDataByAccessRoles(UserRolesToFilter)
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    );

                var data = await query.FirstOrDefaultAsync<JsonRaw?>(
                    cancellationToken: cancellationToken
                );

                return data?.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );
            });
        }

        private Task<IActionResult> GetSingleByHgvId(
            string id,
            string? language,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query("accommodations")
                    .Select("data")
                    .Where("gen_hgvid", "ILIKE", id)
                    .FilterDataByAccessRoles(UserRolesToFilter)
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    );

                var data = await query.FirstOrDefaultAsync<JsonRaw?>(
                    cancellationToken: cancellationToken
                );

                return data?.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );
            });
        }

        private Task<IActionResult> GetSingleBya0r_id(
           string id,
           string? language,
           string[] fields,
           bool removenullvalues,
           CancellationToken cancellationToken
       )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query("accommodations")
                    .Select("data")
                    .Where("gen_a0r_id", "ILIKE", id)
                    .FilterDataByAccessRoles(UserRolesToFilter)
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    );

                var data = await query.FirstOrDefaultAsync<JsonRaw?>(
                    cancellationToken: cancellationToken
                );

                return data?.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );
            });
        }

        private async Task<string> GetAccoIdByHgvId(string id, CancellationToken cancellationToken)
        {
            var query = QueryFactory
                .Query("accommodations")
                .Select("id")
                .Where("gen_hgvid", "ILIKE", id)
                //.Anonymous_Logged_UserRule_GeneratedColumn(FilterClosedData, !ReducedData);
                .FilterDataByAccessRoles(UserRolesToFilter);

            var data = await query.FirstOrDefaultAsync<string?>();

            return data ?? "";
        }

        private async Task<string> GetAccoIdBya0r_id(string id, CancellationToken cancellationToken)
        {
            var query = QueryFactory
                .Query("accommodations")
                .Select("id")
                .Where("gen_a0r_id", "ILIKE", id)
                //.Anonymous_Logged_UserRule_GeneratedColumn(FilterClosedData, !ReducedData);
                .FilterDataByAccessRoles(UserRolesToFilter);

            var data = await query.FirstOrDefaultAsync<string?>();

            return data ?? "";
        }


        private Task<IActionResult> GetAccommodationRooms(
            string id,
            string[] fields,
            string? language,
            bool all,
            string? sourcefilter,
            string? updatefrom,
            string? langfilter,
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

                var languagelist = Helper.CommonListCreator.CreateIdList(langfilter);
                var sourcelist = Helper.CommonListCreator.CreateIdList(sourcefilter);
                var userroles = UserRolesToFilter;

                var query = QueryFactory
                    .Query("accommodationrooms")
                    .Select("data")
                    .Where("gen_a0rid", "ILIKE", id)
                    .When(
                        languagelist.Count > 0,
                        q => q.HasLanguageFilterAnd_GeneratedColumn(languagelist)
                    )
                    .When(sourcelist.Count > 0, q => q.SourceFilter_GeneratedColumn(sourcelist))
                    .When(
                        !String.IsNullOrEmpty(updatefrom),
                        q => q.LastChangedFilter_GeneratedColumn(updatefrom)
                    )
                    .SearchFilter(
                        PostgresSQLWhereBuilder.AccoRoomNameFieldsToSearchFor(language),
                        searchfilter
                    )
                    .ApplyRawFilter(rawfilter)
                    .OrderOnlyByRawSortIfNotNull(rawsort)
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FilterDataByAccessRoles(userroles)
                    .FilterReducedDataByRoles(userroles);

                var data = await query.GetAsync<JsonRaw?>(cancellationToken: cancellationToken);

                return data.Select(raw =>
                    raw?.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );
            });
        }

        /// <summary>
        /// GET Single Accommodation Room
        /// </summary>
        /// <param name="id">ID of Accommodation Room</param>
        /// <returns>Poi Object</returns>
        private Task<IActionResult> GetSingleAccommodationRoom(
            string id,
            string? language,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query("accommodationrooms")
                    .Select("data")
                    .Where("id", id.ToUpper())
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FilterDataByAccessRoles(UserRolesToFilter);

                var data = await query.FirstOrDefaultAsync<JsonRaw?>(
                    cancellationToken: cancellationToken
                );

                return data?.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );
            });
        }

        #endregion

        #region CUSTOM AVAILABILITY GETTER



        #endregion

        #region CATEGORIES

        private Task<IActionResult> GetAccoTypeList(
            string? language,
            string? typefilter,
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
                var query = QueryFactory
                    .Query("accommodationtypes")
                    .SelectRaw("data")
                    .When(
                        !String.IsNullOrEmpty(typefilter),
                        q => q.WhereRaw("data->>'Type' ILIKE $$", typefilter)
                    )
                    .SearchFilter(
                        PostgresSQLWhereBuilder.TypeDescFieldsToSearchFor(language),
                        searchfilter
                    )
                    .ApplyRawFilter(rawfilter)
                    .OrderOnlyByRawSortIfNotNull(rawsort);

                var data = await query.GetAsync<JsonRaw?>();

                return data.Select(raw =>
                    raw?.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );
            });
        }

        private Task<IActionResult> GetAccoTypeSingle(
            string id,
            string? language,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                var query = QueryFactory
                    .Query("accommodationtypes")
                    .Select("data")
                    .Where("id", id.ToLower());

                var data = await query.FirstOrDefaultAsync<JsonRaw?>();

                return data?.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );
            });
        }

        private Task<IActionResult> GetAccoFeatureList(
            string? language,
            string? ltst0filter,
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
                var query = QueryFactory
                    .Query("accommodationfeatures")
                    .SelectRaw("data")
                    .When(
                        !String.IsNullOrEmpty(ltst0filter),
                        q => q.WhereJsonb("CustomId", "like", ltst0filter + "%")
                    )
                    .SearchFilter(
                        PostgresSQLWhereBuilder.TypeDescFieldsToSearchFor(language),
                        searchfilter
                    )
                    .ApplyRawFilter(rawfilter)
                    .OrderOnlyByRawSortIfNotNull(rawsort);

                var data = await query.GetAsync<JsonRaw?>();

                return data.Select(raw =>
                    raw?.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );
            });
        }

        private Task<IActionResult> GetAccoFeatureSingle(
            string id,
            string? language,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                var query = QueryFactory
                    .Query("accommodationfeatures")
                    .Select("data")
                    //.WhereJsonb("Key", "ilike", id)
                    .Where("id", id.ToUpper());

                var data = await query.FirstOrDefaultAsync<JsonRaw?>();

                return data?.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                );
            });
        }

        private IActionResult GetFeatureListXML(CancellationToken cancellationToken)
        {
            XDocument mytins = GetAccommodationDataCDB.GetTinfromCDB(
                "1",
                settings.CDBConfig.Username,
                settings.CDBConfig.Password,
                settings.CDBConfig.ServiceUrl
            );

            //return new ContentResult { Content = mytins.ToString(), ContentType = "text/xml", StatusCode = 200 };

            return Ok(mytins);
        }

        #endregion

        #region POST PUT DELETE

        /// <summary>
        /// POST Insert new Accommodation
        /// </summary>
        /// <param name="accommodation">Accommodation Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(typeof(AccommodationController), nameof(GetAccommodations))]
        //[Authorize(Roles = "DataWriter,DataCreate,AccoManager,AccoCreate,AccommodationWriter,AccommodationManager,AccommodationCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost, Route("Accommodation")]
        public Task<IActionResult> Post([FromBody] AccommodationV2 accommodation)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAdd.TryGetValue("Create", out var additionalfilter);

                accommodation.Id = Helper.IdGenerator.GenerateIDFromType(accommodation);

                //GENERATE HasLanguage
                accommodation.CheckMyInsertedLanguages(
                    new List<string> { "de", "en", "it", "nl", "cs", "pl", "ru", "fr" }
                );
                //POPULATE LocationInfo
                accommodation.LocationInfo = await accommodation.UpdateLocationInfoExtension(
                    QueryFactory
                );
                //TODO DISTANCE Calculation
                //TRIM all strings
                accommodation.TrimStringProperties();

                return await UpsertData<AccommodationV2>(
                    accommodation,
                    new DataInfo("accommodations", CRUDOperation.Create),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing Accommodation
        /// </summary>
        /// <param name="id">Accommodation Id</param>
        /// <param name="accommodation">Accommodation Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(typeof(AccommodationController), nameof(GetAccommodations))]
        [AuthorizeODH(PermissionAction.Update)]
        //[Authorize(Roles = "DataWriter,DataModify,AccoManager,AccoModify,AccommodationWriter,AccommodationManager,AccommodationModify,AccommodationUpdate")]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut, Route("Accommodation/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] AccommodationV2 accommodation)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAdd.TryGetValue("Update", out var additionalfilter);

                accommodation.Id = Helper.IdGenerator.CheckIdFromType<AccommodationV2>(id);

                //GENERATE HasLanguage
                accommodation.CheckMyInsertedLanguages(
                    new List<string> { "de", "en", "it", "nl", "cs", "pl", "ru", "fr" }
                );
                //POPULATE LocationInfo
                accommodation.LocationInfo = await accommodation.UpdateLocationInfoExtension(
                    QueryFactory
                );
                //TODO DISTANCE Calculation
                //TRIM all strings
                accommodation.TrimStringProperties();

                return await UpsertData<AccommodationV2>(
                    accommodation,
                    new DataInfo("accommodations", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE Accommodation by Id
        /// </summary>
        /// <param name="id">Accommodation Id</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(typeof(AccommodationController), nameof(GetAccommodations))]
        [AuthorizeODH(PermissionAction.Delete)]
        //[Authorize(Roles = "DataWriter,DataDelete,AccoManager,AccoDelete,AccommodationWriter,AccommodationManager,AccommodationDelete")]
        [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete, Route("Accommodation/{id}")]
        public Task<IActionResult> Delete(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<AccommodationV2>(id);

                return await DeleteData<AccommodationV2>(
                    id,
                    new DataInfo("accommodations", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion

        #region AVAILABILITYHELPER

        private void RemoveDuplicatesFrom(
            string removeduplicateresultsfrom,
            MssResult? mssresult,
            MssResult? lcsresult
        )
        {
            if (mssresult != null && lcsresult != null)
            {
                if (removeduplicateresultsfrom == "lts")
                {
                    lcsresult.MssResponseShort = lcsresult
                        .MssResponseShort.Where(x =>
                            !mssresult
                                .MssResponseShort.Select(x => x.A0RID)
                                .ToList()
                                .Contains(x.A0RID)
                        )
                        .ToList();
                }
                else if (removeduplicateresultsfrom == "hgv")
                {
                    mssresult.MssResponseShort = mssresult
                        .MssResponseShort.Where(x =>
                            !lcsresult
                                .MssResponseShort.Select(x => x.A0RID)
                                .ToList()
                                .Contains(x.A0RID)
                        )
                        .ToList();
                }
            }
        }

        #endregion
    }
}
