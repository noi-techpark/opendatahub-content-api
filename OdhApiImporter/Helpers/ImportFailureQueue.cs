// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Helper;
using OdhNotifier;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    /// <summary>
    /// Stores failed single imports (LTS pushes) in the table importfailures, so they can be reprocessed.
    /// One row per source/type/id, a new failure of the same object updates the row.
    /// A successful import of the object removes the row.
    /// </summary>
    public static class ImportFailureQueue
    {
        public const string Table = "importfailures";

        //Datatypes supported by LTSAPIImportHelper.UpdateSingleDataFromLTSApi
        private static readonly List<string> ltsdatatypes = new List<string>()
        {
            "event",
            "gastronomy",
            "poi",
            "activity",
            "venue",
            "measuringpoint",
            "webcam",
            "accommodation",
        };

        public static bool IsRetryable(string source, string type)
        {
            return source == "lts" && ltsdatatypes.Contains(type.ToLower());
        }

        //Some importers do not throw but return the error in the UpdateDetail (e.g. LTS not reachable)
        public static bool HasErrors(UpdateDetail updatedetail)
        {
            return updatedetail.error != null && updatedetail.error > 0;
        }

        /// <summary>
        /// Writes the failure if the import returned errors, otherwise removes a stored failure of this object
        /// </summary>
        public static async Task WriteOrResolveFailure(
            QueryFactory queryFactory,
            string source,
            string type,
            string itemid,
            UpdateDetail updatedetail,
            string editedby,
            string importerurl
        )
        {
            if (HasErrors(updatedetail))
                await WriteFailure(
                    queryFactory,
                    source,
                    type,
                    itemid,
                    !String.IsNullOrEmpty(updatedetail.exception) ? updatedetail.exception : "Import returned errors",
                    editedby,
                    importerurl
                );
            else
                await ResolveFailure(queryFactory, source, type, itemid);
        }

        private static string GetId(string source, string type, string itemid)
        {
            return $"{source}_{type}_{itemid}".ToLower();
        }

        public static async Task WriteFailure(
            QueryFactory queryFactory,
            string source,
            string type,
            string itemid,
            string exception,
            string editedby,
            string importerurl
        )
        {
            //Writing to the queue must never break the import response
            try
            {
                if (!IsRetryable(source, type))
                    return;

                var id = GetId(source, type, itemid);

                var failure = await queryFactory
                    .Query(Table)
                    .Select("data")
                    .Where("id", id)
                    .GetObjectSingleAsync<ImportFailure>();

                if (failure == null)
                {
                    failure = new ImportFailure()
                    {
                        Id = id,
                        ItemId = itemid,
                        Type = type.ToLower(),
                        Source = source,
                        FirstFailure = DateTime.Now,
                    };
                    failure.FailureCount = 1;
                    SetFailureData(failure, exception, editedby, importerurl);

                    await queryFactory
                        .Query(Table)
                        .InsertAsync(new JsonBData() { id = id, data = new JsonRaw(failure) });
                }
                else
                {
                    //A new failure from a push is a new change on the source, so it gets retried again from the beginning
                    failure.FailureCount = failure.FailureCount + 1;
                    failure.RetryCount = 0;
                    SetFailureData(failure, exception, editedby, importerurl);

                    await queryFactory
                        .Query(Table)
                        .Where("id", id)
                        .UpdateAsync(new JsonBData() { id = id, data = new JsonRaw(failure) });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ImportFailureQueue: could not write failure for {source} {type} {itemid}: {ex.Message}");
            }
        }

        public static async Task ResolveFailure(
            QueryFactory queryFactory,
            string source,
            string type,
            string itemid
        )
        {
            try
            {
                if (!IsRetryable(source, type))
                    return;

                await queryFactory.Query(Table).Where("id", GetId(source, type, itemid)).DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ImportFailureQueue: could not resolve failure for {source} {type} {itemid}: {ex.Message}");
            }
        }

        /// <summary>
        /// Retries the open failures, oldest first. Returns a result per failure id.
        /// </summary>
        public static async Task<IDictionary<string, string>> RetryOpenFailures(
            ISettings settings,
            QueryFactory queryFactory,
            IOdhPushNotifier odhpushnotifier,
            int elementstoprocess,
            int maxretries,
            CancellationToken cancellationToken
        )
        {
            var results = new Dictionary<string, string>();

            var failures = await queryFactory
                .Query(Table)
                .Select("data")
                .WhereRaw("data->>'Status' = $$", "open")
                .OrderByRaw("data->>'LastChange'")
                .Limit(elementstoprocess)
                .GetObjectListAsync<ImportFailure>();

            foreach (var failure in failures.ToList())
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                failure.RetryCount = failure.RetryCount + 1;
                failure.LastRetry = DateTime.Now;

                try
                {
                    LTSAPIImportHelper ltsapiimporthelper = new LTSAPIImportHelper(
                        settings,
                        queryFactory,
                        failure.ImporterURL,
                        failure.EditedBy,
                        odhpushnotifier
                    );

                    var resulttuple = await ltsapiimporthelper.UpdateSingleDataFromLTSApi(
                        failure.ItemId,
                        failure.Type,
                        cancellationToken
                    );

                    if (HasErrors(resulttuple.Item2))
                        throw new Exception(
                            !String.IsNullOrEmpty(resulttuple.Item2.exception) ? resulttuple.Item2.exception : "Import returned errors"
                        );

                    await queryFactory.Query(Table).Where("id", failure.Id).DeleteAsync();

                    GenericResultsHelper.GetSuccessUpdateResult(
                        resulttuple.Item1,
                        "retryapi",
                        failure.EditedBy,
                        "Update LTS",
                        "single.retry",
                        "Update LTS Retry succeeded",
                        failure.Type,
                        resulttuple.Item2,
                        true
                    );

                    results.Add(failure.Id, "succeeded");
                }
                catch (Exception ex)
                {
                    failure.Exception = ex.Message;
                    failure.LastChange = DateTime.Now;
                    if (failure.RetryCount >= maxretries)
                        failure.Status = "failed";

                    await queryFactory
                        .Query(Table)
                        .Where("id", failure.Id)
                        .UpdateAsync(new JsonBData() { id = failure.Id, data = new JsonRaw(failure) });

                    GenericResultsHelper.GetErrorUpdateResult(
                        failure.ItemId,
                        "retryapi",
                        failure.EditedBy,
                        "Update LTS",
                        "single.retry",
                        "Update LTS Retry failed",
                        failure.Type,
                        default(UpdateDetail),
                        ex,
                        true
                    );

                    results.Add(failure.Id, failure.Status == "failed" ? "failed, max retries reached" : "failed");
                }
            }

            return results;
        }

        private static void SetFailureData(
            ImportFailure failure,
            string exception,
            string editedby,
            string importerurl
        )
        {
            failure.Exception = exception;
            failure.EditedBy = editedby;
            failure.ImporterURL = importerurl;
            failure.Status = "open";
            failure.LastChange = DateTime.Now;
        }
    }
}
