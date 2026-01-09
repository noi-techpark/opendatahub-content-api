// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using LTSAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SqlKata;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers
{
    public interface IImportHelper
    {
        Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        );

        //Task<UpdateDetail> SaveSingleDataToODH(
        //    DateTime? lastchanged = null,
        //    string? id = null,
        //    CancellationToken cancellationToken = default
        //);

        //Task<List<UpdateDetail>> SaveListDataToODH(
        //    DateTime? lastchanged = null,
        //    List<string>? idlist = null,
        //    CancellationToken cancellationToken = default
        //);

        Task<Tuple<int, int>> DeleteOrDisableData<T>(string id, bool delete, bool clearpublishedon = false)
            where T : IActivateable;

        Task<IDictionary<string, NotifierResponse>?> CheckIfObjectChangedAndPush(
            UpdateDetail myupdateresult,
            string id,
            string datatype,
            string pushorigin,
            IDictionary<string, bool>? additionalpushinfo = null
        );

        Task<IDictionary<string, NotifierResponse>?> CheckIfObjectChangedAndPush(
            PGCRUDResult myupdateresult,
            string id,
            string datatype,
            string pushorigin,
            IDictionary<string, bool>? additionalpushinfo = null
        );

        //Task<T> LoadDataFromDB<T>(string id, bool reduced);

        //Task<UpdateDetail> ImportData(ImportObject importobject, CancellationToken cancellationToken);
    }

    public interface IImportHelperLTS : IImportHelper
    {
        Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            bool reduced = false,
            CancellationToken cancellationToken = default
        );

        LtsApi GetLTSApi(bool opendata);
    }

    public class ImportHelper
    {
        protected readonly QueryFactory QueryFactory;
        protected readonly ISettings settings;
        protected readonly string table;
        protected readonly string importerURL;
        protected IOdhPushNotifier OdhPushnotifier;

        public ImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
            this.table = table;
            this.importerURL = importerURL;
            this.OdhPushnotifier = odhpushnotifier;
        }

        public LtsApi GetLTSApi(bool opendata)
        {
            if (!opendata)
            {
                return new LtsApi(
                   settings.LtsCredentials.serviceurl,
                   settings.LtsCredentials.username,
                   settings.LtsCredentials.password,
                   settings.LtsCredentials.ltsclientid,
                   false
               );
            }
            else
            {
                return new LtsApi(
                settings.LtsCredentialsOpen.serviceurl,
                settings.LtsCredentialsOpen.username,
                settings.LtsCredentialsOpen.password,
                settings.LtsCredentialsOpen.ltsclientid,
                true
            );
            }
        }

        /// <summary>
        /// Deletes or disables the data by the selected option
        /// </summary>
        /// <typeparam name="T">ODH Entity to deactivate (to identify the right table)</typeparam>
        /// <param name="id">Id of the data to delete/disable</param>
        /// <param name="delete">Delete the data true/false, if false the data is set to Active = false</param>
        /// <returns>Tuple of ints (updated/deleted)</returns>
        public async Task<Tuple<int, int>> DeleteOrDisableData<T>(string id, bool delete, bool clearpublishedon = false)
            where T : IActivateable
        {
            var deleteresult = 0;
            var updateresult = 0;

            if (delete)
            {
                deleteresult = await QueryFactory.Query(table).Where("id", id).DeleteAsync();
            }
            else
            {
                var query = QueryFactory.Query(table).Select("data").Where("id", id);

                var data = await query.GetObjectSingleAsync<T>();

                if (data != null)
                {
                    if (
                        data.Active != false
                        || (data is ISmgActive && ((ISmgActive)data).SmgActive != false)
                        || (clearpublishedon && data is IPublishedOn && (data as IPublishedOn).PublishedOn != null && (data as IPublishedOn).PublishedOn.Count > 0)
                    )
                    {
                        data.Active = false;
                        if (data is ISmgActive)
                            ((ISmgActive)data).SmgActive = false;

                        if(clearpublishedon)
                        {
                            if(data is IPublishedOn)
                            {                                
                                (data as IPublishedOn).PublishedOn = new List<string>();
                            }                                
                        }


                        updateresult = await QueryFactory
                            .Query(table)
                            .Where("id", id)
                            .UpdateAsync(new JsonBData() { id = id, data = new JsonRaw(data) });

                    }
                }
            }

            return Tuple.Create(updateresult, deleteresult);
        }

        /// <summary>
        /// Deletes or disables the data by the selected option
        /// </summary>
        /// <typeparam name="T">ODH Entity to deactivate (to identify the right table)</typeparam>
        /// <param name="id">Id of the data to delete/disable</param>
        /// <param name="delete">Delete the data true/false, if false the data is set to Active = false</param>
        /// <returns>Tuple of ints (updated/deleted)</returns>
        public async Task<UpdateDetail> DeleteOrDisableDataWithUpdateDetail<T>(string id, EditInfo editinfo, bool delete)
            where T : IActivateable, IIdentifiable, IImportDateassigneable, IMetaData, ISource, IPublishedOn, new()
        {
            UpdateDetail deletedisableresult = default(UpdateDetail);
            PGCRUDResult result = default(PGCRUDResult);

            if (delete)
            {
                //deleteresult = await QueryFactory.Query(table).Where("id", id).DeleteAsync();

                result = await QueryFactory.DeleteData<T>(
                    id,
                    new DataInfo(table, CRUDOperation.Delete),
                    new CRUDConstraints()
                    );

                if (result.errorreason != "Data Not Found")
                {
                    deletedisableresult = new UpdateDetail()
                    {
                        created = result.created,
                        updated = result.updated,
                        deleted = result.deleted,
                        error = result.error,
                        objectchanged = result.objectchanged,
                        objectimagechanged = result.objectimagechanged,
                        comparedobjects =
                        result.compareobject != null && result.compareobject.Value ? 1 : 0,
                        pushchannels = result.pushchannels,
                        changes = result.changes,
                    };
                }
            }
            else
            {
                var query = QueryFactory.Query(table).Select("data").Where("id", id);

                var data = await query.GetObjectSingleAsync<T>();
                
                //How to deal with the Publishedon

                if (data != null)
                {
                    if (
                        data.Active != false
                        || (data is ISmgActive && ((ISmgActive)data).SmgActive != false) 
                        || (data.PublishedOn != null && data.PublishedOn.Count > 0)
                    )
                    {
                        data.Active = false;
                        if (data is ISmgActive)
                            ((ISmgActive)data).SmgActive = false;

                        data.CreatePublishedOnList();

                        //var updateresult = await QueryFactory
                        //    .Query(table)
                        //    .Where("id", id)
                        //    .UpdateAsync(new JsonBData() { id = id, data = new JsonRaw(data) });

                        result = await QueryFactory.UpsertData(
                               data,
                               new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate, true),
                               editinfo,
                               new CRUDConstraints(),
                               new CompareConfig(true, false)
                        );

                        deletedisableresult = new UpdateDetail()
                        {
                            created = result.created,
                            updated = result.updated,
                            deleted = result.deleted,
                            error = result.error,
                            objectchanged = result.objectchanged,
                            objectimagechanged = result.objectimagechanged,
                            comparedobjects =
                                    result.compareobject != null && result.compareobject.Value ? 1 : 0,
                            pushchannels = result.pushchannels,
                            changes = result.changes,
                        };
                    }
                }
            }

            return deletedisableresult;
        }


        /// <summary>
        /// Get All Data by passed Source
        /// </summary>
        /// <param name="syncsourcelist"></param>
        /// <param name="syncsourceinterfacelist"></param>
        /// <returns></returns>
        public async Task<List<string>> GetAllDataBySource(
            List<string> syncsourcelist,
            List<string>? syncsourceinterfacelist = null
        )
        {
            var query = QueryFactory
                .Query(table)
                .Select("id")
                .SourceFilter_GeneratedColumn(syncsourcelist)
                .When(
                    syncsourceinterfacelist != null,
                    x => x.SyncSourceInterfaceFilter_GeneratedColumn(syncsourceinterfacelist)
                );

            var idlist = await query.GetAsync<string>();

            return idlist.ToList();
        }

        public async Task<List<string>> GetAllDataBySourceAndType(
            List<string> sourcelist,
            List<string> typelist
        )
        {
            var query = QueryFactory
                .Query(table)
                .Select("id")
                .SourceFilter_GeneratedColumn(sourcelist)
                .WhereArrayInListOr(typelist, "gen_types");

            var ids = await query.GetAsync<string>();

            return ids.ToList();
        }

        public async Task<List<string>> GetAllDataBySourceAndSyncSourceInterface(
            List<string> sourcelist,
            List<string> syncsourceinterfacelist
        )
        {
            var query = QueryFactory
                .Query(table)
                .Select("id")
                .SourceFilter_GeneratedColumn(sourcelist)
                .SourceOrSyncSourceInterfaceFilter_GeneratedColumn(syncsourceinterfacelist);

            var ids = await query.GetAsync<string>();

            return ids.ToList();
        }

        public async Task<T> LoadDataFromDB<T>(string id, IDStyle? idstyle = IDStyle.uppercase, bool reduced = false)
        {
            string reducedid = "";
            if (reduced)
                reducedid = "_REDUCED";

            var query = QueryFactory
               .Query(table)
               .Select("data")
               .When(idstyle == IDStyle.uppercase, q => q.Where("id", id.ToUpper() + reducedid))
               .When(idstyle == IDStyle.lowercase, q => q.Where("id", id.ToLower() + reducedid.ToLower()))
               .When(idstyle == null, q => q.Where("id", id + reducedid));


            return await query.GetObjectSingleAsync<T>();
        }

        public async Task<IDictionary<string, NotifierResponse>?> CheckIfObjectChangedAndPush(
            UpdateDetail myupdateresult,
            string id,
            string datatype,
            string pushorigin,
            IDictionary<string, bool>? additionalpushinfo = null     
        )
        {
            IDictionary<string, NotifierResponse>? pushresults = default(IDictionary<
                string,
                NotifierResponse
            >);

            //Check if data has changed and Push To all channels
            if (
                myupdateresult.objectchanged != null
                && myupdateresult.objectchanged > 0
                && myupdateresult.pushchannels != null
                && myupdateresult.pushchannels.Count > 0
            )
            {
                if (additionalpushinfo == null)
                    additionalpushinfo = new Dictionary<string, bool>();

                //Check if image has changed and add it to the dictionary
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

        public async Task<IDictionary<string, NotifierResponse >?> CheckIfObjectChangedAndPush(            
            PGCRUDResult myupdateresult,
            string id,
            string datatype,
            string pushorigin,
            IDictionary<string, bool>? additionalpushinfo = null           
        )
        {
            IDictionary<string, NotifierResponse>? pushresults = default(IDictionary<
                string,
                NotifierResponse
            >);

            //Check if data has changed and Push To all channels
            if (
                myupdateresult.objectchanged != null
                && myupdateresult.objectchanged > 0
                && myupdateresult.pushchannels != null
                && myupdateresult.pushchannels.Count > 0
            )
            {
                if (additionalpushinfo == null)
                    additionalpushinfo = new Dictionary<string, bool>();

                //Check if image has changed and add it to the dictionary
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
    }

    public class ImportUtils
    {
        public static void SaveDataAsJson<T>(T data, string filename, string path)
        {
            //Save to to xml folder Features
            var serializer = new JsonSerializer();
            //Save json
            string fileName = Path.Combine(path, filename + ".json");
            using (var writer = File.CreateText(fileName))
            {
                serializer.Serialize(writer, data);
            }
        }

        public static async Task<T> LoadFromJsonAndDeSerialize<T>(string filename, string path)
        {
            using (StreamReader r = new StreamReader(Path.Combine(path, filename + ".json")))
            {
                string json = await r.ReadToEndAsync();

                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        public static async Task<JArray> LoadFromJsonAndDeSerialize(string filename, string path)
        {
            using (StreamReader r = new StreamReader(Path.Combine(path, filename + ".json")))
            {
                string json = await r.ReadToEndAsync();

                return JArray.Parse(json) ?? new JArray();
            }
        }

        public static async Task<IDictionary<string, JArray>> LoadJsonFiles(
            string directory,
            List<string> filenames
        )
        {
            IDictionary<string, JArray> myjsonfiles = new Dictionary<string, JArray>();
            foreach (string filename in filenames)
                myjsonfiles.Add(filename, await LoadFromJsonAndDeSerialize(filename, directory));

            return myjsonfiles;
        }

        public static IDictionary<string, XDocument> LoadXmlFiles(
            string directory,
            List<string> filenames
        )
        {
            //TODO move this files to Database

            IDictionary<string, XDocument> myxmlfiles = new Dictionary<string, XDocument>();

            foreach (var filename in filenames)
            {
                myxmlfiles.Add(filename, XDocument.Load(directory + filename + ".xml"));
            }

            return myxmlfiles;
        }
        
    }
}
