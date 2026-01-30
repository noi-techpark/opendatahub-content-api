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
using Schema.NET;
using ServiceReferenceLCS;
using SqlKata.Execution;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// Events V2 Api (data provided by LTS) SOME DATA Available as OPENDATA
    /// </summary>
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("v2")]
    public class EventFlattenedController : OdhController
    {
        public EventFlattenedController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<EventFlattenedController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        #region SWAGGER Exposed API

        /// <summary>
        /// GET Event List
        /// </summary>
        /// <param name="pagenumber">Pagenumber</param>
        /// <param name="pagesize">Elements per Page, (default:10)</param>
        /// <param name="seed">Seed '1 - 10' for Random Sorting, '0' generates a Random Seed, 'null' disables Random Sorting, (default:null)</param>
        /// <param name="idlist">IDFilter (Separator ',' List of Event IDs, 'null' = No Filter), (default:'null')</param>
        /// <param name="locfilter">Locfilter SPECIAL Separator ',' possible values: reg + REGIONID = (Filter by Region), reg + REGIONID = (Filter by Region), tvs + TOURISMVEREINID = (Filter by Tourismverein), mun + MUNICIPALITYID = (Filter by Municipality), fra + FRACTIONID = (Filter by Fraction), 'null' = (No Filter), (default:'null') <a href="https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#location-filter-locfilter" target="_blank">Wiki locfilter</a></param>
         /// <param name="tagfilter">Filter on Tags. (Endpoint on v1/Tag) Syntax =and/or(Tag.Id,Tag.Id,Tag.Id) example or(summer,hiking) - and(themed hikes,family hikings) - or(hiking) - and(summer) - Combining and/or is not supported at the moment, default: 'null')</param>
        /// <param name="begindate">BeginDate of Events (Format: yyyy-MM-dd), (default: 'null')</param>
        /// <param name="enddate">EndDate of Events (Format: yyyy-MM-dd), (default: 'null')</param>
        /// <param name="sort">Sorting Mode of Events ('asc': Ascending simple sort by next begindate, 'desc': simple descent sorting by next begindate, 'upcoming': Sort Events by next EventDate matching passed startdate, 'upcomingspecial': Sort Events by next EventDate matching passed startdate, multiple day events are showed at bottom, default: if no sort mode passed, sort by shortname )</param>
        /// <param name="active">Active Events Filter (possible Values: 'true' only Active Events, 'false' only Disabled Events), (default:'null')</param>
        /// <param name="source">Filter by Source (Separator ','), (Sources available 'lts','trevilab','drin'),(default: 'null')</param>
        /// <param name="latitude">GeoFilter FLOAT Latitude Format: '46.624975', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="longitude">GeoFilter FLOAT Longitude Format: '11.369909', 'null' = disabled, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="radius">Radius INTEGER to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. Random Sorting is disabled if the GeoFilter Informations are provided, (default:'null') <a href='https://github.com/noi-techpark/odh-docs/wiki/Geosorting-and-Locationfilter-usage#geosorting-functionality' target="_blank">Wiki geosort</a></param>
        /// <param name="publishedon">Published On Filter (Separator ',' List of publisher IDs), (default:'null')</param>
        /// <param name="updatefrom">Returns data changed after this date Format (yyyy-MM-dd), (default: 'null')</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="langfilter">Language filter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="searchfilter">String to search for, Title in all languages are searched, (default: null) <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#searchfilter" target="_blank">Wiki searchfilter</a></param>
        /// <param name="rawfilter"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawfilter" target="_blank">Wiki rawfilter</a></param>
        /// <param name="rawsort"><a href="https://github.com/noi-techpark/odh-docs/wiki/Using-rawfilter-and-rawsort-on-the-Tourism-Api#rawsort" target="_blank">Wiki rawsort</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Collection of Event Objects</returns>
        /// <response code="200">List created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(JsonResult<EventFlattened>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[OdhCacheOutput(ClientTimeSpan = 0, ServerTimeSpan = 3600, CacheKeyGenerator = typeof(CustomCacheKeyGenerator), MustRevalidate = true)]
        //[Authorize]
        [HttpGet, Route("Event")]
        public async Task<IActionResult> Get(
            string? language = null,
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? idlist = null,
            string? venueidfilter = null,
            string? locfilter = null,
            string? tagfilter = null,
            LegacyBool active = null!,
            string? begindate = null,
            string? enddate = null,
            string? sort = null,
            string? updatefrom = null,
            string? seed = null,
            string? publishedon = null,
            string? langfilter = null,
            string? source = null,
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
                venueidfilter: venueidfilter,
                tagfilter: tagfilter,
                searchfilter: searchfilter,
                locfilter: locfilter,
                begindate: begindate,
                enddate: enddate,
                sort: sort,
                active: active,
                seed: seed,
                lastchange: updatefrom,
                langfilter: langfilter,
                source: source,
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
        /// GET Event Single
        /// </summary>
        /// <param name="id">ID of the Event</param>
        /// <param name="language">Language field selector, displays data and fields in the selected language (default:'null' all languages are displayed)</param>
        /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
        /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
        /// <returns>Event Object</returns>
        /// <response code="200">Object created</response>
        /// <response code="400">Request Error</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(typeof(EventFlattened), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Event/{id}", Name = "SingleEventV2")]
        public async Task<IActionResult> GetEventSingle(
            string id,
            string? language,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
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
        [ProducesResponseType(typeof(EventFlattened), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Event/ConvertEventShortToEventV2/{id}")]
        public async Task<IActionResult> ConvertEventShortToEventV2(
            string id,
            bool savetotable = false
        )
        {
            var query = QueryFactory
                .Query("eventeuracnoi")
                .Select("data")
                .Where("id", id.ToLower())
                .FilterDataByAccessRoles(UserRolesToFilterEndpoint("EventShort"));

            var data = await query.GetObjectListAsync<EventShortLinked>();
            var convertresult = EventFlattenedConverter.ConvertEventListToEventFlattened(data);

            if (savetotable)
            {
                List<UpdateDetail> result = new List<UpdateDetail>();
                foreach (var datasingle in convertresult)
                {
                    foreach (var venue in datasingle.Venues)
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
                    foreach (var theevent in datasingle.Events)
                    {
                        result.Add(
                            await QueryFactory.UpsertData<EventFlattened>(
                                theevent,
                                new DataInfo("eventsv2", CRUDOperation.Create),
                                new EditInfo("venueconverter", "api"),
                                new CRUDConstraints(null, new List<string>()),
                                new CompareConfig(false, false)
                            )
                        );
                    }
                }
                return Ok(result);
            }
            else
            {
                return Ok(convertresult);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("Event/ConvertEventToEventV2/{id}")]
        public async Task<IActionResult> ConvertEventToEventV2(string id, bool savetotable = false)
        {
            var query = QueryFactory
                .Query("events")
                .Select("data")
                .Where("id", id.ToUpper())
                .FilterDataByAccessRoles(UserRolesToFilterEndpoint("Event"));

            var data = await query.GetObjectListAsync<EventLinked>();

            var evtypequery = QueryFactory.Query("eventtypes").Select("data");

            var evtypedata = await evtypequery.GetObjectListAsync<EventTypes>();

            var convertresult = EventFlattenedConverter.ConvertEventListToEventFlattened(data, evtypedata);

            if (savetotable)
            {
                List<UpdateDetail> result = new List<UpdateDetail>();

                foreach (var datasingle in convertresult)
                {
                    foreach (var venue in datasingle.Venues)
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
                    foreach (var theevent in datasingle.Events)
                    {
                        result.Add(
                            await QueryFactory.UpsertData<EventFlattened>(
                                theevent,
                                new DataInfo("eventsv2", CRUDOperation.Create),
                                new EditInfo("venueconverter", "api"),
                                new CRUDConstraints(null, new List<string>()),
                                new CompareConfig(false, false)
                            )
                        );
                    }
                }
                return Ok(result);
            }
            else
            {
                return Ok(convertresult);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet, Route("Event/ConvertEventTypesToTags")]
        public async Task<IActionResult> ConvertEventTopicsToTags(bool savetotable = false)
        {
            var query = QueryFactory.Query("eventtypes").Select("data");

            var datalist = await query.GetObjectListAsync<EventTypes>();

            var listtaglinked = new List<TagLinked>();

            List<UpdateDetail> result = new List<UpdateDetail>();

            foreach (var data in datalist)
            {
                var converted = EventFlattenedConverter.ConvertEventTopicToTag(data);
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
            string? tagfilter,
            string? venueidfilter,
            string? searchfilter,
            string? locfilter,
            string? begindate,
            string? enddate,
            string? sort,
            bool? active,
            string? seed,
            string? lastchange,
            string? langfilter,
            string? source,
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

                EventV2Helper myeventhelper = await EventV2Helper.CreateAsync(
                    QueryFactory,
                    idfilter,
                    venueidfilter,
                    locfilter,
                    tagfilter,
                    begindate,
                    enddate,
                    active,
                    lastchange,
                    langfilter,
                    source,
                    publishedon,
                    cancellationToken
                );

                string? sortifseednull = EventHelper.GetEventSortExpression(
                    sort,
                    begindate,
                    enddate,
                    ref seed
                );

                var query = QueryFactory
                    .Query()
                    .When(getasidarray, x => x.Select("id"))
                    .When(!getasidarray, x => x.SelectRaw("data"))
                    .From("eventsv2")
                    .EventV2WhereExpression(
                        idlist: myeventhelper.idlist,
                        venueidlist: myeventhelper.venueidlist,
                        tagdict: myeventhelper.tagdict,
                        districtlist: myeventhelper.districtlist,
                        municipalitylist: myeventhelper.municipalitylist,
                        tourismvereinlist: myeventhelper.tourismvereinlist,
                        regionlist: myeventhelper.regionlist,
                        sourcelist: myeventhelper.sourcelist,
                        languagelist: myeventhelper.languagelist,
                        begindate: myeventhelper.begin,
                        enddate: myeventhelper.end,
                        activefilter: myeventhelper.active,
                        publishedonlist: myeventhelper.publishedonlist,
                        searchfilter: searchfilter,
                        language: language,
                        lastchange: myeventhelper.lastchange,
                        additionalfilter: additionalfilter,
                        userroles: UserRolesToFilterEndpoint("Event")
                    )
                    .ApplyRawFilter(rawfilter)
                    //.OrderByRawIfNotNull(sortifseednull)
                    .ApplyOrdering_GeneratedColumns(
                        ref seed,
                        geosearchresult,
                        rawsort,
                        sortifseednull
                    );
                //.ApplyOrdering(ref seed, geosearchresult, rawsort, sortifseednull);

                //.OrderBySeed(ref seed, sortifseednull)
                //.GeoSearchFilterAndOrderby(geosearchresult);
                //TODO Use sorting

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

        /// <summary>
        /// GET Single Event
        /// </summary>
        /// <param name="id">ID of the Event</param>
        /// <returns>Event Object</returns>
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
                    .Query("eventsv2")
                    .Select("data")
                    .Where("id", id.ToUpper())
                    .When(
                        !String.IsNullOrEmpty(additionalfilter),
                        q => q.FilterAdditionalDataByCondition(additionalfilter)
                    )
                    .FilterDataByAccessRoles(UserRolesToFilterEndpoint("Event"));

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
        /// POST Insert new Event
        /// </summary>
        /// <param name="odhevent">Event Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(nameof(GetEventList))]
        [AuthorizeODH(PermissionAction.Create)]
        //[Authorize(Roles = "DataWriter,DataCreate,EventManager,EventCreate")]
        [ProducesResponseType(typeof(UpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost, Route("Event")]
        public Task<IActionResult> Post([FromBody] EventFlattened odhevent)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Create
                AdditionalFiltersToAdd.TryGetValue("Create", out var additionalfilter);

                odhevent.Id = Helper.IdGenerator.GenerateIDFromType(odhevent);

                //Check all Languages
                odhevent.CheckMyInsertedLanguages(null);

                //Trim all strings
                odhevent.TrimStringProperties();

                //Populate Tags (Id/Source/Type)
                await odhevent.UpdateTagsExtension(QueryFactory);

                return await UpsertData<EventFlattened>(
                    odhevent,
                    new DataInfo("eventsv2", CRUDOperation.Create),
                    new CompareConfig(false, false),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// PUT Modify existing Event
        /// </summary>
        /// <param name="id">Event Id</param>
        /// <param name="odhevent">Event Object</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(nameof(GetEventList))]
        [ProducesResponseType(typeof(UpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [AuthorizeODH(PermissionAction.Update)]
        //[Authorize(Roles = "DataWriter,DataModify,EventManager,EventModify,EventUpdate")]
        [HttpPut, Route("Event/{id}")]
        public Task<IActionResult> Put(string id, [FromBody] EventFlattened odhevent)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Update
                AdditionalFiltersToAdd.TryGetValue("Update", out var additionalfilter);

                odhevent.Id = Helper.IdGenerator.CheckIdFromType<EventFlattened>(id);

                //Check all Languages
                odhevent.CheckMyInsertedLanguages(null);

                //Trim all strings
                odhevent.TrimStringProperties();

                //Populate Tags (Id/Source/Type)
                await odhevent.UpdateTagsExtension(QueryFactory);

                return await UpsertData<EventFlattened>(
                    odhevent,
                    new DataInfo("eventsv2", CRUDOperation.Update, true),
                    new CompareConfig(true, true),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        /// <summary>
        /// DELETE Event by Id
        /// </summary>
        /// <param name="id">Event Id</param>
        /// <returns>Http Response</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[InvalidateCacheOutput(nameof(GetEventList))]
        //[Authorize(Roles = "DataWriter,DataDelete,EventManager,EventDelete")]
        [ProducesResponseType(typeof(UpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [AuthorizeODH(PermissionAction.Delete)]
        [HttpDelete, Route("Event/{id}")]
        public Task<IActionResult> Delete(string id)
        {
            return DoAsyncReturn(async () =>
            {
                //Additional Filters on the Action Delete
                AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

                id = Helper.IdGenerator.CheckIdFromType<EventFlattened>(id);

                return await DeleteData<EventFlattened>(
                    id,
                    new DataInfo("eventsv2", CRUDOperation.Delete),
                    new CRUDConstraints(additionalfilter, UserRolesToFilter)
                );
            });
        }

        #endregion
    }
}
