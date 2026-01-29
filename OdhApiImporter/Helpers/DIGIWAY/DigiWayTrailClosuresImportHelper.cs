// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Extensions;
using Helper.Generic;
using Helper.Tagging;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZOHO;

namespace OdhApiImporter.Helpers
{
    public class DigiWayTrailClosuresImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }

        public DigiWayTrailClosuresImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL
        )
            : base(settings, queryfactory, table, importerURL) 
        {
            idlistinterface = new List<string>();
        }        

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            var zohodata = await GetDataFromZoho.RequestDataFromZoho(
                settings.ZohoConfig.ServiceUrl,
                settings.ZohoConfig.ClientId,
                settings.ZohoConfig.ClientSecret,
                settings.ZohoConfig.AuthUrl,
                settings.ZohoConfig.Scope
            );
            
            var updateresult = await ImportData(zohodata, cancellationToken);

            //Disable Data not in feratel list
            var deleteresult = await SetDataNotinListToInactive(cancellationToken);

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }

        private async Task<UpdateDetail> ImportData(
            IEnumerable<ZohoRootobject> hikingtrailclosureszoho,
            CancellationToken cancellationToken            
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;
            
            foreach (var hikingtrailclosure in hikingtrailclosureszoho)
            {
                var importresult = await ImportDataSingle(hikingtrailclosure);

                newcounter = newcounter + importresult.created ?? newcounter;
                updatecounter = updatecounter + importresult.updated ?? updatecounter;
                errorcounter = errorcounter + importresult.error ?? errorcounter;
            }

            return new UpdateDetail()
            {
                created = newcounter,
                updated = updatecounter,
                deleted = 0,
                error = errorcounter,
            };
        }

        private async Task<UpdateDetail> ImportDataSingle(
            ZohoRootobject hikingtrailclosure
        )
        {
            string idtoreturn = "";
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            try
            {
                idtoreturn = "urn:announcements:zoho:" + hikingtrailclosure.ID;

                idlistinterface.Add(idtoreturn);

                //var query = QueryFactory
                //    .Query("announcements")
                //    .Select("data")
                //    .Where("id", "urn:announcements:zoho:" + hikingtrailclosure.ID);

                //var announcementindb = await query.GetObjectSingleAsync<Announcement>();

                var announcementparsed = ParseZohoDataToAnnouncement.Parse(hikingtrailclosure);


                var queryresult = await InsertDataToDB(
                    announcementparsed,
                    hikingtrailclosure
                );

                newcounter = newcounter + queryresult.created ?? 0;
                updatecounter = updatecounter + queryresult.updated ?? 0;

                WriteLog.LogToConsole(
                    idtoreturn,
                    "dataimport",
                    "single.announcement",
                    new ImportLog()
                    {
                        sourceid = idtoreturn,
                        sourceinterface = "zoho.announcement",
                        success = true,
                        error = "",
                    }
                );

            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    idtoreturn,
                    "dataimport",
                    "single.announcement",
                    new ImportLog()
                    {
                        sourceid = idtoreturn,
                        sourceinterface = "zoho.announcement",
                        success = false,
                        error = ex.Message,
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
            };
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            Announcement announcement,
            ZohoRootobject announcementraw
        )
        {
            try
            {
                //Setting LicenseInfo
                announcement.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<Announcement>(
                    announcement,
                    Helper.LicenseHelper.GetLicenseforAnnouncement
                );
                
                //Remove Set PublishedOn not set automatically
                //eventshort.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(announcementraw);

                //TO CHECK rawdataid is not there!

                return await QueryFactory.UpsertData<Announcement>(
                    new UpsertableAnnouncement(announcement),
                    new DataInfo("announcements", Helper.Generic.CRUDOperation.CreateAndUpdate, true),
                    new EditInfo("zoho.announcement.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false)                    
                );     

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(ZohoRootobject announcementraw)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "eurac",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(announcementraw),
                    sourceinterface = "sentieri_opendatahub",
                    sourceid = announcementraw.ID,
                    sourceurl = "https://creatorapp.zoho.com/",
                    type = "announcement",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        //Deactivates all data that is no more on the interface
        private async Task<UpdateDetail> SetDataNotinListToInactive(
            CancellationToken cancellationToken
        )
        {
            int updateresult = 0;
            int deleteresult = 0;
            int errorresult = 0;

            try
            {
                //Begin SetDataNotinListToInactive
                var idlistdb = await GetAllDataBySource(new List<string>() { "digiway.zoho" });

                var idstodelete = idlistdb.Where(p => !idlistinterface.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var result = await DeleteOrDisableData<WebcamInfoLinked>(idtodelete, false);

                    updateresult = updateresult + result.Item1;
                    deleteresult = deleteresult + result.Item2;
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.zoho.announcement",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "zoho.announcement",
                        success = false,
                        error = ex.Message,
                    }
                );

                errorresult = errorresult + 1;
            }

            return new UpdateDetail()
            {
                created = 0,
                updated = updateresult,
                deleted = deleteresult,
                error = errorresult,
            };
        }


    }
}
