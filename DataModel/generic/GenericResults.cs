// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public IDictionary<string, NotifierResponse>? pushed { get; init; }

        public int? error { get; init; }

        public string id { get; init; }

        public string exception { get; init; }

        public string stacktrace { get; init; }

        public string source { get; init; }
    }

    public struct UpdateDetail
    {
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
    }

    //TO CHECK if this could be unified

    public struct UpdateResultFailureQueue
    {
        public string operation { get; init; }
        public string updatetype { get; init; }
        public string otherinfo { get; init; }
        public string message { get; init; }
        public bool success { get; init; }
        public int? recordsmodified { get; init; }

        //Push Infos
        public ICollection<string>? pushchannels { get; init; }

        public IDictionary<string, ICollection<NotifierResponse>>? pushed { get; init; }

        public int? error { get; init; }

        public string id { get; init; }

        public string exception { get; init; }

        public string stacktrace { get; init; }

        public string source { get; init; }
    }

    public struct UpdateDetailFailureQueue
    {
        //Error
        public int? error { get; init; }

        //Push Infos
        public ICollection<string>? pushchannels { get; init; }

        public IDictionary<string, ICollection<NotifierResponse>>? pushed { get; set; }
    }

    public struct PGCRUDResult
    {
        public string id { get; init; }

        public string? odhtype { get; init; }
        public string operation { get; init; }
        public int? updated { get; init; }
        public int? created { get; init; }
        public int? deleted { get; init; }

        public int? error { get; init; }

        public string? errorreason { get; init; }

        public bool? compareobject { get; init; }
        public int? objectchanged { get; init; }
        public int? objectimagechanged { get; init; }

        public ICollection<string>? pushchannels { get; init; }

        public JToken? changes { get; init; }

        public IDictionary<string, NotifierResponse>? pushed { get; set; }

        public static PGCRUDResult Default => new PGCRUDResult
        {
            created = 0,
            updated = 0,
            deleted = 0,
            error = 0,
            compareobject = false,
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
        public static UpdateDetail MergeUpdateDetail(IEnumerable<UpdateDetail> updatedetails)
        {
            int? updated = 0;
            int? created = 0;
            int? deleted = 0;
            int? error = 0;
            int? objectscompared = 0;
            int? objectchanged = 0;
            int? objectimagechanged = 0;
            List<string>? channelstopush = new List<string>();
            string? exception = null;

            JToken? changes = null;

            IDictionary<string, NotifierResponse> pushed =
                new Dictionary<string, NotifierResponse>();

            foreach (var updatedetail in updatedetails)
            {
                objectscompared = updatedetail.comparedobjects + objectscompared;

                created = updatedetail.created + created;
                updated = updatedetail.updated + updated;
                deleted = updatedetail.deleted + deleted;
                error = updatedetail.error + error;
                if (updatedetail.objectchanged != null)
                    objectchanged = updatedetail.objectchanged + objectchanged;
                if (updatedetail.objectimagechanged != null)
                    objectimagechanged = updatedetail.objectimagechanged + objectimagechanged;

                if (updatedetail.changes != null)
                {
                    if (changes == null)
                        changes = updatedetail.changes;
                    else
                        changes.Append(updatedetail.changes);
                }

                if (updatedetail.pushchannels != null)
                {
                    foreach (var pushchannel in updatedetail.pushchannels)
                    {
                        if (!channelstopush.Contains(pushchannel))
                            channelstopush.Add(pushchannel);
                    }
                }

                if (updatedetail.pushed != null)
                {
                    foreach (var updatedetailpushed in updatedetail.pushed)
                        pushed.TryAdd(updatedetailpushed.Key, updatedetailpushed.Value);
                }

                if(!String.IsNullOrEmpty(updatedetail.exception))
                {
                    exception = updatedetail.exception + exception;
                }
            }

            return new UpdateDetail()
            {
                created = created,
                updated = updated,
                deleted = deleted,
                error = error,
                comparedobjects = objectscompared,
                objectchanged = objectchanged,
                objectimagechanged = objectimagechanged,
                pushchannels = channelstopush,
                pushed = pushed,
                changes = changes,
                exception = exception
            };
        }

        public static UpdateResult GetSuccessUpdateResult(
            string id,
            string source,
            string operation,
            string updatetype,
            string message,
            string otherinfo,
            UpdateDetail detail,
            bool createlog
        )
        {
            var result = new UpdateResult()
            {
                id = id,
                source = source,
                operation = operation,
                updatetype = updatetype,
                otherinfo = otherinfo,
                message = message,
                recordsmodified = (detail.created + detail.updated + detail.deleted),
                created = detail.created,
                updated = detail.updated,
                deleted = detail.deleted,
                objectcompared = detail.comparedobjects,
                objectchanged = detail.objectchanged,
                objectimagechanged = detail.objectimagechanged,
                //objectchanges = detail.changes != null ? JsonConvert.DeserializeObject<dynamic>(detail.changes.ToString(Formatting.None)) : null,
                objectchanges = null,
                objectchangestring =
                    detail.changes != null ? detail.changes.ToString(Formatting.None) : null,
                pushchannels = detail.pushchannels,
                pushed = detail.pushed,
                error = detail.error,
                success = true,
                exception = detail.exception,
                stacktrace = null,
            };

            if (createlog)
                Console.WriteLine(JsonConvert.SerializeObject(result));

            return result;
        }

        public static UpdateResult GetUpdateResultFromPGCRUDResult(           
           string source,
           //string updatetype, not needed single on PGCRUDResult
           string message,
           //string otherinfo, not needed we choose odhtype
           PGCRUDResult pgcrudresult,
           bool createlog
       )
        {
            var result = new UpdateResult()
            {
                id = pgcrudresult.id,
                source = source,
                operation = pgcrudresult.operation,
                updatetype = "single",
                otherinfo = pgcrudresult.odhtype,
                message = message,
                recordsmodified = (pgcrudresult.created + pgcrudresult.updated + pgcrudresult.deleted),
                created = pgcrudresult.created,
                updated = pgcrudresult.updated,
                deleted = pgcrudresult.deleted,
                objectcompared = pgcrudresult.compareobject != null && pgcrudresult.compareobject.Value ? 1 : 0,
                objectchanged = pgcrudresult.objectchanged,
                objectimagechanged = pgcrudresult.objectimagechanged,
                //objectchanges = detail.changes != null ? JsonConvert.DeserializeObject<dynamic>(detail.changes.ToString(Formatting.None)) : null,
                objectchanges = null,
                objectchangestring =
                    pgcrudresult.changes != null ? pgcrudresult.changes.ToString(Formatting.None) : null,
                pushchannels = pgcrudresult.pushchannels,
                pushed = pgcrudresult.pushed,
                error = pgcrudresult.error,
                success =  pgcrudresult.error != null && pgcrudresult.error > 0 ? false : true,
                exception = pgcrudresult.errorreason,
                stacktrace = null,
            };

            if (createlog)
                Console.WriteLine(JsonConvert.SerializeObject(result));

            return result;
        }

        public static UpdateResult GetErrorUpdateResult(
            string id,
            string source,
            string operation,
            string updatetype,
            string message,
            string otherinfo,
            UpdateDetail detail,
            Exception ex,
            bool createlog
        )
        {
            var result = new UpdateResult()
            {
                id = id,
                source = source,
                operation = operation,
                updatetype = updatetype,
                otherinfo = otherinfo,
                message = message,
                recordsmodified = (detail.created + detail.updated + detail.deleted),
                created = detail.created,
                updated = detail.updated,
                deleted = detail.deleted,
                objectcompared = detail.comparedobjects,
                objectchanged = detail.objectchanged,
                objectimagechanged = detail.objectimagechanged,
                objectchanges = null,
                objectchangestring = null,
                pushchannels = detail.pushchannels,
                pushed = detail.pushed,
                error = detail.error,
                success = false,
                exception = ex.Message,
                stacktrace = ex.StackTrace,
            };

            if (createlog)
                Console.WriteLine(JsonConvert.SerializeObject(result));

            return result;
        }       

        public static UpdateResultFailureQueue GetSuccessUpdateResult(
            string id,
            string source,
            string operation,
            string updatetype,
            string message,
            string otherinfo,
            UpdateDetailFailureQueue detail,
            bool createlog
        )
        {
            var result = new UpdateResultFailureQueue()
            {
                id = id,
                source = source,
                operation = operation,
                updatetype = updatetype,
                otherinfo = otherinfo,
                message = message,
                recordsmodified = 0,
                pushchannels = detail.pushchannels,
                pushed = detail.pushed,
                error = detail.error,
                success = true,
                exception = null,
                stacktrace = null,
            };

            if (createlog)
                Console.WriteLine(JsonConvert.SerializeObject(result));

            return result;
        }

        public static UpdateResultFailureQueue GetErrorUpdateResult(
            string id,
            string source,
            string operation,
            string updatetype,
            string message,
            string otherinfo,
            UpdateDetailFailureQueue detail,
            Exception ex,
            bool createlog
        )
        {
            var result = new UpdateResultFailureQueue()
            {
                id = id,
                source = source,
                operation = operation,
                updatetype = updatetype,
                otherinfo = otherinfo,
                message = message,
                recordsmodified = 0,
                pushchannels = detail.pushchannels,
                pushed = detail.pushed,
                error = detail.error,
                success = false,
                exception = ex.Message,
                stacktrace = ex.StackTrace,
            };

            if (createlog)
                Console.WriteLine(JsonConvert.SerializeObject(result));

            return result;
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

    //public class 
}
