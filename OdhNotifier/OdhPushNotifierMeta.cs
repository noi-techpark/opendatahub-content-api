// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Runtime.Internal.Transform;
using DataModel;
using Helper;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Server.IIS.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SqlKata;
using SqlKata.Execution;
using Swashbuckle.AspNetCore.SwaggerGen;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OdhNotifier
{
    public interface IOdhPushNotifier
    {
        Task<IDictionary<string, NotifierResponse>> PushToAllRegisteredServices(
            string id,
            string type,
            string updatemode,
            IDictionary<string, bool>? additionalpushinfo,
            bool isdelete,
            string origin,
            string? referer = null,
            List<string>? excludeservices = null
        );
        Task<IDictionary<string, NotifierResponse>> PushToPublishedOnServices(
            string id,
            string type,
            string updatemode,
            IDictionary<string, bool>? additionalpushinfo,
            bool isdelete,
            string origin,
            List<string> publishedonlist,
            string? referer = null
        );
        Task<
            IDictionary<string, ICollection<NotifierResponse>>
        > PushFailureQueueToPublishedonService(
            List<string> publishedonlist,
            int elementstoprocess,
            string? referer = null
        );
        Task<
            IDictionary<string, ICollection<NotifierResponse>>
        > PushCustomObjectsToPublishedonService(
            List<string> publishedonlist,
            List<string> idlist,
            string odhtype,
            IDictionary<string, bool>? additionalpushinfo,
            string? referer = null
        );
    }

    public class OdhPushNotifier : IOdhPushNotifier, IDisposable
    {
        private readonly ISettings settings;
        protected QueryFactory QueryFactory { get; }

        List<NotifierConfig> notifierconfiglist;

        public OdhPushNotifier(ISettings settings, QueryFactory queryFactory)
        {
            this.settings = settings;
            this.QueryFactory = queryFactory;

            this.notifierconfiglist = settings.NotifierConfig;
        }

        /// <summary>
        /// Pushes to all registered Services in config, a service can be manually excluded by passing and exclude list
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <param name="updatemode"></param>
        /// <param name="origin"></param>
        /// <param name="referer"></param>
        /// <param name="excludeservices"></param>
        /// <returns></returns>
        public async Task<IDictionary<string, NotifierResponse>> PushToAllRegisteredServices(
            string id,
            string type,
            string updatemode,
            IDictionary<string, bool>? additionalpushinfo,
            bool isdelete,
            string origin,
            string? referer = null,
            List<string>? excludeservices = null
        )
        {
            IDictionary<string, NotifierResponse> notifierresponselist =
                new Dictionary<string, NotifierResponse>();

            foreach (var notifyconfig in notifierconfiglist)
            {
                if (
                    excludeservices != null
                    && excludeservices.Contains(notifyconfig.ServiceName.ToLower())
                )
                    continue;

                NotifyMetaGenerated meta = new NotifyMetaGenerated(
                    notifyconfig,
                    id,
                    type,
                    additionalpushinfo,
                    isdelete,
                    updatemode,
                    origin,
                    referer
                );

                NotifierResponse notifierresponse = new NotifierResponse();

                var response = await SendNotify(meta);
                notifierresponse.ObjectId = id;
                notifierresponse.HttpStatusCode = response.Item1;
                notifierresponse.Response = response.Item2;
                notifierresponse.Service = notifyconfig.ServiceName;
                notifierresponse.Success = MapStatusCodeToSuccessProperty(
                    notifierresponse.HttpStatusCode
                );

                notifierresponselist.TryAddOrUpdate(notifyconfig.ServiceName, notifierresponse);
            }

            return notifierresponselist;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <param name="updatemode"></param>
        /// <param name="origin"></param>
        /// <param name="referer"></param>
        /// <param name="excludeservices"></param>
        /// <returns></returns>
        public async Task<IDictionary<string, NotifierResponse>> PushToPublishedOnServices(
            string id,
            string type,
            string updatemode,
            IDictionary<string, bool>? additionalpushinfo,
            bool isdelete,
            string origin,
            List<string> publishedonlist,
            string? referer = null
        )
        {
            IDictionary<string, NotifierResponse> notifierresponselist =
                new Dictionary<string, NotifierResponse>();

            //IF
            foreach (var pushchannel in publishedonlist)
            {
                if (
                    notifierconfiglist
                        .Where(x => x.ServiceName.ToLower() == pushchannel.ToLower())
                        .Count() == 0
                )
                    notifierresponselist.TryAddOrUpdate(
                        pushchannel,
                        new NotifierResponse()
                        {
                            ObjectId = id,
                            Success = false,
                            HttpStatusCode = HttpStatusCode.NotFound,
                            Service = pushchannel,
                            Response = "No configuration found for this publisher",
                        }
                    );
            }

            foreach (var notifyconfig in notifierconfiglist)
            {
                if (publishedonlist.Contains(notifyconfig.ServiceName.ToLower()))
                {
                    //Compare and push?

                    NotifyMetaGenerated meta = new NotifyMetaGenerated(
                        notifyconfig,
                        id,
                        type,
                        additionalpushinfo,
                        isdelete,
                        updatemode,
                        origin,
                        referer
                    );

                    var response = await SendNotify(meta);

                    notifierresponselist.TryAddOrUpdate(notifyconfig.ServiceName, response.Item2);
                }
            }

            return notifierresponselist;
        }

        private static bool MapStatusCodeToSuccessProperty(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                    return true;
                case HttpStatusCode.Created:
                    return true;
                default:
                    return false;
            }
        }

        private async Task<Tuple<HttpStatusCode, NotifierResponse>> SendNotify(
            NotifyMeta notify,
            NotifierFailureQueue? failurequeuedata = null
        )
        {
            var requesturl = notify.Url;

            try
            {
                //If data is from failurequeue proceed directly
                if (failurequeuedata != null || CheckValidTypes(notify))
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);

                        //Add all Headers
                        if (notify.Headers != null)
                        {
                            foreach (var header in notify.Headers)
                            {
                                client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            }
                        }

                        //Add Referer Header
                        if (!String.IsNullOrEmpty(notify.Referer))
                            client.DefaultRequestHeaders.Referrer = new Uri(notify.Referer);

                        //Add Additional Parameters
                        if (notify.Parameters != null)
                        {
                            requesturl = requesturl + "?";
                            foreach (var parameter in notify.Parameters)
                            {
                                requesturl = requesturl + parameter.Key + "=" + parameter.Value;

                                if (notify.Parameters.Last().Key != parameter.Key)
                                {
                                    requesturl = requesturl + "&";
                                }
                            }
                        }

                        HttpResponseMessage response = default(HttpResponseMessage);

                        //GET or POST the data to the service
                        if (notify.Mode.ToLower() == "get")
                        {
                            response = await client.GetAsync(requesturl);
                        }
                        else if (notify.Mode.ToLower() == "post")
                        {
                            var data = new StringContent(
                                System.Text.Json.JsonSerializer.Serialize(
                                    new
                                    {
                                        id = notify.Id,
                                        entity = notify.NotifyType,
                                        skipImage = notify.HasImagechanged ? false : true,
                                        isHardDelete = notify.IsDelete,
                                    }
                                )
                            );

                            //If datatype is accommodation add the skiprooms
                            if (notify.Type == "accommodation")
                                data = new StringContent(
                                    System.Text.Json.JsonSerializer.Serialize(
                                        new
                                        {
                                            id = notify.Id,
                                            entity = notify.NotifyType,
                                            skipImage = notify.HasImagechanged ? false : true,
                                            skipRooms = notify.Roomschanged.HasValue
                                            && notify.Roomschanged.Value
                                                ? false
                                                : true,
                                            isHardDelete = notify.IsDelete,
                                        }
                                    )
                                );

                            data.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                            response = await client.PostAsync(requesturl, data);
                        }

                        if (
                            response != null
                            && (
                                response.StatusCode == HttpStatusCode.OK
                                || response.StatusCode == HttpStatusCode.Created
                            )
                        )
                        {
                            //TODO if all ok set the status to ok and update

                            if (failurequeuedata != null)
                            {
                                failurequeuedata.RetryCount = failurequeuedata.RetryCount + 1;
                                failurequeuedata.Status = "elaborated";
                                failurequeuedata.LastChange = DateTime.Now;

                                await QueryFactory
                                    .Query("notificationfailures")
                                    .Where("id", failurequeuedata.Id)
                                    .UpdateAsync(
                                        new JsonBData()
                                        {
                                            id = failurequeuedata.Id,
                                            data = new JsonRaw(failurequeuedata),
                                        }
                                    );
                            }

                            return await ReturnHttpResponse(
                                response,
                                notify,
                                notify.HasImagechanged,
                                notify.Roomschanged.HasValue ? notify.Roomschanged.Value : false,
                                ""
                            );
                        }
                        else if (response != null)
                        {
                            throw new Exception(response.ReasonPhrase);
                        }
                        else
                        {
                            throw new Exception("response null");
                        }
                    }
                }
                else
                    throw new Exception("invalid_type");
            }
            catch (TaskCanceledException ex)
            {
                if (failurequeuedata == null)
                    await WriteToFailureQueue(notify, ex.Message);
                else
                    await UpdateFailureQueue(notify, ex.Message, failurequeuedata);

                var response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
                return await ReturnHttpResponse(
                    response,
                    notify,
                    notify.HasImagechanged,
                    notify.Roomschanged.HasValue ? notify.Roomschanged.Value : false,
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                if (failurequeuedata == null)
                    await WriteToFailureQueue(notify, ex.Message);
                else
                    await UpdateFailureQueue(notify, ex.Message, failurequeuedata);

                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                return await ReturnHttpResponse(
                    response,
                    notify,
                    notify.HasImagechanged,
                    notify.Roomschanged.HasValue ? notify.Roomschanged.Value : false,
                    ex.Message
                );
            }
        }

        private async Task<Tuple<HttpStatusCode, NotifierResponse>> ReturnHttpResponse(
            HttpResponseMessage response,
            NotifyMeta notify,
            bool imageupdate,
            bool roomsupdate,
            string error
        )
        {
            var responsecontent = await ReadResponse(response, notify.Destination, notify.Id);

            if (
                response.StatusCode == HttpStatusCode.OK
                || response.StatusCode == HttpStatusCode.Created
            )
            {
                GenerateLog(
                    notify.Id,
                    notify.Destination,
                    notify.Type.ToLower() + ".push.trigger",
                    "api",
                    notify.UdateMode,
                    imageupdate,
                    roomsupdate,
                    responsecontent,
                    null,
                    true
                );
            }
            else
            {
                GenerateLog(
                    notify.Id,
                    notify.Destination,
                    notify.Type.ToLower() + ".push.error",
                    "api",
                    notify.UdateMode,
                    imageupdate,
                    roomsupdate,
                    responsecontent,
                    error,
                    false
                );
            }

            //var responseobj = JObject.Parse(responsestring);

            return Tuple.Create(response.StatusCode, responsecontent);
        }

        private async Task<NotifierResponse> ReadResponse(
            HttpResponseMessage response,
            string service,
            string id
        )
        {
            NotifierResponse notifierresponse = new NotifierResponse();
            notifierresponse.ObjectId = id;
            notifierresponse.HttpStatusCode = response.StatusCode;
            notifierresponse.Response = await TryReadingResponse(response, service);
            notifierresponse.Service = service;
            notifierresponse.Success = MapStatusCodeToSuccessProperty(
                notifierresponse.HttpStatusCode
            );

            return notifierresponse;
        }

        private async Task<object?> TryReadingResponse(HttpResponseMessage response, string service)
        {
            try
            {
                //TODO Switch between response service types
                return await response.Content.ReadFromJsonAsync<IdmMarketPlacePushResponse>();
            }
            catch
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        private void GenerateLog(
            string id,
            string destination,
            string message,
            string origin,
            string updatemode,
            bool? imageupdate,
            bool? roomsupdate,
            NotifierResponse response,
            string? exception,
            bool success
        )
        {
            NotifyLog log = new NotifyLog()
            {
                id = id,
                destination = destination,
                message = message,
                origin = origin,
                imageupdate = imageupdate,
                roomsupdate = roomsupdate,
                updatemode = updatemode,
                response = response.Response,
                exception = exception,
                success = success,
            };

            Console.WriteLine(JsonConvert.SerializeObject(log));
        }

        private async Task WriteToFailureQueue(NotifyMeta notify, string exmessage)
        {
            NotifierFailureQueue myfailure = new NotifierFailureQueue();
            myfailure.Id = Guid.NewGuid().ToString();
            myfailure.ItemId = notify.Id;
            myfailure.Type = notify.Type;
            myfailure.NotifyType = notify.NotifyType;
            myfailure.Exception = exmessage;
            myfailure.LastChange = DateTime.Now;
            myfailure.Service = notify.Destination;
            myfailure.PushUrl = notify.Url;
            myfailure.Status = "open";

            if (exmessage == "invalid_type" || notify.NotifyType == "NOT SUPPORTED")
                myfailure.Status = "not_supported";

            myfailure.RetryCount = 1;
            myfailure.IsDeleteOperation = notify.IsDelete;
            myfailure.HasImageChanged = notify.HasImagechanged;
            myfailure.Roomschanged = notify.Roomschanged;

            await QueryFactory
                .Query("notificationfailures")
                .InsertAsync(new JsonBData() { id = myfailure.Id, data = new JsonRaw(myfailure) });
        }

        private async Task UpdateFailureQueue(
            NotifyMeta notify,
            string exmessage,
            NotifierFailureQueue myfailure
        )
        {
            myfailure.ItemId = notify.Id;
            myfailure.Type = notify.Type;
            myfailure.NotifyType = notify.NotifyType;
            myfailure.Exception = exmessage;
            myfailure.LastChange = DateTime.Now;
            myfailure.Service = notify.Destination;
            myfailure.PushUrl = notify.Url;
            myfailure.Status = "open";

            if (exmessage == "invalid_type" || notify.NotifyType == "NOT SUPPORTED")
                myfailure.Status = "not_supported";

            myfailure.RetryCount = myfailure.RetryCount + 1; //CHECK if this works
            myfailure.IsDeleteOperation = notify.IsDelete;
            myfailure.HasImageChanged = notify.HasImagechanged;
            myfailure.Roomschanged = notify.Roomschanged;

            await QueryFactory
                .Query("notificationfailures")
                .Where("id", myfailure.Id)
                .UpdateAsync(new JsonBData() { id = myfailure.Id, data = new JsonRaw(myfailure) });
        }

        //Valid Types for Push
        private static bool CheckValidTypes(NotifyMeta notify)
        {
            if (notify.ValidTypes.Select(x => x.ToLower()).Contains(notify.NotifyType.ToLower()))
                return true;
            else
                return false;
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            //TODO
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        private async Task<IEnumerable<NotifierFailureQueue>> GetFromFailureQueue(
            string service,
            string status,
            int elementstoprocess
        )
        {
            var query = QueryFactory
                .Query("notificationfailures")
                .SelectRaw("data")
                .Where("gen_service", service)
                .Where("gen_status", status)
                .When(elementstoprocess > 0, x => x.Take(elementstoprocess));

            var data = await query.GetObjectListAsync<NotifierFailureQueue>();

            return data;
        }

        public async Task<
            IDictionary<string, ICollection<NotifierResponse>>
        > PushFailureQueueToPublishedonService(
            List<string> publishedonlist,
            int elementstoprocess = 100,
            string? referer = null
        )
        {
            IDictionary<string, ICollection<NotifierResponse>> notifierresponsedict =
                new Dictionary<string, ICollection<NotifierResponse>>();

            foreach (var notifyconfig in notifierconfiglist)
            {
                //GET All failed pushes
                if (publishedonlist.Contains(notifyconfig.ServiceName.ToLower()))
                {
                    var failedpushes = await GetFromFailureQueue(
                        notifyconfig.ServiceName.ToLower(),
                        "open",
                        elementstoprocess
                    );

                    List<NotifierResponse> notifierresponselist = new List<NotifierResponse>();

                    foreach (var failedpush in failedpushes)
                    {
                        var additionalpushinfodict = new Dictionary<string, bool>();

                        if (failedpush.HasImageChanged != null && failedpush.HasImageChanged.Value)
                            additionalpushinfodict.TryAdd("imageschanged", true);

                        if (failedpush.Roomschanged != null && failedpush.Roomschanged.Value)
                            additionalpushinfodict.TryAdd("roomschanged", true);

                        NotifyMetaGenerated meta = new NotifyMetaGenerated(
                            notifyconfig,
                            failedpush.ItemId,
                            failedpush.Type,
                            additionalpushinfodict,
                            false,
                            "failurequeue.push",
                            "api",
                            referer
                        );

                        NotifierResponse notifierresponse = new NotifierResponse();
                        var response = await SendNotify(meta, failedpush);
                        notifierresponse.ObjectId = failedpush.ItemId;
                        notifierresponse.HttpStatusCode = response.Item1;
                        notifierresponse.Service = notifyconfig.ServiceName;
                        notifierresponse.Response = response.Item2;
                        notifierresponse.Success = MapStatusCodeToSuccessProperty(
                            notifierresponse.HttpStatusCode
                        );

                        //TO CHECK if more Elements are pushed it is overwritten
                        notifierresponselist.Add(notifierresponse);
                    }

                    notifierresponsedict.TryAddOrUpdate(
                        notifyconfig.ServiceName,
                        notifierresponselist
                    );
                }
            }

            return notifierresponsedict;
        }

        //Not Updating Images and AccommodationRooms
        public async Task<
            IDictionary<string, ICollection<NotifierResponse>>
        > PushCustomObjectsToPublishedonService(
            List<string> publishedonlist,
            List<string> idlist,
            string odhtype,
            IDictionary<string, bool>? additionalpushinfo,
            string? referer = null
        )
        {
            IDictionary<string, ICollection<NotifierResponse>> notifierresponsedict =
                new Dictionary<string, ICollection<NotifierResponse>>();

            foreach (var notifyconfig in notifierconfiglist)
            {
                //GET All failed pushes
                if (publishedonlist.Contains(notifyconfig.ServiceName.ToLower()))
                {
                    List<NotifierResponse> notifierresponselist = new List<NotifierResponse>();

                    foreach (var id in idlist)
                    {
                        NotifyMetaGenerated meta = new NotifyMetaGenerated(
                            notifyconfig,
                            id,
                            odhtype,
                            null,
                            false,
                            "custom.push",
                            "api",
                            referer
                        );

                        NotifierResponse notifierresponse = new NotifierResponse();
                        var response = await SendNotify(meta, null);
                        notifierresponse.ObjectId = id;
                        notifierresponse.HttpStatusCode = response.Item1;
                        notifierresponse.Service = notifyconfig.ServiceName;
                        notifierresponse.Response = response.Item2;
                        notifierresponse.Success = MapStatusCodeToSuccessProperty(
                            notifierresponse.HttpStatusCode
                        );

                        //TO CHECK if more Elements are pushed it is overwritten
                        notifierresponselist.Add(notifierresponse);
                    }

                    notifierresponsedict.TryAddOrUpdate(
                        notifyconfig.ServiceName,
                        notifierresponselist
                    );
                }
            }

            return notifierresponsedict;
        }
    }

    public class NotifyMetaGenerated : NotifyMeta
    {
        public NotifyMetaGenerated(
            NotifierConfig notifyconfig,
            string id,
            string type,
            IDictionary<string, bool>? additionalpushinfo,
            bool isdelete,
            string updatemode,
            string origin,
            string? referer = null
        )
        {
            //Set by parameters
            this.Id = id;
            this.Type = type;
            this.UdateMode = updatemode;
            this.Origin = origin;
            this.Referer = referer;
            this.HasImagechanged = false;
            this.Roomschanged = false;

            if (additionalpushinfo != null)
            {
                if (additionalpushinfo.ContainsKey("imageschanged"))
                    this.HasImagechanged = additionalpushinfo["imageschanged"];
                if (additionalpushinfo.ContainsKey("roomschanged"))
                    this.Roomschanged = additionalpushinfo["roomschanged"];
            }

            this.IsDelete = isdelete;

            switch (notifyconfig.ServiceName.ToLower())
            {
                case "idm-marketplace":

                    //From Config
                    this.Url = notifyconfig.Url;
                    this.Headers = new Dictionary<string, string>()
                    {
                        { "client_id", notifyconfig.User },
                        { "client_secret", notifyconfig.Password },                       
                    };
                    if (!String.IsNullOrEmpty(notifyconfig.Header))
                        this.Headers.Add(notifyconfig.Header, notifyconfig.Token);

                    //Prefilled
                    this.Destination = "idm-marketplace";
                    this.Mode = "post";

                    this.ValidTypes = new List<string>()
                    {
                        "ACCOMMODATION",
                        "REGION",
                        "MUNICIPALITY",
                        "DISTRICT",
                        "TOURISM_ASSOCIATION",
                        "ODH_ACTIVITY_POI",
                        "EVENT",
                        "ODH_TAG",
                        "SKI_AREA",
                        "TAG",
                    };

                    this.NotifyType = TransformType(this.Type, "idm-marketplace");

                    break;
                case "sinfo":

                    //From Config
                    this.Url = notifyconfig.Url;
                    this.Parameters = new Dictionary<string, string>()
                    {
                        { "skipimage", this.HasImagechanged ? "false" : "true" },
                    };

                    //Prefilled
                    this.Destination = "sinfo";
                    this.Mode = "get";

                    this.ValidTypes = new List<string>()
                    {
                        "accommodation",
                        "activity",
                        "article",
                        "event",
                        "metaregion",
                        "region",
                        "experiencearea",
                        "municipality",
                        "tvs",
                        "district",
                        "skiregion",
                        "skiarea",
                        "gastronomy",
                        "odhactivitypoi",
                        "smgtags",
                        "odhtag",
                        "tag"
                    };

                    this.NotifyType = TransformType(this.Type, "sinfo");

                    break;
                default:
                    this.Mode = "get";
                    this.Url = notifyconfig.Url;
                    break;
            }
        }

        public string TransformType(string type, string servicename)
        {
            if (servicename == "idm-marketplace")
            {
                return type.ToLower() switch
                {
                    "accommodation" => "ACCOMMODATION",
                    "activity" => "ODH_ACTIVITY_POI", //deprecated
                    "article" => "NOT SUPPORTED", //Recipes (only 1 time import in AEM)
                    "event" => "EVENT",
                    "metaregion" => "NOT SUPPORTED", //to check
                    "region" => "REGION",
                    "experiencearea" => "NOT SUPPORTED", //to check
                    "municipality" => "MUNICIPALITY",
                    "tvs" => "TOURISM_ASSOCIATION",
                    "tv" => "TOURISM_ASSOCIATION",
                    "tourismassociation" => "TOURISM_ASSOCIATION",
                    "district" => "DISTRICT",
                    "skiregion" => "NOT SUPPORTED", //to check
                    "skiarea" => "SKI_AREA",
                    "gastronomy" => "ODH_ACTIVITY_POI", //deprecated
                    "poi" => "ODH_ACTIVITY_POI", //deprecated
                    "odhactivitypoi" => "ODH_ACTIVITY_POI",
                    "odhtag" => "ODH_TAG",                    
                    "tag" => "TAG", 
                    "ski_area" => "SKI_AREA",
                    "odh_activity_poi" => "ODH_ACTIVITY_POI",
                    "webcam" => "NOT SUPPORTED",
                    "measuringpoint" => "NOT SUPPORTED",
                    "venue" => "NOT SUPPORTED",
                    "wine" => "NOT SUPPORTED",
                    _ => "NOT SUPPORTED",
                };
            }
            else
                return type;
        }
    }
}
