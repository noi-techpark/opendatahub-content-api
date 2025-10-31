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
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using SqlKata;
using SqlKata.Execution;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Helper
{
    public static class QueryFactoryExtension
    {
        #region Query Extension Methods Common used

        //Duplicates?

        //public static async Task<T?> GetFirstOrDefaultAsObject<T>(this Query query) {

        //    var rawdata = await query.FirstOrDefaultAsync<JsonRaw>();
        //    return rawdata != null ? JsonConvert.DeserializeObject<T>(rawdata.Value) : default(T);
        //}

        //public static async Task<IEnumerable<T>> GetAllAsObject<T>(this Query query)
        //{
        //    var rawdatalist = await query.GetAsync<JsonRaw>();
        //    List<T> datalist = new List<T>();

        //    foreach (var rawdata in rawdatalist)
        //    {
        //        var value = JsonConvert.DeserializeObject<T>(rawdata.Value);
        //        if (value != null)
        //            datalist.Add(value);
        //    }
        //    return datalist;
        //}

        #endregion


        //Using Newtonsoft
        public static async Task<T?> GetObjectSingleAsync<T>(
            this Query query,
            CancellationToken cancellationToken = default
        )
            where T : notnull
        {
            //using this ContractResolver avoids duplicate Lists
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new GetOnlyContractResolver(),
            };

            var result = await query.FirstOrDefaultAsync<JsonRaw>();
            return result != null
                ? JsonConvert.DeserializeObject<T>(result.Value, settings)
                : default!;
            //return JsonConvert.DeserializeObject<T>(result.Value, settings) ?? default(T);
        }

        //Using System.Text.Json --> producing Exception when single object is not found!
        //public static async Task<T> GetObjectSingleAsyncV2<T>(this Query query, CancellationToken cancellationToken = default) where T : notnull
        //{
        //    var result = await query.FirstOrDefaultAsync<JsonRaw>();
        //    return System.Text.Json.JsonSerializer.Deserialize<T>(result.Value) ?? default!;
        //}

        //Using Newtonsoft
        public static async Task<IEnumerable<T>> GetObjectListAsync<T>(
            this Query query,
            CancellationToken cancellationToken = default
        )
            where T : notnull
        {
            //using this ContractResolver avoids duplicate Lists
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new GetOnlyContractResolver(),
            };

            var result = await query.GetAsync<JsonRaw>();
            return result.Select(x => JsonConvert.DeserializeObject<T>(x.Value, settings)!)
                ?? default!;
        }

        #region Using Reflection
        public static async Task<IIdentifiable?> GetObjectSingleAsync<T>(
            this Query query,
            Type type,
            CancellationToken cancellationToken = default
        )
        {
            //using this ContractResolver avoids duplicate Lists
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new GetOnlyContractResolver(),
            };
            var method = typeof(JsonConvert)
                .GetMethods()
                .Where(x => x.Name == "DeserializeObject")
                .Where(x => x.IsGenericMethod)
                .Where(x => x.GetParameters().Any(x => x.Name!.Equals("settings")));

            var resultraw = await query.FirstOrDefaultAsync<JsonRaw>();

            var methodinfo = method.FirstOrDefault()!.MakeGenericMethod(type);
            //var parseddata = methodinfo.Invoke(null, new object[] { datar.Value });
            var parseddata = methodinfo.Invoke(null, new object[] { resultraw.Value, settings });
            if (parseddata != null && parseddata is IIdentifiable)
                return parseddata as IIdentifiable;
            else
                return null;
        }

        //Implementation with refletion not using Generics
        public static async Task<IEnumerable<IIdentifiable>> GetObjectListAsync(
            this Query query,
            Type type,
            CancellationToken cancellationToken = default
        )
        {
            //using this ContractResolver avoids duplicate Lists
            //Reflection Json Deserialize
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new GetOnlyContractResolver(),
            };
            List<IIdentifiable> datalist = new List<IIdentifiable>();

            var method = typeof(JsonConvert)
                .GetMethods()
                .Where(x => x.Name == "DeserializeObject")
                .Where(x => x.IsGenericMethod)
                .Where(x => x.GetParameters().Any(x => x.Name!.Equals("settings")));

            var dataraw = await query.GetAsync<JsonRaw>();

            foreach (var datar in dataraw)
            {
                var methodinfo = method.FirstOrDefault()!.MakeGenericMethod(type);
                //var parseddata = methodinfo.Invoke(null, new object[] { datar.Value });
                var parseddata = methodinfo.Invoke(null, new object[] { datar.Value, settings });
                if (parseddata != null)
                    datalist.Add(parseddata as IIdentifiable);
            }

            //End reflection Json Deserialize

            return datalist;
        }

        #endregion

        //Using System.Text.Json
        //public static async Task<IEnumerable<T>> GetObjectListAsyncV2<T>(this Query query, CancellationToken cancellationToken = default) where T : notnull
        //{
        //    var result = await query.GetAsync<JsonRaw>();
        //    return result.Select(x => System.Text.Json.JsonSerializer.Deserialize<T>(x.Value)!) ?? default!;
        //}

        //Insert also data in Raw table
        public static async Task<int> InsertInRawtableAndGetIdAsync(
            this QueryFactory queryfactory,
            RawDataStore rawData,
            CancellationToken cancellationToken = default
        )
        {
            return await queryfactory.Query("rawdata").InsertGetIdAsync<int>(rawData);
        }

        #region PG CRUD Helpers

        /// <summary>
        /// Prepared data item ready for database write
        /// </summary>
        private class PreparedDataItem<T>
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            public T Data { get; set; }
            public string IdToProcess { get; set; }
            public T? ExistingData { get; set; }
            public bool IsCreate { get; set; }
            public List<string> PushChannels { get; set; } = new List<string>();
            public int? ObjectChanged { get; set; }
            public int? ObjectImageChanged { get; set; }
            public EqualityResult? ComparisonResult { get; set; }
            public PGCRUDResult? ErrorResult { get; set; } // Set if preparation failed
        }

        /// <summary>
        /// Prepares a single data item for upsert (validation, metadata, comparison)
        /// </summary>
        private static PreparedDataItem<T> PrepareDataItem<T>(
            T data,
            T? existingData,
            DataInfo dataconfig,
            EditInfo editinfo,
            CRUDConstraints createConstraints,
            CRUDConstraints updateConstraints,
            CompareConfig compareConfig,
            bool reduced = false
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            var prepared = new PreparedDataItem<T>();
            List<string> channelstopublish = new List<string>();
            int? objectchangedcount = null;
            int? objectimagechangedcount = null;

            string reducedId = reduced ? "_REDUCED" : "";
            prepared.IdToProcess = IdGenerator.CheckIdFromType<T>(data.Id + reducedId);
            prepared.ExistingData = existingData;
            prepared.IsCreate = existingData == null;

            //Setting MetaInfo
            data._Meta = MetadataHelper.GetMetadataobject<T>(data, reduced);
            //Setting Editinfo
            data._Meta.UpdateInfo = new UpdateInfo()
            {
                UpdatedBy = editinfo.Editor,
                UpdateSource = editinfo.Source,
            };
            //Setting the MetaData UpdateInfo.UpdateHistory
            MetadataHelper.SetUpdateHistory(existingData?._Meta, data._Meta);

            //Setting Firstimport to Now if null
            if (data.FirstImport == null)
                data.FirstImport = DateTime.Now;
            //New Data set last change to now
            data.LastChange = DateTime.Now;

            // Use the appropriate constraint based on whether this is a create or update
            var constraintToCheck = prepared.IsCreate ? createConstraints : updateConstraints;

            //Check data condition return not allowed if it fails
            if (!CheckCRUDCondition.CRUDOperationAllowed(data, constraintToCheck.Condition))
            {
                prepared.ErrorResult = new PGCRUDResult()
                {
                    id = data.Id,
                    odhtype = data._Meta.Type,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    errorreason = "Not Allowed",
                    operation = dataconfig.Operation.ToString(),
                    changes = null,
                    compareobject = false,
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
                    prepared.ErrorResult = new PGCRUDResult()
                    {
                        id = data.Id,
                        odhtype = data._Meta.Type,
                        created = 0,
                        updated = 0,
                        deleted = 0,
                        error = 1,
                        errorreason = "Data to update Not Found",
                        operation = dataconfig.Operation.ToString(),
                        changes = null,
                        compareobject = false,
                        objectchanged = objectchangedcount,
                        objectimagechanged = objectimagechangedcount,
                        pushchannels = channelstopublish,
                    };
                    return prepared;
                }

                if (data is IPublishedOn)
                {
                    if ((data as IPublishedOn).PublishedOn == null)
                        (data as IPublishedOn).PublishedOn = new List<string>();

                    channelstopublish.AddRange((data as IPublishedOn).PublishedOn);
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
                    prepared.ErrorResult = new PGCRUDResult()
                    {
                        id = data.Id,
                        odhtype = data._Meta.Type,
                        created = 0,
                        updated = 0,
                        deleted = 0,
                        error = 1,
                        errorreason = "Data exists already",
                        operation = dataconfig.Operation.ToString(),
                        changes = null,
                        compareobject = false,
                        objectchanged = objectchangedcount,
                        objectimagechanged = objectimagechangedcount,
                        pushchannels = channelstopublish,
                    };
                    return prepared;
                }

                //Set the FirstImport of the old data
                if (existingData.FirstImport != null)
                    data.FirstImport = existingData.FirstImport;

                //Set the Lastchanged of the old data, only if the Comparator is active
                if (existingData.LastChange != null && compareConfig.CompareData)
                    data.LastChange = existingData.LastChange;

                //Compare the data
                bool imagesequal = false;
                EqualityResult equalityresult = new EqualityResult() { isequal = false, patch = null };

                if (compareConfig.CompareData)
                {
                    equalityresult = EqualityHelper.CompareClassesTest<T>(
                        existingData,
                        data,
                        new List<string>() { "LastChange", "_Meta", "FirstImport" },
                        true
                    );
                    if (equalityresult.isequal)
                        objectchangedcount = 0;
                    else
                    {
                        objectchangedcount = 1;
                        data.LastChange = DateTime.Now;
                    }
                }

                //Compare Image Gallery
                if (
                    compareConfig.CompareImages
                    && data is IImageGalleryAware
                    && existingData is IImageGalleryAware
                )
                {
                    imagesequal = EqualityHelper.CompareImageGallery(
                        (data as IImageGalleryAware).ImageGallery,
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
                if (data is IPublishedOn && existingData is IPublishedOn)
                {
                    if ((data as IPublishedOn).PublishedOn == null)
                        (data as IPublishedOn).PublishedOn = new List<string>();

                    channelstopublish.AddRange(
                        (data as IPublishedOn).PublishedOn.UnionIfNotNull(
                            (existingData as IPublishedOn).PublishedOn
                        )
                    );
                }
            }

            prepared.Data = data;
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
            IEnumerable<T> dataList,
            DataInfo dataconfig,
            EditInfo editinfo,
            CRUDConstraints createConstraints,
            CRUDConstraints updateConstraints,
            CompareConfig compareConfig,
            bool reduced = false
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            var batchResult = new BatchCRUDResult();
            var results = new List<PGCRUDResult>();

            if (dataList == null || !dataList.Any())
            {
                batchResult.Results = results;
                return batchResult;
            }

            string reducedId = reduced ? "_REDUCED" : "";

            // Prepare all IDs
            var allIds = dataList
                .Select(d => IdGenerator.CheckIdFromType<T>(d.Id + reducedId))
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
            foreach (var data in dataList)
            {
                try
                {
                    var idToProcess = IdGenerator.CheckIdFromType<T>(data.Id + reducedId);
                    existingLookup.TryGetValue(idToProcess.ToLower(), out var existingData);

                    var prepared = PrepareDataItem<T>(
                        data,
                        existingData,
                        dataconfig,
                        editinfo,
                        createConstraints,
                        updateConstraints,
                        compareConfig,
                        reduced
                    );

                    preparedItems.Add(prepared);
                }
                catch (Exception ex)
                {
                    var metadata = MetadataHelper.GetMetadataobject<T>(data, reduced);
                    results.Add(new PGCRUDResult
                    {
                        id = data.Id,
                        odhtype = metadata?.Type,
                        operation = "Error",
                        error = 1,
                        errorreason = ex.Message,
                        created = 0,
                        updated = 0,
                        deleted = 0,
                        compareobject = false,
                        objectchanged = null,
                        objectimagechanged = null,
                        pushchannels = new List<string>()
                    });
                    batchResult.Errors++;
                    batchResult.TotalProcessed++;
                }
            }

            // Separate items into creates, updates, and errors
            var itemsToCreate = preparedItems
                .Where(p => p.ErrorResult == null && p.IsCreate)
                .ToList();
            var itemsToUpdate = preparedItems
                .Where(p => p.ErrorResult == null && !p.IsCreate && p.ObjectChanged != 0)
                .ToList();
            var itemsUnchanged = preparedItems
                .Where(p => p.ErrorResult == null && !p.IsCreate && p.ObjectChanged == 0)
                .ToList();
            var itemsWithErrors = preparedItems
                .Where(p => p.ErrorResult != null)
                .ToList();

            // Add error results
            foreach (var errorItem in itemsWithErrors)
            {
                if (errorItem.ErrorResult.HasValue)
                {
                    results.Add(errorItem.ErrorResult.Value);
                    batchResult.Errors++;
                    batchResult.TotalProcessed++;
                }
            }

            // BULK INSERT - Process all creates in a transaction
            if (itemsToCreate.Any())
            {
                // SqlKata's bulk insert doesn't handle JsonRaw properly
                // Use individual inserts within the same connection for better performance
                foreach (var prepared in itemsToCreate)
                {
                    try
                    {
                        await QueryFactory
                            .Query(dataconfig.Table)
                            .InsertAsync(new JsonBData
                            {
                                id = prepared.IdToProcess,
                                data = new JsonRaw(prepared.Data)
                            });

                        results.Add(new PGCRUDResult
                        {
                            id = prepared.Data.Id,
                            odhtype = prepared.Data._Meta.Type,
                            operation = CRUDOperation.Create.ToString(),
                            created = 1,
                            updated = 0,
                            deleted = 0,
                            error = 0,
                            errorreason = null,
                            compareobject = compareConfig.CompareData,
                            objectchanged = prepared.ObjectChanged,
                            objectimagechanged = prepared.ObjectImageChanged,
                            pushchannels = prepared.PushChannels,
                            changes = null
                        });
                        batchResult.Created++;
                        batchResult.TotalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new PGCRUDResult
                        {
                            id = prepared.Data.Id,
                            odhtype = prepared.Data._Meta.Type,
                            operation = "Error",
                            error = 1,
                            errorreason = $"Insert failed: {ex.Message}",
                            created = 0,
                            updated = 0,
                            deleted = 0,
                            compareobject = false,
                            objectchanged = null,
                            objectimagechanged = null,
                            pushchannels = new List<string>()
                        });
                        batchResult.Errors++;
                        batchResult.TotalProcessed++;
                    }
                }
            }

            // var connection = QueryFactory.Connection;
            // var transaction = connection.BeginTransaction();

            // BULK UPDATE - Process updates (unfortunately SqlKata doesn't support bulk update well)
            // We need to do individual updates but can batch them in a transaction if needed
            foreach (var prepared in itemsToUpdate)
            {
                try
                {
                    var updateResult = await QueryFactory
                        .Query(dataconfig.Table)
                        .Where("id", prepared.IdToProcess)
                        .UpdateAsync(new JsonBData
                        {
                            id = prepared.IdToProcess,
                            data = new JsonRaw(prepared.Data)
                        });

                    if (updateResult > 0)
                    {
                        results.Add(new PGCRUDResult
                        {
                            id = prepared.Data.Id,
                            odhtype = prepared.Data._Meta.Type,
                            operation = CRUDOperation.Update.ToString(),
                            created = 0,
                            updated = 1,
                            deleted = 0,
                            error = 0,
                            errorreason = null,
                            compareobject = compareConfig.CompareData,
                            objectchanged = prepared.ObjectChanged,
                            objectimagechanged = prepared.ObjectImageChanged,
                            pushchannels = prepared.PushChannels,
                            changes = prepared.ComparisonResult?.patch
                        });
                        batchResult.Updated++;
                        batchResult.TotalProcessed++;

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
                        results.Add(new PGCRUDResult
                        {
                            id = prepared.Data.Id,
                            odhtype = prepared.Data._Meta.Type,
                            operation = "Error",
                            error = 1,
                            errorreason = "Update failed - no rows affected",
                            created = 0,
                            updated = 0,
                            deleted = 0,
                            compareobject = false,
                            objectchanged = null,
                            objectimagechanged = null,
                            pushchannels = new List<string>()
                        });
                        batchResult.Errors++;
                        batchResult.TotalProcessed++;
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new PGCRUDResult
                    {
                        id = prepared.Data.Id,
                        odhtype = prepared.Data._Meta.Type,
                        operation = "Error",
                        error = 1,
                        errorreason = ex.Message,
                        created = 0,
                        updated = 0,
                        deleted = 0,
                        compareobject = false,
                        objectchanged = null,
                        objectimagechanged = null,
                        pushchannels = new List<string>()
                    });
                    batchResult.Errors++;
                    batchResult.TotalProcessed++;
                }
            }

            // Handle unchanged items
            foreach (var prepared in itemsUnchanged)
            {
                results.Add(new PGCRUDResult
                {
                    id = prepared.Data.Id,
                    odhtype = prepared.Data._Meta.Type,
                    operation = CRUDOperation.Update.ToString(),
                    created = 0,
                    updated = 1,
                    deleted = 0,
                    error = 0,
                    errorreason = null,
                    compareobject = compareConfig.CompareData,
                    objectchanged = 0,
                    objectimagechanged = prepared.ObjectImageChanged,
                    pushchannels = prepared.PushChannels,
                    changes = null
                });
                batchResult.Unchanged++;
                batchResult.TotalProcessed++;
            }

            batchResult.Results = results;
            return batchResult;
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
            T data,
            DataInfo dataconfig,
            EditInfo editinfo,
            CRUDConstraints constraints,
            CompareConfig compareConfig,
            int? rawdataid = null,
            bool reduced = false
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData, new()
        {
            //TOCHECK: What if no id is passed? Generate ID?
            //TOCHECK: Id Uppercase or Lowercase depending on table
            //TOCHECK: Shortname population?
            
            //If no data is passed return error
            if (data == null)
                return new PGCRUDResult()
                {
                    id = "",
                    odhtype = "",
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    errorreason = "No Data",
                    operation = dataconfig.Operation.ToString(),
                    changes = null,
                    compareobject = false,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    pushchannels = new List<string>(),
                };

            string reducedId = reduced ? "_REDUCED" : "";
            var idtoprocess = IdGenerator.CheckIdFromType<T>(data.Id + reducedId);
            IdGenerator.CheckIdFromType<T>(data);

            //Check if data exists already
            var queryresult = await QueryFactory
                .Query(dataconfig.Table)
                .Select("data")
                .Where("id", idtoprocess)
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
                compareConfig,
                reduced
            );

            // If preparation failed, return error result
            if (prepared.ErrorResult.HasValue)
                return prepared.ErrorResult.Value;

            int createresult = 0;
            int updateresult = 0;

            // Perform database write
            if (prepared.IsCreate)
            {
                if (rawdataid == null)
                {
                    createresult = await QueryFactory
                        .Query(dataconfig.Table)
                        .InsertAsync(new JsonBData() { id = prepared.IdToProcess, data = new JsonRaw(prepared.Data) });
                }
                else
                {
                    createresult = await QueryFactory
                        .Query(dataconfig.Table)
                        .InsertAsync(new JsonBDataRaw() { id = prepared.IdToProcess, data = new JsonRaw(prepared.Data), rawdataid = rawdataid.Value });
                }

                dataconfig.Operation = CRUDOperation.Create;
            }
            else
            {
                if (rawdataid == null)
                {
                    updateresult = await QueryFactory
                        .Query(dataconfig.Table)
                        .Where("id", prepared.IdToProcess)
                        .UpdateAsync(new JsonBData() { id = prepared.IdToProcess, data = new JsonRaw(prepared.Data) });
                }
                else
                {
                    updateresult = await QueryFactory
                        .Query(dataconfig.Table)
                        .Where("id", prepared.IdToProcess)
                        .UpdateAsync(new JsonBDataRaw() { id = prepared.IdToProcess, data = new JsonRaw(prepared.Data), rawdataid = rawdataid.Value });
                }

                dataconfig.Operation = CRUDOperation.Update;
            }

            // Check if write succeeded
            if (createresult == 0 && updateresult == 0)
                return new PGCRUDResult()
                {
                    id = prepared.Data.Id,
                    odhtype = prepared.Data._Meta.Type,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    errorreason = "Internal Error",
                    operation = dataconfig.Operation.ToString(),
                    changes = null,
                    compareobject = false,
                    objectchanged = prepared.ObjectChanged,
                    objectimagechanged = prepared.ObjectImageChanged,
                    pushchannels = prepared.PushChannels,
                };

            //If changes should be saved to DB
            if (dataconfig.SaveChangesToDB && prepared.ObjectChanged != null && prepared.ObjectChanged > 0)
            {
                await SaveChangesToRawChangesTable(
                    QueryFactory,
                    prepared.Data,
                    editinfo,
                    prepared.ComparisonResult ?? new EqualityResult() { isequal = false, patch = null }
                );
            }

            return new PGCRUDResult()
            {
                id = prepared.Data.Id,
                odhtype = prepared.Data._Meta.Type,
                created = createresult,
                updated = updateresult,
                deleted = 0,
                error = 0,
                errorreason = null,
                operation = dataconfig.Operation.ToString(),
                compareobject = compareConfig.CompareData,
                objectchanged = prepared.ObjectChanged,
                objectimagechanged = prepared.ObjectImageChanged,
                pushchannels = prepared.PushChannels,
                changes = prepared.ComparisonResult?.patch,
            };
        }

        /// <summary>
        /// Deletes the data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="QueryFactory"></param>
        /// <param name="id"></param>
        /// <param name="dataconfig"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static async Task<PGCRUDResult> DeleteData<T>(
            this QueryFactory QueryFactory,
            string id,
            DataInfo dataconfig,
            CRUDConstraints constraints,
            bool reduced = false
        )
            where T : IIdentifiable, IImportDateassigneable, IMetaData
        {
            List<string> channelstopublish = new List<string>();

            if (string.IsNullOrEmpty(id))
                return new PGCRUDResult()
                {
                    id = "",
                    odhtype = "",
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    errorreason = "Bad Request",
                    operation = dataconfig.Operation.ToString(),
                    changes = null,
                    compareobject = false,
                    objectchanged = null,
                    objectimagechanged = null,
                    pushchannels = channelstopublish,
                };

            string reducedId = "";
            if (reduced)
                reducedId = "_REDUCED";

            //Hack for old reduced ids
            string idtoprocess = id;
            if (!id.ToLower().Contains("_reduced"))
                idtoprocess = id + reducedId;

            var idtodelete = Helper.IdGenerator.CheckIdFromType<T>(idtoprocess);

            //Check if data exists
            var queryresult = await QueryFactory
                .Query(dataconfig.Table)
                .Select("data")
                .Where("id", idtodelete)
                .When(
                    constraints.AccessRole.Count() > 0,
                    q => q.FilterDataByAccessRoles(constraints.AccessRole)
                )
                .GetObjectSingleAsync<T>();

            var deleteresult = 0;
            var errorreason = "";

            if (queryresult == null)
            {
                return new PGCRUDResult()
                {
                    id = idtodelete,
                    odhtype = null,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    errorreason = "Data Not Found",
                    operation = dataconfig.Operation.ToString(),
                    changes = null,
                    compareobject = false,
                    objectchanged = null,
                    objectimagechanged = null,
                    pushchannels = channelstopublish,
                };
            }
            else
            {
                //Check data condition
                if (!CheckCRUDCondition.CRUDOperationAllowed(queryresult, constraints.Condition))
                {
                    return new PGCRUDResult()
                    {
                        id = idtodelete,
                        odhtype = queryresult._Meta.Type,
                        created = 0,
                        updated = 0,
                        deleted = 0,
                        error = 1,
                        errorreason = "Not Allowed",
                        operation = dataconfig.Operation.ToString(),
                        changes = null,
                        compareobject = false,
                        objectchanged = null,
                        objectimagechanged = null,
                        pushchannels = channelstopublish,
                    };
                }

                if (queryresult is IPublishedOn && ((IPublishedOn)queryresult).PublishedOn != null)
                {
                    channelstopublish.AddRange(((IPublishedOn)queryresult).PublishedOn);
                }

                deleteresult = await QueryFactory
                    .Query(dataconfig.Table)
                    .Where("id", idtodelete)
                    .DeleteAsync();
            }

            if (deleteresult == 0)
                return new PGCRUDResult()
                {
                    id = idtodelete,
                    odhtype = queryresult._Meta.Type,
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    errorreason = "Internal Error",
                    operation = dataconfig.Operation.ToString(),
                    changes = null,
                    compareobject = false,
                    objectchanged = null,
                    objectimagechanged = null,
                    pushchannels = channelstopublish,
                };

            return new PGCRUDResult()
            {
                id = idtodelete,
                odhtype = queryresult._Meta.Type,
                created = 0,
                updated = 0,
                deleted = deleteresult,
                error = 0,
                errorreason = errorreason,
                operation = dataconfig.Operation.ToString(),
                changes = null,
                compareobject = false,
                objectchanged = null,
                objectimagechanged = null,
                pushchannels = channelstopublish,
            };
        }

        public static async Task<int> SaveChangesToRawChangesTable<T>(
            this QueryFactory QueryFactory,
            T data,
              EditInfo editinfo,
              EqualityResult equalityresult
            ) where T : IIdentifiable, IImportDateassigneable, IMetaData, new ()
        {
            try
            {
                RawChangesStore datachanges = new RawChangesStore();
                datachanges.editsource = editinfo.Source ?? "";
                datachanges.editedby = editinfo.Editor;
                datachanges.date = data._Meta.LastUpdate ?? DateTime.Now;
                datachanges.datasource = data._Meta.Source;
                datachanges.changes = equalityresult.patch != null ? new JsonRaw(equalityresult.patch.ToString()) : null;
                datachanges.sourceid = data.Id;
                datachanges.type = data._Meta.Type;
                datachanges.license = "unknown";

                if (data is ILicenseInfo)
                {
                    if ((data as ILicenseInfo).LicenseInfo != null)
                        datachanges.license = (data as ILicenseInfo).LicenseInfo.ClosedData ? "closed" : "open";
                }

                var resultrawchangesinsert = await QueryFactory
                   .Query("rawchanges")
                   .InsertAsync(datachanges);

                return resultrawchangesinsert;
            }
            catch (Exception ex)
            {
                //Create a Log entry
                GenericResultsHelper.GetErrorUpdateResult(
                    data.Id,
                    "api",
                    "Insert Rawchanges",
                    "single",
                    "Insert Rawchanges failed, " + equalityresult.patch != null ? equalityresult.patch.ToString() : "no change",
                    data._Meta.Type,
                    new UpdateDetail()
                    {
                        updated = 0,
                        changes = null,
                        comparedobjects = null,
                        created = 0,
                        deleted = 0,
                        error = 1,                        
                        objectchanged = 0,
                        objectimagechanged = 0,
                        pushed = null,
                        pushchannels = null,
                    },
                    ex,
                    true
                );

                return 0;
            }
        }

        //TODO
        public static async Task<PGCRUDResult> UpsertDataDestinationData<T, V>(
            this QueryFactory QueryFactory,
            T data,
            V destinationdata,
            string table,
            bool errorwhendataexists = false,
            bool errorwhendataisnew = false,
            bool comparedata = false,
            bool compareimagedata = false
        )
            where T : IIdentifiable,
                IImportDateassigneable,
                IMetaData,
                IPublishedOn,
                IImageGalleryAware,
                new()
            where V : IIdentifiable, IImportDateassigneable, IMetaData
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "no data");

            //Check if data exists
            var query = QueryFactory.Query(table).Select("data").Where("id", data.Id);

            var queryresult = await query.GetObjectSingleAsync<T>();

            string operation = "";

            int createresult = 0;
            int updateresult = 0;
            int errorresult = 0;
            //bool compareresult = false;
            EqualityResult equalityresult = new EqualityResult() { isequal = false, patch = null };

            bool imagecompareresult = false;
            List<string> channelstopublish = new List<string>();

            data.LastChange = DateTime.Now;
            destinationdata.LastChange = DateTime.Now;
            //Setting MetaInfo
            data._Meta = MetadataHelper.GetMetadataobject<T>(data);
            destinationdata._Meta = MetadataHelper.GetMetadataobject<V>(destinationdata);

            if (data.FirstImport == null)
            {
                data.FirstImport = DateTime.Now;
                destinationdata.FirstImport = DateTime.Now;
            }

            if (queryresult == null)
            {
                if (errorwhendataisnew)
                    throw new ArgumentNullException(nameof(data.Id), "Id does not exist");

                createresult = await QueryFactory
                    .Query(table)
                    .InsertAsync(
                        new JsonBDataDestinationData()
                        {
                            id = data.Id,
                            data = new JsonRaw(data),
                            destinationdata = new JsonRaw(destinationdata),
                        }
                    );
                operation = "INSERT";
            }
            else
            {
                //Compare the data
                if (comparedata && queryresult != null)
                    equalityresult = EqualityHelper.CompareClassesTest<T>(
                        queryresult,
                        data,
                        new List<string>() { "LastChange", "_Meta", "FirstImport" },
                        true
                    );

                //Compare Image Gallery
                if (compareimagedata && queryresult != null)
                    imagecompareresult = EqualityHelper.CompareImageGallery(
                        data.ImageGallery,
                        queryresult.ImageGallery,
                        new List<string>() { }
                    );

                //Check if Publishedon List changed and populate channels to publish information
                channelstopublish.AddRange(
                    data.PublishedOn.UnionIfNotNull(queryresult.PublishedOn)
                );

                if (errorwhendataexists)
                    throw new ArgumentNullException(nameof(data.Id), "Id exists already");

                updateresult = await QueryFactory
                    .Query(table)
                    .Where("id", data.Id)
                    .UpdateAsync(
                        new JsonBDataDestinationData()
                        {
                            id = data.Id,
                            data = new JsonRaw(data),
                            destinationdata = new JsonRaw(destinationdata),
                        }
                    );
                operation = "UPDATE";
            }

            if (createresult == 0 && updateresult == 0)
                errorresult = 1;

            return new PGCRUDResult()
            {
                id = data.Id,
                created = createresult,
                updated = updateresult,
                deleted = 0,
                error = errorresult,
                operation = operation,
                compareobject = comparedata,
                objectchanged = equalityresult.isequal ? 0 : 1,
                objectimagechanged = imagecompareresult ? 0 : 1,
                pushchannels = channelstopublish,
                changes = equalityresult.patch,
            };
        }

        #endregion

        #region RawDataStore

        
        //OBSOLETE INCLUDED IN UpsertData Generic Method

        //public static async Task<PGCRUDResult> UpsertData<T>(
        //    this QueryFactory QueryFactory,
        //    T data,
        //    string table,
        //    int rawdataid,
        //    string editor,
        //    string editsource,
        //    bool errorwhendataexists = false
        //)
        //    where T : IIdentifiable, IImportDateassigneable, IMetaData
        //{
        //    if (data == null)
        //        throw new ArgumentNullException(nameof(data), "no data");

        //    //Check if data exists
        //    var queryresult = await QueryFactory.Query(table).Select("data").Where("id", data.Id)
        //        .GetObjectSingleAsync<T>(); ;
        
        //    string operation = "";

        //    int createresult = 0;
        //    int updateresult = 0;
        //    int errorresult = 0;

        //    //Setting MetaInfo
        //    data._Meta = MetadataHelper.GetMetadataobject<T>(data);

        //    if (data.FirstImport == null)
        //        data.FirstImport = DateTime.Now;
        //    data.LastChange = DateTime.Now;

        //    //Setting Editinfo
        //    data._Meta.UpdateInfo = new UpdateInfo()
        //    {
        //        UpdatedBy = editor,
        //        UpdateSource = editsource,
        //    };
        //    //Setting the MetaData UpdateInfo.UpdateHistory
        //    MetadataHelper.SetUpdateHistory(queryresult != null ? queryresult._Meta : null, data._Meta);

        //    if (queryresult == null)
        //    {
        //        createresult = await QueryFactory
        //            .Query(table)
        //            .InsertAsync(
        //                new JsonBDataRaw()
        //                {
        //                    id = data.Id,
        //                    data = new JsonRaw(data),
        //                    rawdataid = rawdataid,
        //                }
        //            );
        //        operation = "INSERT";
        //    }
        //    else
        //    {
        //        if (errorwhendataexists)
        //            throw new ArgumentNullException(nameof(data), "Id exists already");

        //        updateresult = await QueryFactory
        //            .Query(table)
        //            .Where("id", data.Id)
        //            .UpdateAsync(
        //                new JsonBDataRaw()
        //                {
        //                    id = data.Id,
        //                    data = new JsonRaw(data),
        //                    rawdataid = rawdataid,
        //                }
        //            );
        //        operation = "UPDATE";
        //    }

        //    if (createresult == 0 && updateresult == 0)
        //        errorresult = 1;

        //    return new PGCRUDResult()
        //    {
        //        id = data.Id,
        //        created = createresult,
        //        updated = updateresult,
        //        deleted = 0,
        //        error = errorresult,
        //        operation = operation,
        //    };
        //}

        #endregion
    }
}
