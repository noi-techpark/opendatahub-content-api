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
using NetTopologySuite.GeometriesGraph;
using Newtonsoft.Json.Linq;
using OdhApiImporter.Helpers.LTSAPI;
using OdhApiImporter.Helpers.RAVEN;
using OdhNotifier;
using RAVEN;
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

                    //Get full data
                    updateresult = await ltsapieventimporthelper.SaveSingleDataToODH(id, false, cancellationToken);

                    //Get reduced data
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

                    //Get full data
                    updateresult = await ltsapigastroimporthelper.SaveSingleDataToODH(id, false, cancellationToken);

                    //Get reduced data
                    updateresultreduced = await ltsapigastroimporthelper.SaveSingleDataToODH(id, true, cancellationToken);

                    //Push data with smgpoi prefixed lowercase id
                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                "smgpoi" + id.ToLower(),
                                datatype
                            );

                    break;

                case "poi":
                    LTSApiPoiImportHelper ltsapipoiimporthelper = new LTSApiPoiImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    //Get full data
                    updateresult = await ltsapipoiimporthelper.SaveSingleDataToODH(id, false, cancellationToken);

                    //Get reduced data
                    updateresultreduced = await ltsapipoiimporthelper.SaveSingleDataToODH(id, true, cancellationToken);

                    //Push data with smgpoi prefixed lowercase id
                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                "smgpoi" + id.ToLower(),
                                datatype
                            );

                    break;

                case "activity":
                    LTSApiActivityImportHelper ltsapiactivityimporthelper = new LTSApiActivityImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    //Get full data
                    updateresult = await ltsapiactivityimporthelper.SaveSingleDataToODH(id, false, cancellationToken);

                    //Get reduced data                  
                    updateresultreduced = await ltsapiactivityimporthelper.SaveSingleDataToODH(id, true, cancellationToken);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                "smgpoi" + id.ToLower(),
                                datatype
                            );

                    break;

                case "venue":
                    LTSApiVenueImportHelper ltsapivenueimporthelper = new LTSApiVenueImportHelper(
                        settings,
                        QueryFactory,
                        "venues",
                        importerURL
                        );

                    //Get full data
                    updateresult = await ltsapivenueimporthelper.SaveSingleDataToODH(id, false, cancellationToken);

                    //Get reduced data                  
                    updateresultreduced = await ltsapivenueimporthelper.SaveSingleDataToODH(id, true, cancellationToken);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                id,
                                datatype
                            );

                    break;

                case "measuringpoint":
                    LTSApiMeasuringpointImportHelper ltsapimeasuringpointimporthelper = new LTSApiMeasuringpointImportHelper(
                        settings,
                        QueryFactory,
                        "measuringpoints",
                        importerURL
                        );

                    //Get full data
                    updateresult = await ltsapimeasuringpointimporthelper.SaveSingleDataToODH(id, false, cancellationToken);

                    //Get reduced data                  
                    updateresultreduced = await ltsapimeasuringpointimporthelper.SaveSingleDataToODH(id, true, cancellationToken);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                id,
                                datatype
                            );

                    break;

                case "webcam":
                    LTSApiWebcamImportHelper ltsapiwebcamimporthelper = new LTSApiWebcamImportHelper(
                        settings,
                        QueryFactory,
                        "webcams",
                        importerURL
                        );

                    //Get full data
                    updateresult = await ltsapiwebcamimporthelper.SaveSingleDataToODH(id, false, cancellationToken);

                    //Get reduced data                  
                    updateresultreduced = await ltsapiwebcamimporthelper.SaveSingleDataToODH(id, true, cancellationToken);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                id,
                                datatype
                            );

                    break;

                case "accommodation":
                    LTSApiAccommodationImportHelper ltsapiaccommodationimporthelper = new LTSApiAccommodationImportHelper(
                        settings,
                        QueryFactory,
                        "accommodations",
                        importerURL
                        );

                    //Get full data
                    updateresult = await ltsapiaccommodationimporthelper.SaveSingleDataToODH(id, false, cancellationToken);

                    //Get reduced data
                    updateresultreduced = await ltsapiaccommodationimporthelper.SaveSingleDataToODH(id, true, cancellationToken);

                    //TODO When update rooms, hgv rooms, hgv data

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

        //Delete or Deactivate Single Data        
        public async Task<Tuple<string, UpdateDetail>> DeleteSingleDataFromLTSApi(
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

                    //Deactivate Full
                    updateresult = await ltsapieventimporthelper.DeleteOrDisableEventData(id, false, false);

                    //Delete Reduced                    
                    updateresultreduced = await ltsapieventimporthelper.DeleteOrDisableEventData(id, true, true);

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

                    //Deactivate Full
                    updateresult = await ltsapigastroimporthelper.DeleteOrDisableGastronomiesData(id, false, false);

                    //Delete Reduced               
                    updateresultreduced = await ltsapigastroimporthelper.DeleteOrDisableGastronomiesData(id, true, true);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                "smgpoi" + id.ToLower(),
                                datatype
                            );
                    break;

                case "poi":
                    LTSApiPoiImportHelper ltsapipoiimporthelper = new LTSApiPoiImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    //Deactivate Full
                    updateresult = await ltsapipoiimporthelper.DeleteOrDisablePoisData(id, false, false);

                    //Delete Reduced                  
                    updateresultreduced = await ltsapipoiimporthelper.DeleteOrDisablePoisData(id, true, true);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                "smgpoi" + id.ToLower(),
                                datatype
                            );
                    break;

                case "activity":
                    LTSApiActivityImportHelper ltsapiactivityimporthelper = new LTSApiActivityImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    //Deactivate Full
                    updateresult = await ltsapiactivityimporthelper.DeleteOrDisableActivitiesData(id, false, false);

                    //Delete Reduced
                    updateresultreduced = await ltsapiactivityimporthelper.DeleteOrDisableActivitiesData(id, true, true);
                    
                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                "smgpoi" + id.ToLower(),
                                datatype
                            );
                    break;

                case "venue":
                    LTSApiVenueImportHelper ltsapivenueimporthelper = new LTSApiVenueImportHelper(
                        settings,
                        QueryFactory,
                        "venues",
                        importerURL
                        );

                    //Deactivate Full
                    updateresult = await ltsapivenueimporthelper.DeleteOrDisableVenuesData(id, false, false);

                    //Delete Reduced
                    updateresultreduced = await ltsapivenueimporthelper.DeleteOrDisableVenuesData(id, true, true);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                id,
                                datatype
                            );
                    break;

                case "measuringpoint":
                    LTSApiMeasuringpointImportHelper ltsapimeasuringpointimporthelper = new LTSApiMeasuringpointImportHelper(
                        settings,
                        QueryFactory,
                        "measuringpoints",
                        importerURL
                        );

                    //Deactivate Full
                    updateresult = await ltsapimeasuringpointimporthelper.DeleteOrDisableMeasuringpointsData(id, false, false);

                    //Delete Reduced
                    updateresultreduced = await ltsapimeasuringpointimporthelper.DeleteOrDisableMeasuringpointsData(id, true, true);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                id,
                                datatype
                            );
                    break;

                case "webcam":
                    LTSApiWebcamImportHelper ltsapiwebcamimporthelper = new LTSApiWebcamImportHelper(
                        settings,
                        QueryFactory,
                        "webcams",
                        importerURL
                        );

                    //Deactivate Full
                    updateresult = await ltsapiwebcamimporthelper.DeleteOrDisableWebcamsData(id, false, false);

                    //Delete Reduced
                    updateresultreduced = await ltsapiwebcamimporthelper.DeleteOrDisableWebcamsData(id, true, true);

                    updateresult.pushed = await CheckIfObjectChangedAndPush(
                                updateresult,
                                id,
                                datatype
                            );
                    break;

                case "accommodation":
                    LTSApiAccommodationImportHelper ltsapiaccommodationimporthelper = new LTSApiAccommodationImportHelper(
                        settings,
                        QueryFactory,
                        "accommodations",
                        importerURL
                        );

                    //Deactivate Full
                    updateresult = await ltsapiaccommodationimporthelper.DeleteOrDisableAccommodationsData(id, false, false);

                    //Delete Reduced                    
                    updateresultreduced = await ltsapiaccommodationimporthelper.DeleteOrDisableAccommodationsData(id, true, true);

                    //TODO WHAT ABOUT ROOMS

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

        //Update LastChanged Data (By using Update single data)
        public async Task<Tuple<string, UpdateDetail>> UpdateLastChangedDataFromLTSApi(
            DateTime lastchanged,
            string datatype,
            CancellationToken cancellationToken
        )
        {
            Tuple<string, UpdateDetail> updatedetail = default(Tuple<string, UpdateDetail>);
            var lastchangedlist = default(List<string>);
            int? updatecounter = 0;
            int? createcounter = 0;
            int? deletecounter = 0;
            int? errorcounter = 0;

            switch (datatype.ToLower())
            {
                case "event":
                    LTSApiEventImportHelper ltsapieventimporthelper = new LTSApiEventImportHelper(
                        settings,
                        QueryFactory,
                        "events",
                        importerURL
                        );

                    lastchangedlist = await ltsapieventimporthelper.GetLastChangedData(lastchanged, false, cancellationToken);

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
                            datatype.ToLower(),
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

                case "gastronomy":
                    LTSApiGastronomyImportHelper ltsapigastroimporthelper = new LTSApiGastronomyImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    lastchangedlist = await ltsapigastroimporthelper.GetLastChangedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG
                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "gastronomy", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.lastchanged",
                            "Update LTS succeeded",
                            datatype.ToLower(),
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

                case "poi":
                    LTSApiPoiImportHelper ltsapipoiimporthelper = new LTSApiPoiImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    lastchangedlist = await ltsapipoiimporthelper.GetLastChangedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG
                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "poi", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.lastchanged",
                            "Update LTS succeeded",
                            datatype.ToLower(),
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

                case "activity":
                    LTSApiActivityImportHelper ltsapiactivityimporthelper = new LTSApiActivityImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    lastchangedlist = await ltsapiactivityimporthelper.GetLastChangedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG
                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "activity", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.lastchanged",
                            "Update LTS succeeded",
                            datatype.ToLower(),
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

                case "venue":
                    LTSApiVenueImportHelper ltsapivenueimporthelper = new LTSApiVenueImportHelper(
                        settings,
                        QueryFactory,
                        "venues",
                        importerURL
                        );

                    lastchangedlist = await ltsapivenueimporthelper.GetLastChangedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG
                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "venue", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.lastchanged",
                            "Update LTS succeeded",
                            datatype.ToLower(),
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

                case "measuringpoint":
                    LTSApiMeasuringpointImportHelper ltsapimeasuringpointimporthelper = new LTSApiMeasuringpointImportHelper(
                        settings,
                        QueryFactory,
                        "measuringpoints",
                        importerURL
                        );

                    lastchangedlist = await ltsapimeasuringpointimporthelper.GetLastChangedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG
                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "measuringpoint", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.lastchanged",
                            "Update LTS succeeded",
                            datatype.ToLower(),
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

                case "webcam":
                    LTSApiWebcamImportHelper ltsapiwebcamimporthelper = new LTSApiWebcamImportHelper(
                        settings,
                        QueryFactory,
                        "webcams",
                        importerURL
                        );

                    lastchangedlist = await ltsapiwebcamimporthelper.GetLastChangedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG
                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "webcam", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.lastchanged",
                            "Update LTS succeeded",
                            datatype.ToLower(),
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

                case "accommodation":
                    LTSApiAccommodationImportHelper ltsapiaccommodationimporthelper = new LTSApiAccommodationImportHelper(
                        settings,
                        QueryFactory,
                        "accommodations",
                        importerURL
                        );

                    lastchangedlist = await ltsapiaccommodationimporthelper.GetLastChangedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG
                    foreach (var id in lastchangedlist)
                    {
                        var resulttuple = await UpdateSingleDataFromLTSApi(id, "accommodation", cancellationToken);

                        GenericResultsHelper.GetSuccessUpdateResult(
                            resulttuple.Item1,
                            "api",
                            "Update LTS",
                            "single.lastchanged",
                            "Update LTS succeeded",
                            datatype.ToLower(),
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

        //Update Deleted Data (By using Delete or deactivate single data)
        public async Task<Tuple<string, UpdateDetail>> UpdateDeletedDataFromLTSApi(
            DateTime lastchanged,
            string datatype,
            CancellationToken cancellationToken
        )
        {
            Tuple<string, UpdateDetail> updatedetail = default(Tuple<string, UpdateDetail>);
            var lastchangedlist = default(List<string>);
            int? updatecounter = 0;
            int? createcounter = 0;
            int? deletecounter = 0;
            int? errorcounter = 0;

            switch (datatype.ToLower())
            {
                case "event":
                    LTSApiEventImportHelper ltsapieventimporthelper = new LTSApiEventImportHelper(
                        settings,
                        QueryFactory,
                        "events",
                        importerURL
                        );

                    lastchangedlist = await ltsapieventimporthelper.GetLastDeletedData(lastchanged, false, cancellationToken);
                    
                
                    foreach (var id in lastchangedlist)
                    {
                        var updateresult = default(UpdateDetail);
                        var updateresultreduced = default(UpdateDetail);

                        //Use the DeleteOrDisable method
                        updateresult = await ltsapieventimporthelper.DeleteOrDisableEventData(id, false, false);

                        //Use the DeleteOrDisable method
                        updateresultreduced = await ltsapieventimporthelper.DeleteOrDisableEventData(id, true, true);

                        updateresult.pushed = await CheckIfObjectChangedAndPush(
                                    updateresult,
                                    id,
                                    datatype
                                );

                        //Create Delete/Disable Log
                        GenericResultsHelper.GetSuccessUpdateResult(
                            id,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype.ToLower(),
                            updateresult,
                            true
                        );

                        createcounter = updateresult.created + createcounter;
                        updatecounter = updateresult.updated + updatecounter;
                        deletecounter = updateresult.deleted + deletecounter;
                        errorcounter = updateresult.error + errorcounter;

                        //Add also Reduced info
                        if(updateresultreduced.created != null)
                            createcounter = createcounter + updateresultreduced.created;
                        if (updateresultreduced.updated != null)
                            updatecounter = updatecounter + updateresultreduced.updated;
                        if (updateresultreduced.deleted != null)
                            deletecounter = deletecounter + updateresultreduced.deleted;
                        if (updateresultreduced.error != null)
                            errorcounter = errorcounter + updateresultreduced.error;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;

                case "gastronomy":
                    LTSApiGastronomyImportHelper ltsapigastroimporthelper = new LTSApiGastronomyImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    lastchangedlist = await ltsapigastroimporthelper.GetLastDeletedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG or write directly??
                    //Use the DeleteOrDisable method?

                    //ensure lowercase ids in lastchangedlist
                    lastchangedlist = lastchangedlist.Select(x => x.ToLower()).ToList();

                    foreach (var id in lastchangedlist)
                    {
                        var updateresult = default(UpdateDetail);
                        var updateresultreduced = default(UpdateDetail);

                        updateresult = await ltsapigastroimporthelper.DeleteOrDisableGastronomiesData(id, false, false);

                        //Get Reduced                    
                        updateresultreduced = await ltsapigastroimporthelper.DeleteOrDisableGastronomiesData(id, true, true);

                        updateresult.pushed = await CheckIfObjectChangedAndPush(
                                    updateresult,
                                    "smgpoi" + id.ToLower(),
                                    datatype
                                );

                        //Create Delete/Disable Log
                        GenericResultsHelper.GetSuccessUpdateResult(
                            id,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype.ToLower(),
                            updateresult,
                            true
                        );

                        createcounter = updateresult.created + createcounter;
                        updatecounter = updateresult.updated + updatecounter;
                        deletecounter = updateresult.deleted + deletecounter;
                        errorcounter = updateresult.error + errorcounter;

                        //Add also Reduced info
                        if (updateresultreduced.created != null)
                            createcounter = createcounter + updateresultreduced.created;
                        if (updateresultreduced.updated != null)
                            updatecounter = updatecounter + updateresultreduced.updated;
                        if (updateresultreduced.deleted != null)
                            deletecounter = deletecounter + updateresultreduced.deleted;
                        if (updateresultreduced.error != null)
                            errorcounter = errorcounter + updateresultreduced.error;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;

                case "poi":
                    LTSApiPoiImportHelper ltsapipoiimporthelper = new LTSApiPoiImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    lastchangedlist = await ltsapipoiimporthelper.GetLastDeletedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG or write directly??
                    //Use the DeleteOrDisable method?

                    //ensure lowercase ids in lastchangedlist
                    lastchangedlist = lastchangedlist.Select(x => x.ToLower()).ToList();

                    foreach (var id in lastchangedlist)
                    {
                        var updateresult = default(UpdateDetail);
                        var updateresultreduced = default(UpdateDetail);

                        updateresult = await ltsapipoiimporthelper.DeleteOrDisablePoisData(id, false, false);

                        //Get Reduced                    
                        updateresultreduced = await ltsapipoiimporthelper.DeleteOrDisablePoisData(id, true, true);

                        updateresult.pushed = await CheckIfObjectChangedAndPush(
                                    updateresult,
                                    "smgpoi" + id.ToLower(),
                                    datatype
                                );

                        //Create Delete/Disable Log
                        GenericResultsHelper.GetSuccessUpdateResult(
                            id,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype.ToLower(),
                            updateresult,
                            true
                        );

                        createcounter = updateresult.created + createcounter;
                        updatecounter = updateresult.updated + updatecounter;
                        deletecounter = updateresult.deleted + deletecounter;
                        errorcounter = updateresult.error + errorcounter;

                        //Add also Reduced info
                        if (updateresultreduced.created != null)
                            createcounter = createcounter + updateresultreduced.created;
                        if (updateresultreduced.updated != null)
                            updatecounter = updatecounter + updateresultreduced.updated;
                        if (updateresultreduced.deleted != null)
                            deletecounter = deletecounter + updateresultreduced.deleted;
                        if (updateresultreduced.error != null)
                            errorcounter = errorcounter + updateresultreduced.error;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;

                case "activity":
                    LTSApiActivityImportHelper ltsapiactivityimporthelper = new LTSApiActivityImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    lastchangedlist = await ltsapiactivityimporthelper.GetLastDeletedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG or write directly??
                    //Use the DeleteOrDisable method?

                    //ensure lowercase ids in lastchangedlist
                    lastchangedlist = lastchangedlist.Select(x => x.ToLower()).ToList();

                    foreach (var id in lastchangedlist)
                    {
                        var updateresult = default(UpdateDetail);
                        var updateresultreduced = default(UpdateDetail);

                        updateresult = await ltsapiactivityimporthelper.DeleteOrDisableActivitiesData(id, false, false);

                        //Get Reduced                    
                        updateresultreduced = await ltsapiactivityimporthelper.DeleteOrDisableActivitiesData(id, true, true);

                        updateresult.pushed = await CheckIfObjectChangedAndPush(
                                    updateresult,
                                    "smgpoi" + id.ToLower(),
                                    datatype
                                );

                        //Create Delete/Disable Log
                        GenericResultsHelper.GetSuccessUpdateResult(
                            id,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype.ToLower(),
                            updateresult,
                            true
                        );

                        createcounter = updateresult.created + createcounter;
                        updatecounter = updateresult.updated + updatecounter;
                        deletecounter = updateresult.deleted + deletecounter;
                        errorcounter = updateresult.error + errorcounter;

                        //Add also Reduced info
                        if (updateresultreduced.created != null)
                            createcounter = createcounter + updateresultreduced.created;
                        if (updateresultreduced.updated != null)
                            updatecounter = updatecounter + updateresultreduced.updated;
                        if (updateresultreduced.deleted != null)
                            deletecounter = deletecounter + updateresultreduced.deleted;
                        if (updateresultreduced.error != null)
                            errorcounter = errorcounter + updateresultreduced.error;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;

                case "venue":
                    LTSApiVenueImportHelper ltsapivenueimporthelper = new LTSApiVenueImportHelper(
                        settings,
                        QueryFactory,
                        "venues",
                        importerURL
                        );

                    lastchangedlist = await ltsapivenueimporthelper.GetLastDeletedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG or write directly??
                    //Use the DeleteOrDisable method?

                    //ensure lowercase ids in lastchangedlist
                    lastchangedlist = lastchangedlist.Select(x => x.ToLower()).ToList();

                    foreach (var id in lastchangedlist)
                    {
                        var updateresult = default(UpdateDetail);
                        var updateresultreduced = default(UpdateDetail);

                        updateresult = await ltsapivenueimporthelper.DeleteOrDisableVenuesData(id, false, false);

                        //Get Reduced                    
                        updateresultreduced = await ltsapivenueimporthelper.DeleteOrDisableVenuesData(id, true, true);

                        updateresult.pushed = await CheckIfObjectChangedAndPush(
                                    updateresult,
                                    id,
                                    datatype
                                );

                        //Create Delete/Disable Log
                        GenericResultsHelper.GetSuccessUpdateResult(
                            id,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype.ToLower(),
                            updateresult,
                            true
                        );

                        createcounter = updateresult.created + createcounter;
                        updatecounter = updateresult.updated + updatecounter;
                        deletecounter = updateresult.deleted + deletecounter;
                        errorcounter = updateresult.error + errorcounter;

                        //Add also Reduced info
                        if (updateresultreduced.created != null)
                            createcounter = createcounter + updateresultreduced.created;
                        if (updateresultreduced.updated != null)
                            updatecounter = updatecounter + updateresultreduced.updated;
                        if (updateresultreduced.deleted != null)
                            deletecounter = deletecounter + updateresultreduced.deleted;
                        if (updateresultreduced.error != null)
                            errorcounter = errorcounter + updateresultreduced.error;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;

                case "measuringpoint":
                    LTSApiMeasuringpointImportHelper ltsapimeasuringpointimporthelper = new LTSApiMeasuringpointImportHelper(
                        settings,
                        QueryFactory,
                        "measuringpoints",
                        importerURL
                        );

                    lastchangedlist = await ltsapimeasuringpointimporthelper.GetLastDeletedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG or write directly??
                    //Use the DeleteOrDisable method?

                    //ensure lowercase ids in lastchangedlist
                    lastchangedlist = lastchangedlist.Select(x => x.ToLower()).ToList();

                    foreach (var id in lastchangedlist)
                    {
                        var updateresult = default(UpdateDetail);
                        var updateresultreduced = default(UpdateDetail);

                        updateresult = await ltsapimeasuringpointimporthelper.DeleteOrDisableMeasuringpointsData(id, false, false);

                        //Get Reduced                    
                        updateresultreduced = await ltsapimeasuringpointimporthelper.DeleteOrDisableMeasuringpointsData(id, true, true);

                        updateresult.pushed = await CheckIfObjectChangedAndPush(
                                    updateresult,
                                    id,
                                    datatype
                                );

                        //Create Delete/Disable Log
                        GenericResultsHelper.GetSuccessUpdateResult(
                            id,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype.ToLower(),
                            updateresult,
                            true
                        );

                        createcounter = updateresult.created + createcounter;
                        updatecounter = updateresult.updated + updatecounter;
                        deletecounter = updateresult.deleted + deletecounter;
                        errorcounter = updateresult.error + errorcounter;

                        //Add also Reduced info
                        if (updateresultreduced.created != null)
                            createcounter = createcounter + updateresultreduced.created;
                        if (updateresultreduced.updated != null)
                            updatecounter = updatecounter + updateresultreduced.updated;
                        if (updateresultreduced.deleted != null)
                            deletecounter = deletecounter + updateresultreduced.deleted;
                        if (updateresultreduced.error != null)
                            errorcounter = errorcounter + updateresultreduced.error;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;

                case "webcam":
                    LTSApiWebcamImportHelper ltsapiwebcamimporthelper = new LTSApiWebcamImportHelper(
                        settings,
                        QueryFactory,
                        "webcams",
                        importerURL
                        );

                    lastchangedlist = await ltsapiwebcamimporthelper.GetLastDeletedData(lastchanged, false, cancellationToken);

                    //Call Single Update and write LOG or write directly??
                    //Use the DeleteOrDisable method?

                    //ensure lowercase ids in lastchangedlist
                    lastchangedlist = lastchangedlist.Select(x => x.ToLower()).ToList();

                    foreach (var id in lastchangedlist)
                    {
                        var updateresult = default(UpdateDetail);
                        var updateresultreduced = default(UpdateDetail);

                        updateresult = await ltsapiwebcamimporthelper.DeleteOrDisableWebcamsData(id, false, false);

                        //Get Reduced                    
                        updateresultreduced = await ltsapiwebcamimporthelper.DeleteOrDisableWebcamsData(id, true, true);

                        updateresult.pushed = await CheckIfObjectChangedAndPush(
                                    updateresult,
                                    id,
                                    datatype
                                );

                        //Create Delete/Disable Log
                        GenericResultsHelper.GetSuccessUpdateResult(
                            id,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype.ToLower(),
                            updateresult,
                            true
                        );

                        createcounter = updateresult.created + createcounter;
                        updatecounter = updateresult.updated + updatecounter;
                        deletecounter = updateresult.deleted + deletecounter;
                        errorcounter = updateresult.error + errorcounter;

                        //Add also Reduced info
                        if (updateresultreduced.created != null)
                            createcounter = createcounter + updateresultreduced.created;
                        if (updateresultreduced.updated != null)
                            updatecounter = updatecounter + updateresultreduced.updated;
                        if (updateresultreduced.deleted != null)
                            deletecounter = deletecounter + updateresultreduced.deleted;
                        if (updateresultreduced.error != null)
                            errorcounter = errorcounter + updateresultreduced.error;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;

                case "accommodation":
                    LTSApiAccommodationImportHelper ltsapiaccommodationimporthelper = new LTSApiAccommodationImportHelper(
                        settings,
                        QueryFactory,
                        "accommodations",
                        importerURL
                        );

                    lastchangedlist = await ltsapiaccommodationimporthelper.GetLastDeletedData(lastchanged, false, cancellationToken);


                    foreach (var id in lastchangedlist)
                    {
                        var updateresult = default(UpdateDetail);
                        var updateresultreduced = default(UpdateDetail);

                        //Use the DeleteOrDisable method
                        updateresult = await ltsapiaccommodationimporthelper.DeleteOrDisableAccommodationsData(id, false, false);

                        //Use the DeleteOrDisable method
                        updateresultreduced = await ltsapiaccommodationimporthelper.DeleteOrDisableAccommodationsData(id, true, true);

                        //TODO WHAT AVOUT ROOMS

                        updateresult.pushed = await CheckIfObjectChangedAndPush(
                                    updateresult,
                                    id,
                                    datatype
                                );

                        //Create Delete/Disable Log
                        GenericResultsHelper.GetSuccessUpdateResult(
                            id,
                            "api",
                            "Update LTS",
                            "single.deleted",
                            "Update LTS succeeded",
                            datatype.ToLower(),
                            updateresult,
                            true
                        );

                        createcounter = updateresult.created + createcounter;
                        updatecounter = updateresult.updated + updatecounter;
                        deletecounter = updateresult.deleted + deletecounter;
                        errorcounter = updateresult.error + errorcounter;

                        //Add also Reduced info
                        if (updateresultreduced.created != null)
                            createcounter = createcounter + updateresultreduced.created;
                        if (updateresultreduced.updated != null)
                            updatecounter = updatecounter + updateresultreduced.updated;
                        if (updateresultreduced.deleted != null)
                            deletecounter = deletecounter + updateresultreduced.deleted;
                        if (updateresultreduced.error != null)
                            errorcounter = errorcounter + updateresultreduced.error;
                    }

                    updatedetail = Tuple.Create(String.Join(",", lastchangedlist), new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });

                    break;

                default:
                    throw new Exception("no match found");
            }

            return updatedetail;
        }

        //Finish Active/Inactive Sync (TO CHECK IF needed outside of Gastromy)
        public async Task<Tuple<string, UpdateDetail>> UpdateActiveInactiveDataFromLTSApi(
            bool onlyactive,
            string datatype,
            CancellationToken cancellationToken
        )
        {
            Tuple<string, UpdateDetail> updatedetail = default(Tuple<string, UpdateDetail>);
            var activelist = default(List<string>);
            var activelistinDB = default(List<string>);
            var idstodelete = default(List<string>?);
            var idstoimport = default(List<string>?);

            List<string> datatoprocesslist = new List<string>() { "full", "reduced" };

            bool reduced = false;

            List<UpdateDetail> updatedetaillist = new List<UpdateDetail>();
            List<string> updatedidlist = new List<string>();

            switch (datatype.ToLower())
            {
                case "event":
                    LTSApiEventImportHelper ltsapieventimporthelper = new LTSApiEventImportHelper(
                        settings,
                        QueryFactory,
                        "events",
                        importerURL
                        );

                    foreach (var datatoprocess in datatoprocesslist)
                    {
                        int? updatecounter = 0;
                        int? createcounter = 0;
                        int? deletecounter = 0;
                        int? errorcounter = 0;

                        if (datatoprocess == "reduced")
                            reduced = true;

                        activelist = await ltsapieventimporthelper.GetActiveList(onlyactive, reduced, cancellationToken);

                        activelistinDB = await GetAllDataBySource("event", new List<string>() { "lts" }, null, true);

                        //Compare with DB and deactivate all inactive items
                        idstodelete = activelistinDB.Where(p => !activelist.Any(p2 => p2 == p.Replace("_REDUCED", "").ToUpper())).ToList();

                        //Ids only present on LTS Interface ?
                        idstoimport = activelist.Where(p => !activelistinDB.Any(p2 => p2 == p.Replace("_REDUCED", "").ToUpper())).ToList();

                        //Delete Disable all Inactive Data from DB
                        foreach (var id in idstodelete)
                        {
                            var updateresult = default(UpdateDetail);
                            var updateresultreduced = default(UpdateDetail);

                            if (!reduced)
                            {
                                updateresult = await ltsapieventimporthelper.DeleteOrDisableEventData(id, false, false);

                                updateresult.pushed = await CheckIfObjectChangedAndPush(
                                            updateresult,
                                            id,
                                            datatype
                                        );
                            }

                            if(reduced)
                                //Get Reduced                    
                                updateresultreduced = await ltsapieventimporthelper.DeleteOrDisableEventData(id, true, true);


                            //Create Delete/Disable Log
                            GenericResultsHelper.GetSuccessUpdateResult(
                                id,
                                "api",
                                "Update LTS",
                                "single.inactivesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                updateresult,
                                true
                            );

                            createcounter = updateresult.created + createcounter;
                            updatecounter = updateresult.updated + updatecounter;
                            deletecounter = updateresult.deleted + deletecounter;
                            errorcounter = updateresult.error + errorcounter;

                            //Add also Reduced info
                            if (updateresultreduced.created != null)
                                createcounter = createcounter + updateresultreduced.created;
                            if (updateresultreduced.updated != null)
                                updatecounter = updatecounter + updateresultreduced.updated;
                            if (updateresultreduced.deleted != null)
                                deletecounter = deletecounter + updateresultreduced.deleted;
                            if (updateresultreduced.error != null)
                                errorcounter = errorcounter + updateresultreduced.error;
                        }

                        //Call Single Update for all active Items not present in DB
                        foreach (var id in idstoimport)
                        {
                            var resulttuple = await UpdateSingleDataFromLTSApi(id, "event", cancellationToken);

                            GenericResultsHelper.GetSuccessUpdateResult(
                                resulttuple.Item1,
                                "api",
                                "Update LTS",
                                "single.activesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                resulttuple.Item2,
                                true
                            );

                            createcounter = resulttuple.Item2.created + createcounter;
                            updatecounter = resulttuple.Item2.updated + updatecounter;
                            deletecounter = resulttuple.Item2.deleted + deletecounter;
                            errorcounter = resulttuple.Item2.error + errorcounter;
                        }

                        updatedetaillist.Add(new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });
                        updatedidlist.AddRange(idstodelete);
                        updatedidlist.AddRange(idstoimport);
                    }
                    break;

                case "gastronomy":
                    LTSApiGastronomyImportHelper ltsapigastroimporthelper = new LTSApiGastronomyImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    foreach(var datatoprocess in datatoprocesslist)
                    {
                        int? updatecounter = 0;
                        int? createcounter = 0;
                        int? deletecounter = 0;
                        int? errorcounter = 0;

                        if (datatoprocess == "reduced")
                            reduced = true;

                        activelist = await ltsapigastroimporthelper.GetActiveList(onlyactive, reduced, cancellationToken);
                        activelistinDB = await GetAllDataBySource("smgpois", new List<string>() { "lts" }, new List<string>() { "gastronomicdata" }, true, reduced);

                        //Compare with DB and deactivate all inactive items
                        idstodelete = activelistinDB.Where(p => !activelist.Any(p2 => p2 == p.Replace("smgpoi", "").Replace("_reduced", "").ToUpper())).ToList();

                        //Ids only present on LTS Interface ?
                        idstoimport = activelist.Where(p => !activelistinDB.Any(p2 => p2.Replace("smgpoi", "").Replace("_reduced", "").ToUpper() == p)).ToList();

                        //Delete Disable all Inactive Data from DB
                        foreach (var id in idstodelete)
                        {
                            var updateresult = default(UpdateDetail);
                            var updateresultreduced = default(UpdateDetail);

                            if(!reduced)
                            {
                                updateresult = await ltsapigastroimporthelper.DeleteOrDisableGastronomiesData(id.Replace("smgpoi",""), false, false);

                                updateresult.pushed = await CheckIfObjectChangedAndPush(
                                            updateresult,
                                            "smgpoi" + id.ToLower(),
                                            datatype
                                        );
                            }
                                
                            if(reduced)
                                updateresultreduced = await ltsapigastroimporthelper.DeleteOrDisableGastronomiesData(id.Replace("smgpoi", ""), true, true);


                            //Create Delete/Disable Log
                            GenericResultsHelper.GetSuccessUpdateResult(
                                id,
                                "api",
                                "Update LTS",
                                "single.inactivesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                updateresult,
                                true
                            );

                            if (updateresult.created != null)
                                createcounter = updateresult.created + createcounter;
                            if (updateresult.updated != null)
                                updatecounter = updateresult.updated + updatecounter;
                            if (updateresult.deleted != null)
                                deletecounter = updateresult.deleted + deletecounter;
                            if (updateresult.error != null)
                                errorcounter = updateresult.error + errorcounter;


                            //Add also Reduced info
                            if (updateresultreduced.created != null)
                                createcounter = createcounter + updateresultreduced.created;
                            if (updateresultreduced.updated != null)
                                updatecounter = updatecounter + updateresultreduced.updated;
                            if (updateresultreduced.deleted != null)
                                deletecounter = deletecounter + updateresultreduced.deleted;
                            if (updateresultreduced.error != null)
                                errorcounter = errorcounter + updateresultreduced.error;
                        }

                        //Call Single Update for all active Items not present in DB
                        //Do this only for the full workflow otherwise double import
                        foreach (var id in idstoimport)
                        {
                            var resulttuple = await UpdateSingleDataFromLTSApi(id, "gastronomy", cancellationToken);

                            GenericResultsHelper.GetSuccessUpdateResult(
                                resulttuple.Item1,
                                "api",
                                "Update LTS",
                                "single.activesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                resulttuple.Item2,
                                true
                            );

                            createcounter = resulttuple.Item2.created + createcounter;
                            updatecounter = resulttuple.Item2.updated + updatecounter;
                            deletecounter = resulttuple.Item2.deleted + deletecounter;
                            errorcounter = resulttuple.Item2.error + errorcounter;
                        }

                        updatedetaillist.Add(new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });
                        updatedidlist.AddRange(idstodelete);
                        updatedidlist.AddRange(idstoimport);
                    }

                    break;

                case "poi":
                    LTSApiPoiImportHelper ltsapipoiimporthelper = new LTSApiPoiImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    foreach (var datatoprocess in datatoprocesslist)
                    {
                        int? updatecounter = 0;
                        int? createcounter = 0;
                        int? deletecounter = 0;
                        int? errorcounter = 0;

                        if (datatoprocess == "reduced")
                            reduced = true;

                        activelist = await ltsapipoiimporthelper.GetActiveList(onlyactive, reduced, cancellationToken);
                        activelistinDB = await GetAllDataBySource("smgpois", new List<string>() { "lts" }, new List<string>() { "poidata" }, true, reduced);

                        //Compare with DB and deactivate all inactive items
                        idstodelete = activelistinDB.Where(p => !activelist.Any(p2 => p2 == p.Replace("smgpoi", "").Replace("_reduced", "").ToUpper())).ToList();

                        //Ids only present on LTS Interface ?
                        idstoimport = activelist.Where(p => !activelistinDB.Any(p2 => p2.Replace("smgpoi", "").Replace("_reduced", "").ToUpper() == p)).ToList();

                        //Delete Disable all Inactive Data from DB
                        foreach (var id in idstodelete)
                        {
                            var updateresult = default(UpdateDetail);
                            var updateresultreduced = default(UpdateDetail);

                            if (!reduced)
                            {
                                updateresult = await ltsapipoiimporthelper.DeleteOrDisablePoisData(id.Replace("smgpoi", ""), false, false);

                                updateresult.pushed = await CheckIfObjectChangedAndPush(
                                            updateresult,
                                            "smgpoi" + id.ToLower(),
                                            datatype
                                        );
                            }

                            if (reduced)
                                updateresultreduced = await ltsapipoiimporthelper.DeleteOrDisablePoisData(id.Replace("smgpoi", ""), true, true);


                            //Create Delete/Disable Log
                            GenericResultsHelper.GetSuccessUpdateResult(
                                id,
                                "api",
                                "Update LTS",
                                "single.inactivesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                updateresult,
                                true
                            );

                            if (updateresult.created != null)
                                createcounter = updateresult.created + createcounter;
                            if (updateresult.updated != null)
                                updatecounter = updateresult.updated + updatecounter;
                            if (updateresult.deleted != null)
                                deletecounter = updateresult.deleted + deletecounter;
                            if (updateresult.error != null)
                                errorcounter = updateresult.error + errorcounter;


                            //Add also Reduced info
                            if (updateresultreduced.created != null)
                                createcounter = createcounter + updateresultreduced.created;
                            if (updateresultreduced.updated != null)
                                updatecounter = updatecounter + updateresultreduced.updated;
                            if (updateresultreduced.deleted != null)
                                deletecounter = deletecounter + updateresultreduced.deleted;
                            if (updateresultreduced.error != null)
                                errorcounter = errorcounter + updateresultreduced.error;
                        }

                        //Call Single Update for all active Items not present in DB
                        foreach (var id in idstoimport)
                        {
                            var resulttuple = await UpdateSingleDataFromLTSApi(id, "poi", cancellationToken);

                            GenericResultsHelper.GetSuccessUpdateResult(
                                resulttuple.Item1,
                                "api",
                                "Update LTS",
                                "single.activesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                resulttuple.Item2,
                                true
                            );

                            createcounter = resulttuple.Item2.created + createcounter;
                            updatecounter = resulttuple.Item2.updated + updatecounter;
                            deletecounter = resulttuple.Item2.deleted + deletecounter;
                            errorcounter = resulttuple.Item2.error + errorcounter;
                        }

                        updatedetaillist.Add(new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });
                        updatedidlist.AddRange(idstodelete);
                        updatedidlist.AddRange(idstoimport);
                    }

                    break;

                case "activity":
                    LTSApiActivityImportHelper ltsapiactivityimporthelper = new LTSApiActivityImportHelper(
                        settings,
                        QueryFactory,
                        "smgpois",
                        importerURL
                        );

                    foreach (var datatoprocess in datatoprocesslist)
                    {
                        int? updatecounter = 0;
                        int? createcounter = 0;
                        int? deletecounter = 0;
                        int? errorcounter = 0;

                        if (datatoprocess == "reduced")
                            reduced = true;

                        activelist = await ltsapiactivityimporthelper.GetActiveList(onlyactive, reduced, cancellationToken);
                        activelistinDB = await GetAllDataBySource("smgpois", new List<string>() { "lts" }, new List<string>() { "activitydata" }, true, reduced);

                        //Compare with DB and deactivate all inactive items
                        idstodelete = activelistinDB.Where(p => !activelist.Any(p2 => p2 == p.Replace("smgpoi", "").Replace("_reduced", "").ToUpper())).ToList();

                        //Ids only present on LTS Interface ?
                        idstoimport = activelist.Where(p => !activelistinDB.Any(p2 => p2.Replace("smgpoi", "").Replace("_reduced", "").ToUpper() == p)).ToList();

                        //Delete Disable all Inactive Data from DB
                        foreach (var id in idstodelete)
                        {
                            var updateresult = default(UpdateDetail);
                            var updateresultreduced = default(UpdateDetail);

                            if (!reduced)
                            {
                                updateresult = await ltsapiactivityimporthelper.DeleteOrDisableActivitiesData(id.Replace("smgpoi", ""), false, false);

                                updateresult.pushed = await CheckIfObjectChangedAndPush(
                                            updateresult,
                                            id,
                                            datatype
                                        );
                            }

                            if (reduced)
                                updateresultreduced = await ltsapiactivityimporthelper.DeleteOrDisableActivitiesData(id.Replace("smgpoi", ""), true, true);


                            //Create Delete/Disable Log
                            GenericResultsHelper.GetSuccessUpdateResult(
                                id,
                                "api",
                                "Update LTS",
                                "single.inactivesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                updateresult,
                                true
                            );

                            if (updateresult.created != null)
                                createcounter = updateresult.created + createcounter;
                            if (updateresult.updated != null)
                                updatecounter = updateresult.updated + updatecounter;
                            if (updateresult.deleted != null)
                                deletecounter = updateresult.deleted + deletecounter;
                            if (updateresult.error != null)
                                errorcounter = updateresult.error + errorcounter;


                            //Add also Reduced info
                            if (updateresultreduced.created != null)
                                createcounter = createcounter + updateresultreduced.created;
                            if (updateresultreduced.updated != null)
                                updatecounter = updatecounter + updateresultreduced.updated;
                            if (updateresultreduced.deleted != null)
                                deletecounter = deletecounter + updateresultreduced.deleted;
                            if (updateresultreduced.error != null)
                                errorcounter = errorcounter + updateresultreduced.error;
                        }

                        //Call Single Update for all active Items not present in DB
                        foreach (var id in idstoimport)
                        {
                            var resulttuple = await UpdateSingleDataFromLTSApi(id, "activity", cancellationToken);

                            GenericResultsHelper.GetSuccessUpdateResult(
                                resulttuple.Item1,
                                "api",
                                "Update LTS",
                                "single.activesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                resulttuple.Item2,
                                true
                            );

                            createcounter = resulttuple.Item2.created + createcounter;
                            updatecounter = resulttuple.Item2.updated + updatecounter;
                            deletecounter = resulttuple.Item2.deleted + deletecounter;
                            errorcounter = resulttuple.Item2.error + errorcounter;
                        }

                        updatedetaillist.Add(new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });
                        updatedidlist.AddRange(idstodelete);
                        updatedidlist.AddRange(idstoimport);
                    }

                    break;

                case "venue":
                    LTSApiVenueImportHelper ltsapivenueimporthelper = new LTSApiVenueImportHelper(
                        settings,
                        QueryFactory,
                        "venues",
                        importerURL
                        );

                    foreach (var datatoprocess in datatoprocesslist)
                    {
                        int? updatecounter = 0;
                        int? createcounter = 0;
                        int? deletecounter = 0;
                        int? errorcounter = 0;

                        if (datatoprocess == "reduced")
                            reduced = true;

                        activelist = await ltsapivenueimporthelper.GetActiveList(onlyactive, reduced, cancellationToken);
                        activelistinDB = await GetAllDataBySource("venues", new List<string>() { "lts" }, null, true, reduced);

                        //Compare with DB and deactivate all inactive items //to check reduced in lowercase???
                        idstodelete = activelistinDB.Where(p => !activelist.Any(p2 => p2 == p.Replace("_REDUCED", "").ToUpper())).ToList();

                        //Ids only present on LTS Interface ?
                        idstoimport = activelist.Where(p => !activelistinDB.Any(p2 => p2.Replace("_REDUCED", "").ToUpper() == p)).ToList();

                        //Delete Disable all Inactive Data from DB
                        foreach (var id in idstodelete)
                        {
                            var updateresult = default(UpdateDetail);
                            var updateresultreduced = default(UpdateDetail);

                            if (!reduced)
                            {
                                updateresult = await ltsapivenueimporthelper.DeleteOrDisableVenuesData(id, false, false);

                                updateresult.pushed = await CheckIfObjectChangedAndPush(
                                            updateresult,
                                            id,
                                            datatype
                                        );
                            }

                            if (reduced)
                                updateresultreduced = await ltsapivenueimporthelper.DeleteOrDisableVenuesData(id, true, true);


                            //Create Delete/Disable Log
                            GenericResultsHelper.GetSuccessUpdateResult(
                                id,
                                "api",
                                "Update LTS",
                                "single.inactivesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                updateresult,
                                true
                            );

                            if (updateresult.created != null)
                                createcounter = updateresult.created + createcounter;
                            if (updateresult.updated != null)
                                updatecounter = updateresult.updated + updatecounter;
                            if (updateresult.deleted != null)
                                deletecounter = updateresult.deleted + deletecounter;
                            if (updateresult.error != null)
                                errorcounter = updateresult.error + errorcounter;


                            //Add also Reduced info
                            if (updateresultreduced.created != null)
                                createcounter = createcounter + updateresultreduced.created;
                            if (updateresultreduced.updated != null)
                                updatecounter = updatecounter + updateresultreduced.updated;
                            if (updateresultreduced.deleted != null)
                                deletecounter = deletecounter + updateresultreduced.deleted;
                            if (updateresultreduced.error != null)
                                errorcounter = errorcounter + updateresultreduced.error;
                        }

                        //Call Single Update for all active Items not present in DB
                        foreach (var id in idstoimport)
                        {
                            var resulttuple = await UpdateSingleDataFromLTSApi(id, "venue", cancellationToken);

                            GenericResultsHelper.GetSuccessUpdateResult(
                                resulttuple.Item1,
                                "api",
                                "Update LTS",
                                "single.activesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                resulttuple.Item2,
                                true
                            );

                            createcounter = resulttuple.Item2.created + createcounter;
                            updatecounter = resulttuple.Item2.updated + updatecounter;
                            deletecounter = resulttuple.Item2.deleted + deletecounter;
                            errorcounter = resulttuple.Item2.error + errorcounter;
                        }

                        updatedetaillist.Add(new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });
                        updatedidlist.AddRange(idstodelete);
                        updatedidlist.AddRange(idstoimport);
                    }

                    break;

                case "measuringpoint":
                    LTSApiMeasuringpointImportHelper ltsapimeasuringpointimporthelper = new LTSApiMeasuringpointImportHelper(
                        settings,
                        QueryFactory,
                        "measuringpoints",
                        importerURL
                        );

                    foreach (var datatoprocess in datatoprocesslist)
                    {
                        int? updatecounter = 0;
                        int? createcounter = 0;
                        int? deletecounter = 0;
                        int? errorcounter = 0;

                        if (datatoprocess == "reduced")
                            reduced = true;

                        activelist = await ltsapimeasuringpointimporthelper.GetActiveList(onlyactive, reduced, cancellationToken);
                        activelistinDB = await GetAllDataBySource("measuringpoints", new List<string>() { "lts" }, null, true, reduced);

                        //Compare with DB and deactivate all inactive items
                        idstodelete = activelistinDB.Where(p => !activelist.Any(p2 => p2 == p.Replace("_REDUCED", "").ToUpper())).ToList();

                        //Ids only present on LTS Interface ?
                        idstoimport = activelist.Where(p => !activelistinDB.Any(p2 => p2.Replace("_REDUCED", "").ToUpper() == p)).ToList();

                        //Delete Disable all Inactive Data from DB
                        foreach (var id in idstodelete)
                        {
                            var updateresult = default(UpdateDetail);
                            var updateresultreduced = default(UpdateDetail);

                            if (!reduced)
                            {
                                updateresult = await ltsapimeasuringpointimporthelper.DeleteOrDisableMeasuringpointsData(id, false, false);

                                updateresult.pushed = await CheckIfObjectChangedAndPush(
                                            updateresult,
                                            id,
                                            datatype
                                        );
                            }

                            if (reduced)
                                updateresultreduced = await ltsapimeasuringpointimporthelper.DeleteOrDisableMeasuringpointsData(id, true, true);


                            //Create Delete/Disable Log
                            GenericResultsHelper.GetSuccessUpdateResult(
                                id,
                                "api",
                                "Update LTS",
                                "single.inactivesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                updateresult,
                                true
                            );

                            if (updateresult.created != null)
                                createcounter = updateresult.created + createcounter;
                            if (updateresult.updated != null)
                                updatecounter = updateresult.updated + updatecounter;
                            if (updateresult.deleted != null)
                                deletecounter = updateresult.deleted + deletecounter;
                            if (updateresult.error != null)
                                errorcounter = updateresult.error + errorcounter;


                            //Add also Reduced info
                            if (updateresultreduced.created != null)
                                createcounter = createcounter + updateresultreduced.created;
                            if (updateresultreduced.updated != null)
                                updatecounter = updatecounter + updateresultreduced.updated;
                            if (updateresultreduced.deleted != null)
                                deletecounter = deletecounter + updateresultreduced.deleted;
                            if (updateresultreduced.error != null)
                                errorcounter = errorcounter + updateresultreduced.error;
                        }

                        //Call Single Update for all active Items not present in DB
                        foreach (var id in idstoimport)
                        {
                            var resulttuple = await UpdateSingleDataFromLTSApi(id, "measuringpoint", cancellationToken);

                            GenericResultsHelper.GetSuccessUpdateResult(
                                resulttuple.Item1,
                                "api",
                                "Update LTS",
                                "single.activesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                resulttuple.Item2,
                                true
                            );

                            createcounter = resulttuple.Item2.created + createcounter;
                            updatecounter = resulttuple.Item2.updated + updatecounter;
                            deletecounter = resulttuple.Item2.deleted + deletecounter;
                            errorcounter = resulttuple.Item2.error + errorcounter;
                        }

                        updatedetaillist.Add(new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });
                        updatedidlist.AddRange(idstodelete);
                        updatedidlist.AddRange(idstoimport);
                    }

                    break;

                case "webcam":
                    LTSApiWebcamImportHelper ltsapiwebcamimporthelper = new LTSApiWebcamImportHelper(
                        settings,
                        QueryFactory,
                        "webcams",
                        importerURL
                        );

                    foreach (var datatoprocess in datatoprocesslist)
                    {
                        int? updatecounter = 0;
                        int? createcounter = 0;
                        int? deletecounter = 0;
                        int? errorcounter = 0;

                        if (datatoprocess == "reduced")
                            reduced = true;

                        activelist = await ltsapiwebcamimporthelper.GetActiveList(onlyactive, reduced, cancellationToken);
                        activelistinDB = await GetAllDataBySource("webcams", new List<string>() { "lts" }, null, true, reduced);

                        //Compare with DB and deactivate all inactive items
                        idstodelete = activelistinDB.Where(p => !activelist.Any(p2 => p2 == p.Replace("_reduced", "").ToUpper())).ToList();

                        //Ids only present on LTS Interface ?
                        idstoimport = activelist.Where(p => !activelistinDB.Any(p2 => p2.Replace("_reduced", "").ToUpper() == p)).ToList();

                        //Delete Disable all Inactive Data from DB
                        foreach (var id in idstodelete)
                        {
                            var updateresult = default(UpdateDetail);
                            var updateresultreduced = default(UpdateDetail);

                            if (!reduced)
                            {
                                updateresult = await ltsapiwebcamimporthelper.DeleteOrDisableWebcamsData(id, false, false);

                                updateresult.pushed = await CheckIfObjectChangedAndPush(
                                            updateresult,
                                            id,
                                            datatype
                                        );
                            }

                            if (reduced)
                                updateresultreduced = await ltsapiwebcamimporthelper.DeleteOrDisableWebcamsData(id, true, true);


                            //Create Delete/Disable Log
                            GenericResultsHelper.GetSuccessUpdateResult(
                                id,
                                "api",
                                "Update LTS",
                                "single.inactivesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                updateresult,
                                true
                            );

                            if (updateresult.created != null)
                                createcounter = updateresult.created + createcounter;
                            if (updateresult.updated != null)
                                updatecounter = updateresult.updated + updatecounter;
                            if (updateresult.deleted != null)
                                deletecounter = updateresult.deleted + deletecounter;
                            if (updateresult.error != null)
                                errorcounter = updateresult.error + errorcounter;


                            //Add also Reduced info
                            if (updateresultreduced.created != null)
                                createcounter = createcounter + updateresultreduced.created;
                            if (updateresultreduced.updated != null)
                                updatecounter = updatecounter + updateresultreduced.updated;
                            if (updateresultreduced.deleted != null)
                                deletecounter = deletecounter + updateresultreduced.deleted;
                            if (updateresultreduced.error != null)
                                errorcounter = errorcounter + updateresultreduced.error;
                        }

                        //Call Single Update for all active Items not present in DB
                        foreach (var id in idstoimport)
                        {
                            var resulttuple = await UpdateSingleDataFromLTSApi(id, "webcam", cancellationToken);

                            GenericResultsHelper.GetSuccessUpdateResult(
                                resulttuple.Item1,
                                "api",
                                "Update LTS",
                                "single.activesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                resulttuple.Item2,
                                true
                            );

                            createcounter = resulttuple.Item2.created + createcounter;
                            updatecounter = resulttuple.Item2.updated + updatecounter;
                            deletecounter = resulttuple.Item2.deleted + deletecounter;
                            errorcounter = resulttuple.Item2.error + errorcounter;
                        }

                        updatedetaillist.Add(new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });
                        updatedidlist.AddRange(idstodelete);
                        updatedidlist.AddRange(idstoimport);
                    }

                    break;

                case "accommodation":
                    LTSApiAccommodationImportHelper ltsapiaccommodationimporthelper = new LTSApiAccommodationImportHelper(
                        settings,
                        QueryFactory,
                        "accommodations",
                        importerURL
                        );

                    foreach (var datatoprocess in datatoprocesslist)
                    {
                        int? updatecounter = 0;
                        int? createcounter = 0;
                        int? deletecounter = 0;
                        int? errorcounter = 0;

                        if (datatoprocess == "reduced")
                            reduced = true;

                        activelist = await ltsapiaccommodationimporthelper.GetActiveList(onlyactive, reduced, cancellationToken);

                        activelistinDB = await GetAllDataBySource("accommodation", new List<string>() { "lts" }, null, true);

                        //Compare with DB and deactivate all inactive items
                        idstodelete = activelistinDB.Where(p => !activelist.Any(p2 => p2 == p.Replace("_REDUCED", "").ToUpper())).ToList();

                        //Ids only present on LTS Interface ?
                        idstoimport = activelist.Where(p => !activelistinDB.Any(p2 => p2 == p.Replace("_REDUCED", "").ToUpper())).ToList();

                        //Delete Disable all Inactive Data from DB
                        foreach (var id in idstodelete)
                        {
                            var updateresult = default(UpdateDetail);
                            var updateresultreduced = default(UpdateDetail);

                            if (!reduced)
                            {
                                updateresult = await ltsapiaccommodationimporthelper.DeleteOrDisableAccommodationsData(id, false, false);

                                //TODO ACCOMMODATION ROOMS

                                updateresult.pushed = await CheckIfObjectChangedAndPush(
                                            updateresult,
                                            id,
                                            datatype
                                        );
                            }

                            if (reduced)
                                //Get Reduced                    
                                updateresultreduced = await ltsapiaccommodationimporthelper.DeleteOrDisableAccommodationsData(id, true, true);


                            //Create Delete/Disable Log
                            GenericResultsHelper.GetSuccessUpdateResult(
                                id,
                                "api",
                                "Update LTS",
                                "single.inactivesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                updateresult,
                                true
                            );

                            createcounter = updateresult.created + createcounter;
                            updatecounter = updateresult.updated + updatecounter;
                            deletecounter = updateresult.deleted + deletecounter;
                            errorcounter = updateresult.error + errorcounter;

                            //Add also Reduced info
                            if (updateresultreduced.created != null)
                                createcounter = createcounter + updateresultreduced.created;
                            if (updateresultreduced.updated != null)
                                updatecounter = updatecounter + updateresultreduced.updated;
                            if (updateresultreduced.deleted != null)
                                deletecounter = deletecounter + updateresultreduced.deleted;
                            if (updateresultreduced.error != null)
                                errorcounter = errorcounter + updateresultreduced.error;
                        }

                        //Call Single Update for all active Items not present in DB
                        foreach (var id in idstoimport)
                        {
                            var resulttuple = await UpdateSingleDataFromLTSApi(id, "accommodation", cancellationToken);

                            GenericResultsHelper.GetSuccessUpdateResult(
                                resulttuple.Item1,
                                "api",
                                "Update LTS",
                                "single.activesync",
                                "Update LTS succeeded",
                                datatype.ToLower(),
                                resulttuple.Item2,
                                true
                            );

                            createcounter = resulttuple.Item2.created + createcounter;
                            updatecounter = resulttuple.Item2.updated + updatecounter;
                            deletecounter = resulttuple.Item2.deleted + deletecounter;
                            errorcounter = resulttuple.Item2.error + errorcounter;
                        }

                        updatedetaillist.Add(new UpdateDetail() { error = errorcounter, updated = updatecounter, created = createcounter, deleted = deletecounter });
                        updatedidlist.AddRange(idstodelete);
                        updatedidlist.AddRange(idstoimport);
                    }
                    break;

                default:
                    throw new Exception("no match found");
            }
            updatedetail = Tuple.Create(String.Join(",", updatedidlist), GenericResultsHelper.MergeUpdateDetail(updatedetaillist));

            return updatedetail;
        }

        /// <summary>
        /// Get All Data by passed Source
        /// </summary>
        /// <param name="syncsourcelist"></param>
        /// <param name="syncsourceinterfacelist"></param>
        /// <returns></returns>
        public async Task<List<string>> GetAllDataBySource(
            string table,
            List<string> syncsourcelist,
            List<string>? syncsourceinterfacelist = null,
            bool? onlyactive = true,
            bool? reduced = false
        )
        {
            var query = QueryFactory
                .Query(table)
                .Select("id")
                .SourceFilter_GeneratedColumn(syncsourcelist)
                .When(
                    syncsourceinterfacelist != null,
                    x => x.SyncSourceInterfaceFilter_GeneratedColumn(syncsourceinterfacelist)
                )
                .When(
                    onlyactive != null,
                    x => x.ActiveFilter_GeneratedColumn(onlyactive)
                )
                .When(
                    reduced != null,
                    x => x.Where("gen_reduced", reduced)
                );

            var idlist = await query.GetAsync<string>();

            return idlist.ToList();
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

        public static async Task<IDictionary<string, JArray>> LoadJsonFiles(string directory, List<string> filenames)
        {
            IDictionary<string, JArray> myjsonfiles = new Dictionary<string, JArray>();
            foreach (var filename in filenames)
            {
                myjsonfiles.Add(filename, await LoadFromJsonAndDeSerialize(filename, directory));
            }
            return myjsonfiles;
        }

        public static async Task<JArray> LoadFromJsonAndDeSerialize(string filename, string path)
        {
            using (StreamReader r = new StreamReader(Path.Combine(path, filename + ".json")))
            {
                string json = await r.ReadToEndAsync();

                return JArray.Parse(json) ?? new JArray();
            }
        }

        public static IDictionary<string, XDocument> LoadXmlFiles(string directory, string filename)
        {
            //TODO move this files to Database

            IDictionary<string, XDocument> myxmlfiles = new Dictionary<string, XDocument>();
            myxmlfiles.Add(filename, XDocument.Load(directory + filename + ".xml"));

            return myxmlfiles;
        }
    }
}


