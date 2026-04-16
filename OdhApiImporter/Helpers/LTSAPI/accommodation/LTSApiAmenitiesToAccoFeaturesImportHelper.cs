// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using LTSAPI;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class LTSApiAmenitiesToAccoFeaturesImportHelper : ImportHelper, IImportHelper
    {
        public LTSApiAmenitiesToAccoFeaturesImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        private async Task<List<JObject>> GetAccommodationAmenitiesFromLTSV2()
        {
            try
            {
                LtsApi ltsapi = new LtsApi(
                    settings.LtsCredentials.serviceurl,
                    settings.LtsCredentials.username,
                    settings.LtsCredentials.password,
                    settings.LtsCredentials.ltsclientid,
                    false
                );
                var qs = new LTSQueryStrings() { page_size = 100 };
                var dict = ltsapi.GetLTSQSDictionary(qs);

                var ltsdata = await ltsapi.AmenityListRequest(dict, true);

                return ltsdata;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.accommodations.amenities.accofeatures",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "lts.accommodations.amenities.accofeatures",
                        success = false,
                        error = ex.Message,
                    }
                );

                return null;
            }
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //Import the List
            var eventtags = await GetAccommodationAmenitiesFromLTSV2();
            //Import Single Data & Deactivate Data
            var result = await SaveAccommodationAmenitiesToPG(eventtags);

            return result;
        }

        private async Task<UpdateDetail> SaveAccommodationAmenitiesToPG(List<JObject> ltsdata)
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            if (ltsdata != null)
            {
                List<string> idlistlts = new List<string>();
                List<string> typelistlts = new List<string>();

                List<LTSAmenity> tagdata = new List<LTSAmenity>();

                foreach (var ltsdatasingle in ltsdata)
                {
                    tagdata.AddRange(ltsdatasingle["data"].ToObject<IList<LTSAmenity>>());
                }               

                foreach (var data in tagdata)
                {
                    string id = data.rid;

                    bool insertdata = false;

                    //See if data exists
                    var query = QueryFactory.Query("accommodationfeatures").Select("data").Where("id", id);
                    var objecttosave = await query.GetObjectSingleAsync<AccoFeatures>();

                    if (objecttosave == null)
                    {
                        objecttosave = new AccoFeatures();
                        insertdata = true;
                    }
                        

                    //TODO Some Accommodation Amenities are also Gastronomy facilitycodes_equipment facilitycodes_cuisinecodes
                    //TODO this on all tag imports

                    objecttosave.Id = data.rid;
                    objecttosave.Key = data.rid;

                    objecttosave.Type = "AccommodationFeature";
                    objecttosave.Bitmask = 0;

                    objecttosave.CustomId = data.code;
                    objecttosave.TypeDesc = (Dictionary<string, string>)data.name;

                    if (objecttosave.CustomId != null && !objecttosave.CustomId.EndsWith("000.000.000"))
                    {
                        //Adding Cluster Information

                        var clusteridtosearch = objecttosave.CustomId.Substring(0, 6) + "000.000.000";

                        //Load Data
                        var clusterfeature = tagdata
                            .Where(x => x.code == clusteridtosearch).FirstOrDefault();

                        if (clusterfeature != null)
                        {
                            objecttosave.ClusterId = clusterfeature.rid;
                            objecttosave.ClusterCustomId = clusterfeature.code;
                        }
                    }

                    //Insert in DB

                    int updateresult = 0;
                    int insertresult = 0;

                    if (insertdata)
                    {
                        //Insert
                        insertresult = await QueryFactory
                            .Query("accommodationfeatures")
                            .InsertAsync(
                                new JsonBData()
                                {
                                    id = objecttosave.Id,
                                    data = new JsonRaw(objecttosave),
                                }
                            );
                    }
                    else
                    {
                        //Update
                        updateresult = await QueryFactory
                            .Query("accommodationfeatures")
                            .Where("id", objecttosave.Id)
                            .UpdateAsync(
                                new JsonBData()
                                {
                                    id = objecttosave.Id,
                                    data = new JsonRaw(objecttosave),
                                }
                            );
                    }


                    newimportcounter = newimportcounter + insertresult;
                    updateimportcounter = updateimportcounter + updateresult;                    

                    idlistlts.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.accommodations.amenities.accofeatures",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "lts.accommodations.amenities.accofeatures",
                            success = true,
                            error = "",
                        }
                    );
                }

                if (idlistlts.Count > 0)
                {
                    var query = QueryFactory
                        .Query("accommodationfeatures")
                        .Select("id");

                    var idlistdb = await query.GetAsync<string>();

                    var idstodelete = idlistdb.Where(p => !idlistlts.Any(p2 => p2 == p));

                    foreach (var idtodelete in idstodelete)
                    {
                        var deleteresult = await QueryFactory.Query("accommodationfeatures").Where("id", idtodelete).DeleteAsync();


                        WriteLog.LogToConsole(
                                idtodelete,
                                "dataimport",
                                "single.accommodations.amenities.accofeatures.delete",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "lts.accommodations.amenities.accofeatures",
                                    success = true,
                                    error = "",
                                }
                            );

                        deleteimportcounter =
                            deleteimportcounter + deleteresult;
                    }
                }
            }
            else
                errorimportcounter = 1;

            return new UpdateDetail()
            {
                updated = updateimportcounter,
                created = newimportcounter,
                deleted = deleteimportcounter,
                error = errorimportcounter,
            };
        }        
    }
}
