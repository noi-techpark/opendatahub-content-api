// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.AccommodationRoomsExtension;
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
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers.HGV
{
    public class MSSApiAccommodationImportHelper : ImportHelper, IImportHelper
    {
        public bool opendata = false;
        public bool pushdata = true;
        
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

        public Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null, 
            List<string>? idlist = null, 
            CancellationToken cancellationToken = default
            )
        {
            throw new NotImplementedException();
        }

        public async Task<UpdateDetail> SaveDataToODH(            
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //If single Id is imported, deactivate only if HGVInfo is not present
            //If Full update is done use push logic

            //Import the List
            var accommodationhgv = await GetAccommodationsFromHGVMSS(idlist);

            //Check if Data is accessible on LTS            
            var updateresult =  await SaveAccommodationsToPG(idlist, accommodationhgv);

            //Import Single Data & Deactivate Data
            if (idlist == null)
            {
                var deleteresult = await SetHGVInfoForDataNotInListToNull(accommodationhgv, cancellationToken);

                return GenericResultsHelper.MergeUpdateDetail(
                    new List<UpdateDetail>() { updateresult, deleteresult }
                );
            }

            return updateresult;
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

        private async Task<UpdateDetail> SaveAccommodationsToPG(List<string> idlist, IEnumerable<MssResponseBaseSearch> hgvdata)
        {
            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            //TODO if data is not updateable on HGV clear the AccoHGVInfo!

            if (hgvdata != null)
            {                             
                foreach (var data in hgvdata)
                {
                    //Load Accommodation and fill out HGV Info                    
                    var accommodation = await LoadDataFromDB<AccommodationV2>(data.id_lts, IDStyle.uppercase);

                    //Add HGV Info for Accommodation
                    await AddHGVInfoToAccommodation(data, accommodation);

                    //Add Cincode and id to mapping
                    await AddHGVMappingToAccommodation(data, accommodation);

                    await accommodation.UpdateAccoRoomInfosExtension(QueryFactory, new List<string>() { "hgv" }, null);

                    var result = await InsertDataToDB(accommodation, data);

                    if(pushdata)
                        result.pushed = await CheckIfObjectChangedAndPush(
                            result,
                            accommodation.Id,
                            "accommodation",
                            "accommodations.hgvinfo"
                        );

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

                //this is to check should only clear the hgv data + hgv rooms
                foreach(var idtoclear in idlist.Except(hgvdata.Select(x => x.id_lts)))
                {
                    var deactivateresult = await ClearHgvInfoForDataNotInList(idtoclear);

                    if (pushdata)
                        deactivateresult.pushed = await CheckIfObjectChangedAndPush(
                            deactivateresult,
                            idtoclear,
                            "accommodation",
                            "accommodations.hgvinfo"
                        );

                    updatedetails.Add(new UpdateDetail()
                    {
                        created = deactivateresult.created,
                        updated = deactivateresult.updated,
                        deleted = deactivateresult.deleted,
                        error = deactivateresult.error,
                        objectchanged = deactivateresult.objectchanged,
                        objectimagechanged = deactivateresult.objectimagechanged,
                        comparedobjects =
                        deactivateresult.compareobject != null && deactivateresult.compareobject.Value ? 1 : 0,
                        pushchannels = deactivateresult.pushchannels,
                        changes = deactivateresult.changes,
                    });

                    WriteLog.LogToConsole(
                        idtoclear,
                        "dataimport",
                        "single.accommodations",
                        new ImportLog()
                        {
                            sourceid = idtoclear,
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
            List<UpdateDetail> updatedetaillist = new List<UpdateDetail>();

            try
            {
                //TODOUpdateAccommodationHGVFieldsWhichAreNotMoreonHGVList
                //TODO What about MSS Rooms, delete?

                var hotellisthgvltsrids = hgvdata.Where(x => !String.IsNullOrEmpty(x.id_lts)).Select(x => x.id_lts).ToList();
                               
                var hotellistquery = QueryFactory
                    .Query("accommodations")
                    .Select("id")
                    .WhereRaw("data->>'AccoHGVInfo' is not null");

                var hotelswithhgvinfoondb = await hotellistquery.GetAsync<string>();

                var idstocheck = hotelswithhgvinfoondb.Except(hotellisthgvltsrids);

                foreach (var idtoclear in idstocheck)
                {
                    var deactivateresult = await ClearHgvInfoForDataNotInList(idtoclear);

                    if (pushdata)
                        deactivateresult.pushed = await CheckIfObjectChangedAndPush(
                            deactivateresult,
                            idtoclear,
                            "accommodation",
                            "accommodations.hgvinfo"
                        );

                    updatedetaillist.Add(new UpdateDetail()
                    {
                        created = deactivateresult.created,
                        updated = deactivateresult.updated,
                        deleted = deactivateresult.deleted,
                        error = deactivateresult.error,
                        objectchanged = deactivateresult.objectchanged,
                        objectimagechanged = deactivateresult.objectimagechanged,
                        comparedobjects =
                        deactivateresult.compareobject != null && deactivateresult.compareobject.Value ? 1 : 0,
                        pushchannels = deactivateresult.pushchannels,
                        changes = deactivateresult.changes,
                    });

                    WriteLog.LogToConsole(
                       idtoclear,
                       "dataimport",
                       "single.accommodations",
                       new ImportLog()
                       {
                           sourceid = idtoclear,
                           sourceinterface = "hgv.accommodations",
                           success = true,
                           error = "",
                       }
                   );

                }
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

                updatedetaillist.Add(new UpdateDetail() { error = 1, exception = ex.Message });
            }

            return GenericResultsHelper.MergeUpdateDetail(updatedetaillist);
        }

        private async Task<PGCRUDResult> ClearHgvInfoForDataNotInList(string id)
        {
            //Load Accommodation and clear HGV Info                    
            var accommodation = await LoadDataFromDB<AccommodationV2>(id, IDStyle.uppercase);

            //Clear HGV Infos
            accommodation.AccoHGVInfo = null;

            return await QueryFactory.UpsertData<AccommodationV2>(
                    accommodation,
                    new DataInfo("accommodations", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("hgv.accommodations.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false)                    
                );
        }

        private async Task AddHGVInfoToAccommodation(MssResponseBaseSearch hgvdata, AccommodationV2 accommodation)
        {
            //Fill HGV Infos
            AccoHGVInfo accohgvinfo = new AccoHGVInfo();
            accohgvinfo.PriceFrom = Convert.ToInt32(hgvdata.price_from);
            accohgvinfo.AvailableFrom = hgvdata.available_from;
            accohgvinfo.Bookable = Convert.ToBoolean(Convert.ToInt16(hgvdata.bookable));

            accommodation.AccoHGVInfo = accohgvinfo;
        }

        private async Task AddHGVMappingToAccommodation(MssResponseBaseSearch hgvdata, AccommodationV2 accommodation)
        {
            //If no lts mapping is there
            if (accommodation.Mapping == null)
                accommodation.Mapping = new Dictionary<string, IDictionary<string, string>>();

            IDictionary<string, string> hgvdict = new Dictionary<string, string>();

            if (accommodation.Mapping.ContainsKey("hgv"))
                hgvdict = accommodation.Mapping["hgv"];

            if(!String.IsNullOrEmpty(hgvdata.id))
                hgvdict.TryAddOrUpdate("id", hgvdata.id);

            //Add Cin from HGV to mapping
            if (!String.IsNullOrEmpty(hgvdata.cin))
                hgvdict.TryAddOrUpdate("cincode", hgvdata.cin);
            else
            {
                if (hgvdict.ContainsKey("cincode"))
                {
                    hgvdict.Remove("cincode");
                }
            }

             accommodation.Mapping.TryAddOrUpdate("hgv", hgvdict);
        }
        
    }

}
