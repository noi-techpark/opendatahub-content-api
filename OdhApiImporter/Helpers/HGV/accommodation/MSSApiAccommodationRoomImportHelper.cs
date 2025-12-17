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
    public class MSSApiAccommodationRoomImportHelper : ImportHelper, IImportHelper
    {
        public bool opendata = false;        
        
        public MSSApiAccommodationRoomImportHelper(
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
            throw new NotImplementedException();
        }

        public async Task<UpdateDetail> SaveDataToODH(            
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            List<UpdateDetail> updatedetailist = new List<UpdateDetail>();

            var xmlfiles = ImportUtils.LoadXmlFiles(
                    Path.Combine(".\\xml\\"),
                    new List<string>() { "RoomAmenities" });


            foreach (var id in idlist)
            {
                //Import the List
                var accommodationroomshgv = await GetAccommodationRoomsFromHGVMSS(id, xmlfiles);

                //Save Accommodationrooms to DB  
                var updateresult = await SaveAccommodationRoomsToPG(accommodationroomshgv, xmlfiles);

               
                updatedetailist.Add(updateresult);
            }

            return GenericResultsHelper.MergeUpdateDetail(updatedetailist);
        }
        
        private async Task<Dictionary<string, XElement>> GetAccommodationRoomsFromHGVMSS(
            string accoid,
            IDictionary<string, XDocument> xmlfiles
        )
        {
            try
            {
                var client = new HttpClient();
                var result = default(IEnumerable<AccommodationRoomLinked>);

                if (!String.IsNullOrEmpty(accoid))
                {                 
                    XElement roomdetail = new XElement("room_details", 69932);

                    //Parallel					
                    var myroomlistdetask = GetMssRoomlist.GetMssRoomlistAsync(client, "de", accoid, "lts", roomdetail, xmlfiles["RoomAmenities"], "sinfo", "2", settings.MssConfig.ServiceUrl, settings.MssConfig.Username, settings.MssConfig.Password);
                    var myroomlistittask = GetMssRoomlist.GetMssRoomlistAsync(client, "it", accoid, "lts", roomdetail, xmlfiles["RoomAmenities"], "sinfo", "2", settings.MssConfig.ServiceUrl, settings.MssConfig.Username, settings.MssConfig.Password);
                    var myroomlistentask = GetMssRoomlist.GetMssRoomlistAsync(client, "en", accoid, "lts", roomdetail, xmlfiles["RoomAmenities"], "sinfo", "2", settings.MssConfig.ServiceUrl, settings.MssConfig.Username, settings.MssConfig.Password);

                    await Task.WhenAll(myroomlistdetask, myroomlistittask, myroomlistentask);

                    return new Dictionary<string, XElement>()
                    {
                        { "de", await myroomlistdetask },
                        { "it", await myroomlistittask },
                        { "en", await myroomlistentask },
                    };
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "list.accommodations.rooms",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "hgv.accommodations.rooms",
                        success = false,
                        error = ex.Message,
                    }
                );

                return null;
            }
        }


        private async Task<UpdateDetail> SaveAccommodationRoomsToPG(Dictionary<string, XElement> hgvdata, IDictionary<string, XDocument> xmlfiles)
        {
            List<UpdateDetail> updatedetails = new List<UpdateDetail>();

            if (hgvdata != null)
            {
                List<string> idlistlts = new List<string>();

                //Parse Rooms
                var roomlistde = ParseMssRoomResponse.ParseMyRoomResponse("de", hgvdata["de"], xmlfiles["RoomAmenities"]);
                var roomlistit = ParseMssRoomResponse.ParseMyRoomResponse("it", hgvdata["it"], xmlfiles["RoomAmenities"]);
                var roomlisten = ParseMssRoomResponse.ParseMyRoomResponse("en", hgvdata["en"], xmlfiles["RoomAmenities"]);

                var rooms = MergeRooms(roomlistde, roomlistit, roomlisten);

                foreach (var data in rooms)
                {
                    var result = await InsertDataToDB(data, hgvdata["de"]);

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
                        data.Id,
                        "dataimport",
                        "single.accommodations",
                        new ImportLog()
                        {
                            sourceid = data.Id,
                            sourceinterface = "hgv.accommodations",
                            success = true,
                            error = "",
                        }
                    );
                }

                //Deactivate all AccommodationRooms from HGV
                var deleteresult = await DisableRoomsNotMorepresent(rooms.FirstOrDefault().A0RID, rooms);
                updatedetails.Add(deleteresult);
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
            AccommodationRoomLinked objecttosave,
            XElement hgvdata
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

                //Populate Tags (Id/Source/Type)
                //await objecttosave.UpdateTagsExtension(QueryFactory);

                var rawdataid = await InsertInRawDataDB(hgvdata);

                return await QueryFactory.UpsertData<AccommodationRoomLinked>(
                    objecttosave,
                    new DataInfo("accommodations", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("hgv.accommodations.rooms.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(XElement data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "hgv",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(data),
                    sourceinterface = "accommodations.rooms",
                    sourceid = data.Element("room_id").Value,
                    sourceurl = "http://www.easymailing.eu/mss/mss_service_test.php",
                    type = "accommodations.rooms",
                    license = "closed",
                    rawformat = "xml",
                }
            );
        }

        private async Task<UpdateDetail> DisableRoomsNotMorepresent(
           string accommodationid,
           IEnumerable<AccommodationRoomLinked> hgvdata
       )
        {
            int updateresult = 0;
            int deleteresult = 0;
            int errorresult = 0;

            try
            {
                var accoroomhgvquery = QueryFactory
                    .Query("accommodationrooms")
                    .Select("id")
                    .Where("gen_source", "hgv")
                    .Where("gen_a0rid", accommodationid);

                var accommodationroomids = await accoroomhgvquery.GetAsync<string>();

                var accommodationroomidstodeactivate = accommodationroomids.Except(hgvdata.Select(x => x.Id).ToList());

                //TODO Check all Rooms with this ID, disable/delete all rooms that are no more present
                foreach(var accoroom in accommodationroomidstodeactivate)
                {
                    var result = await DeleteOrDisableData<AccommodationRoomLinked>(accoroom, true);

                    updateresult = updateresult + result.Item1;
                    deleteresult = deleteresult + result.Item2;
                }
            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    "",
                    "dataimport",
                    "deactivate.accommodations.rooms",
                    new ImportLog()
                    {
                        sourceid = "",
                        sourceinterface = "hgv.accommodations.rooms",
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

        private IEnumerable<AccommodationRoomLinked> MergeRooms(IEnumerable<AccommodationRoomLinked> roomlistde, IEnumerable<AccommodationRoomLinked> roomlistit, IEnumerable<AccommodationRoomLinked> roomlisten)
        {
            List<AccommodationRoomLinked> roomlisttoreturn = new List<AccommodationRoomLinked>();

            foreach (var room in roomlistde)
            {
                room.HasLanguage = new List<string>() { "de" };

                //Englisch + IT suachn
                var roomit = roomlistit.Where(x => x.HGVId == room.HGVId).FirstOrDefault();
                if (roomit != null)
                {
                    var accoroomdetailit = roomit.AccoRoomDetail.Where(x => x.Key == "it").FirstOrDefault().Value;
                    if (accoroomdetailit != null)
                    {
                        room.AccoRoomDetail.TryAddOrUpdate("it", accoroomdetailit);
                        room.HasLanguage.Add("it");
                    }

                }
                var roomen = roomlisten.Where(x => x.HGVId == room.HGVId).FirstOrDefault();
                if (roomen != null)
                {
                    var accoroomdetailen = roomen.AccoRoomDetail.Where(x => x.Key == "en").FirstOrDefault().Value;

                    if (accoroomdetailen != null)
                    {
                        room.AccoRoomDetail.TryAddOrUpdate("en", accoroomdetailen);
                        room.HasLanguage.Add("en");
                    }

                }
                //Features auf henglishc??
                if (roomen.Features != null)
                    room.Features = roomen.Features;

                roomlisttoreturn.Add(room);
            }

            return roomlisttoreturn;
        }
    }

}
