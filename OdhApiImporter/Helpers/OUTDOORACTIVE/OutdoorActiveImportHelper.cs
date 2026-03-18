// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using LTSAPI;
using OdhNotifier;
using OUTDOORACTIVE;
using OUTDOORACTIVE.Parser;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers
{
    public class OutdoorActiveImportHelper : ImportHelper, IImportHelper
    {
        //lts-tours, lts-points
        public string? type { get; set; }
        public DateTime? updatefrom { get; set; }

        public bool syncelevation { get; set; }

        private IOdhPushNotifier OdhPushnotifier;

        public OutdoorActiveImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL)
        {
            this.OdhPushnotifier = odhpushnotifier;
            updatefrom = DateTime.Now.AddDays(-1);
            syncelevation = false;
        }

        public void SetType(string typepassed)
        {
            if (typepassed == "activity")
                this.type = "lts-tours-new-template";
            else if (typepassed == "poi")
                this.type = "lts-poi-new-template";
            else
                throw new Exception("invalid type passed");
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            if (type == null)
                throw new Exception("no type given");

            //GET Data
            var data = await GetData(cancellationToken);

            //UPDATE all data
            var updateresult = await ImportData(data, cancellationToken);
            
            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult }
            );
        }

        //Get Data from Source
        private async Task<XDocument> GetData(CancellationToken cancellationToken)
        {
            return await GetOutDooractiveData.GetOutdooractiveData(type, settings.OutdooractiveConfig.ServiceUrl);
        }

        //Get Data from OA Single
        private async Task<XDocument> GetDataSingle(string id, CancellationToken cancellationToken)
        {            
            return await GetOutDooractiveData.GetOutdooractiveDetail(id, "de", settings.OutdooractiveConfig.Password, settings.OutdooractiveConfig.ServiceUrlDetail);
        }

        //Import the Data
        public async Task<UpdateDetail> ImportData(
            XDocument oadata,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            if (
                oadata != null
                && oadata.Root.Elements("content") != null
            )
            {
                //loop trough outdooractive items
                foreach (XElement oadatael in oadata.Root.Elements("content"))
                {
                    var importresult = await ImportDataSingle(oadatael, cancellationToken);

                    newcounter = newcounter + importresult.created ?? newcounter;
                    updatecounter = updatecounter + importresult.updated ?? updatecounter;
                    errorcounter = errorcounter + importresult.error ?? errorcounter;
                }
            }

            return new UpdateDetail()
            {
                created = newcounter,
                updated = updatecounter,
                deleted = deletecounter,
                error = errorcounter,
            };
        }

        //Parsing the Data
        public async Task<UpdateDetail> ImportDataSingle(XElement oadata, CancellationToken cancellationToken)
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;
            IDictionary<string, NotifierResponse>? pushresponse = null;

            //id
            string returnid = "";

            try
            {
                var ltsid = oadata.Attribute("foreignKey").Value;

                if (ltsid.StartsWith("lts-points"))
                    ltsid = ltsid.Replace("lts-points.21430.", "").Trim();

                if (ltsid.StartsWith("lts-tours"))
                    ltsid = ltsid.Replace("lts-tours.21450.", "").Trim();
                
                returnid = "smgpoi" + ltsid;

                var outdooractiveid = oadata.Attribute("id").Value;
                var state = oadata.Attribute("state").Value;
                var lastchanged = DateTime.Parse(oadata.Attribute("lastModifiedAt").Value);

                if (state == "published" && !String.IsNullOrEmpty(ltsid))
                {
                    if (lastchanged > updatefrom)
                    {
                        //GET OLD Activity
                        var odhactivitypoiindb = await LoadDataFromDB<ODHActivityPoiLinked>(returnid, IDStyle.lowercase);

                        if (odhactivitypoiindb != null)
                        {
                            odhactivitypoiindb.OutdooractiveID = outdooractiveid;

                            string oaelevation = null;
                            
                            //If elevation SYNC enabled sync also the Elevation
                            if (syncelevation)
                            {
                                //TODO Add this also to Rawdata ?
                                var oadatasingle = await GetDataSingle(outdooractiveid, cancellationToken);
                                var parsedoadatasingle = ParseOutdooractiveData.ParseOADataDetail(oadatasingle);
                                if (parsedoadatasingle != null)
                                {
                                    oaelevation = parsedoadatasingle.elevationprofile_id.ToString();                                    
                                }
                            }

                            if (odhactivitypoiindb.Mapping != null && odhactivitypoiindb.Mapping.ContainsKey("outdooractive"))
                            {
                                var oaiddict = odhactivitypoiindb.Mapping["outdooractive"];
                                oaiddict.TryAddOrUpdate("id", outdooractiveid);
                                if (!String.IsNullOrEmpty(oaelevation))
                                {
                                    odhactivitypoiindb.OutdooractiveElevationID = outdooractiveid;
                                    oaiddict.TryAddOrUpdate("elevationid", oaelevation);
                                }
                                odhactivitypoiindb.Mapping.TryAddOrUpdate("outdooractive", oaiddict);
                            }
                            else
                            {                                
                                var oaiddict = new Dictionary<string, string>() { { "id", outdooractiveid } };
                                if (!String.IsNullOrEmpty(oaelevation))
                                {
                                    odhactivitypoiindb.OutdooractiveElevationID = outdooractiveid;
                                    oaiddict.TryAddOrUpdate("elevationid", oaelevation);
                                }
                                    
                                odhactivitypoiindb.Mapping.TryAddOrUpdate("outdooractive", oaiddict);
                            }

                            //Save parsedobject to DB + Save Rawdata to DB
                            var pgcrudresult = await InsertDataToDB(
                                odhactivitypoiindb,
                                new KeyValuePair<string, XElement>(returnid, oadata)
                            );

                            newcounter = newcounter + pgcrudresult.created ?? 0;
                            updatecounter = updatecounter + pgcrudresult.updated ?? 0;

                            //Push to MP
                            //Push Data if changed
                            //push modified data to all published Channels                            
                            pushresponse = await ImportUtils.CheckIfObjectChangedAndPush(
                                OdhPushnotifier,
                                pgcrudresult,
                                pgcrudresult.id,
                                "odhactivitypoi",
                                null,
                                "outdooractive." + type + ".update"
                            );

                            WriteLog.LogToConsole(
                                odhactivitypoiindb.Id,
                                "dataimport",
                                "single.outdooractive",
                                new ImportLog()
                                {
                                    sourceid = odhactivitypoiindb.Id,
                                    sourceinterface = "outdooractive." + type,
                                    success = true,
                                    error = "",
                                }
                            );
                        }
                        else
                        {
                            WriteLog.LogToConsole(
                                returnid,
                                "dataimport",
                                "single.outdooractive",
                                new ImportLog()
                                {
                                    sourceid = ltsid,
                                    sourceinterface = "outdooractive." + type,
                                    success = false,
                                    error = $"outdooractive {ltsid} not found",
                                }
                            );
                        }
                    }
                }
                else if(state == "rejected" || state == "incomplete")
                {
                    if (lastchanged > updatefrom)
                    {
                        //GET OLD Activity
                        var odhactivitypoiindb = await LoadDataFromDB<ODHActivityPoiLinked>(returnid, IDStyle.lowercase);

                        if (odhactivitypoiindb != null)
                        {
                            if(!String.IsNullOrEmpty(odhactivitypoiindb.OutdooractiveID) || odhactivitypoiindb.Mapping.ContainsKey("outdooractive"))
                            {
                                odhactivitypoiindb.OutdooractiveID = null;

                                if (odhactivitypoiindb.Mapping != null && odhactivitypoiindb.Mapping.ContainsKey("outdooractive"))
                                {                                    
                                    odhactivitypoiindb.Mapping.Remove("outdooractive");
                                }

                                //Save parsedobject to DB + Save Rawdata to DB
                                var pgcrudresult = await InsertDataToDB(
                                    odhactivitypoiindb,
                                    new KeyValuePair<string, XElement>(returnid, oadata)
                                );

                                newcounter = newcounter + pgcrudresult.created ?? 0;
                                updatecounter = updatecounter + pgcrudresult.updated ?? 0;

                                //Push to MP
                                //Push Data if changed
                                //push modified data to all published Channels                            
                                pushresponse = await ImportUtils.CheckIfObjectChangedAndPush(
                                    OdhPushnotifier,
                                    pgcrudresult,
                                    pgcrudresult.id,
                                    "odhactivitypoi",
                                    null,
                                    "outdooractive." + type + ".update"
                                );

                                WriteLog.LogToConsole(
                                    odhactivitypoiindb.Id,
                                    "dataimport",
                                    "single.outdooractive",
                                    new ImportLog()
                                    {
                                        sourceid = odhactivitypoiindb.Id,
                                        sourceinterface = "outdooractive." + type,
                                        success = true,
                                        error = "",
                                    }
                                );
                            }                            
                        }                        
                    }
                }

            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    returnid,
                    "dataimport",
                    "single.outdooractive",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "outdooractive." + type,
                        success = false,
                        error = $"outdooractive {type} could not be parsed",
                    }
                );

                errorcounter = errorcounter + 1;
            }

            return new UpdateDetail()
            {
                created = newcounter,
                updated = updatecounter,
                deleted = 0,
                error = errorcounter,
                pushed = pushresponse
            };
        }

        //Inserting into DB
        private async Task<PGCRUDResult> InsertDataToDB(
            ODHActivityPoiLinked odhactivitypoi,
            KeyValuePair<string, XElement> oadata
        )
        {
            var rawdataid = await InsertInRawDataDB(oadata);
                        
            var pgcrudresult = await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                odhactivitypoi,
                new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                new EditInfo($"outdooractive.{type}.import", importerURL),
                new CRUDConstraints(),
                new CompareConfig(true, false),
                rawdataid
            );

            return pgcrudresult;
        }

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, XElement> data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "outdooractive",
                    rawformat = "xml",
                    importdate = DateTime.Now,
                    license = "closed",
                    sourceinterface = "activitypois",
                    sourceurl = settings.OutdooractiveConfig.ServiceUrl,
                    type = "odhactivitypoi",
                    sourceid = data.Key,
                    raw = data.Value.ToString(),
                }
            );
        }          
    }
}
