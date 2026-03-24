using DataModel;
using Helper;
using Helper.Converters;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OdhNotifier;
using Schema.NET;
using SqlKata.Execution;
using System;
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
        /// GET Event from EventShort
        /// </summary>
        /// <param name="id">EventShort Id</param>
        [ProducesResponseType(typeof(JsonResult<EventLinked>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet, Route("Converter/EventShortToEvent/{id}", Name = "SingleEventShortToEventConverter")]
        public async Task<IActionResult> GetEventShortToEvent(
            string id,
            string? language = null,
            [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,
            bool removenullvalues = false,
            CancellationToken cancellationToken = default
        )
        {
            return await GetEventShortToEventSingle(
                id,
                language,
                fields: fields ?? Array.Empty<string>(),
                removenullvalues: removenullvalues,
                cancellationToken
            );            
        }

        private Task<IActionResult> GetEventShortToEventSingle(
            string id,
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
                 .Where("id", id.ToLower())
                 .FilterDataByAccessRoles(UserRolesToFilter);

                var data = await query.GetObjectSingleAsync<EventShortLinked>();

                var converted = EventEventShortConverter.ConvertEventShortToEvent(data);

                var jsonraw = new JsonRaw(converted);

                return jsonraw?.TransformRawData(
                        language,
                        fields,
                        filteroutNullValues: removenullvalues,
                        urlGenerator: UrlGenerator,
                        fieldstohide: null
                    );
            });
        }
            
    }
}
