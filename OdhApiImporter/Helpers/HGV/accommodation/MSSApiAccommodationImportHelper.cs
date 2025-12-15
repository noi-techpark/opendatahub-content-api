// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using Helper.Tagging;
using MSS;
using Newtonsoft.Json;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers.HGV
{
    public class MSSApiAccommodationImportHelper : ImportHelper, IImportHelper
    {
        public bool opendata = false;
        
        public MSSApiAccommodationImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

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

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //Import the List
            var accommodationhgv = await GetAccommodationsFromHGVMSS(idlist);

            //Check if Data is accessible on LTS            
            var updateresult =  await SaveAccommodationsToPG(accommodationhgv, idlist != null ? false : true);

            //Import Single Data & Deactivate Data
            var deleteresult = await SetHGVInfoForDataNotInListToNull(accommodationhgv, cancellationToken);

            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult, deleteresult }
            );
        }
   
        private async Task<IEnumerable<MssResponseBaseSearch>> GetAccommodationsFromHGVMSS(
            List<string> accoids
        )
        {
            try
            {
                var client = new HttpClient();
                var result = default(IEnumerable<MssResponseBaseSearch>);

                if (accoids != null)
                {
                    //Get Single Accommodation
                    result = await GetMssData.GetMssBaseDataResponse(
                        client,
                        accoids, 
                        "lts",                        
                        "de", 
                        null, 
                        new XElement("hotel_details", 1), 
                        "sinfo", 
                        "2",
                        settings.MssConfig.ServiceUrl,
                        settings.MssConfig.Username,
                        settings.MssConfig.Password);
                }
                else 
                {
                    //Get the whole Accommodations list
                    result = await GetMssData.GetMssBaseDataResponse(
                        client,
                        null,
                        "hgv",
                        "de",
                        null,
                        new XElement("hotel_details", 1),
                        "sinfo",
                        "2",
                        settings.MssConfig.ServiceUrl,
                        settings.MssConfig.Username,
                        settings.MssConfig.Password);
                }

                return result;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.accommodations",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "hgv.accommodations",
                        success = false,
                        error = ex.Message,
                    }
                );

                return null;
            }
        }

        private async Task<UpdateDetail> SaveAccommodationsToPG(IEnumerable<MssResponseBaseSearch> hgvdata, bool updateaccosnomoreonlist)
        {
            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            if (hgvdata != null)
            {
                List<string> idlistlts = new List<string>();
             
                foreach (var data in hgvdata)
                {
                    //Load Accommodation and fill out HGV Info                    
                    var accommodation = await LoadDataFromDB<AccommodationV2>(data.id_lts, IDStyle.uppercase);

                    //Fill HGV Infos
                    AccoHGVInfo accohgvinfo = new AccoHGVInfo();
                    accohgvinfo.PriceFrom = Convert.ToInt32(data.price_from);
                    accohgvinfo.AvailableFrom = data.available_from;
                    accohgvinfo.Bookable = Convert.ToBoolean(Convert.ToInt16(data.bookable));

                    accommodation.AccoHGVInfo = accohgvinfo;

                    var result = await InsertDataToDB(accommodation, data);

                    updatedetails.Add(new UpdateDetail()
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
                    });
                    
                    WriteLog.LogToConsole(
                        data.id_lts,
                        "dataimport",
                        "single.accommodations",
                        new ImportLog()
                        {
                            sourceid = data.id_lts,
                            sourceinterface = "hgv.accommodations",
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
                    comparedobjects = 0,
                    pushchannels = null,
                    changes = null
                });
            }

            return GenericResultsHelper.MergeUpdateDetail(
                updatedetails
            );
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            AccommodationV2 objecttosave,
            MssResponseBaseSearch data
        )
        {
            try
            {
                //LicenseInfo, PublishedonList and MetaInfo NOT needed here since it is an addition
                
                //objecttosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                //    objecttosave,
                //    Helper.LicenseHelper.GetLicenseforAccommodation
                //);

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                //objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave);

                //Set PublishedOn
                //objecttosave.CreatePublishedOnList();

                //Tags also not touched
                //Populate Tags (Id/Source/Type)
                //await objecttosave.UpdateTagsExtension(QueryFactory);

                var rawdataid = await InsertInRawDataDB(data);

                return await QueryFactory.UpsertData<AccommodationV2>(
                    objecttosave,
                    new DataInfo("accommodations", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("hgv.accommodations.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false),
                    rawdataid
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> InsertInRawDataDB(MssResponseBaseSearch data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "hgv",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(data),
                    sourceinterface = "accommodations",
                    sourceid = data.id_lts,
                    sourceurl = "http://www.easymailing.eu/mss/mss_service_test.php",
                    type = "accommodations",
                    license = "closed",
                    rawformat = "json",
                }
            );
        }

        private async Task<UpdateDetail> SetHGVInfoForDataNotInListToNull(
           IEnumerable<MssResponseBaseSearch> hgvdata,
           CancellationToken cancellationToken
       )
        {
            int updateresult = 0;
            int deleteresult = 0;
            int errorresult = 0;

            try
            {
                //TODO CHECK IF EVERY Accommodation was requested

                //TODOUpdateAccommodationHGVFieldsWhichAreNotMoreonHGVList

                var hotellisthgvltsrids = hgvdata.Where(x => !String.IsNullOrEmpty(x.id_lts)).Select(x => x.id_lts).ToList();

                //List<string?> mymuseumroot =
                //    mymuseumlist
                //        .Root?.Elements("Museum")
                //        .Select(x => x.Attribute("ID")?.Value)
                //        .ToList() ?? new();

                //var mymuseumquery = QueryFactory
                //    .Query("smgpois")
                //    .SelectRaw("data->'Mapping'->'siag'->>'museId'")
                //    .Where("gen_syncsourceinterface", "museumdata");

                //var mymuseumsondb = await mymuseumquery.GetAsync<string>();

                //var idstodelete = mymuseumsondb.Where(p => !mymuseumroot.Any(p2 => p2 == p));

                //foreach (var idtodelete in idstodelete)
                //{
                //    var result = await DeleteOrDisableData<ODHActivityPoiLinked>(idtodelete, false);

                //    updateresult = updateresult + result.Item1;
                //    deleteresult = deleteresult + result.Item2;
                //}
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.accommodations",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "hgv.accommodations",
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
