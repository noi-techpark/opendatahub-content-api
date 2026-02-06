// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using Helper.Tagging;
using LTSAPI;
using LTSAPI.Parser;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhApiImporter.Helpers.RAVEN;
using SqlKata.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiWebcamImportHelper : ImportHelper, IImportHelperLTS
    {
        public bool opendata = false;

        public LTSApiWebcamImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) { }


        //Not implemented here
        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<UpdateDetail>> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public async Task<UpdateDetail> SaveSingleDataToODH(
            string id,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            opendata = reduced;

            //Import the List
            var webcamlts = await GetWebcamsFromLTSV2(id, null, null, null);

            //Check if Data is accessible on LTS
            if (webcamlts != null && webcamlts.FirstOrDefault().ContainsKey("success") && (Boolean)webcamlts.FirstOrDefault()["success"]) //&& gastronomylts.FirstOrDefault()["Success"] == true
            {     //Import Single Data & Deactivate Data
                var result = await SaveWebcamsToPG(webcamlts);
                return result;
            }
            //If data is not accessible on LTS Side, delete or disable it
            else if (webcamlts != null && webcamlts.FirstOrDefault().ContainsKey("status") && ((int)webcamlts.FirstOrDefault()["status"] == 403 || (int)webcamlts.FirstOrDefault()["status"] == 404))
            {
                if (!opendata)
                {
                    //Data is pushed to marketplace with disabled status
                    return await DeleteOrDisableWebcamsData(id, false, false);
                }
                else
                {
                    //Data is pushed to marketplace as deleted
                    return await DeleteOrDisableWebcamsData(id, true, true);
                }
            }
            else
            {
                return new UpdateDetail()
                {
                    updated = 0,
                    created = 0,
                    deleted = 0,
                    error = 1,
                };
            }
        }

        public async Task<List<string>> GetLastChangedData(
           DateTime lastchanged,
           bool reduced = false,
           CancellationToken cancellationToken = default
       )
        {
            //Import the List
            var lastchangedlts = await GetWebcamsFromLTSV2(null, lastchanged, null, null);
            List<string> lastchangedlist = new List<string>();

            if (lastchangedlts != null && lastchangedlts.FirstOrDefault().ContainsKey("success") && (Boolean)lastchangedlts.FirstOrDefault()["success"])
            {
                var lastchangedrids = lastchangedlts.FirstOrDefault()["data"].ToObject<List<LtsRidList>>();

                lastchangedlist = lastchangedrids.Select(x => x.rid).ToList();
            }
            else
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "lastchanged.webcams",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.webcams",
                        success = false,
                        error = "Could not fetch last changed List",
                    }
                );
            }

            return lastchangedlist;
        }

        public async Task<List<string>> GetLastDeletedData(
            DateTime deletedfrom,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            //Import the List
            var deletedlts = await GetWebcamsFromLTSV2(null, null, deletedfrom, null);
            List<string> lastdeletedlist = new List<string>();

            if (deletedlts != null && deletedlts.FirstOrDefault().ContainsKey("success") && (Boolean)deletedlts.FirstOrDefault()["success"])
            {
                var lastchangedrids = deletedlts.FirstOrDefault()["data"].ToObject<List<LtsRidList>>();

                lastdeletedlist = lastchangedrids.Select(x => x.rid).ToList();
            }
            else
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deleted.webcams",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.webcams",
                        success = false,
                        error = "Could not fetch deleted List",
                    }
                );
            }

            return lastdeletedlist;
        }

        public async Task<List<string>> GetActiveList(
            bool active,
            bool reduced = false,
            CancellationToken cancellationToken = default
        )
        {
            opendata = reduced;

            //Import the List
            var activelistlts = await GetWebcamsFromLTSV2(null, null, null, active);
            List<string> activeList = new List<string>();

            if (activelistlts != null && activelistlts.FirstOrDefault().ContainsKey("success") && (Boolean)activelistlts.FirstOrDefault()["success"])
            {
                var activerids = activelistlts.FirstOrDefault()["data"].ToObject<List<LtsRidList>>();

                activeList = activerids.Select(x => x.rid).ToList();
            }
            else
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "active.webcams",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.webcams",
                        success = false,
                        error = "Could not fetch active List",
                    }
                );
            }

            return activeList;
        }

        private async Task<List<JObject>> GetWebcamsFromLTSV2(string webcamid, DateTime? lastchanged, DateTime? deletedfrom, bool? activelist)
        {
            try
            {
                LtsApi ltsapi = GetLTSApi(opendata);
                
                if(webcamid != null)
                {
                    //Get Single Venue

                    var qs = new LTSQueryStrings() { page_size = 1 };
                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.WebcamDetailRequest(webcamid, dict);
                }
                else if (lastchanged != null)
                {
                    //Get the Last Changed Webcams list

                    var qs = new LTSQueryStrings() { fields = "rid" };

                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.WebcamListRequest(dict, true);
                }
                else if (deletedfrom != null)
                {
                    //Get the Active Webcams list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid" };
                
                    if (deletedfrom != null)
                        qs.filter_lastUpdate = deletedfrom;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.WebcamDeletedRequest(dict, true);
                }
                else if (activelist != null)
                {
                    //Get the Active Webcams list with filter[onlyActive]=1&fields=rid&filter[onlyTourismOrganizationMember]=0&filter[representationMode]=full

                    var qs = new LTSQueryStrings() { fields = "rid", filter_onlyActive = true, filter_representationMode = "full" };
                
                    if (lastchanged != null)
                        qs.filter_lastUpdate = lastchanged;

                    var dict = ltsapi.GetLTSQSDictionary(qs);

                    return await ltsapi.WebcamListRequest(dict, true);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.webcams",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.webcams",
                        success = false,
                        error = ex.Message,
                    }
                );
                return null;
            }
        }

        private async Task<UpdateDetail> SaveWebcamsToPG(List<JObject> ltsdata)
        {
            //var newimportcounter = 0;
            //var updateimportcounter = 0;
            //var errorimportcounter = 0;
            //var deleteimportcounter = 0;

            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();

                List<LTSWebcam> webcamdata = new List<LTSWebcam>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    webcamdata.Add(
                        ltsdatasingle.ToObject<LTSWebcam>()
                    );
                }

                //Load the json Data
                //IDictionary<string, JArray> jsondata = default(Dictionary<string, JArray>);

                //if (!opendata)
                //{
                //    jsondata = await LTSAPIImportHelper.LoadJsonFiles(
                //    settings.JsonConfig.Jsondir,
                //    new List<string>()
                //        {
                //           "GenericTags",
                //           "AutoPublishTags"
                //        }
                //    );
                //}

                foreach (var data in webcamdata)
                {
                    string id = data.data.rid.ToUpper();

                    var webcamparsed = WebcamInfoParser.ParseLTSWebcam(data.data, false);
                    
                    //GET OLD Webcam
                    var webcamindb = await LoadDataFromDB<WebcamInfoLinked>(id, IDStyle.uppercase);

                    //Add manual assigned Tags to TagIds TO check if this should be activated
                    await MergeWebcamTags(webcamparsed, webcamindb);
              
                    if (!opendata)
                    {
                        
                    }

                    //Create Tags and preserve the old TagEntries
                    await webcamparsed.UpdateTagsExtension(QueryFactory, null);

                    var result = await InsertDataToDB(webcamparsed, data.data);

                    updatedetails.Add(result);

                    idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.webcams",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.webcams",
                            success = true,
                            error = "",
                        }
                    );
                }          
            }
            else
            {
                updatedetails.Add(new UpdateDetail()
                {
                    created = 0,
                    updated = 0,
                    deleted = 0,
                    error = 1,
                    objectchanged = 0,
                    objectimagechanged = 0,
                    objectcompared = 0,
                    pushchannels = null,
                    changes = null                    
                });
            }

            return updatedetails.FirstOrDefault();
        }

        private async Task<UpdateDetail> InsertDataToDB(
            WebcamInfoLinked objecttosave,
            LTSWebcamData webcamlts
            //IDictionary<string, JArray>? jsonfiles
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = LicenseHelper.GetLicenseforWebcam(objecttosave, opendata);

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave, opendata);

                //Set PublishedOn
                objecttosave.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(webcamlts);
                
                objecttosave.Id = objecttosave.Id.ToUpper();

                return await QueryFactory.UpsertData<WebcamInfoLinked>(
                    objecttosave,
                    new DataInfo("webcams", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                    new EditInfo("lts.webcams.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid,
                    opendata
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(LTSWebcamData webcamlts)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "lts",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(webcamlts),
                    sourceinterface = "webcams",
                    sourceid = webcamlts.rid,
                    sourceurl = "https://go.lts.it/api/v1/webcams",
                    type = "webcam",
                    license = "open",
                    rawformat = "json",
                }
            );
        }
        
        public async Task<UpdateDetail> DeleteOrDisableWebcamsData(string id, bool delete, bool reduced)
        {
            UpdateDetail result = default(UpdateDetail);

            if (delete)
            {
                result = await QueryFactory.DeleteData<WebcamInfoLinked>(
                id.ToUpper(),
                new DataInfo("webcams", CRUDOperation.Delete),
                new CRUDConstraints(),
                reduced
                );
                
            }
            else
            {
                var query = QueryFactory.Query(table).Select("data").Where("id", id.ToUpper());

                var data = await query.GetObjectSingleAsync<WebcamInfoLinked>();

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

                        //Recreate PublishedOn Helper for not active Items
                        data.CreatePublishedOnList();

                        result = await QueryFactory.UpsertData<WebcamInfoLinked>(
                               data,
                               new DataInfo("webcams", Helper.Generic.CRUDOperation.CreateAndUpdate, !opendata),
                               new EditInfo("lts.webcams.import.deactivate", importerURL),
                               new CRUDConstraints(),
                               new CompareConfig(true, false)
                        );                        
                    }
                }
            }

            return result;
        }

     
        private async Task MergeWebcamTags(WebcamInfoLinked vNew, WebcamInfoLinked vOld)
        {
            if (vOld != null)
            {
                //Readd all Redactional Tags to check if this query fits
                var redactionalassignedTags = vOld.Tags != null ? vOld.Tags.Where(x => x.Source != "lts" && x.Source != "idm").ToList() : null;
                if (redactionalassignedTags != null)
                {
                    foreach (var tag in redactionalassignedTags)
                    {
                        vNew.TagIds.Add(tag.Id);
                    }
                }
            }

            //TODO import ODHTags (eating drinking, gastronomy etc...) to Tags?

            //TODO import the Redactional Tags from SmgTags into Tags?
        }   

    }
}
