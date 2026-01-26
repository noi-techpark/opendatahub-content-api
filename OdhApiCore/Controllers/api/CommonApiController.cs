// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Helper.Identity;
using Helper.Tagging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Responses;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers.api
{
    public class CommonController : OdhController
    {
        public CommonController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<CommonController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        //Standard GETTER

        /// <summary>
        /// GET MetaRegion List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of MetaRegion Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<MetaRegionLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles = "DataReader,CommonReader")]
        [HttpGet, Route("MetaRegion")]
        public async Task<IActionResult> GetMetaRegions(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,            
            string? odhtagfilter = null,
            string? tagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? source = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                languagefilter: langfilter,
                null,
                tagfilter,
                source,
                active?.Value,
                odhactive?.Value,
                odhtagfilter,                
                lastchange: updatefrom,
                publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await CommonGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,
                    tablename: "metaregions",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "MetaRegion",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await CommonGetListHelper(
                    tablename: "metaregions",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "MetaRegion",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET MetaRegion Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>MetaRegion Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(MetaRegionLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("MetaRegion/{id}", Name = "SingleMetaRegion")]
        public async Task<IActionResult> GetMetaRegionSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "metaregions",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "MetaRegion",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Experiencearea List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="visibleinsearch">Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of ExperienceArea Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<ExperienceAreaLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("ExperienceArea")]
        public async Task<IActionResult> GetExperienceAreas(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? odhtagfilter = null,
            string? tagfilter = null, 
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? source = null,
            bool? visibleinsearch = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                languagefilter: langfilter,                
                visibleinsearch,
                tagfilter,
                source,
                activefilter: active?.Value,
                smgactivefilter: odhactive?.Value,
                smgtags: odhtagfilter,
                lastchange: updatefrom,
                publishedonfilter: publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await CommonGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,
                    tablename: "experienceareas",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "ExperienceArea",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await CommonGetListHelper(
                    tablename: "experienceareas",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "ExperienceArea",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET ExperienceArea Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>ExperienceArea Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(ExperienceAreaLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("ExperienceArea/{id}", Name = "SingleExperienceArea")]
        public async Task<IActionResult> GetExperienceAreaSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "experienceareas",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "ExperienceArea",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Region List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of Region Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<RegionLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Region")]
        public async Task<IActionResult> GetRegions(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? odhtagfilter = null,
            string? tagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? source = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                languagefilter: langfilter,
                null,
                tagfilter,
                source,
                activefilter: active?.Value,
                smgactivefilter: odhactive?.Value,
                smgtags: odhtagfilter,
                lastchange: updatefrom,
                publishedonfilter: publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await CommonGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,
                    tablename: "regions",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "Region",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await CommonGetListHelper(
                    tablename: "regions",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "Region",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET Region Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Region Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(RegionLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Region/{id}", Name = "SingleRegion")]
        public async Task<IActionResult> GetRegionSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "regions",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "Region",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET TourismAssociation List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of Tourismverein Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<TourismvereinLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("TourismAssociation")]
        public async Task<IActionResult> GetTourismverein(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? odhtagfilter = null,
            string? tagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? source = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                languagefilter: langfilter,
                null,
                tagfilter,
                source,
                activefilter: active?.Value,
                smgactivefilter: odhactive?.Value,
                smgtags: odhtagfilter,
                lastchange: updatefrom,
                publishedonfilter: publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await CommonGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,
                    tablename: "tvs",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "TourismAssociation",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await CommonGetListHelper(
                    tablename: "tvs",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "TourismAssociation",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET TourismAssociation Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Tourismverein Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(TourismvereinLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("TourismAssociation/{id}", Name = "SingleTourismAssociation")]
        public async Task<IActionResult> GetTourismvereinSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "tvs",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "TourismAssociation",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Municipality List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="visibleinsearch">Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of Municipality Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<MunicipalityLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Municipality")]
        public async Task<IActionResult> GetMunicipality(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            bool? visibleinsearch = null,
            string? idlist = null,
            string? odhtagfilter = null,
            string? tagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? source = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                languagefilter: langfilter,
                visibleinsearch,
                tagfilter,
                source,
                active?.Value,
                odhactive?.Value,
                smgtags: odhtagfilter,
                lastchange: updatefrom,
                publishedonfilter: publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await CommonGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,
                    tablename: "municipalities",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "Municipality",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await CommonGetListHelper(
                    tablename: "municipalities",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "Municipality",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET Municipality Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Municipality Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(MunicipalityLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Municipality/{id}", Name = "SingleMunicipality")]
        public async Task<IActionResult> GetMunicipalitySingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "municipalities",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "Municipality",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET District List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="visibleinsearch">Filter only Elements flagged with visibleinsearch: (possible values: 'true','false'), (default:'false')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of District Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<DistrictLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("District")]
        public async Task<IActionResult> GetDistrict(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? odhtagfilter = null,
            string? tagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? source = null,
            bool? visibleinsearch = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                languagefilter: langfilter,
                visibleinsearch,
                tagfilter,
                source,
                active?.Value,
                odhactive?.Value,
                smgtags: odhtagfilter,
                lastchange: updatefrom,
                publishedonfilter: publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await CommonGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,
                    tablename: "districts",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "District",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await CommonGetListHelper(
                    tablename: "districts",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "District",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET District Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>District Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(DistrictLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("District/{id}", Name = "SingleDistrict")]
        public async Task<IActionResult> GetDistrictSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "districts",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "District",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET Area List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of Area Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<AreaLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Area")]
        public async Task<IActionResult> GetAreas(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? odhtagfilter = null,
            string? tagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? source = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                languagefilter: langfilter,
                null,
                tagfilter,
                source,
                active?.Value,
                odhactive?.Value,
                smgtags: odhtagfilter,
                lastchange: updatefrom,
                publishedonfilter: publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await CommonGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,
                    tablename: "areas",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: null,
                    geosearchresult: new PGGeoSearchResult(),
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "Area",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await CommonGetListHelper(
                    tablename: "areas",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: null,
                    geosearchresult: new PGGeoSearchResult(),
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "Area",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET Area Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Area Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(AreaLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Area/{id}", Name = "SingleArea")]
        public async Task<IActionResult> GetAreaSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "areas",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "Area",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET SkiRegion List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of SkiRegion Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<SkiRegionLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("SkiRegion")]
        public async Task<IActionResult> GetSkiRegion(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? odhtagfilter = null,
            string? tagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? source = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            string? searchfilter = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                languagefilter: langfilter,
                null,
                tagfilter,
                source,
                active?.Value,
                odhactive?.Value,
                smgtags: odhtagfilter,
                lastchange: updatefrom,
                publishedonfilter: publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await CommonGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,
                    tablename: "skiregions",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "SkiRegion",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await CommonGetListHelper(
                    tablename: "skiregions",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "SkiRegion",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET SkiRegion Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>SkiRegion Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(SkiRegionLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("SkiRegion/{id}", Name = "SingleSkiRegion")]
        public async Task<IActionResult> GetSkiRegionSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "skiregions",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "SkiRegion",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        /// <summary>
        /// GET SkiArea List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="polygon">valid WKT (Well-known text representation of geometry) Format, Examples (POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))) / By Using the GeoShapes Api (v1/GeoShapes) and passing Country.Type.Id OR Country.Type.Name Example (it.municipality.3066) / Bounding Box Filter bbc: 'Bounding Box Contains', 'bbi': 'Bounding Box Intersects', followed by a List of Comma Separated Longitude Latitude Tuples, 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#polygon-filter-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of SkiArea Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<SkiAreaLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("SkiArea")]
        public async Task<IActionResult> GetSkiArea(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? odhtagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? tagfilter = null,
            string? source = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? searchfilter = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            string? polygon = null,
            string? rawfilter = null,
            string? rawsort = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            var geosearchresult = Helper.GeoSearchHelper.GetPGGeoSearchResult(
                latitude,
                longitude,
                radius
            );
            var polygonsearchresult = await Helper.GeoSearchHelper.GetPolygon(
                polygon,
                QueryFactory
            );

            CommonHelper commonhelper = await CommonHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                languagefilter: langfilter,
                null,
                tagfilter,
                source,
                active?.Value,
                odhactive?.Value,
                smgtags: odhtagfilter,
                lastchange: updatefrom,
                publishedonfilter: publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await CommonGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,
                    tablename: "skiareas",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "SkiArea",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await CommonGetListHelper(
                    tablename: "skiareas",
                    seed: seed,
                    publishedon: publishedon,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    polygonsearchresult: polygonsearchresult,
                    geosearchresult: geosearchresult,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "SkiArea",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET SkiArea Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>SkiArea Object</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(SkiAreaLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("SkiArea/{id}", Name = "SingleSkiArea")]
        public async Task<IActionResult> GetSkiAreaSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "skiareas",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "SkiArea",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        ////Special GETTER

        /// <summary>
        /// GET Wine Awards List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of data IDs), (default:'null')</param>
        /// <param name="odhtagfilter">Taglist Filter (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=common'), (default:'null')</param>
        /// <param name="active">Active data Filter (possible Values: 'true' only Active data, 'false' only Disabled data), (default:'null')</param>
        /// <param name="odhactive">Odhactive (Published) data Filter (possible Values: 'true' only published data, 'false' only not published data, (default:'null')</param>
        /// <param name="companyid">Filter by Company Id, (default:'null')</param>
        /// <param name="wineid">Filter by Wine Id, (default:'null')</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<WineLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("WineAward")]
        public async Task<IActionResult> GetWineAwardsList(
            uint? pagenumber = null,
            PageSize pagesize = null!,
            string? idlist = null,
            string? odhtagfilter = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? source = null,
            string? wineid = null,
            string? companyid = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            string? langfilter = null,
            string? updatefrom = null,
            string? seed = null,
            string? rawfilter = null,
            string? rawsort = null,
            string? publishedon = null,
            string? searchfilter = null,
            bool removenullvalues = false,
            bool getasidarray = false,
            CancellationToken cancellationToken = default
        )
        {
            WineHelper commonhelper = await WineHelper.CreateAsync(
                QueryFactory,
                idfilter: idlist,
                companyid,
                wineid,
                languagefilter: langfilter,
                null,
                source,                
                active?.Value,
                odhactive?.Value,
                smgtags: odhtagfilter,
                lastchange: updatefrom,
                publishedon,
                cancellationToken
            );

            if (pagenumber.HasValue)
            {
                return await WineGetPagedListHelper(
                    pagenumber.Value,
                    pagesize,                    
                    tablename: "wines",
                    idlist: idlist,
                    seed: seed,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "WineAward",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
            else
            {
                return await WineGetListHelper(
                    tablename: "wines",
                    idlist: idlist,
                    seed: seed,
                    searchfilter: searchfilter,
                    fields: fields ?? Array.Empty<string>(),
                    language: language,
                    commonhelper,
                    rawfilter: rawfilter,
                    rawsort: rawsort,
                    endpoint: "WineAward",
                    removenullvalues: removenullvalues,
                    getasidarray: getasidarray,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// GET Wine Award Single
        /// </summary>
        /// <param name="id">ID of the requested data</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Wine Object</returns>
        [ProducesResponseType(typeof(WineLinked), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("WineAward/{id}", Name = "SingleWineAward")]
        public async Task<IActionResult> GetWineAwardsSingle(
            string id,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            string? language = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await CommonGetSingleHelper(
                id: id,
                tablename: "wines",
                fields: fields ?? Array.Empty<string>(),
                language: language,
                endpoint: "WineAward",
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        #endregion

        #region GETTER

        private Task<IActionResult> CommonGetListHelper(
            string tablename,
            string? seed,
            string? publishedon,
            string? searchfilter,
            string[] fields,
            string? language,
            CommonHelper commonhelper,
            GeoPolygonSearchResult? polygonsearchresult,
            PGGeoSearchResult geosearchresult,
            string? rawfilter,
            string? rawsort,
            string endpoint,
            bool removenullvalues,
            bool getasidarray,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAddEndpoint(endpoint)
                    .TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From(tablename)
                    .CommonWhereExpression(
                        idlist: commonhelper.idlist,
                        languagelist: commonhelper.languagelist,
                        visibleinsearch: commonhelper.visibleinsearch,
                        commonhelper.smgtaglist,
                        activefilter: commonhelper.active,
                        odhactivefilter: commonhelper.smgactive,
                        tagdict: commonhelper.tagdict,
                        publishedonlist: commonhelper.publishedonlist,
                        sourcelist: commonhelper.sourcelist,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: commonhelper.lastchange,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilterEndpoint(endpoint)
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
                    .ApplyOrdering_GeneratedColumns(ref seed, geosearchresult, rawsort)
                    .FilterDataByAccessRoles(UserRolesToFilterEndpoint(endpoint)); //.ApplyOrdering(ref seed, new PGGeoSearchResult() { geosearch = false }, rawsort);

                //IF getasidarray set simply return array of ids
                if (getasidarray)
                {
                    return await query.GetAsync<string>();
                }

                // Get paginated data
                var data = await query.GetAsync<JsonRaw>();

                return data.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );
            });
        }

        private Task<IActionResult> CommonGetPagedListHelper(
            uint pagenumber,
            int? pagesize,
            string tablename,
            string? seed,
            string? publishedon,
            string? searchfilter,
            string[] fields,
            string? language,
            CommonHelper commonhelper,
            GeoPolygonSearchResult? polygonsearchresult,
            PGGeoSearchResult geosearchresult,
            string? rawfilter,
            string? rawsort,
            string endpoint,
            bool removenullvalues,
            bool getasidarray,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAddEndpoint(endpoint)
                    .TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From(tablename)
                    .CommonWhereExpression(
                        idlist: commonhelper.idlist,
                        languagelist: commonhelper.languagelist,
                        visibleinsearch: commonhelper.visibleinsearch,
                        commonhelper.smgtaglist,                        
                        activefilter: commonhelper.active,
                        odhactivefilter: commonhelper.smgactive,
                        commonhelper.tagdict,
                        publishedonlist: commonhelper.publishedonlist,
                        sourcelist: commonhelper.sourcelist,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: commonhelper.lastchange,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilterEndpoint(endpoint)
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
                    .ApplyOrdering_GeneratedColumns(ref seed, geosearchresult, rawsort)
                    .FilterDataByAccessRoles(UserRolesToFilterEndpoint(endpoint));

                //IF getasidarray set simply return array of ids
                if (getasidarray)
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

                return ResponseHelpers.GetResult(
                    pagenumber,
                    totalpages,
                    totalcount,
                    seed,
                    dataTransformed,
                    Url
                );
            });
        }

        private Task<IActionResult> WineGetListHelper(
            string tablename,
            string? idlist, 
            string? seed,
            string? searchfilter,
            string[] fields,
            string? language,
            WineHelper winehelper,
            string? rawfilter,
            string? rawsort,
            string endpoint,
            bool removenullvalues,
            bool getasidarray,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAddEndpoint(endpoint)
                    .TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From(tablename)
                    .WineWhereExpression(
                        idlist: winehelper.idlist,
                        languagelist: new List<string>(),
                        lastchange: winehelper.lastchange,
                        wineid: winehelper.wineidlist,
                        companyid: winehelper.companyidlist,
                        activefilter: winehelper.active,
                        odhactivefilter: winehelper.smgactive,
                        sourcelist: winehelper.sourcelist,
                        publishedonlist: winehelper.publishedonlist,
                        searchfilter: searchfilter,
                        language: language,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilterEndpoint(endpoint)
                    )
                    .ApplyRawFilter(rawfilter)
                    .ApplyOrdering(ref seed, new PGGeoSearchResult() { geosearch = false }, rawsort)
                    .FilterDataByAccessRoles(UserRolesToFilterEndpoint(endpoint));

                //IF getasidarray set simply return array of ids
                if (getasidarray)
                {
                    return await query.GetAsync<string>();
                }

                // Get paginated data
                var data = await query.GetAsync<JsonRaw>();

                return data.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );
            });
        }

        private Task<IActionResult> WineGetPagedListHelper(
            uint pagenumber,
            int? pagesize,
            string tablename,
            string? idlist,
            string? seed,
            string? searchfilter,
            string[] fields,
            string? language,
            WineHelper winehelper,
            string? rawfilter,
            string? rawsort,
            string endpoint,
            bool removenullvalues,
            bool getasidarray,
            CancellationToken cancellationToken
        )
        {
            //Additional Read Filters to Add Check
            AdditionalFiltersToAddEndpoint(endpoint).TryGetValue("Read", out var additionalfilter);

            return DoAsyncReturn(async () =>
            {
                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From(tablename)
                    .WineWhereExpression(
                        idlist: winehelper.idlist,
                        languagelist: new List<string>(),
                        lastchange: winehelper.lastchange,
                        wineid: winehelper.wineidlist,
                        companyid: winehelper.companyidlist,
                        activefilter: winehelper.active,
                        odhactivefilter: winehelper.smgactive,
                        sourcelist: winehelper.sourcelist,
                        publishedonlist: winehelper.publishedonlist,
                        searchfilter: searchfilter,
                        language: language,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilterEndpoint(endpoint)
                    )
                    .ApplyRawFilter(rawfilter)
                    .ApplyOrdering(ref seed, new PGGeoSearchResult() { geosearch = false }, rawsort)
                    .FilterDataByAccessRoles(UserRolesToFilterEndpoint(endpoint));

                //IF getasidarray set simply return array of ids
                if (getasidarray)
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

                return ResponseHelpers.GetResult(
                    pagenumber,
                    totalpages,
                    totalcount,
                    seed,
                    dataTransformed,
                    Url
                );
            });
        }

        private Task<IActionResult> CommonGetSingleHelper(
            string id,
            string tablename,
            string[] fields,
            string? language,
            string endpoint,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAddEndpoint(endpoint)
                    .TryGetValue("Read", out var additionalfilter);

                string idtocheck = id.ToUpper();
                if (endpoint == "WineAward")
                    idtocheck = id.ToLower();

                var query = QueryFactory
                    .Query(tablename)
                    .Select("data")
                    .Where("id", idtocheck)
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FilterDataByAccessRoles(UserRolesToFilterEndpoint(endpoint));

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

        #endregion

        #region POST PUT DELETE

        /// <summary>
        /// POST Insert new MetaRegion
        /// </summary>
        /// <param name="data">MetaRegion Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,MetaRegionCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("MetaRegion")]
        public Task<IActionResult> Post([FromBody] MetaRegionLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("MetaRegion")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);



                return await UpsertData<MetaRegionLinked>(
                    data,
                    new DataInfo("metaregions", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// POST Insert new Region
        /// </summary>
        /// <param name="data">Region Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,RegionCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("Region")]
        public Task<IActionResult> Post([FromBody] RegionLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("Region")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<RegionLinked>(
                    data,
                    new DataInfo("regions", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// POST Insert new ExperienceArea
        /// </summary>
        /// <param name="data">ExperienceArea Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,ExperienceAreaCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("ExperienceArea")]
        public Task<IActionResult> Post([FromBody] ExperienceAreaLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("ExperienceArea")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<ExperienceAreaLinked>(
                    data,
                    new DataInfo("experienceareas", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// POST Insert new TourismAssociation
        /// </summary>
        /// <param name="data">TourismAssociation Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,TourismAssociationCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("TourismAssociation")]
        public Task<IActionResult> Post([FromBody] TourismvereinLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("TourismAssociation")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<TourismvereinLinked>(
                    data,
                    new DataInfo("tvs", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// POST Insert new Municipality
        /// </summary>
        /// <param name="data">Municipality Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,MunicipalityCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("Municipality")]
        public Task<IActionResult> Post([FromBody] MunicipalityLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("Municipality")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<MunicipalityLinked>(
                    data,
                    new DataInfo("municipalities", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// POST Insert new District
        /// </summary>
        /// <param name="data">District Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,DistrictCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("District")]
        public Task<IActionResult> Post([FromBody] DistrictLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("District")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<DistrictLinked>(
                    data,
                    new DataInfo("districts", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// POST Insert new Area
        /// </summary>
        /// <param name="data">Area Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,AreaCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("Area")]
        public Task<IActionResult> Post([FromBody] AreaLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("Area")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                //data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                //await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<AreaLinked>(
                    data,
                    new DataInfo("areas", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// POST Insert new SkiRegion
        /// </summary>
        /// <param name="data">SkiRegion Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,SkiRegionCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("SkiRegion")]
        public Task<IActionResult> Post([FromBody] SkiRegionLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("SkiRegion")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<SkiRegionLinked>(
                    data,
                    new DataInfo("skiregions", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// POST Insert new SkiArea
        /// </summary>
        /// <param name="data">SkiArea Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,SkiAreaCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("SkiArea")]
        public Task<IActionResult> Post([FromBody] SkiAreaLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("SkiArea")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<SkiAreaLinked>(
                    data,
                    new DataInfo("skiareas", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// POST Insert new Wine
        /// </summary>
        /// <param name="data">Wine Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataCreate,CommonManager,CommonCreate,WineAwardCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [HttpPost, Route("WineAward")]
        public Task<IActionResult> Post([FromBody] WineLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAddEndpoint("WineAward")
                    .TryGetValue("Create", out var additionalfilter);

                data.Id = Helper.IdGenerator.GenerateIDFromType(data);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                //await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<WineLinked>(
                    data,
                    new DataInfo("wines", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing MetaRegion
        /// </summary>
        /// <param name="id">MetaRegion Id</param>
        /// <param name="data">MetaRegion Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,MetaRegionUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("MetaRegion/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] MetaRegionLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("MetaRegion")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<MetaRegionLinked>(id);
                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<MetaRegionLinked>(
                    data,
                    new DataInfo("metaregions", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing Region
        /// </summary>
        /// <param name="id">Region Id</param>
        /// <param name="data">Region Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,RegionUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("Region/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] RegionLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("Region")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<RegionLinked>(id);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<RegionLinked>(
                    data,
                    new DataInfo("regions", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing ExperienceArea
        /// </summary>
        /// <param name="id">ExperienceArea Id</param>
        /// <param name="data">ExperienceArea Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,ExperienceAreaUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("ExperienceArea/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] ExperienceAreaLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("ExperienceArea")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<ExperienceAreaLinked>(id);
                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<ExperienceAreaLinked>(
                    data,
                    new DataInfo("experienceareas", CRUDOperation.Update, true),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing TourismAssociation
        /// </summary>
        /// <param name="id">TourismAssociation Id</param>
        /// <param name="data">TourismAssociation Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,TourismAssociationUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("TourismAssociation/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] TourismvereinLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("TourismAssociation")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<TourismvereinLinked>(id);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<TourismvereinLinked>(
                    data,
                    new DataInfo("tvs", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing Municipality
        /// </summary>
        /// <param name="id">Municipality Id</param>
        /// <param name="data">Municipality Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,MunicipalityUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("Municipality/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] MunicipalityLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("Municipality")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<MunicipalityLinked>(id);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<MunicipalityLinked>(
                    data,
                    new DataInfo("municipalities", CRUDOperation.Update, true),
                    new CompareConfig(true, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing District
        /// </summary>
        /// <param name="id">District Id</param>
        /// <param name="data">District Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,DistrictUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("District/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] DistrictLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("District")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<DistrictLinked>(id);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<DistrictLinked>(
                    data,
                    new DataInfo("districts", CRUDOperation.Update, true),
                    new CompareConfig(true, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing Area
        /// </summary>
        /// <param name="id">Area Id</param>
        /// <param name="data">Area Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,AreaUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("Area/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] AreaLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("Area")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<AreaLinked>(id);

                //GENERATE HasLanguage
                //data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                //await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<AreaLinked>(
                    data,
                    new DataInfo("areas", CRUDOperation.Update, true),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing SkiRegion
        /// </summary>
        /// <param name="id">SkiRegion Id</param>
        /// <param name="data">SkiRegion Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,SkiRegionUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("SkiRegion/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] SkiRegionLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("SkiRegion")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<SkiRegionLinked>(id);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<SkiRegionLinked>(
                    data,
                    new DataInfo("skiregions", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing SkiArea
        /// </summary>
        /// <param name="id">SkiArea Id</param>
        /// <param name="data">SkiArea Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,SkiAreaUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("SkiArea/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] SkiAreaLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("SkiArea")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<SkiAreaLinked>(id);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<SkiAreaLinked>(
                    data,
                    new DataInfo("skiareas", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing WineAward
        /// </summary>
        /// <param name="id">WineAward Id</param>
        /// <param name="data">WineAward Object</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataModify,CommonManager,CommonModify,CommonUpdate,WineAwardUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [HttpPut, Route("WineAward/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] WineLinked data)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAddEndpoint("WineAward")
                    .TryGetValue("Update", out var additionalfilter);

                data.Id = Helper.IdGenerator.CheckIdFromType<WineLinked>(id);

                //GENERATE HasLanguage
                data.CheckMyInsertedLanguages(null);
                //TRIM all strings
                data.TrimStringProperties();
                //Populate Tags (Id/Source/Type)
                //await data.UpdateTagsExtension(QueryFactory);

                return await UpsertData<WineLinked>(
                    data,
                    new DataInfo("wines", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE MetaRegion by Id
        /// </summary>
        /// <param name="id">MetaRegion Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,MetaRegionDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("MetaRegion/{id}")]
        public Task<IActionResult> DeleteMetaRegion(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("MetaRegion")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<MetaRegionLinked>(id);
                return await DeleteData<MetaRegionLinked>(
                    id,
                    new DataInfo("metaregions", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE Region by Id
        /// </summary>
        /// <param name="id">Region Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,RegionDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("Region/{id}")]
        public Task<IActionResult> DeleteRegion(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("Region")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<RegionLinked>(id);
                return await DeleteData<RegionLinked>(
                    id,
                    new DataInfo("regions", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE ExperienceArea by Id
        /// </summary>
        /// <param name="id">ExperienceArea Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,ExperienceAreaDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("ExperienceArea/{id}")]
        public Task<IActionResult> DeleteExperienceArea(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("ExperienceArea")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<ExperienceAreaLinked>(id);
                return await DeleteData<ExperienceAreaLinked>(
                    id,
                    new DataInfo("experienceareas", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE TourismAssociation by Id
        /// </summary>
        /// <param name="id">TourismAssociation Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,TourismAssociationDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("TourismAssociation/{id}")]
        public Task<IActionResult> DeleteTourismAssociation(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("TourismAssociation")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<TourismvereinLinked>(id);
                return await DeleteData<TourismvereinLinked>(
                    id,
                    new DataInfo("tvs", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE Municipality by Id
        /// </summary>
        /// <param name="id">Municipality Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,MunicipalityDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("Municipality/{id}")]
        public Task<IActionResult> DeleteMunicipality(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("Municipality")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<MunicipalityLinked>(id);
                return await DeleteData<MunicipalityLinked>(
                    id,
                    new DataInfo("municipalities", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE District by Id
        /// </summary>
        /// <param name="id">District Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,DistrictDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("District/{id}")]
        public Task<IActionResult> DeleteDistrict(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("District")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<DistrictLinked>(id);
                return await DeleteData<DistrictLinked>(
                    id,
                    new DataInfo("districts", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE Area by Id
        /// </summary>
        /// <param name="id">Area Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,AreaDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("Area/{id}")]
        public Task<IActionResult> DeleteArea(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("Area")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<AreaLinked>(id);
                return await DeleteData<AreaLinked>(
                    id,
                    new DataInfo("areas", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE SkiRegion by Id
        /// </summary>
        /// <param name="id">SkiRegion Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,SkiRegionDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("SkiRegion/{id}")]
        public Task<IActionResult> DeleteSkiRegion(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("SkiRegion")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<SkiRegionLinked>(id);
                return await DeleteData<SkiRegionLinked>(
                    id,
                    new DataInfo("skiregions", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE SkiArea by Id
        /// </summary>
        /// <param name="id">SkiArea Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,SkiAreaDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("SkiArea/{id}")]
        public Task<IActionResult> DeleteSkiArea(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("SkiArea")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<SkiAreaLinked>(id);
                return await DeleteData<SkiAreaLinked>(
                    id,
                    new DataInfo("skiareas", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE WineAward by Id
        /// </summary>
        /// <param name="id">WineAward Id</param>
        /// <returns>Http Response</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "DataWriter,DataDelete,CommonManager,CommonDelete,WineAwardDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("WineAward/{id}")]
        public Task<IActionResult> DeleteWineAward(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAddEndpoint("WineAward")
                    .TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<WineLinked>(id);
                return await DeleteData<WineLinked>(
                    id,
                    new DataInfo("wines", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion
    }
}
