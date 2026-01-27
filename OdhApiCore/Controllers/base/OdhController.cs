// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    [ApiController]
    [Route("v1")]
    [FormatFilter]
    public abstract class OdhController : ControllerBase
    {
        private readonly IWebHostEnvironment env;
        private readonly ISettings settings;

        public OdhController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<OdhController> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
        {
            this.env = env;
            this.settings = settings;
            this.Logger = logger;
            this.QueryFactory = queryFactory;
            this.OdhPushnotifier = odhpushnotifier;

            //TO CHECK ControllerContext.RouteData is null in constructor
            //UserRolesToFilter = GetEndPointAccessRole(endpoint);
        }

        protected ILogger<OdhController> Logger { get; }

        protected QueryFactory QueryFactory { get; }

        protected IOdhPushNotifier OdhPushnotifier;

        //Ensure this is only called 1 time per Controller operation
        protected IEnumerable<string> UserRolesToFilter
        {
            get { return GetEndPointAccessRole(); }
        }

        //Hack Method for Search api which passes endpoint
        protected IEnumerable<string> UserRolesToFilterEndpoint(string endpoint)
        {
            return GetEndPointAccessRole(endpoint);
        }

        //Ensure this is only called 1 time per Controller operation
        protected IDictionary<string, string> AdditionalFiltersToAdd
        {
            get { return GetAdditionalFilterDictionary(); }
        }

        //Hack Method for Search api which passes endpoint
        protected IDictionary<string, string> AdditionalFiltersToAddEndpoint(string endpoint)
        {
            return GetAdditionalFilterDictionary(endpoint);
        }

        private IDictionary<string, string> GetAdditionalFilterDictionary(
            string? pathtocheck = null
        )
        {
            var additionalfilterdict = new Dictionary<string, string>();

            if (pathtocheck == null)
                pathtocheck = ControllerContext.RouteData.Values["controller"]?.ToString();

            if (pathtocheck != null)
            {
                var rolesforendpoint = User.Claims.Where(c =>
                    c.Type == ClaimTypes.Role && c.Value.StartsWith(pathtocheck + "_")
                );
                foreach (var role in rolesforendpoint)
                {
                    var splittedrole = role.Value.Split("_");
                    if (splittedrole.Length == 3)
                    {
                        //TODO if key is already there add the info on the value with a &
                        if (additionalfilterdict.ContainsKey(splittedrole[1]))
                        {
                            additionalfilterdict.TryAddOrUpdate(
                                splittedrole[1],
                                additionalfilterdict[splittedrole[1]] + "&" + splittedrole[2]
                            );
                        }
                        else
                        {
                            additionalfilterdict.TryAddOrUpdate(splittedrole[1], splittedrole[2]);
                        }
                    }
                }
            }
            return additionalfilterdict;
        }

        private List<string> GetEndPointAccessRole(string? path = null)
        {
            List<string> rolelist = new List<string>();

            var dict = GetAdditionalFilterDictionary(path);

            if (dict.ContainsKey("Read"))
            {
                if (dict["Read"].Split("&").Any(x => x.Contains("accessrole")))
                {
                    foreach (
                        var tocheck in dict["Read"].Split("&").Where(x => x.Contains("accessrole"))
                    )
                    {
                        foreach (var role in tocheck.Split("=").LastOrDefault().Split(","))
                        {
                            rolelist.Add(role);
                        }
                    }
                }
            }

            return rolelist.Count == 0 ? new List<string>() { "ANONYMOUS" } : rolelist;
        }

        //NOT Needed at moment
        protected IEnumerable<string> FieldsToHide
        {
            get
            {
                List<string> fieldstohide = new();

                var roleclaims = User
                    .Claims.Where(x =>
                        x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                    )
                    .Select(claim => claim.Value)
                    .ToList();

                //Search all settings with Entity = Controllername
                var fields = settings.Field2HideConfig.Where(x =>
                    x.Entity == this.ControllerContext.RouteData.Values["controller"]?.ToString()
                    || string.IsNullOrEmpty(x.Entity)
                );

                foreach (var field in fields)
                {
                    if (roleclaims.Intersect(field.DisplayOnRoles ?? new()).Count() == 0)
                    {
                        fieldstohide.AddRange(field.Fields ?? new());
                    }
                }

                return fieldstohide;
            }
        }

        protected Func<string, string> UrlGenerator
        {
            get
            {
                return self =>
                {
                    var chunks = self.Split('/', 2);
                    if (chunks.Length < 2)
                        return self;

                    //Hack for the Weather Controller if there is another / in the route to check if it is not generating side effects
                    //TODO find a better solution here
                    if (chunks[0].StartsWith("Weather") && chunks[1].Split('/').Count() > 1)
                    {
                        var chunksadditional = chunks[1].Split('/');
                        chunks[0] = chunks[0] + chunksadditional[0];
                        chunks[1] = chunksadditional[1];
                    }                    

                    var (controller, id) = (chunks[0], chunks[1]);

                    if (!String.IsNullOrEmpty(controller) && !String.IsNullOrEmpty(id))
                        return Uri.UnescapeDataString(Url.Link($"Single{controller}", new { id })!);
                    else
                        return self;
                };
            }
        }

        protected Func<string, string> UrlGeneratorStatic
        {
            get
            {
                return self =>
                {
                    var location = new Uri($"{Request.Scheme}://{Request.Host}/" + self);
                    return location.AbsoluteUri;
                };
            }
        }

        protected async Task<IActionResult> DoAsync(Func<Task<IActionResult>> f)
        {
            try
            {
                return await f();
            }
            catch (PostGresSQLHelperException ex)
            {
                return this.StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = ex.Message }
                );
            }
            catch (JsonPathException ex)
            {
                return this.BadRequest(
                    new
                    {
                        error = "Invalid JSONPath selection",
                        path = ex.Path,
                        details = env.IsDevelopment() ? ex.ToString() : ex.Message,
                    }
                );
            }
            catch (Exception ex)
            {
                if (ex.Message == "Request Error")
                    return this.BadRequest(
                        new { error = env.IsDevelopment() ? ex.ToString() : ex.Message }
                    );
                else if (ex.Message == "No data")
                    return this.StatusCode(
                        StatusCodes.Status404NotFound,
                        new { error = env.IsDevelopment() ? ex.ToString() : ex.Message }
                    );
                else
                    return this.StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new { error = env.IsDevelopment() ? ex.ToString() : ex.Message }
                    );
            }
        }

        //GET Data
        protected Task<IActionResult> DoAsyncReturn(Func<Task<object?>> f)
        {
            return DoAsync(async () =>
            {
                object? result = await f();
                if (result == null)
                    return this.NotFound();
                else if (result is ActionResult)
                    return (IActionResult)result;
                else
                    return this.Ok(result);
            });
        }

        //CREATE and UPDATE data
        #region LEGACY UpsertData

        protected async Task<IActionResult> UpsertData<T>(
            T data,
            DataInfo datainfo,
            CompareConfig compareconfig,
            CRUDConstraints crudconstraints,
            string editsource = "api"
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            //TODO Username and provenance of the insert/edit
            //Get the Name Identifier TO CHECK what about service accounts?
            string editor =
                this.User != null && this.User.Claims != null ?
                this.User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value
                    : "anonymous";

            if (
                HttpContext.Request.Headers.ContainsKey("Referer")
                && !String.IsNullOrEmpty(HttpContext.Request.Headers["Referer"])
            )
            {
                editsource = HttpContext.Request.Headers["Referer"];

                //Hack if Referer is infrastructure v2 api make an upsert
                if (HttpContext.Request.Headers["Referer"] == "https://tourism.importer.v2")
                {
                    datainfo.ErrorWhendataExists = false;
                    datainfo.ErrorWhendataIsNew = false;
                }
            }

            var result = await QueryFactory.UpsertData<T>(
                data,
                datainfo,
                new EditInfo(editor, editsource),
                crudconstraints,
                compareconfig
            );

            //push modified data to all published Channels
            result.pushed = await CheckIfObjectChangedAndPush(result, result.id, result.type);


            //return ReturnCRUDResult(result);

            //Use newer UpdateResult ?
            return ReturnUpdateResult(result, editsource, "", true);
        }
        
        #endregion

        protected async Task<IActionResult> UpsertData<T>(
            QueryFactoryExtension.Upsertable<T> data,
            DataInfo datainfo,
            CompareConfig compareconfig,
            CRUDConstraints crudconstraints,
            string editsource = "api"
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            //TODO Username and provenance of the insert/edit
            //Get the Name Identifier TO CHECK what about service accounts?
            string editor =
                this.User != null && this.User.Claims != null ? 
                this.User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value
                    : "anonymous";

            if (
                HttpContext.Request.Headers.ContainsKey("Referer")
                && !String.IsNullOrEmpty(HttpContext.Request.Headers["Referer"])
            )
            {
                editsource = HttpContext.Request.Headers["Referer"];
            }

            var result = await QueryFactory.UpsertData<T>(
                data,
                datainfo,
                new EditInfo(editor, editsource),
                crudconstraints,
                compareconfig
            );

            //push modified data to all published Channels
            result.pushed = await CheckIfObjectChangedAndPush(result, result.id, result.type);

            //return ReturnCRUDResult(result);

            //Use newer UpdateResult ?
            return ReturnUpdateResult(result, editsource, "", true);
        }

        //BATCH CREATE and UPDATE data
        protected async Task<IActionResult> UpsertDataArray<T>(
            IEnumerable<Helper.QueryFactoryExtension.Upsertable<T>> dataList,
            DataInfo datainfo,
            CompareConfig compareconfig,
            CRUDConstraints createConstraints,
            CRUDConstraints updateConstraints,
            string editsource = "api"
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            //Get the Name Identifier
            string editor =
                this.User != null && this.User.Claims != null ?
                this.User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault()?.Value
                    ?? "anonymous"
                    : "anonymous";

            if (
                HttpContext.Request.Headers.ContainsKey("Referer")
                && !String.IsNullOrEmpty(HttpContext.Request.Headers["Referer"])
            )
            {
                editsource = HttpContext.Request.Headers["Referer"];
            }

            try
            {
                var batchResult = await QueryFactory.UpsertDataArray<T>(
                    dataList,
                    datainfo,
                    new EditInfo(editor, editsource),
                    createConstraints,
                    updateConstraints,
                    compareconfig
                );

                // Success - return the result
                return Ok(batchResult);
            }
            catch (BatchValidationException ex)
            {
                // Convert structured validation errors to ModelState
                foreach (var error in ex.ValidationErrors)
                {
                    foreach (var errorMessage in error.Value)
                    {
                        ModelState.AddModelError(error.Key, errorMessage);
                    }
                }
                return ValidationProblem(ModelState);
            }
        }

        //DELETE data
        protected async Task<IActionResult> DeleteData<T>(
            string id,
            DataInfo datainfo,
            CRUDConstraints crudconstraints,
            string editsource = "api"
        )
            where T : IIdentifiable, IMetaData, IImportDateassigneable, new()
        {
            //TODO Add logic which checks if user is authorized to delete data
            //Return not found if wrong ID
            //Return forbitten 403 if
            //Return 401 if unauthorized

            var result = await QueryFactory.DeleteData<T>(id, datainfo, crudconstraints);
            //push modified data to all published Channels
            result.pushed = await PushDeletedObject(result, result.id, result.type);

            //return ReturnCRUDResult(result);

            //Use newer UpdateResult ?
            return ReturnUpdateResult(result, editsource, "", true);
        }

        //PUSH Modified data
        protected async Task<IDictionary<string, NotifierResponse>?> CheckIfObjectChangedAndPush(
            UpdateDetail myupdateresult,
            string id,
            string datatype,
            IDictionary<string, bool>? additionalpushinfo = null,
            string pushorigin = "odh.api.push"
        )
        {
            IDictionary<string, NotifierResponse>? pushresults = default(IDictionary<
                string,
                NotifierResponse
            >);

            //Check if data has changed and Push To all channels
            if (
                myupdateresult.error == 0
                && myupdateresult.objectchanged != null
                && myupdateresult.objectchanged > 0
                && myupdateresult.pushchannels != null
                && myupdateresult.pushchannels.Count > 0
            )
            {
                if (additionalpushinfo == null)
                    additionalpushinfo = new Dictionary<string, bool>();

                //Check if image has changed
                if (
                    myupdateresult.objectimagechanged != null
                    && myupdateresult.objectimagechanged.Value > 0
                )
                    additionalpushinfo.TryAdd("imageschanged", true);
                else
                    additionalpushinfo.TryAdd("imageschanged", false);

                pushresults = await OdhPushnotifier.PushToPublishedOnServices(
                    id,
                    datatype.ToLower(),
                    pushorigin,
                    additionalpushinfo,
                    false,
                    "api",
                    myupdateresult.pushchannels.ToList()
                );
            }

            return pushresults;
        }

        //PUSH Deleted data
        private async Task<IDictionary<string, NotifierResponse>?> PushDeletedObject(
            UpdateDetail myupdateresult,
            string id,
            string datatype,
            string pushorigin = "odh.api.push"
        )
        {
            IDictionary<string, NotifierResponse>? pushresults = default(IDictionary<
                string,
                NotifierResponse
            >);

            //Check if data has changed and Push To all channels
            if (myupdateresult.deleted > 0 && myupdateresult.pushchannels.Count > 0)
            {
                pushresults = await OdhPushnotifier.PushToPublishedOnServices(
                    id,
                    datatype.ToLower(),
                    pushorigin,
                    null,
                    true,
                    "api",
                    myupdateresult.pushchannels.ToList()
                );
            }

            return pushresults;
        }

        protected IActionResult ReturnUpdateDetail(UpdateDetail result)
        {
            ///Give shorter Error messages to display directly in the databrowser
            ///TODO some optimizations
            switch (result.exception)
            {
                case "":
                case null:
                    return Ok(result);
                case "Not Allowed":
                    return StatusCode(403, "Not enough permissions");
                case "Not Found":
                    return NotFound();
                case "Bad Request":
                    return BadRequest();
                case "No Data":
                    return BadRequest(result.exception);
                case "Data exists already":
                    return BadRequest(result.exception);
                case "Data to update Not Found":
                    return BadRequest(result.exception);
                case "Internal Error":
                    return StatusCode(500);
                default:
                    return BadRequest(result);
            }
        }

        protected IActionResult ReturnUpdateResult(UpdateDetail detailresult, string? source, string? message, bool createlog)
        {
            //Use UpdateResult here
            var result = GenericResultsHelper.GetUpdateResult(null, source, detailresult.operation, "single", message, detailresult.type, new List<UpdateDetail>() { detailresult }, null, createlog);

            ///Give shorter Error messages to display directly in the databrowser
            ///TODO some optimizations
            switch (result.exception)
            {
                case "":
                case null:
                    return Ok(result);
                case "Not Allowed":
                    return CustomApiActionResults.ForbiddenValidation("You do not have permission to perform this action");
                case "Not Found":
                    return NotFound();
                case "Bad Request":
                    return BadRequest();
                case "No Data":
                    return CustomApiActionResults.BadRequestValidation(result.exception);
                case "Data exists already":
                    return CustomApiActionResults.BadRequestValidation(result.exception);
                case "Data to update Not Found":
                    return CustomApiActionResults.BadRequestValidation(result.exception);
                case "Internal Error":
                    return StatusCode(500);
                default:
                    return CustomApiActionResults.BadRequestValidation(result.exception);
            }
        }

    }

    public abstract class OdhControllerWithSearch : OdhController
    {
        public OdhControllerWithSearch(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<OdhControllerWithSearch> logger,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier
        )
            : base(env, settings, logger, queryFactory, odhpushnotifier) { }

        // Generic POST search method
        [HttpPost("[controller]Search")]
        public virtual async Task<IActionResult> Search([FromBody] Dictionary<string, object> searchParams)
        {
            // Build query string from posted JSON
            var queryString = string.Join("&",
                searchParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value?.ToString() ?? string.Empty)}"));

            // Get the current request path and modify it for GET
            var getUrl = $"{Request.Path.Value.Replace("Search", string.Empty)}?{queryString}";

            // Option 2: Internal forwarding (better approach)
            Request.QueryString = new QueryString($"?{queryString}");
            Request.Method = "GET";

            // Call the GET method through reflection or direct invocation
            var getMethod = GetType().GetMethod("Get",
                BindingFlags.Public | BindingFlags.Instance);

            if (getMethod != null)
            {
                // Build parameters for the GET method
                var parameters = getMethod.GetParameters();
                var args = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramName = parameters[i].Name;
                    if (searchParams.ContainsKey(paramName))
                    {
                        try
                        {
                            // Convert to appropriate type
                            if (parameters[i].ParameterType == typeof(LegacyBool))
                            {
                                {
                                    var boolresult = Convert.ToBoolean(searchParams[paramName]);
                                    args[i] = new LegacyBool(boolresult);
                                }
                            }
                            else if (parameters[i].ParameterType == typeof(PageSize))
                            {
                                {
                                    var intresult = Convert.ToInt32(searchParams[paramName]);
                                    args[i] = new PageSize(intresult);
                                }
                            }
                            else
                            {
                                var value = Convert.ChangeType(searchParams[paramName], parameters[i].ParameterType);
                                args[i] = value;
                            }                             
                        }
                        catch (Exception)
                        {                            
                                // If conversion fails, use default value
                                args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;                            
                        }
                    }
                    else
                    {
                        if (parameters[i].ParameterType == typeof(PageSize))
                        {
                            args[i] = new PageSize(null);
                        }
                        else if (parameters[i].ParameterType == typeof(LegacyBool))
                        {
                            args[i] = new LegacyBool(null);
                        }
                        else
                        {
                            args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
                        }
                    }
                }

                var result = getMethod.Invoke(this, args);

                // Handle async methods
                if (result is Task<IActionResult> taskResult)
                {
                    return await taskResult;
                }
                else if (result is Task task)
                {
                    await task;
                    // Get the Result property for Task<T>
                    var resultProperty = task.GetType().GetProperty("Result");
                    return (IActionResult)resultProperty?.GetValue(task);
                }
                else
                {
                    // Synchronous method
                    return (IActionResult)result;
                }
            }

            return BadRequest("GET method not found");
        }

    }    
}
