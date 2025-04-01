// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;
using Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SqlKata;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers
{
    public interface IImportHelper
    {
        Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        );

        Task<Tuple<int, int>> DeleteOrDisableData<T>(string id, bool delete)
            where T : IActivateable;

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
    }

    public class ImportHelper
    {
        protected readonly QueryFactory QueryFactory;
        protected readonly ISettings settings;
        protected readonly string table;
        protected readonly string importerURL;

        public ImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
            this.table = table;
            this.importerURL = importerURL;
        }

        /// <summary>
        /// Deletes or disables the data by the selected option
        /// </summary>
        /// <typeparam name="T">ODH Entity to deactivate (to identify the right table)</typeparam>
        /// <param name="id">Id of the data to delete/disable</param>
        /// <param name="delete">Delete the data true/false, if false the data is set to Active = false</param>
        /// <returns>Tuple of ints (updated/deleted)</returns>
        public async Task<Tuple<int, int>> DeleteOrDisableData<T>(string id, bool delete)
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
                    )
                    {
                        data.Active = false;
                        if (data is ISmgActive)
                            ((ISmgActive)data).SmgActive = false;

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

        public static async Task<IDictionary<
            string,
            NotifierResponse
        >?> CheckIfObjectChangedAndPush(
            IOdhPushNotifier OdhPushnotifier,
            PGCRUDResult myupdateresult,
            string id,
            string datatype,
            IDictionary<string, bool>? additionalpushinfo = null,
            string pushorigin = ""
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

        public static async Task<IDictionary<
            string,
            NotifierResponse
        >?> CheckIfObjectChangedAndPush(
            IOdhPushNotifier OdhPushnotifier,
            UpdateDetail myupdateresult,
            string id,
            string datatype,
            IDictionary<string, bool>? additionalpushinfo = null,
            string pushorigin = ""
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
}
