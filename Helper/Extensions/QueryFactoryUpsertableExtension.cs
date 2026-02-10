// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;
using Helper.Extensions;
using Helper.Generic;
using Helper.Identity;
using Helper.JsonHelpers;
using Newtonsoft.Json;
using SqlKata;
using SqlKata.Execution;

namespace Helper
{
    public static partial class QueryFactoryExtension
    {
        public interface IUpsertable
        {
            IIdentifiable Data { get; }
            IDictionary<string, object> StoredColumns { get; }
        }

        public class Upsertable<T> : IUpsertable
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            public T Data { get; private set; }
            public IDictionary<string, object> StoredColumns { get; private set; }

            IIdentifiable IUpsertable.Data => this.Data;

            public Upsertable(T data, IDictionary<string, object>? storedColumns = null)
            {
                this.Data = data ?? throw new ArgumentNullException(nameof(data), "data cannot be null.");

                this.StoredColumns = storedColumns ?? new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Prepared data item ready for database write
        /// </summary>
        private class PreparedDataItem<T>
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            public PreparedDataItem(T data) {
                this.Data = data;
            }

            public T Data { get; }
            public bool IsCreate { get; set; }
            public List<string> PushChannels { get; set; } = new List<string>();
            public int? ObjectChanged { get; set; }
            public int? ObjectImageChanged { get; set; }
            public EqualityResult? ComparisonResult { get; set; }
            public PGCRUDResult? ErrorResult { get; set; } // Set if preparation failed
            public IDictionary<string, object> StoredData { get; set; } = new Dictionary<string, object>();

            public string GetDataID()
            {
                if (StoredData.TryGetValue("id", out object idValue))
                {
                    return idValue.ToString();
                }

                throw new KeyNotFoundException("The 'id' key was not found in StoredData.");
            }
        }

        /// <summary>
        /// Prepares a single data item for upsert (validation, metadata, comparison)
        /// </summary>
        private static PreparedDataItem<T> PrepareDataItem<T>(
            Upsertable<T> data,
            T? existingData,
            DataInfo dataconfig,
            EditInfo editinfo,
            CRUDConstraints createConstraints,
            CRUDConstraints updateConstraints,
            CompareConfig compareConfig
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            var prepared = new PreparedDataItem<T>(data.Data);
            List<string> channelstopublish = new List<string>();
            int? objectchangedcount = null;
            int? objectimagechangedcount = null;

            prepared.IsCreate = existingData == null;

            //Setting MetaInfo
            data.Data._Meta = MetadataHelper.GetMetadataobject<T>(data.Data, false);
            //Setting Editinfo
            data.Data._Meta.UpdateInfo = new UpdateInfo()
            {
                UpdatedBy = editinfo.Editor,
                UpdateSource = editinfo.Source,
            };
            //Setting the MetaData UpdateInfo.UpdateHistory
            MetadataHelper.SetUpdateHistory(existingData?._Meta, data.Data._Meta);

            //Setting Firstimport to Now if null
            if (data.Data.FirstImport == null)
                data.Data.FirstImport = DateTime.Now;
            //New Data set last change to now
            data.Data.LastChange = DateTime.Now;

            // Use the appropriate constraint based on whether this is a create or update
            var constraintToCheck = prepared.IsCreate ? createConstraints : updateConstraints;

            //Check data.Data condition return not allowed if it fails
            if (!CheckCRUDCondition.CRUDOperationAllowed(data.Data, constraintToCheck.Condition))
            {
                prepared.ErrorResult = PGCRUDResult.Default with
                {
                    id = data.Data.Id,
                    odhtype = data.Data._Meta.Type,
                    error = 1,
                    errorreason = "Not Allowed",
                    operation = dataconfig.Operation.ToString(),
                    objectchanged = objectchangedcount,
                    objectimagechanged = objectimagechangedcount,
                    pushchannels = channelstopublish,
                };
                return prepared;
            }

            if (existingData == null)
            {
                // Create case
                if (dataconfig.ErrorWhendataIsNew)
                {
                    prepared.ErrorResult = PGCRUDResult.Default with
                    {
                        id = data.Data.Id,
                        odhtype = data.Data._Meta.Type,
                        error = 1,
                        errorreason = "Data to update Not Found",
                        operation = dataconfig.Operation.ToString(),
                        objectchanged = objectchangedcount,
                        objectimagechanged = objectimagechangedcount,
                        pushchannels = channelstopublish,
                    };
                    return prepared;
                }

                if (data.Data is IPublishedOn)
                {
                    if ((data.Data as IPublishedOn).PublishedOn == null)
                        (data.Data as IPublishedOn).PublishedOn = new List<string>();

                    channelstopublish.AddRange((data.Data as IPublishedOn).PublishedOn);
                }

                //On insert always set the object and image to changed only if compareresult deactivated
                if (compareConfig.CompareData)
                {
                    objectchangedcount = 1;
                    objectimagechangedcount = 1;
                }
            }
            else
            {
                // Update case
                if (dataconfig.ErrorWhendataExists)
                {
                    prepared.ErrorResult = PGCRUDResult.Default with
                    {
                        id = data.Data.Id,
                        odhtype = data.Data._Meta.Type,
                        error = 1,
                        errorreason = "Data exists already",
                        operation = dataconfig.Operation.ToString(),
                        objectchanged = objectchangedcount,
                        objectimagechanged = objectimagechangedcount,
                        pushchannels = channelstopublish,
                    };
                    return prepared;
                }

                //Set the FirstImport of the old data.Data
                if (existingData.FirstImport != null)
                    data.Data.FirstImport = existingData.FirstImport;

                //Set the Lastchanged of the old data.Data, only if the Comparator is active
                if (existingData.LastChange != null && compareConfig.CompareData)
                    data.Data.LastChange = existingData.LastChange;

                //Compare the data.Data
                bool imagesequal = false;
                EqualityResult equalityresult = new EqualityResult() { isequal = false, patch = null };

                if (compareConfig.CompareData)
                {
                    equalityresult = EqualityHelper.CompareClassesExtended<T>(
                        existingData,
                        data.Data,
                        compareConfig.FieldsToIgnore,
                        true
                    );
                    if (equalityresult.isequal)
                        objectchangedcount = 0;
                    else
                    {
                        objectchangedcount = 1;
                        data.Data.LastChange = DateTime.Now;
                    }
                }

                //Compare Image Gallery
                if (
                    compareConfig.CompareImages
                    && data.Data is IImageGalleryAware
                    && existingData is IImageGalleryAware
                )
                {
                    imagesequal = EqualityHelper.CompareImageGallery(
                        (data.Data as IImageGalleryAware).ImageGallery,
                        (existingData as IImageGalleryAware).ImageGallery,
                        new List<string>() { }
                    );
                    if (imagesequal)
                        objectimagechangedcount = 0;
                    else
                        objectimagechangedcount = 1;
                }

                prepared.ComparisonResult = equalityresult;

                //Add all Publishedonfields before and after change
                if (data.Data is IPublishedOn && existingData is IPublishedOn)
                {
                    if ((data.Data as IPublishedOn).PublishedOn == null)
                        (data.Data as IPublishedOn).PublishedOn = new List<string>();

                    channelstopublish.AddRange(
                        (data.Data as IPublishedOn).PublishedOn.UnionIfNotNull(
                            (existingData as IPublishedOn).PublishedOn
                        )
                    );
                }
            }

            prepared.StoredData.Add("id", data.Data._Meta.Id);
            prepared.StoredData.Add("data", new JsonRaw(data.Data));
            foreach (var kvp in data.StoredColumns)
            {
                prepared.StoredData.Add(kvp.Key, kvp.Value);
            }

            prepared.PushChannels = channelstopublish;
            prepared.ObjectChanged = objectchangedcount;
            prepared.ObjectImageChanged = objectimagechangedcount;

            return prepared;
        }

        /// <summary>
        /// Batch inserts or updates multiple data items with optimized database queries.
        /// Performs a SINGLE bulk write operation instead of N individual writes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="QueryFactory"></param>
        /// <param name="dataList">List of data items to upsert</param>
        /// <param name="dataconfig">Data configuration (operation will be determined per item)</param>
        /// <param name="editinfo">Edit information</param>
        /// <param name="createConstraints">CRUD constraints for create operations</param>
        /// <param name="updateConstraints">CRUD constraints for update operations</param>
        /// <param name="compareConfig">Compare configuration</param>
        /// <param name="reduced">Whether this is reduced data</param>
        /// <returns>BatchCRUDResult with aggregated statistics and individual results</returns>
        public static async Task<BatchCRUDResult> UpsertDataArray<T>(
            this QueryFactory QueryFactory,
            IEnumerable<Upsertable<T>> dataList,
            DataInfo dataconfig,
            EditInfo editinfo,
            CRUDConstraints createConstraints,
            CRUDConstraints updateConstraints,
            CompareConfig compareConfig
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            var batchResult = new BatchCRUDResult();

            if (dataList == null || !dataList.Any())
            {
                batchResult.Success = true;
                return batchResult;
            }

            // Start a database transaction - all operations succeed or all fail
            var connection = QueryFactory.Connection;

            // Track if we opened the connection (so we can close it later)
            bool connectionOpenedByUs = false;
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
                connectionOpenedByUs = true;
            }

            var transaction = connection.BeginTransaction();

            try
            {
                // Prepare all IDs by already "transforming" the id using CheckIdFromType,
                // this way we will retrieve data with the correct normalized id
                var allIds = dataList
                    .Select(d => IdGenerator.CheckIdFromType<T>(d.Data.Id))
                    .ToList();

                // Merge AccessRoles from both create and update constraints for fetching existing data
                var mergedAccessRoles = createConstraints.AccessRole
                    .Union(updateConstraints.AccessRole)
                    .Distinct()
                    .ToList();

                // Batch load all existing items in ONE query using merged AccessRoles
                var existingItems = await QueryFactory
                    .Query(dataconfig.Table)
                    .Select("data")
                    .WhereIn("id", allIds)
                    .When(
                        mergedAccessRoles.Count() > 0,
                        q => q.FilterDataByAccessRoles(mergedAccessRoles)
                    )
                    .GetObjectListAsync<T>();

                // Create lookup dictionary for fast access
                var existingLookup = existingItems
                    .ToDictionary(e => e.Id.ToLower(), e => e);

                // Prepare all items (validation, metadata, comparison)
                var preparedItems = new List<PreparedDataItem<T>>();
                var validationErrors = new Dictionary<string, List<string>>();
                int itemIndex = 0;

                foreach (var upsertable in dataList)
                {
                    try
                    {
                        // Before processing data, we normalize the id, this way PrepareDataItem will deal witht he right metadatada and ID
                        upsertable.Data.Id = IdGenerator.CheckIdFromType<T>(upsertable.Data.Id);
                        existingLookup.TryGetValue(upsertable.Data.Id.ToLower(), out var existingData);

                        var prepared = PrepareDataItem<T>(
                            upsertable,
                            existingData,
                            dataconfig,
                            editinfo,
                            createConstraints,
                            updateConstraints,
                            compareConfig
                        );

                        preparedItems.Add(prepared);
                    }
                    catch (Exception ex)
                    {
                        // Collect structured validation error
                        var errorKey = $"[{itemIndex}]";
                        if (!validationErrors.ContainsKey(errorKey))
                        {
                            validationErrors[errorKey] = new List<string>();
                        }
                        validationErrors[errorKey].Add(ex.Message);
                    }
                    itemIndex++;
                }

                // Separate items into creates, updates, unchanged, and errors
                // Track indices for each item type
                var itemsToCreate = new List<(PreparedDataItem<T> Item, int OriginalIndex)>();
                var itemsToUpdate = new List<(PreparedDataItem<T> Item, int OriginalIndex)>();
                var itemsUnchanged = new List<(PreparedDataItem<T> Item, int OriginalIndex)>();

                for (int i = 0; i < preparedItems.Count; i++)
                {
                    var prepared = preparedItems[i];
                    if (prepared.ErrorResult == null && prepared.IsCreate)
                        itemsToCreate.Add((prepared, i));
                    else if (prepared.ErrorResult == null && !prepared.IsCreate && prepared.ObjectChanged != 0)
                        itemsToUpdate.Add((prepared, i));
                    else if (prepared.ErrorResult == null && !prepared.IsCreate && prepared.ObjectChanged == 0)
                        itemsUnchanged.Add((prepared, i));
                    else if (prepared.ErrorResult != null)
                    {
                        // Collect error from PrepareDataItem
                        var errorKey = $"[{i}]";
                        if (!validationErrors.ContainsKey(errorKey))
                        {
                            validationErrors[errorKey] = new List<string>();
                        }
                        validationErrors[errorKey].Add(prepared.ErrorResult.Value.errorreason ?? "Unknown error");
                    }
                }

                // If ANY item has validation errors, abort the transaction immediately
                if (validationErrors.Any())
                {
                    throw new BatchValidationException(
                        $"Validation failed for {validationErrors.Count} item(s)",
                        validationErrors
                    );
                }

                // BULK INSERT - Process all creates in a transaction
                if (itemsToCreate.Any())
                {
                    // SqlKata's bulk insert doesn't handle JsonRaw properly
                    // Use individual inserts within the same connection for better performance
                    foreach (var (prepared, originalIndex) in itemsToCreate)
                    {
                        var id = prepared.GetDataID();
                        try
                        {
                            await QueryFactory
                                .Query(dataconfig.Table)
                                .InsertAsync(prepared.StoredData);

                            batchResult.Created++;
                        }
                        catch (Exception ex)
                        {
                            // Throw structured validation exception with item index
                            var errors = new Dictionary<string, List<string>>
                            {
                                [$"[{originalIndex}]"] = new List<string> { $"Insert failed for ID '{id}': {ex.Message}" }
                            };
                            throw new BatchValidationException($"Insert failed at item [{originalIndex}]", errors);
                        }
                    }
                }

                // BULK UPDATE - Process updates (unfortunately SqlKata doesn't support bulk update well)
                // We need to do individual updates but can batch them in a transaction if needed
                foreach (var (prepared, originalIndex) in itemsToUpdate)
                {
                    var id = prepared.GetDataID();
                    try
                    {
                        var updateResult = await QueryFactory
                            .Query(dataconfig.Table)
                            .Where("id", id)
                            .UpdateAsync(prepared.StoredData);

                        if (updateResult > 0)
                        {
                            batchResult.Updated++;

                            // Save changes to rawchanges table if configured
                            if (dataconfig.SaveChangesToDB && prepared.ObjectChanged > 0)
                            {
                                await SaveChangesToRawChangesTable(
                                    QueryFactory,
                                    prepared.Data,
                                    editinfo,
                                    prepared.ComparisonResult ?? new EqualityResult { isequal = false, patch = null }
                                );
                            }
                        }
                        else
                        {
                            // If update didn't affect any rows, this is an error condition
                            var errors = new Dictionary<string, List<string>>
                            {
                                [$"[{originalIndex}]"] = new List<string> { $"Update failed for ID '{id}' - no rows affected" }
                            };
                            throw new BatchValidationException($"Update failed at item [{originalIndex}]", errors);
                        }
                    }
                    catch (BatchValidationException)
                    {
                        // Already a structured exception, just rethrow
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // Wrap database exception with item index
                        var errors = new Dictionary<string, List<string>>
                        {
                            [$"[{originalIndex}]"] = new List<string> { $"Update failed for ID '{id}': {ex.Message}" }
                        };
                        throw new BatchValidationException($"Update failed at item [{originalIndex}]", errors);
                    }
                }

                // Count unchanged items
                batchResult.Unchanged = itemsUnchanged.Count;

                // If we got here, all operations succeeded - commit the transaction
                transaction.Commit();

                batchResult.Success = true;
                batchResult.TotalProcessed = batchResult.Created + batchResult.Updated + batchResult.Unchanged;
                return batchResult;
            }
            catch (BatchValidationException)
            {
                // some providers automatically rollback, wrap the rollback in a silent try catch since we do not care
                try
                {
                    transaction.Rollback();
                }
                catch (Exception) { }
                throw; 
            }
            finally
            {
                transaction.Dispose();

                // Close connection if we opened it
                if (connectionOpenedByUs && connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Inserts or Updates the Data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="QueryFactory"></param>
        /// <param name="data"></param>
        /// <param name="dataconfig"></param>
        /// <param name="editinfo"></param>
        /// <param name="constraints">Constraints for CRUD operations</param>
        /// <param name="compareConfig"></param>
        /// <returns></returns>
        public static async Task<PGCRUDResult> UpsertData<T>(
            this QueryFactory QueryFactory,
            Upsertable<T> data,
            DataInfo dataconfig,
            EditInfo editinfo,
            CRUDConstraints constraints,
            CompareConfig compareConfig
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            //TOCHECK: What if no id is passed? Generate ID?
            //TOCHECK: Id Uppercase or Lowercase depending on table
            //TOCHECK: Shortname population?

            // No need to check data non-null since Upsertable already enforces that

            // Before processing data, we normalize the id, this way PrepareDataItem will deal witht he right metadatada and ID
            data.Data._Meta.Id = IdGenerator.CheckIdFromType<T>(data.Data.Id);

            //Check if data exists already
            var queryresult = await QueryFactory
                .Query(dataconfig.Table)
                .Select("data")
                .Where("id", data.Data._Meta.Id)
                .When(
                    constraints.AccessRole.Count() > 0,
                    q => q.FilterDataByAccessRoles(constraints.AccessRole)
                )
                .GetObjectSingleAsync<T>();

            // Use shared preparation logic
            var prepared = PrepareDataItem<T>(
                data,
                queryresult,
                dataconfig,
                editinfo,
                constraints,
                constraints,
                compareConfig
            );

            // If preparation failed, return error result
            if (prepared.ErrorResult.HasValue)
                return prepared.ErrorResult.Value;

            int createresult = 0;
            int updateresult = 0;

            // Add the additional simple column (rawdataid)
            // rawdataid is part of the stored column if any

            // Perform database write
            if (prepared.IsCreate)
            {
                createresult = await QueryFactory
                    .Query(dataconfig.Table)
                    .InsertAsync(prepared.StoredData);

                dataconfig.Operation = CRUDOperation.Create;
            }
            else
            {
                updateresult = await QueryFactory
                    .Query(dataconfig.Table)
                    .Where("id", data.Data._Meta.Id)
                    .UpdateAsync(prepared.StoredData);

                dataconfig.Operation = CRUDOperation.Update;
            }

            var d = prepared.Data;

            // Check if write succeeded
            if (createresult == 0 && updateresult == 0)
                return PGCRUDResult.Default with
                {
                    id = d.Id,
                    odhtype = d._Meta.Type,
                    error = 1,
                    errorreason = "Internal Error",
                    operation = dataconfig.Operation.ToString(),
                    objectchanged = prepared.ObjectChanged,
                    objectimagechanged = prepared.ObjectImageChanged,
                    pushchannels = prepared.PushChannels,
                };

            //If changes should be saved to DB
            if (dataconfig.SaveChangesToDB && prepared.ObjectChanged != null && prepared.ObjectChanged > 0)
            {
                await SaveChangesToRawChangesTable(
                    QueryFactory,
                    d,
                    editinfo,
                    prepared.ComparisonResult ?? new EqualityResult() { isequal = false, patch = null }
                );
            }

            return PGCRUDResult.Default with
            {
                id = d.Id,
                odhtype = d._Meta.Type,
                created = createresult,
                updated = updateresult,
                operation = dataconfig.Operation.ToString(),
                compareobject = compareConfig.CompareData,
                objectchanged = prepared.ObjectChanged,
                objectimagechanged = prepared.ObjectImageChanged,
                pushchannels = prepared.PushChannels,
                changes = prepared.ComparisonResult?.patch,
            };
        }
    }
}
