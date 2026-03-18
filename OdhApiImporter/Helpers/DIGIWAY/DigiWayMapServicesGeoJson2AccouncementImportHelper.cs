// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using DIGIWAY;
using DIGIWAY.Model.GeoJsonReadModel;
using Helper;
using Helper.Extensions;
using Helper.Generic;
using Helper.Tagging;
using Microsoft.AspNetCore.Components.Forms;
using NetTopologySuite.Densify;
using Newtonsoft.Json;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers
{
    public class DigiWayMapServicesGeoJson2AccouncementImportHelper : ImportHelper, IImportHelper
    {
        public List<string> idlistinterface { get; set; }
        public string? identifier { get; set; }
        public string? source { get; set; }

        public string? srid { get; set; }

        public DigiWayMapServicesGeoJson2AccouncementImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) 
        {
            idlistinterface = new List<string>();
        }        

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            if (identifier == null || source == null || srid == null)
                throw new Exception("no identifier|source|srid defined");

            List<UpdateDetail> resultlist = new List<UpdateDetail>();

            ////UPDATE all data
            var data = await GetData(cancellationToken);            
 
            resultlist.Add(await ImportData(data, cancellationToken));

            //Disable Data not in list
            resultlist.Add(await SetDataNotinListToInactive(cancellationToken));

            return GenericResultsHelper.MergeUpdateDetail(
                resultlist
            );
        }

        //Get Data from Source
        private async Task<ICollection<GeoJsonFeature>?> GetData(CancellationToken cancellationToken)
        {
            return await GetDigiwayData.GetDigiWayGeoJsonDataFromMapSercvicesAsync("","", settings.DigiWayConfig[identifier].ServiceUrl);
        }

        private async Task<UpdateDetail> ImportData(
            ICollection<GeoJsonFeature> datacollection,
            CancellationToken cancellationToken            
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;
            
            foreach (var data in datacollection)
            {
                var importresult = await ImportDataSingle(data);

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
            GeoJsonFeature digiwaydata
        )
        {
            string idtoreturn = "";
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            try
            {
                idtoreturn = "urn:announcements:tirol.mapservices.eu:" + digiwaydata.Attributes["id"].ToString();

                idlistinterface.Add(idtoreturn);

                //Import only data with Geometries (To chek)
                if (digiwaydata.Geometry != null && digiwaydata.Geometry.Length > 0)
                {
                    var announcementparsed = ParseMapServicesDataToAnnouncement.Parse(digiwaydata);

                    var queryresult = await InsertDataToDB(
                        announcementparsed,
                        digiwaydata
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
                            sourceinterface = "digiway." + identifier,
                            success = true,
                            error = "",
                        }
                    );
                }

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
                        sourceinterface = "digiway." + identifier,
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
            GeoJsonFeature digiwaydata
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

                var rawdataid = await InsertInRawDataDB(digiwaydata);

                //TO CHECK rawdataid is not there!

                return await QueryFactory.UpsertData<Announcement>(
                    new UpsertableAnnouncement(announcement),
                    new DataInfo("announcements", Helper.Generic.CRUDOperation.CreateAndUpdate, true),
                    new EditInfo("tirol.mapservices.eu.announcement.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false)
                );

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(GeoJsonFeature announcementraw)
        {
            var serializer = NetTopologySuite.IO.GeoJsonSerializer.Create();
            var settings = new JsonSerializerSettings();
            foreach (var converter in serializer.Converters)
                settings.Converters.Add(converter);            

            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "tirol.mapservices.eu",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(announcementraw, settings),
                    sourceinterface = "trailclosures",
                    sourceid = announcementraw.Attributes["id"].ToString(),
                    sourceurl = "https://tirol.mapservices.eu/nefos_app/web/api/closures",
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
                var idlistdb = await GetAllDataBySource(new List<string>() { "tirol.mapservices.eu" });

                var idstodelete = idlistdb.Where(p => !idlistinterface.Any(p2 => p2 == p));

                foreach (var idtodelete in idstodelete)
                {
                    var result = await DeleteOrDisableData<Announcement>(idtodelete, false);

                    updateresult = updateresult + result.Item1;
                    deleteresult = deleteresult + result.Item2;
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.tirol.mapservices.eu.announcement",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "digiway." + identifier,
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
