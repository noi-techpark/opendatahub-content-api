﻿using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SqlKata.Execution;
using OdhApiCore.Filters;
using OdhApiCore.GenericHelpers;
using EBMS;
using NINJA;
using NINJA.Parser;
using System.Net.Http;
using RAVEN;
using Microsoft.Extensions.Hosting;

namespace OdhApiCore.Controllers.api
{
    [ApiExplorerSettings(IgnoreApi = true)]    
    [ApiController]
    public class UpdateApiController : OdhController
    {
        private readonly ISettings settings;
        private readonly IWebHostEnvironment env;

        public UpdateApiController(IWebHostEnvironment env, ISettings settings, ILogger<UpdateApiController> logger, QueryFactory queryFactory)
            : base(env, settings, logger, queryFactory)
        {
            this.env = env;
            this.settings = settings;
        }

        #region ODH RAVEN exposed

        [HttpGet, Route("Raven/{datatype}/Update/{id}")]
        //[Authorize(Roles = "DataWriter,DataCreate,DataUpdate")]
        public async Task<IActionResult> UpdateFromRaven(string id, string datatype, CancellationToken cancellationToken)
        {
            try
            {
                var result = await GetFromRavenAndTransformToPGObject(id, datatype, cancellationToken);

                var updateresult = new UpdateResult
                {
                    operation = "Update Raven",
                    updatetype = "single",
                    otherinfo = datatype,
                    id = id,
                    message = "",
                    recordsmodified = (result.created + result.updated + result.deleted),
                    created = result.created,
                    updated = result.updated,
                    deleted = result.deleted,
                    success = true
                };

                Console.WriteLine(JsonConvert.SerializeObject(updateresult));
                //Trying with logger not working
                //Logger.LogInformation(JsonConvert.SerializeObject(updateresult));


                ////TODO Invalidate the cache based on what was updated like in this doc
                ////https://github.com/filipw/Strathweb.CacheOutput
                //// now get cache instance
                //var cache = Configuration.CacheOutputConfiguration().GetCacheOutputProvider(Request);

                //// and invalidate cache for method "Get" of "TeamsController"
                //cache.RemoveStartsWith(Configuration.CacheOutputConfiguration().MakeBaseCachekey((TeamsController t) => t.Get()));

                //We use https://github.com/Iamcerba/AspNetCore.CacheOutput it seems there is only the Attribute solution in the docs


                return Ok(updateresult);
            }
            catch (Exception ex)
            {
                var updateerror = new UpdateResult
                {
                    operation = "Update Raven",
                    updatetype = "single",
                    otherinfo = "",
                    id = id,
                    message = "Update Raven failed: " + ex.Message,
                    recordsmodified = 0,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    success = false
                };

                //Logger.LogError(JsonConvert.SerializeObject(updateerror));
                Console.WriteLine(JsonConvert.SerializeObject(updateerror));

                return BadRequest(updateerror);
            }
        }

        #endregion

        #region ODHRAVEN Helpers

        private async Task<UpdateDetail> GetFromRavenAndTransformToPGObject(string id, string datatype, CancellationToken cancellationToken)
        {
            var mydata = default(IIdentifiable);
            var mypgdata = default(IIdentifiable);

            var myupdateresult = default(UpdateDetail);
            var updateresultreduced = default(UpdateDetail);

            switch (datatype.ToLower())
            {
                case "accommodation":
                    mydata = await GetDataFromRaven.GetRavenData<AccommodationLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<AccommodationLinked, AccommodationLinked>((AccommodationLinked)mydata, TransformToPGObject.GetAccommodationPGObject);
                    else
                        throw new Exception("No data found!");

                    myupdateresult = await SaveRavenObjectToPG<AccommodationLinked>((AccommodationLinked)mypgdata, "accommodations");
                    
                    //Check if data has to be reduced and save it
                    if (ReduceDataTransformer.ReduceDataCheck<AccommodationLinked>((AccommodationLinked)mypgdata) == true)
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject((AccommodationLinked)mypgdata, ReduceDataTransformer.CopyLTSAccommodationToReducedObject);

                        updateresultreduced = await SaveRavenObjectToPG<AccommodationLinked>((AccommodationLinkedReduced)reducedobject, "accommodations");
                    }                    

                    //UPDATE ACCOMMODATIONROOMS
                    var myroomdatalist = await GetDataFromRaven.GetRavenData<IEnumerable<AccommodationRoomLinked>>("accommodationroom", id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken, "AccommodationRoom?accoid=");

                    if (myroomdatalist != null)
                    {
                        foreach (var myroomdata in myroomdatalist)
                        {
                            var mypgroomdata = TransformToPGObject.GetPGObject<AccommodationRoomLinked, AccommodationRoomLinked>((AccommodationRoomLinked)myroomdata, TransformToPGObject.GetAccommodationRoomPGObject);

                            var accoroomresult = await SaveRavenObjectToPG<AccommodationRoomLinked>((AccommodationRoomLinked)mypgroomdata, "accommodationrooms");

                            //Merge with updateresult
                            myupdateresult = GenericResultsHelper.MergeUpdateDetail(new List<UpdateDetail> { myupdateresult, accoroomresult });
                        }
                    }
                    else
                        throw new Exception("No data found!");
                    
                    break;

                case "gastronomy":
                    mydata = await GetDataFromRaven.GetRavenData<GastronomyLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<GastronomyLinked, GastronomyLinked>((GastronomyLinked)mydata, TransformToPGObject.GetGastronomyPGObject);
                    else
                        throw new Exception("No data found!");

                    myupdateresult = await SaveRavenObjectToPG<GastronomyLinked>((GastronomyLinked)mypgdata, "gastronomies");

                    //Check if data has to be reduced and save it
                    if(ReduceDataTransformer.ReduceDataCheck<GastronomyLinked>((GastronomyLinked)mypgdata) == true)
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject((GastronomyLinked)mypgdata, ReduceDataTransformer.CopyLTSGastronomyToReducedObject);

                        updateresultreduced = await SaveRavenObjectToPG<GastronomyLinked>((GastronomyLinkedReduced)reducedobject, "gastronomies");
                    }

                    break;
                case "activity":
                    mydata = await GetDataFromRaven.GetRavenData<LTSActivityLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<LTSActivityLinked, LTSActivityLinked>((LTSActivityLinked)mydata, TransformToPGObject.GetActivityPGObject);
                    else
                        throw new Exception("No data found!");

                    myupdateresult = await SaveRavenObjectToPG<LTSActivityLinked>((LTSActivityLinked)mypgdata, "activities");

                    //Check if data has to be reduced and save it
                    if (ReduceDataTransformer.ReduceDataCheck<LTSActivityLinked>((LTSActivityLinked)mypgdata) == true)
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject((LTSActivityLinked)mypgdata, ReduceDataTransformer.CopyLTSActivityToReducedObject);

                        updateresultreduced = await SaveRavenObjectToPG<LTSActivityLinked>((LTSActivityLinkedReduced)reducedobject, "activities");
                    }

                    break;

                case "poi":
                    mydata = await GetDataFromRaven.GetRavenData<LTSPoiLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<LTSPoiLinked, LTSPoiLinked>((LTSPoiLinked)mydata, TransformToPGObject.GetPoiPGObject);
                    else
                        throw new Exception("No data found!");

                    myupdateresult = await SaveRavenObjectToPG<LTSPoiLinked>((LTSPoiLinked)mypgdata, "pois");

                    //Check if data has to be reduced and save it
                    if (ReduceDataTransformer.ReduceDataCheck<LTSPoiLinked>((LTSPoiLinked)mypgdata) == true)
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject((LTSPoiLinked)mypgdata, ReduceDataTransformer.CopyLTSPoiToReducedObject);

                        updateresultreduced = await SaveRavenObjectToPG<LTSPoiLinked>((LTSPoiLinkedReduced)reducedobject, "pois");
                    }

                    break;

                case "odhactivitypoi":                    

                    mydata = await GetDataFromRaven.GetRavenData<ODHActivityPoiLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<ODHActivityPoiLinked, ODHActivityPoiLinked>((ODHActivityPoiLinked)mydata, TransformToPGObject.GetODHActivityPoiPGObject);
                    else
                        throw new Exception("No data found!");

                    //Special get all Taglist and traduce it on import
                    //var myalltaglist = GenericTaggingHelper.GetAllGenericTagsfromJson(settings.JsonConfig.Jsondir);
                    //if (myalltaglist != null)
                    //     GenericTaggingHelper.GenerateNewTagging(((ODHActivityPoiLinked)mypgdata).SmgTags, myalltaglist);
                    

                    myupdateresult = await SaveRavenObjectToPG<ODHActivityPoiLinked>((ODHActivityPoiLinked)mypgdata, "smgpois");

                    //Check if data has to be reduced and save it
                    if (ReduceDataTransformer.ReduceDataCheck<ODHActivityPoiLinked>((ODHActivityPoiLinked)mypgdata) == true)
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject((ODHActivityPoiLinked)mypgdata, ReduceDataTransformer.CopyLTSODHActivtyPoiToReducedObject);

                        updateresultreduced = await SaveRavenObjectToPG<ODHActivityPoiLinked>((LTSODHActivityPoiReduced)reducedobject, "smgpois");
                    }

                    break;

                case "event":
                    mydata = await GetDataFromRaven.GetRavenData<EventLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<EventLinked, EventLinked>((EventLinked)mydata, TransformToPGObject.GetEventPGObject);
                    else
                        throw new Exception("No data found!");

                    myupdateresult = await SaveRavenObjectToPG<EventLinked>((EventLinked)mypgdata, "events");

                    //Check if data has to be reduced and save it
                    if (ReduceDataTransformer.ReduceDataCheck<EventLinked>((EventLinked)mypgdata) == true)
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject((EventLinked)mypgdata, ReduceDataTransformer.CopyLTSEventToReducedObject);

                        updateresultreduced = await SaveRavenObjectToPG<EventLinked>((EventLinkedReduced)reducedobject, "events");
                    }

                    break;

                case "webcam":
                    mydata = await GetDataFromRaven.GetRavenData<WebcamInfoLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken, "WebcamInfo/");
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<WebcamInfoLinked, WebcamInfoLinked>((WebcamInfoLinked)mydata, TransformToPGObject.GetWebcamInfoPGObject);
                    else
                        throw new Exception("No data found!");

                    myupdateresult = await SaveRavenObjectToPG<WebcamInfoLinked>((WebcamInfoLinked)mypgdata, "webcams");

                    //Check if data has to be reduced and save it
                    if (ReduceDataTransformer.ReduceDataCheck<WebcamInfoLinked>((WebcamInfoLinked)mypgdata) == true)
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject((WebcamInfoLinked)mypgdata, ReduceDataTransformer.CopyLTSWebcamInfoToReducedObject);

                        updateresultreduced = await SaveRavenObjectToPG<WebcamInfoLinked>((WebcamInfoLinkedReduced)reducedobject, "webcams");
                    }

                    break;

                case "metaregion":
                    mydata = await GetDataFromRaven.GetRavenData<MetaRegionLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<MetaRegionLinked, MetaRegionLinked>((MetaRegionLinked)mydata, TransformToPGObject.GetMetaRegionPGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<MetaRegionLinked>((MetaRegionLinked)mypgdata, "metaregions");

                case "region":
                    mydata = await GetDataFromRaven.GetRavenData<RegionLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<RegionLinked, RegionLinked>((RegionLinked)mydata, TransformToPGObject.GetRegionPGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<RegionLinked>((RegionLinked)mypgdata, "regions");

                case "tv":
                    mydata = await GetDataFromRaven.GetRavenData<TourismvereinLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken, "TourismAssociation/");
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<TourismvereinLinked, TourismvereinLinked>((TourismvereinLinked)mydata, TransformToPGObject.GetTourismAssociationPGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<TourismvereinLinked>((TourismvereinLinked)mypgdata, "tvs");

                case "municipality":
                    mydata = await GetDataFromRaven.GetRavenData<MunicipalityLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<MunicipalityLinked, MunicipalityLinked>((MunicipalityLinked)mydata, TransformToPGObject.GetMunicipalityPGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<MunicipalityLinked>((MunicipalityLinked)mypgdata, "municipalities");

                case "district":
                    mydata = await GetDataFromRaven.GetRavenData<DistrictLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<DistrictLinked, DistrictLinked>((DistrictLinked)mydata, TransformToPGObject.GetDistrictPGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<DistrictLinked>((DistrictLinked)mypgdata, "districts");

                case "experiencearea":
                    mydata = await GetDataFromRaven.GetRavenData<ExperienceAreaLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<ExperienceAreaLinked, ExperienceAreaLinked>((ExperienceAreaLinked)mydata, TransformToPGObject.GetExperienceAreaPGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<ExperienceAreaLinked>((ExperienceAreaLinked)mypgdata, "experienceareas");

                case "skiarea":
                    mydata = await GetDataFromRaven.GetRavenData<SkiAreaLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<SkiAreaLinked, SkiAreaLinked>((SkiAreaLinked)mydata, TransformToPGObject.GetSkiAreaPGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<SkiAreaLinked>((SkiAreaLinked)mypgdata, "skiareas");

                case "skiregion":
                    mydata = await GetDataFromRaven.GetRavenData<SkiRegionLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<SkiRegionLinked, SkiRegionLinked>((SkiRegionLinked)mydata, TransformToPGObject.GetSkiRegionPGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<SkiRegionLinked>((SkiRegionLinked)mypgdata, "skiregions");

                case "article":
                    mydata = await GetDataFromRaven.GetRavenData<ArticlesLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<ArticlesLinked, ArticlesLinked>((ArticlesLinked)mydata, TransformToPGObject.GetArticlePGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<ArticlesLinked>((ArticlesLinked)mypgdata, "articles");

                case "odhtag":
                    mydata = await GetDataFromRaven.GetRavenData<ODHTagLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<ODHTagLinked, ODHTagLinked>((ODHTagLinked)mydata, TransformToPGObject.GetODHTagPGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<ODHTagLinked>((ODHTagLinked)mypgdata, "smgtags");

                case "measuringpoint":
                    mydata = await GetDataFromRaven.GetRavenData<MeasuringpointLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken, "Weather/Measuringpoint/");
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<MeasuringpointLinked, MeasuringpointLinked>((MeasuringpointLinked)mydata, TransformToPGObject.GetMeasuringpointPGObject);
                    else
                        throw new Exception("No data found!");

                    myupdateresult = await SaveRavenObjectToPG<MeasuringpointLinked>((MeasuringpointLinked)mypgdata, "measuringpoints");

                    //Check if data has to be reduced and save it
                    if (ReduceDataTransformer.ReduceDataCheck<MeasuringpointLinked>((MeasuringpointLinked)mypgdata) == true)
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject((MeasuringpointLinked)mypgdata, ReduceDataTransformer.CopyLTSMeasuringpointToReducedObject);

                        updateresultreduced = await SaveRavenObjectToPG<MeasuringpointLinked>((MeasuringpointLinkedReduced)reducedobject, "measuringpoints");
                    }

                    break;

                case "venue":
                    mydata = await GetDataFromRaven.GetRavenData<DDVenue>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<DDVenue, DDVenue>((DDVenue)mydata, TransformToPGObject.GetVenuePGObject);
                    else
                        throw new Exception("No data found!");

                    myupdateresult = await SaveRavenObjectToPG<DDVenue>((DDVenue)mypgdata, "venues");
                    
                    //Check if data has to be reduced and save it
                    if (ReduceDataTransformer.ReduceDataCheck<DDVenue>((DDVenue)mypgdata) == true)
                    {
                        var reducedobject = ReduceDataTransformer.GetReducedObject((DDVenue)mypgdata, ReduceDataTransformer.CopyLTSVenueToReducedObject);

                        updateresultreduced = await SaveRavenObjectToPG<DDVenue>((DDVenueReduced)reducedobject, "venues");
                    }

                    break;

                case "wine":
                    mydata = await GetDataFromRaven.GetRavenData<WineLinked>(datatype, id, settings.RavenConfig.ServiceUrl, settings.RavenConfig.User, settings.RavenConfig.Password, cancellationToken);
                    if (mydata != null)
                        mypgdata = TransformToPGObject.GetPGObject<WineLinked, WineLinked>((WineLinked)mydata, TransformToPGObject.GetWinePGObject);
                    else
                        throw new Exception("No data found!");

                    return await SaveRavenObjectToPG<WineLinked>((WineLinked)mypgdata, "wines");

                default:
                    throw new Exception("no match found");
            }

            var mycompleteupdateresult = GenericResultsHelper.MergeUpdateDetail(new List<UpdateDetail> { myupdateresult, updateresultreduced });

            return myupdateresult;
        }

        private async Task<UpdateDetail> SaveRavenObjectToPG<T>(T datatosave, string table) where T : IIdentifiable, IImportDateassigneable, IMetaData, ILicenseInfo
        {
            datatosave._Meta.LastUpdate = datatosave.LastChange;

            //Temporary Hack will be moved to the importer workerservice

            var result = await QueryFactory.UpsertData<T>(datatosave, table);

            return new UpdateDetail() { created = result.created, updated = result.updated, deleted = result.deleted };
        }        

        #endregion
    }
}