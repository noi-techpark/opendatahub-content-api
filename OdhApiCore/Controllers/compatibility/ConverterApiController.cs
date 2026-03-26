// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Geo.Measure;
using Helper;
using Helper.Converters;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OdhApiCore.Responses;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    /// <summary>
    /// Converter Api
    /// </summary>    
    [ApiExplorerSettings(IgnoreApi = true)]
    [EnableCors("CorsPolicy")]
    [NullStringParameterActionFilter]
    public class ConverterController : OdhController
    {
        public ConverterController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<ConverterController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        /// <summary>
        /// GET Events from EventShort
        /// </summary>
        /// <param name="id">EventShort Idlist</param>
        [ProducesResponseType(typeof(JsonResult<EventLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Converter/EventShortToEvent")]
        public async Task<IActionResult> GetEventShortToEvent(
            string? language = null,
            uint pagenumber = 1,
            PageSize pagesize = null!,
            string? idlist = null,
            bool denormalize = false,            
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetEventShortToEventList(                
                denormalize,
                fields: fields ?? Array.Empty<string>(),
                language,
                pagenumber: pagenumber,
                pagesize: pagesize,
                idfilter: idlist,
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }


        /// <summary>
        /// GET Event from EventShort
        /// </summary>
        /// <param name="id">EventShort Id</param>
        [ProducesResponseType(typeof(JsonResult<EventLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Converter/EventShortToEvent/{id}", Name = "SingleEventShortToEventConverter")]
        public async Task<IActionResult> GetEventShortToEventSingle(
            string id,
            bool denormalize = false,
            string? language = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetEventShortToEventById(
                id,
                denormalize,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues: removenullvalues,
                cancellationToken
            );            
        }        

        /// <summary>
        /// GET Venues from EventShort
        /// </summary>
        /// <param name="id">EventShort Id</param>
        [ProducesResponseType(typeof(IEnumerable<VenueV2>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Converter/EventShortToVenues")]
        public async Task<IActionResult> GetEventShortToVenues(
            string? language = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetEventShortToVenueList(                      
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues: removenullvalues,
                cancellationToken
            );
        }

        private Task<IActionResult> GetEventShortToEventList(           
            bool denormalize,
            string[] fields,
            string? language,
            uint pagenumber,
            int? pagesize,
            string? idfilter,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //check if there are additionalfilters to add
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var idlist = idfilter != null ? idfilter.Split(",").ToList() : null;

                var query = QueryFactory
                 .Query("eventeuracnoi")
                 .Select("data")
                 .When(idlist != null, x => x.IdIlikeFilter(idlist))
                 .FilterDataByAccessRoles(UserRolesToFilter);

                //var data = await query.GetObjectListAsync<EventShortLinked>();
                // Get paginated data
                var dataRaw = await query.PaginateAsync<JsonRaw>(
                    page: (int)pagenumber,
                    perPage: pagesize ?? 25
                );
                
                var dataMapped = new
                {
                    dataRaw.TotalPages,
                    dataRaw.Page,
                    dataRaw.PerPage,
                    dataRaw.Count,
                    List = dataRaw.List.Select(jr => EventEventShortConverter.ConvertEventShortToEventByType(JsonConvert.DeserializeObject<EventShortLinked>(jr.Value), denormalize)!).ToList()
                };

                var dataMappedjsonraw = new
                {
                    dataMapped.TotalPages,
                    dataMapped.Page,
                    dataMapped.PerPage,
                    dataMapped.Count,
                    List = dataMapped.List.Select(jr => new JsonRaw(jr)).ToList()
                };

                var dataTransformed = dataMappedjsonraw.List.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    )
                );

                uint totalpages = (uint)dataMappedjsonraw.TotalPages;
                uint totalcount = (uint)dataMappedjsonraw.Count;

                return ResponseHelpers.GetResult(
                    pagenumber,
                    totalpages,
                    totalcount,
                    null,
                    dataTransformed,
                    Url
                );

            });
        }

        private Task<IActionResult> GetEventShortToEventById(
            string id,
            bool denormalize,
            string? language,
            string[] fields,
            bool removenullvalues,
            CancellationToken cancellationToken
        )
        {
            return DoAsyncReturn(async () =>
            {
                //check if there are additionalfilters to add
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);
                
                var query = QueryFactory
                 .Query("eventeuracnoi")
                 .Select("data")
                 .Where("id", id)
                 .FilterDataByAccessRoles(UserRolesToFilter);

                var data = await query.GetObjectSingleAsync<EventShortLinked>();

                var converted = EventEventShortConverter.ConvertEventShortToEventByType(data, denormalize);

                var jsonrawlist = converted.Select(x => new JsonRaw(x)).ToList();


                if (converted.Count() == 1)
                {
                    var dataTransformed = jsonrawlist.FirstOrDefault().TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null                        
                    );

                    return dataTransformed;
                }
                else
                {
                    var dataTransformed = jsonrawlist.Select(raw =>
                    raw.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                        )
                    );

                    return dataTransformed;
                }
            });
        }

        private Task<IActionResult> GetEventShortToVenueList(           
           string? language,
           string[] fields,
           bool removenullvalues,
           CancellationToken cancellationToken
       )
        {
            return DoAsyncReturn(async () =>
            {
                //check if there are additionalfilters to add
                AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

                var query = QueryFactory
                 .Query("eventeuracnoi")
                 .SourceFilter_GeneratedColumn(new List<string>() { "noi", "ebms", "eurac", "unibz", "nobis" })
                 .Select("data")
                 .FilterDataByAccessRoles(UserRolesToFilter);

                var data = await query.GetObjectListAsync<EventShortLinked>();

                var converted = EventEventShortConverter.ConvertEventShortsToVenueList(data);

                var jsonrawlist = converted.Select(x => new JsonRaw(x)).ToList();
                
                var dataTransformed = jsonrawlist.Select(raw =>
                raw.TransformRawData(
                    language,
                    fields,
                    filteroutNullValues: removenullvalues,
                    urlGenerator: UrlGenerator,
                    fieldstohide: null
                    )
                );

                return dataTransformed;
                
            });
        }

    }
}
