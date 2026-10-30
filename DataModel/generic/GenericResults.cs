// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataModel
{
    public struct UpdateResult
    {
        public string operation { get; init; }
        public string updatetype { get; init; }
        public string otherinfo { get; init; }
        public string message { get; init; }
        public bool success { get; init; }
        public int? recordsmodified { get; init; }

        public int? updated { get; init; }
        public int? created { get; init; }
        public int? deleted { get; init; }

        public int? objectcompared { get; init; }
        public int? objectchanged { get; init; }
        public int? objectimagechanged { get; init; }

        public dynamic? objectchanges { get; init; }
        public string? objectchangestring { get; init; }

        //Push Infos
        public ICollection<string>? pushchannels { get; init; }

        //All push responses grouped by channel
        public IDictionary<string, ICollection<NotifierResponse>>? pushed { get; init; }

        public int? error { get; init; }

        public string id { get; init; }

        public string exception { get; init; }

        public string stacktrace { get; init; }

        public string source { get; init; }

        public string editedby { get; init; }
    }

    public struct UpdateDetail
    {
        //Record Info (set on single record operations)
        public string id { get; init; }
        public string? odhtype { get; init; }
        public string operation { get; init; }

        //Crud
        public int? updated { get; init; }
        public int? created { get; init; }
        public int? deleted { get; init; }

        //Error
        public int? error { get; init; }

        //Comparision
        public int? comparedobjects { get; init; }
        public int? objectchanged { get; init; }
        public int? objectimagechanged { get; init; }

        public JToken? changes { get; init; }

        //Push Infos
        public ICollection<string>? pushchannels { get; init; }

        public IDictionary<string, NotifierResponse>? pushed { get; set; }

        public string? exception { get; set; }

        public static UpdateDetail Default => new UpdateDetail
        {
            created = 0,
            updated = 0,
            deleted = 0,
            error = 0,
            comparedobjects = 0,
            objectchanged = 0,
            objectimagechanged = 0,
            pushchannels = new List<string>(),
            pushed = new Dictionary<string, NotifierResponse>()
        };
    }

    public struct JsonGenerationResult
    {
        public string operation { get; init; }
        public string type { get; init; }
        public string message { get; init; }
        public bool success { get; init; }
        public string exception { get; init; }
    }

    public class GenericResultsHelper
    {
        /// <summary>
        /// Merges multiple UpdateDetails into one (counters are summed, null counters are ignored)
        /// </summary>
        public static UpdateDetail MergeUpdateDetail(IEnumerable<UpdateDetail> updatedetails)
        {
            var details = updatedetails.ToList();

            return new UpdateDetail()
            {
                created = SumOrNull(details.Select(x => x.created)),
                updated = SumOrNull(details.Select(x => x.updated)),
                deleted = SumOrNull(details.Select(x => x.deleted)),
                error = SumOrNull(details.Select(x => x.error)),
                comparedobjects = SumOrNull(details.Select(x => x.comparedobjects)),
                //objectchanged / objectimagechanged are 0 (not null) if no detail has a value
                objectchanged = details.Sum(x => x.objectchanged ?? 0),
                objectimagechanged = details.Sum(x => x.objectimagechanged ?? 0),
                pushchannels = MergePushChannels(details) ?? new List<string>(),
                pushed = MergePushedSingle(details),
                changes = MergeChanges(details),
                exception = MergeExceptions(details),
            };
        }

        public static UpdateDetail MergeUpdateDetail(IDictionary<string, UpdateDetail> updatedetaildict)
        {
            return MergeUpdateDetail(updatedetaildict.Values);
        }

        /// <summary>
        /// Creates the UpdateResult (logged and returned) out of one or more UpdateDetails.
        /// Counters are summed, push responses are grouped by channel.
        /// success is false if an Exception is passed, can be overwritten with the success parameter.
        /// </summary>
        public static UpdateResult GetUpdateResult(
            string? id,
            string source,
            string editedby,
            string operation,
            string updatetype,
            string message,
            string otherinfo,
            IEnumerable<UpdateDetail>? updatedetails,
            Exception? ex,
            bool createlog,
            IDictionary<string, ICollection<NotifierResponse>>? pushed = null,
            bool? success = null
        )
        {
            var details = updatedetails?.ToList() ?? new List<UpdateDetail>();

            int? created = SumOrNull(details.Select(x => x.created));
            int? updated = SumOrNull(details.Select(x => x.updated));
            int? deleted = SumOrNull(details.Select(x => x.deleted));

            var changes = MergeChanges(details);

            var result = new UpdateResult()
            {
                id = id,
                source = source,
                editedby = editedby,
                operation = operation,
                updatetype = updatetype,
                otherinfo = otherinfo,
                message = message,
                recordsmodified = created + updated + deleted,
                created = created,
                updated = updated,
                deleted = deleted,
                objectcompared = SumOrNull(details.Select(x => x.comparedobjects)),
                objectchanged = SumOrNull(details.Select(x => x.objectchanged)),
                objectimagechanged = SumOrNull(details.Select(x => x.objectimagechanged)),
                objectchanges = null,
                objectchangestring = ex == null && changes != null ? changes.ToString(Formatting.None) : null,
                pushchannels = MergePushChannels(details) ?? pushed?.Keys,
                pushed = MergePushed(details, pushed),
                error = SumOrNull(details.Select(x => x.error)),
                success = success ?? ex == null,
                exception = ex != null ? ex.Message : MergeExceptions(details),
                stacktrace = ex?.StackTrace,
            };

            if (createlog)
                Console.WriteLine(JsonConvert.SerializeObject(result));

            return result;
        }

        public static UpdateResult GetSuccessUpdateResult(
            string? id,
            string source,
            string editedby,
            string operation,
            string updatetype,
            string message,
            string otherinfo,
            UpdateDetail detail,
            bool createlog
        )
        {
            return GetUpdateResult(id, source, editedby, operation, updatetype, message, otherinfo, new List<UpdateDetail>() { detail }, null, createlog);
        }

        public static UpdateResult GetErrorUpdateResult(
            string? id,
            string source,
            string editedby,
            string operation,
            string updatetype,
            string message,
            string otherinfo,
            UpdateDetail detail,
            Exception ex,
            bool createlog
        )
        {
            return GetUpdateResult(id, source, editedby, operation, updatetype, message, otherinfo, new List<UpdateDetail>() { detail }, ex, createlog);
        }

        //Sum ignoring null values, null if all values are null
        private static int? SumOrNull(IEnumerable<int?> values)
        {
            return values.Any(x => x != null) ? values.Sum() : null;
        }

        private static ICollection<string>? MergePushChannels(IList<UpdateDetail> details)
        {
            if (details.Count == 1)
                return details[0].pushchannels;

            var channels = details.Where(x => x.pushchannels != null).SelectMany(x => x.pushchannels!).Distinct().ToList();
            return channels.Count > 0 || details.Any(x => x.pushchannels != null) ? channels : null;
        }

        //UpdateDetail holds one response per channel, on merge the responses of the same channel are joined
        private static IDictionary<string, NotifierResponse>? MergePushedSingle(IList<UpdateDetail> details)
        {
            var response = new Dictionary<string, NotifierResponse>();

            foreach (var pushed in details.Where(x => x.pushed != null).SelectMany(x => x.pushed!))
            {
                if (response.TryGetValue(pushed.Key, out var existing))
                {
                    response[pushed.Key] = new NotifierResponse()
                    {
                        Response = existing.Response,
                        HttpStatusCode = existing.HttpStatusCode,
                        Success = existing.Success,
                        ObjectId = existing.ObjectId + "," + pushed.Value.ObjectId,
                        Service = existing.Service + "," + pushed.Value.Service,
                    };
                }
                else
                {
                    response.Add(pushed.Key, pushed.Value);
                }
            }

            return response.Count > 0 ? response : null;
        }

        //UpdateResult holds all responses per channel
        private static IDictionary<string, ICollection<NotifierResponse>>? MergePushed(
            IList<UpdateDetail> details,
            IDictionary<string, ICollection<NotifierResponse>>? pushed
        )
        {
            var response = new Dictionary<string, ICollection<NotifierResponse>>();

            foreach (var kvp in details.Where(x => x.pushed != null).SelectMany(x => x.pushed!))
            {
                if (!response.ContainsKey(kvp.Key))
                    response.Add(kvp.Key, new List<NotifierResponse>());
                response[kvp.Key].Add(kvp.Value);
            }

            if (pushed != null)
            {
                foreach (var kvp in pushed)
                {
                    if (!response.ContainsKey(kvp.Key))
                        response.Add(kvp.Key, new List<NotifierResponse>());
                    foreach (var notifierresponse in kvp.Value)
                        response[kvp.Key].Add(notifierresponse);
                }
            }

            //Keep an empty pushed object if a single detail passed an empty one
            if (response.Count == 0 && !details.Any(x => x.pushed != null) && pushed == null)
                return null;

            return response;
        }

        //One change is kept as it is, multiple changes are collected in an array
        private static JToken? MergeChanges(IList<UpdateDetail> details)
        {
            var changes = details.Where(x => x.changes != null).Select(x => x.changes!).ToList();

            if (changes.Count == 0)
                return null;
            if (changes.Count == 1)
                return changes[0];

            return new JArray(changes);
        }

        private static string? MergeExceptions(IList<UpdateDetail> details)
        {
            if (details.Count == 1)
                return details[0].exception;

            var exceptions = details.Where(x => !String.IsNullOrEmpty(x.exception)).Select(x => x.exception).ToList();
            return exceptions.Count > 0 ? String.Join(",", exceptions) : null;
        }
        public static JsonGenerationResult GetSuccessJsonGenerateResult(
            string operation,
            string type,
            string message,
            bool createlog
        )
        {
            var result = new JsonGenerationResult()
            {
                operation = operation,
                type = type,
                message = message,
                success = true,
                exception = null,
            };

            if (createlog)
                Console.WriteLine(JsonConvert.SerializeObject(result));

            return result;
        }

        public static JsonGenerationResult GetErrorJsonGenerateResult(
            string operation,
            string type,
            string message,
            Exception ex,
            bool createlog
        )
        {
            var result = new JsonGenerationResult()
            {
                operation = operation,
                type = type,
                message = message,
                success = true,
                exception = ex.Message,
            };

            if (createlog)
                Console.WriteLine(JsonConvert.SerializeObject(result));

            return result;
        }
    }

    #region Pushnotifications

    public class NotifyLog
    {
        public string message { get; set; }
        public string id { get; set; }
        public string origin { get; set; }
        public string destination { get; set; }
        public bool? imageupdate { get; set; }
        public bool? roomsupdate { get; set; }
        public string updatemode { get; set; }

        public object? response { get; set; }

        public string? exception { get; set; }

        public bool? success { get; set; }
    }

    public class NotifierFailureQueue
    {
        public string Id { get; set; }
        public bool? HasImageChanged { get; set; }
        public bool? Roomschanged { get; set; }
        public bool? IsDeleteOperation { get; set; }
        public string ItemId { get; set; }
        public string Type { get; set; }
        public string NotifyType { get; set; }
        public string Exception { get; set; }
        public string Status { get; set; }
        public string PushUrl { get; set; }
        public string Service { get; set; }
        public DateTime LastChange { get; set; }
        public Nullable<int> RetryCount { get; set; }
    }

    public class NotifierResponse
    {
        public object? Response { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public string Service { get; set; }
        public bool Success { get; set; }
        public string? ObjectId { get; set; }
    }

    public class IdmMarketPlacePushResponse
    {
        public string notificationId { get; set; }
    }

    public class EqualityResult
    {
        public bool isequal { get; set; }

        //public IList<Operation>? operations {get;set;}
        public JToken? patch { get; set; }
    }

    public class BatchCRUDResult
    {
        /// <summary>
        /// Indicates if the entire batch operation succeeded (all items processed successfully)
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the batch operation failed (transaction was rolled back)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Total number of items in the batch
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Number of items created (0 if transaction rolled back)
        /// </summary>
        public int Created { get; set; }

        /// <summary>
        /// Number of items updated (0 if transaction rolled back)
        /// </summary>
        public int Updated { get; set; }

        /// <summary>
        /// Number of items that were unchanged
        /// </summary>
        public int Unchanged { get; set; }

        /// <summary>
        /// Number of items with errors
        /// </summary>
        public int Errors { get; set; }
    }

    /// <summary>
    /// Custom exception for batch operation validation errors
    /// Contains structured error information that can be converted to ModelState errors
    /// </summary>
    public class BatchValidationException : Exception
    {
        public IDictionary<string, List<string>> ValidationErrors { get; }

        public BatchValidationException(string message, IDictionary<string, List<string>> validationErrors)
            : base(message)
        {
            ValidationErrors = validationErrors;
        }
    }

    #endregion
}
