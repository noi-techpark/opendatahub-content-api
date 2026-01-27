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
        public int? recordsmodified { get { return (this.updated ?? 0) + (this.created ?? 0) + (this.deleted ?? 0); } }

        public int? updated { get; init; }
        public int? created { get; init; }
        public int? deleted { get; init; }

        public int? objectcompared { get; init; }
        public int? objectchanged { get; init; }
        public int? objectimagechanged { get; init; }

        public ICollection<JToken>? objectchanges { get; init; }
        public ICollection<string> objectchangestring { get {
            return objectchanges != null && objectchanges.Count > 0 ?
                    objectchanges.Select(x => x.ToString()).ToList()
                    : null;
                    } }

        //Push Infos
        public ICollection<string>? pushchannels { get; init; }

        //public IDictionary<string, NotifierResponse>? pushed { get; init; }

        public IDictionary<string, ICollection<NotifierResponse>>? pushed { get; init; }

        public int? error { get; init; }

        public string id { get; init; }

        public string exception { get; init; }

        public string stacktrace { get; init; }

        public string source { get; init; }

        public bool success { get { return (this.error != null && this.error > 0 && this.exception.Count() > 0) ? false : true; } }
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
        public int? objectcompared { get; init; }
        public int? objectchanged { get; init; }
        public int? objectimagechanged { get; init; }

        public JToken? changes { get; init; }

        //Push Infos
        public ICollection<string>? pushchannels { get; init; }

        public IDictionary<string, NotifierResponse>? pushed { get; set; }

        public string? exception { get; set; }

        //from PGCrudResult
        public string id { get; init; }
        public string? type { get; init; }
        public string operation { get; init; }

        public static UpdateDetail Default => new UpdateDetail
        {
            created = 0,
            updated = 0,
            deleted = 0,
            error = 0,
            objectcompared = 0,
            objectchanged = 0,
            objectimagechanged = 0,
            pushchannels = new List<string>(),
            pushed = new Dictionary<string, NotifierResponse>()
        };
    }
    
    public class GenericResultsHelper
    {
        public static UpdateResult GetUpdateResult(
            string? id,
            string source,
            string operation,
            string updatetype,
            string message,
            string otherinfo,
            IEnumerable<UpdateDetail> updatedetails,
            Exception? ex,
            bool createlog
        )
        {
            var exceptions = new List<string>();

            if (ex?.Message != null)
                exceptions.Add(ex.Message);

            if (updatedetails != null)
            {
                exceptions.AddRange(
                    updatedetails
                        .Select(x => x.exception)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                );
            }
            int errorcount = 0;
            if (ex != null)
                errorcount = 1;


            var result = new UpdateResult()
            {
                id =  String.IsNullOrEmpty(id) && updatedetails != null ? String.Join(",", updatedetails.Select(x => x.id)) : id,
                source = source,
                operation = operation,
                updatetype = updatetype,
                otherinfo = otherinfo,
                message = message,

                created = updatedetails != null ? updatedetails.Sum(x => x.created) : 0,
                updated = updatedetails != null ? updatedetails.Sum(x => x.updated) : 0,
                deleted = updatedetails != null ? updatedetails.Sum(x => x.deleted) : 0,
                objectcompared = updatedetails != null ? updatedetails.Sum(x => x.objectcompared) : 0,
                objectchanged = updatedetails != null ? updatedetails.Sum(x => x.objectchanged) : 0,
                objectimagechanged = updatedetails != null ? updatedetails.Sum(x => x.objectimagechanged) : 0,

                objectchanges = updatedetails != null ? updatedetails.Select(x => x.changes).ToList() : null,

                pushchannels = updatedetails != null ? updatedetails.SelectMany(x => x.pushchannels).Distinct().ToList() : null, 
                pushed = updatedetails.Where(x => x.pushed != null)
                                    .SelectMany(x => x.pushed!)
                                    .GroupBy(kvp => kvp.Key, kvp => kvp.Value)
                                    .ToDictionary(
                                        g => g.Key,
                                        g => (ICollection<NotifierResponse>)g.ToList()
                                    ),
                error = updatedetails != null ? updatedetails.Sum(x => x.error) + errorcount : errorcount,
                exception = String.Join(",", exceptions),
                stacktrace = ex.StackTrace
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

        //Obsolete?
        public static UpdateDetail MergeUpdateDetailIntoOne(IEnumerable<UpdateDetail> updatedetails)
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
                objectscompared = updatedetail.objectcompared + objectscompared;

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

                if (!String.IsNullOrEmpty(updatedetail.exception))
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
                objectcompared = objectscompared,
                objectchanged = objectchanged,
                objectimagechanged = objectimagechanged,
                pushchannels = channelstopush,
                pushed = pushed,
                changes = changes,
                exception = exception
            };
        }
    }

    


    #region Json Generation

    public struct JsonGenerationResult
    {
        public string operation { get; init; }
        public string type { get; init; }
        public string message { get; init; }
        public bool success { get; init; }
        public string exception { get; init; }
    }

    #endregion    

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

    #endregion

    #region Comparator

    public class EqualityResult
    {
        public bool isequal { get; set; }

        //public IList<Operation>? operations {get;set;}
        public JToken? patch { get; set; }
    }

    #endregion

    #region Batch Update

    public class BatchUpdateResult
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
