// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Amazon.Auth.AccessControlPolicy;
using DataModel;
using Helper;
using Helper.Generic;
using Helper.Tagging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using OdhApiImporter.Helpers.LTSAPI;
using OdhApiImporter.Helpers.RAVEN;
using OdhNotifier;
using RAVEN;
using SqlKata;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers
{
    public class LTSAPIImportHelper
    {
        private readonly QueryFactory QueryFactory;
        private readonly ISettings settings;
        private string importerURL;
        private IOdhPushNotifier OdhPushnotifier;

        public LTSAPIImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
            this.importerURL = importerURL;
            this.OdhPushnotifier = odhpushnotifier;
        }

        //Update Single Data                
        public async Task<Tuple<string, UpdateDetail>> UpdateSingleDataFromLTSApi(
            string id,
            string datatype,
            CancellationToken cancellationToken
        )
        {     
            var updateresult = default(UpdateDetail);
            var updateresultreduced = default(UpdateDetail);

            switch (datatype.ToLower())
            {                
                case "event":
                    LTSApiEventImportHelper ltsapieventimporthelper = new LTSApiEventImportHelper(
                        settings,
                        QueryFactory, 
                        "events",
                        importerURL
                        );

                    updateresult = await ltsapieventimporthelper.SaveSingleDataToODH(id, false, cancellationToken);

                    //Get Reduced                    
                    updateresultreduced = await ltsapieventimporthelper.SaveSingleDataToODH(id, true, cancellationToken);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                id,
                                datatype
                            );
                       
                    break;
                case "gastronomy":
                    LTSApiGastronomyImportHelper ltsapigastroimporthelper = new LTSApiGastronomyImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    updateresult = await ltsapigastroimporthelper.SaveDataToODH(null, new List<string>() { id }, false, cancellationToken);

                    //Get Reduced                    
                    updateresultreduced = await ltsapigastroimporthelper.SaveDataToODH(null, new List<string>() { id }, true, cancellationToken);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                id,
                                datatype
                            );

                    break;

                default:
                    throw new Exception("no match found");
            }

            var mergelist = new List<UpdateDetail>() { updateresult };

            if (
                updateresultreduced.updated != null
                || updateresultreduced.created != null
                || updateresultreduced.deleted != null
            )
            {
                mergelist.Add(updateresultreduced);
            }                

            return Tuple.Create<string, UpdateDetail>(
                id,
                GenericResultsHelper.MergeUpdateDetail(mergelist)
            );
        }

        //Update LastChanged Data
        public async Task<Tuple<string, UpdateDetail>> UpdateLastChangedDataFromLTSApi(
            DateTime lastchanged,
            string datatype,
            CancellationToken cancellationToken
        )
        {
            Tuple<string, UpdateDetail> updatedetail = default(Tuple<string, UpdateDetail>); 

            switch (datatype.ToLower())
            {
                case "event":
                    LTSApiEventImportHelper ltsapieventimporthelper = new LTSApiEventImportHelper(
                        settings,
                        QueryFactory,
                        "events",
                        importerURL
                        );

                    var lastchangedlist = await ltsapieventimporthelper.GetLastChangedData(lastchanged, false, cancellationToken);

                    int? updatecounter = 0;
                    int? createcounter = 0;
                    int? deletecounter = 0;
                    int? errorcounter = 0;

                    //Call Single Update and write LOG
                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "event", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.lastchanged",
                            "Update LTS succeeded",
                            datatype,
                            resulttuple.Item2,
                            true
                        );

                        createcounter = resulttuple.Item2.created + createcounter;
                        updatecounter = resulttuple.Item2.updated + updatecounter;
                        deletecounter = resulttuple.Item2.deleted + deletecounter;
                        errorcounter = resulttuple.Item2.error + errorcounter;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;
              

                default:
                    throw new Exception("no match found");
            }

            return updatedetail;
        }

        //Update Deleted Data        
        public async Task<Tuple<string, UpdateDetail>> UpdateDeletedDataFromLTSApi(
            DateTime lastchanged,
            string datatype,
            CancellationToken cancellationToken
        )
        {
            Tuple<string, UpdateDetail> updatedetail = default(Tuple<string, UpdateDetail>);

            switch (datatype.ToLower())
            {
                case "event":
                    LTSApiEventImportHelper ltsapieventimporthelper = new LTSApiEventImportHelper(
                        settings,
                        QueryFactory,
                        "events",
                        importerURL
                        );

                    var lastchangedlist = await ltsapieventimporthelper.GetLastDeletedData(lastchanged, false, cancellationToken);

                    int? updatecounter = 0;
                    int? createcounter = 0;
                    int? deletecounter = 0;
                    int? errorcounter = 0;

                    //Call Single Update and write LOG or write directly??
                    //Use the DeleteOrDisable method?

                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "event", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype,
                            resulttuple.Item2,
                            true
                        );

                        createcounter = resulttuple.Item2.created + createcounter;
                        updatecounter = resulttuple.Item2.updated + updatecounter;
                        deletecounter = resulttuple.Item2.deleted + deletecounter;
                        errorcounter = resulttuple.Item2.error + errorcounter;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;


                default:
                    throw new Exception("no match found");
            }

            return updatedetail;
        }

        //TODO Finish Active/Inactive Sync
        public async Task<Tuple<string, UpdateDetail>> UpdateActiveInactiveDataFromLTSApi(
            bool onlyactive,
            string datatype,
            CancellationToken cancellationToken
        )
        {
            Tuple<string, UpdateDetail> updatedetail = default(Tuple<string, UpdateDetail>);

            switch (datatype.ToLower())
            {
                case "event":
                    LTSApiEventImportHelper ltsapieventimporthelper = new LTSApiEventImportHelper(
                        settings,
                        QueryFactory,
                        "events",
                        importerURL
                        );

                    var lastchangedlist = await ltsapieventimporthelper.GetActiveList(onlyactive, false, cancellationToken);

                    //TODO Compare with DB and deactivate all inactive items
                    //Use the DeleteOrDisable method?

                    int? updatecounter = 0;
                    int? createcounter = 0;
                    int? deletecounter = 0;
                    int? errorcounter = 0;

                    //Call Single Update and write LOG or write directly??
                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "event", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype,
                            resulttuple.Item2,
                            true
                        );

                        createcounter = resulttuple.Item2.created + createcounter;
                        updatecounter = resulttuple.Item2.updated + updatecounter;
                        deletecounter = resulttuple.Item2.deleted + deletecounter;
                        errorcounter = resulttuple.Item2.error + errorcounter;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;


                default:
                    throw new Exception("no match found");
            }

            return updatedetail;
        }


        public async Task<IDictionary<string, NotifierResponse>?> CheckIfObjectChangedAndPush(
            UpdateDetail myupdateresult,
            string id,
            string datatype,
            IDictionary<string, bool>? additionalpushinfo = null,
            string pushorigin = "lts.push"
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
