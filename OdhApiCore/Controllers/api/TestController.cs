// // SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
// //
// // SPDX-License-Identifier: AGPL-3.0-or-later

// using DataModel;
// using DataModel.Annotations;
// using Helper;
// using Helper.Generic;
// using Helper.Identity;
// using Helper.Tagging;
// using LTSAPI;
// using MessagePack.Formatters;
// using Microsoft.AspNetCore.Cors;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
// using OdhApiCore.Controllers.api;
// using OdhApiCore.Repositories;
// using OdhApiCore.Responses;
// using OdhNotifier;
// using Schema.NET;
// using ServiceReferenceLCS;
// using SqlKata.Execution;
// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// namespace OdhApiCore.Controllers
// {
//     [EnableCors("CorsPolicy")]
//     [NullStringParameterActionFilter]
//     public class TestController : OdhController
//     {
//         public TestController(
//             IWebHostEnvironment env,
//             ISettings settings,
//             ILogger<TestController> logger,
//             QueryFactory queryFactory,
//             IOdhPushNotifier odhpushnotifier
//         )
//             : base(env, settings, logger, queryFactory, odhpushnotifier) { }

//         #region SWAGGER Exposed API

//         /// <summary>
//         /// GET Testdata List
//         /// </summary>
//         /// <param name="pagenumber">Pagenumber</param>
//         /// <param name="pagesize">Elements per Page, (default:10)</param>
//         /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
//         /// <param name="langfilter">Langfilter (returns only data available in the selected Language, Separator ',' possible values: 'de,it,en,nl,sc,pl,fr,ru', 'null': Filter disabled)</param>
//         /// <param name="idlist">IDFilter (Separator ',' List of IDs, 'null' = No Filter), (default:'null')</param>
//         /// <param name="source">Source Filter, (default:'null')</param>
//         /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
//         /// <param name="getasidarray">Get result only as Array of Ids, (default:false)  Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
//         /// <returns>Collection of Testdata Objects</returns>
//         /// <response code="200">List created</response>
//         /// <response code="400">Request Error</response>
//         /// <response code="500">Internal Server Error</response>
//         [ProducesResponseType(typeof(JsonResult<Testdata>), StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status400BadRequest)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         [HttpGet, Route("Testdata")]
//         public async Task<IActionResult> GetTestdataAsync(
//             uint? pagenumber = 1,
//             PageSize pagesize = null!,
//             string? language = null,
//             string? langfilter = null,
//             string? idlist = null,
//             bool removenullvalues = false,
//             bool getasidarray = false,
//             CancellationToken cancellationToken = default
//         )
//         {
//             return await Get(
//                 pagenumber,
//                 pagesize,
//                 language,
//                 langfilter,
//                 idlist,
//                 removenullvalues: removenullvalues,
//                 getasidarray: getasidarray,
//                 cancellationToken
//             );
//         }

//         /// <summary>
//         /// GET Testdata Single
//         /// </summary>
//         /// <param name="id">ID of the Testdata</param>
//         /// <param name="language">Language field selector, displays data and fields available in the selected language (default:'null' all languages are displayed)</param>
//         /// <param name="fields">Select fields to display, More fields are indicated by separator ',' example fields=Id,Active,Shortname (default:'null' all fields are displayed). <a href="https://github.com/noi-techpark/odh-docs/wiki/Common-parameters%2C-fields%2C-language%2C-searchfilter%2C-removenullvalues%2C-updatefrom#fields" target="_blank">Wiki fields</a></param>
//         /// <param name="removenullvalues">Remove all Null values from json output. Useful for reducing json size. By default set to false. Documentation on <a href='https://github.com/noi-techpark/odh-docs/wiki/Common-parameters,-fields,-language,-searchfilter,-removenullvalues,-updatefrom#removenullvalues' target="_blank">Opendatahub Wiki</a></param>
//         /// <returns>Testdata Object</returns>
//         /// <response code="200">Object created</response>
//         /// <response code="400">Request Error</response>
//         /// <response code="500">Internal Server Error</response>
//         [ProducesResponseType(typeof(Testdata), StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status400BadRequest)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         [HttpGet, Route("Testdata/{id}", Name = "SingleTestdata")]
//         public async Task<IActionResult> GetTestdataSingle(
//             string id,
//             string? language = null,
//             [ModelBinder(typeof(CommaSeparatedArrayBinder))] string[]? fields = null,            
//             bool removenullvalues = false,
//             CancellationToken cancellationToken = default
//         )
//         {
//             return await GetSingle(
//                 id,
//                 language,
//                 fields: fields ?? Array.Empty<string>(),
//                 removenullvalues: removenullvalues,
//                 cancellationToken
//             );
//         }

//         #endregion

//         #region GETTER
//         public class TestdataHelper
//         {
//             public List<string> idlist;
//             public List<string> languagelist;


//             public static async Task<TestdataHelper> CreateAsync(
//                 QueryFactory queryFactory,
//                 string? idfilter,
//                 string? languagefilter,
//                 CancellationToken cancellationToken
//             )
//             {            
//                 return new TestdataHelper(
//                     idfilter,
//                     languagefilter
//                 );
//             }

//             private TestdataHelper(
//                 string? idfilter,
//                 string? languagefilter
//             )
//             {   
//                 // testdatas id are forced to be lowercase
//                 idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToLower());
//                 languagelist = Helper.CommonListCreator.CreateIdList(languagefilter);
//             }
//         }

//         private Task<IActionResult> Get(
//             uint? pagenumber,
//             int? pagesize,
//             string? language,
//             string? languagefilter,
//             string? idfilter,
//             bool removenullvalues,
//             bool getasidarray,
//             CancellationToken cancellationToken
//         )
//         {
//             return DoAsyncReturn(async () =>
//             {
//                 //Additional Read Filters to Add Check
//                 AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

//                 TestdataHelper helper =
//                             await TestdataHelper.CreateAsync(
//                                 queryFactory: QueryFactory,
//                                 idfilter: idfilter,
//                                 languagefilter: languagefilter,
//                                 cancellationToken
//                             );

//                 var query = QueryFactory
//                     .Query()
//                     .When(getasidarray, x => x.Select("id"))
//                     .When(!getasidarray, x => x.SelectRaw("data"))
//                     .From("testdatas")
//                     .When(helper.idlist != null && helper.idlist.Count > 0, q => q.WhereIn("id", helper.idlist))
//                     .When(
//                         helper.languagelist.Count > 0,
//                         q => q.HasLanguageFilterAnd_GeneratedColumn(helper.languagelist)
//                     );

//                 //IF getasidarray set simply return array of ids
//                 if (getasidarray)
//                 {
//                     return await query.GetAsync<string>();
//                 }

//                 // Get paginated data
//                 var data = await query.PaginateAsync<JsonRaw>(
//                     page: (int)pagenumber,
//                     perPage: pagesize ?? 25
//                 );

//                 var dataTransformed = data.List.Select(raw =>
//                     raw.TransformRawData(
//                         language,
//                         [],
//                         filteroutNullValues: removenullvalues,
//                         urlGenerator: UrlGenerator,
//                         fieldstohide: null
//                     )
//                 );

//                 uint totalpages = (uint)data.TotalPages;
//                 uint totalcount = (uint)data.Count;

//                 return ResponseHelpers.GetResult(
//                     (uint)pagenumber,
//                     totalpages,
//                     totalcount,
//                     null,
//                     dataTransformed,
//                     Url
//                 );
//             });
//         }

//         private Task<IActionResult> GetSingle(
//             string id,
//             string? language,
//             string[] fields,
//             bool removenullvalues,
//             CancellationToken cancellationToken
//         )
//         {
//             return DoAsyncReturn(async () =>
//             {
//                 //Additional Read Filters to Add Check
//                 AdditionalFiltersToAdd.TryGetValue("Read", out var additionalfilter);

//                 var data = await QueryFactory
//                     .Query("testdatas")
//                     .Select("data")
//                     .Where("id", id.ToLower())
//                     .When(
//                         !String.IsNullOrEmpty(additionalfilter),
//                         q => q.FilterAdditionalDataByCondition(additionalfilter)
//                     )
//                     .FilterDataByAccessRoles(UserRolesToFilter)
//                     .FirstOrDefaultAsync<JsonRaw>();

//                 return data?.TransformRawData(
//                     language,
//                     fields,
//                     filteroutNullValues: removenullvalues,
//                     urlGenerator: UrlGenerator,
//                     fieldstohide: null
//                 );
//             });
//         }

//         #endregion

//         #region POST PUT DELETE
        
//         /// <summary>
//         /// POST Insert new Testdata
//         /// </summary>
//         /// <param name="testdata">Testdata Object</param>
//         /// <returns>Http Response</returns>
//         [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status400BadRequest)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         [AuthorizeODH(PermissionAction.Create)]
//         [HttpPost, Route("Testdata")]
//         public Task<IActionResult> Post([FromBody] Testdata testdata)
//         {
//             return DoAsyncReturn(async () =>
//             {
//                 //Additional Read Filters to Add Check
//                 AdditionalFiltersToAdd.TryGetValue("Create", out var createFilter);

//                 testdata.Id = Helper.IdGenerator.GenerateIDFromType(testdata);

//                 if (testdata.LicenseInfo == null)
//                     testdata.LicenseInfo = new LicenseInfo() { ClosedData = false };

//                 //TRIM all strings
//                 testdata.TrimStringProperties();

//                 return await UpsertData<Testdata>(
//                     testdata,
//                     new DataInfo("testdatas", CRUDOperation.Create),
//                     new CompareConfig(false, false),
//                     new CRUDConstraints(createFilter, UserRolesToFilter)
//                 );
//             });
//         }

//         /// <summary>
//         /// PUT Modify existing Testdata
//         /// </summary>
//         /// <param name="id">Testdata Id</param>
//         /// <param name="testdata">Testdata Object</param>
//         /// <returns>Http Response</returns>
//         [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status400BadRequest)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         [HttpPut, Route("Testdata/{id}")]
//         public Task<IActionResult> Put(string id, [FromBody] Testdata testdata)
//         {
//             return DoAsyncReturn(async () =>
//             {
//                 //Additional Read Filters to Add Check
//                 AdditionalFiltersToAdd.TryGetValue("Update", out var updateFilter);

//                 testdata.Id = Helper.IdGenerator.CheckIdFromType<Testdata>(id);

//                 //TRIM all strings
//                 testdata.TrimStringProperties();

//                 return await UpsertData<Testdata>(
//                     testdata,
//                     new DataInfo("testdatas", CRUDOperation.Update, true),
//                     new CompareConfig(true, false),
//                     new CRUDConstraints(updateFilter, UserRolesToFilter)
//                 );
//             });
//         }

//         /// <summary>
//         /// DELETE Testdata by Id
//         /// </summary>
//         /// <param name="id">Testdata Id</param>
//         /// <returns>Http Response</returns>
//         //[Authorize(Roles = "DataWriter,DataDelete,TestdataManager,TestdataDelete")]
//         [AuthorizeODH(PermissionAction.Delete)]
//         [ProducesResponseType(typeof(PGCRUDResult), StatusCodes.Status200OK)]
//         [ProducesResponseType(StatusCodes.Status400BadRequest)]
//         [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//         [HttpDelete, Route("Testdata/{id}")]
//         public Task<IActionResult> Delete(string id)
//         {
//             return DoAsyncReturn(async () =>
//             {
//                 //Additional Read Filters to Add Check
//                 AdditionalFiltersToAdd.TryGetValue("Delete", out var additionalfilter);

//                 id = Helper.IdGenerator.CheckIdFromType<Testdata>(id);
                
//                 return await DeleteData<Testdata>(
//                     id,
//                     new DataInfo("testdatas", CRUDOperation.Delete),
//                     new CRUDConstraints(additionalfilter, UserRolesToFilter)
//                 );
//             });
//         }

//         #endregion
//     }
// }
