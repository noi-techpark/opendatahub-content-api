// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.CacheOutput;
using DataModel;
using Helper;
using Helper.Converters;
using Helper.Generic;
using Helper.Identity;
using Helper.Tagging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhApiCore.Filters;
using OdhApiCore.Responses;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// Venue Api (data provided by LTS Venues) SOME DATA Available as OPENDATA
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("v2")]
    public class VenueFlattenedController : OdhController
    {
        public VenueFlattenedController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<VenueController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Venue V2 List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page (max 1024), (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, not provided disables Random Sorting, (default:'null') </param>
        /// <param name="idlist">IDFilter (Separator ',' List of Venue IDs), (default:'null')</param>
        /// <param name="locfilter">Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a></param>
        /// <param name="categoryfilter">Venue Category Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:category), (default:'null')</param>
        /// <param name="featurefilter">Venue Features Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:feature), (default:'null')</param>
        /// <param name="setuptypefilter">Venue SetupType Filter (BITMASK) (Separator ',' List of Venuetype Bitmasks, refer to api/VenueTypes type:seatType), (default:'null')</param>
        /// <param name="source">Source Filter(String, ), (default:'null')</param>
        /// <param name="capacityfilter">Capacity Range Filter (Separator ',' example Value: 50,100 All Venues with rooms from 50 to 100 people), (default:'null')</param>
        /// <param name="roomcountfilter">Room Count Range Filter (Separator ',' example Value: 2,5 All Venues with 2 to 5 rooms), (default:'null')</param>
        /// <param name="odhtagfilter">ODH Taglist Filter (refers to Array SmgTags) (String, Separator ',' more Tags possible, available Tags reference to 'v1/ODHTag?validforentity=venue'), (default:'null')</param>
        /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="active">Active Venue Filter (possible Values: 'true' only Active Venues, 'false' only Disabled Venues), (default:'null')</param>
        /// <param name="odhactive">ODH Active (Published) Venue Filter (possible Values: 'true' only published Venue, 'false' only not published Venue), (default:'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null)<a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="destinationdataformat">If set to true, data will be returned in AlpineBits Destinationdata Format</param>
        /// <returns>Collection of VenueV2 Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<VenueFlattened>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        //[Authorize(Roles = "DataReader,VenueReader")]
        //[Authorize]
        [HttpGet, Route("Venue")]
        public async Task<IActionResult> Get(
            string? language = null,
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? categoryfilter = null,
            string? capacityfilter = null,
            string? roomcountfilter = null,
            string? idlist = null,
            string? locfilter = null,
            string? featurefilter = null,
            string? setuptypefilter = null,            
            string? tagfilter = null,
            string? source = null,
            LegacyBool active = null!,
            LegacyBool odhactive = null!,
            string? publishedon = null,
            string? updatefrom = null,
            string? langfilter = null,
            string? seed = null,
            string? latitude = null,
            string? longitude = null,
            string? radius = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
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

            return await GetFiltered(
                fields: fields ?? Array.Empty<string>(),
                language: language,
                pagenumber: pagenumber,
                pagesize: pagesize,
                idfilter: idlist,
                categoryfilter: categoryfilter,
                capacityfilter: capacityfilter,
                searchfilter: searchfilter,
                locfilter: locfilter,
                roomcountfilter: roomcountfilter,
                featurefilter: featurefilter,
                setuptypefilter: setuptypefilter,
                sourcefilter: source,
                active: active,
                smgactive: odhactive,
                tagfilter: tagfilter,
                seed: seed,
                lastchange: updatefrom,
                langfilter: langfilter,
                publishedon: publishedon,
                geosearchresult: geosearchresult,
                rawfilter: rawfilter,
                rawsort: rawsort,
                removenullvalues: removenullvalues,
                getasidarray: getasidarray,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// GET Venue Single
        /// </summary>
        /// <param name="id">ID of the Venue</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="destinationdataformat">If set to true, data will be returned in AlpineBits Destinationdata Format</param>
        /// <returns>VenueV2 Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        /// //[Authorize(Roles = "DataReader,VenueReader")]
        [ProducesResponseType(typeof(VenueFlattened), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Venue/{id}", Name = "SingleVenueV2")]
        public async Task<IActionResult> GetVenueV2Single(
            string id,
            string? language,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            bool destinationdataformat = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetSingle(
                id,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        #endregion

        #region Converters

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("Venue/ConvertVenueToVenueFlattened/{id}")]
        public async Task<IActionResult> ConvertVenueToVenueFlattened(string id, bool savetotable = false)
        {
            var query = QueryFactory
                .Query("venues_v2")
                .Select("data")
                .When(id != "all", x => x.Where("id", id.ToUpper()))
                .FilterDataByAccessRoles(UserRolesToFilterEndpoint("Venue"));

            var data = await query.GetObjectListAsync<VenueLinked>();

            var venuecodequery = QueryFactory.Query("venuetypes").Select("data");

            var venuecodedata = await venuecodequery.GetObjectListAsync<DDVenueCodes>();

            var convertresult = VenueFlattenedConverter.ConvertVenueListToVenueFlattened(data, venuecodedata);

            if (savetotable)
            {
                List<UpdateDetail> result = new List<UpdateDetail>();
                foreach (var venue in convertresult)
                {
                    result.Add(
                        await QueryFactory.UpsertData<VenueFlattened>(
                            venue,
                            new DataInfo("venuesv2", CRUDOperation.Create),
                            new EditInfo("venueconverter", "api"),
                            new CRUDConstraints(null, new List<string>()),
                            new CompareConfig(false, false)
                        )
                    );
                }

                return Ok(result);
            }
            else
            {
                return Ok(convertresult);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("Venue/ConvertVenueTypesToTags")]
        public async Task<IActionResult> ConvertVenueTypesToTags(bool savetotable = false)
        {
            var query = QueryFactory.Query("venuetypes").Select("data");

            var datalist = await query.GetObjectListAsync<DDVenueCodes>();

            var listtaglinked = new List<TagLinked>();

            List<UpdateDetail> result = new List<UpdateDetail>();

            foreach (var data in datalist)
            {
                var converted = VenueFlattenedConverter.ConvertVenueTagToTag(data);
                listtaglinked.Add(converted);

                if (savetotable)
                {
                    result.Add(
                        await QueryFactory.UpsertData<TagLinked>(
                            converted,
                            new DataInfo("tag", CRUDOperation.Create),
                            new EditInfo("converter", "api"),
                            new CRUDConstraints(null, new List<string>()),
                            new CompareConfig(false, false)
                        )
                    );
                }
            }

            if (savetotable)
            {
                return Ok(result);
            }

            return Ok(listtaglinked);
        }

        #endregion

        #region GETTER

        private Task<IActionResult> GetFiltered(
            string[] fields,
            string? language,
            uint pagenumber,
            int? pagesize,
            string? idfilter,
            string? categoryfilter,
            string? capacityfilter,
            string? searchfilter,
            string? locfilter,
            string? roomcountfilter,
            string? featurefilter,
            string? setuptypefilter,
            string? sourcefilter,
            bool? active,
            bool? smgactive,
            string? tagfilter,
            string? seed,
            string? lastchange,
            string? langfilter,
            string? publishedon,
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

                VenueHelper myvenuehelper = await VenueHelper.CreateAsync(
                    QueryFactory,
                    idfilter,
                    categoryfilter,
                    featurefilter,
                    setuptypefilter,
                    locfilter,
                    langfilter,
                    sourcefilter,
                    active,                    
                    tagfilter,
                    lastchange,
                    publishedon,
                    cancellationToken
                );

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From("venuesv2")
                    .VenueWhereExpression(
                        languagelist: myvenuehelper.languagelist,
                        idlist: myvenuehelper.idlist,
                        categorylist: myvenuehelper.categorylist,
                        featurelist: myvenuehelper.featurelist,
                        setuptypelist: myvenuehelper.setuptypelist,
                        tagdict: myvenuehelper.tagdict,
                        districtlist: myvenuehelper.districtlist,
                        municipalitylist: myvenuehelper.municipalitylist,
                        tourismvereinlist: myvenuehelper.tourismvereinlist,
                        regionlist: myvenuehelper.regionlist,
                        sourcelist: myvenuehelper.sourcelist,
                        activefilter: myvenuehelper.active,
                        publishedonlist: myvenuehelper.publishedonlist,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: myvenuehelper.lastchange,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilterEndpoint("Venue")
                    )
                    .ApplyRawFilter(rawfilter)
                    .ApplyOrdering_GeneratedColumns(ref seed, geosearchresult, rawsort); //.ApplyOrdering(ref seed, geosearchresult, rawsort);

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
                    .Query("venuesv2")
                    .Select("data")
                    .Where("id", id.ToUpper())
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FilterDataByAccessRoles(UserRolesToFilterEndpoint("Venue"));

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
        /// POST Insert new Venue
        /// </summary>
        /// <param name="poi">Venue Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(nameof(GetVenueList))]
        //[Authorize(Roles = "DataWriter,DataCreate,VenueManager,VenueCreate")]
        [AuthorizeODH(PermissionAction.Create)]
        [ProducesResponseType(typeof(UpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost, Route("Venue")]
        public Task<IActionResult> Post([FromBody] VenueFlattened venue)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Create", out var additionalfilter);

                venue.Id = Helper.IdGenerator.GenerateIDFromType(venue);

                //Check all Languages
                venue.CheckMyInsertedLanguages(null);
                //Trim all strings
                venue.TrimStringProperties();

                //Populate Tags (Id/Source/Type)
                await venue.UpdateTagsExtension(QueryFactory);

                //TODO UPDATE/INSERT ALSO in Destinationdata Column Or lets not support Destinationdata anymore?
                return await UpsertData<VenueFlattened>(
                    venue,
                    new DataInfo("venuesv2", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing Venue
        /// </summary>
        /// <param name="id">Venue Id</param>
        /// <param name="venue">Venue Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(nameof(GetVenueList))]
        //[Authorize(Roles = "DataWriter,DataModify,VenueManager,VenueModify,VenueUpdate")]
        [AuthorizeODH(PermissionAction.Update)]
        [ProducesResponseType(typeof(UpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut, Route("Venue/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] VenueFlattened venue)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Update", out var additionalfilter);

                venue.Id = Helper.IdGenerator.CheckIdFromType<VenueFlattened>(id);

                //Check all Languages
                venue.CheckMyInsertedLanguages(null);

                //Trim all strings
                venue.TrimStringProperties();

                //Populate Tags (Id/Source/Type)
                await venue.UpdateTagsExtension(QueryFactory);

                //TODO UPDATE/INSERT ALSO in Destinationdata Column
                return await UpsertData<VenueFlattened>(
                    venue,
                    new DataInfo("venuesv2", CRUDOperation.Update, true),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE Venue by Id
        /// </summary>
        /// <param name="id">Venue Id</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(nameof(GetVenueList))]
        //[Authorize(Roles = "DataWriter,DataDelete,VenueManager,VenueDelete")]
        [AuthorizeODH(PermissionAction.Delete)]
        [ProducesResponseType(typeof(UpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete, Route("Venue/{id}")]
        public Task<IActionResult> Delete(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Read Filters to Add Check
                AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<VenueFlattened>(id);

                return await DeleteData<VenueFlattened>(
                    id,
                    new DataInfo("venuesv2", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion
    }
}
