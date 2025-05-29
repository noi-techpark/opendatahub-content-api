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
using Helper.Generic;
using Helper.Tagging;
using MongoDB.Driver;
using OdhApiImporter.Helpers.LTSAPI;
using OdhApiImporter.Helpers.RAVEN;
using OdhNotifier;
using RAVEN;
using SqlKata;
using SqlKata.Execution;

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
                
        public async Task<Tuple<string, UpdateDetail>> GetFromLTSApiTransformToPGObject(
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

                    updateresult = await ltsapieventimporthelper.SaveDataToODH(null, new List<string>() { id }, false, cancellationToken);

                    //Get Reduced                    
                    updateresultreduced = await ltsapieventimporthelper.SaveDataToODH(null, new List<string>() { id }, true, cancellationToken);

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

        //TODO Add LastChanged Sync

        //TODO Add Active/Inactive Sync

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
