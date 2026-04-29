// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Extensions;
using Helper.Generic;
using Helper.Tagging;
using MOMENTUS;
using MOMENTUS.Model;
using Newtonsoft.Json;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers
{
    public class MomentusVenuesImportHelper : ImportHelper, IImportHelper
    {        
        public MomentusVenuesImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        #region MOMENTUS Helpers

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            var result = await GetDataFromMomentus.RequestMomentusRooms(
                settings.MomentusConfig.ServiceUrl, 
                settings.MomentusConfig.ClientId, 
                settings.MomentusConfig.ClientSecret, 
                settings.MomentusConfig.AuthUrl);
            
            var updateresult = await ImportData(result, cancellationToken);

            return updateresult;
        }

        private async Task<UpdateDetail> ImportData(
            IEnumerable<MomentusRoom> result,
            CancellationToken cancellationToken            
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            var momentusgroupedrooms = result.Select(x => x.Group).Distinct().ToList();

            foreach (var momentusroomgroup in momentusgroupedrooms)
            {
                //Load NOI Room
                var venue = await QueryFactory
                       .Query("venues")
                       .Select("data")
                       .WhereRaw("data#>>'\\{Mapping,momentus,group\\}' = $$", momentusroomgroup)
                       .GetObjectSingleAsync<VenueV2>();
                

                if (venue != null && momentusroomgroup != null)
                {
                    var importresult = await ImportDataSingle(venue, momentusroomgroup, result.Where(x => x.Group == momentusroomgroup).ToList());
                    newcounter = newcounter + importresult.created ?? newcounter;
                    updatecounter = updatecounter + importresult.updated ?? updatecounter;
                    errorcounter = errorcounter + importresult.error ?? errorcounter;
                }
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
            VenueV2 venue,
            string momentusroomgroup,
            IEnumerable<MomentusRoom> momentusrooms
        )
        {
            string idtoreturn = "";
            int updatecounter = 0;
            int newcounter = 0;
            int errorcounter = 0;

            try
            {
                foreach (var momentusroom in momentusrooms)
                {
                    if (momentusroom.Name != null)
                    {
                        //Check if venue Room has all infos
                        var venueroom = venue.RoomDetails?.Where(x => momentusroom.Name.Replace("NOI - ", "").Replace("EURAC - ", "") == x.Shortname).FirstOrDefault();
                        bool add = false;

                        if (venueroom == null)
                        {
                            add = true;
                            venueroom = new VenueRoomDetailsV2();
                            venueroom.Shortname = momentusroom.Name.Replace("NOI - ", "").Replace("EURAC - ", "");
                            venueroom.Detail.Add("en", new Detail() { Language = "en", Title = momentusroom.Name.Replace("NOI - ", "").Replace("EURAC - ", "") });
                        }

                        venueroom.Active = momentusroom.IsActive;
                        
                        if (momentusroom.SquareFootage != null)
                        {
                            venueroom.VenueRoomProperties = new VenueRoomProperties();
                            venueroom.VenueRoomProperties.SquareMeters = momentusroom.SquareFootage;
                        }

                        if (momentusroom.MaxCapacity != null)
                        {
                            venueroom.MaxCapacity = momentusroom.MaxCapacity;
                            //add also as Tag
                        }


                        if (venueroom.Mapping == null)
                            venueroom.Mapping = new Dictionary<string, IDictionary<string, string>>();

                        Dictionary<string, string> mapping = new Dictionary<string, string>();
                        mapping.Add("name", momentusroom.Name);
                        mapping.Add("isComboRoom", momentusroom.IsComboRoom.ToString());
                        mapping.Add("id", momentusroom.Id);
                        mapping.Add("group", momentusroom.Group);

                        mapping.Add("subRoomIds", String.Join(",", momentusroom.SubRoomIds));
                        mapping.Add("conflictingRoomIds", String.Join(",", momentusroom.ConflictingRoomIds));

                        if (momentusroom.ItemCode != null)
                            mapping.Add("itemCode", momentusroom.ItemCode);

                        venueroom.Mapping["momentus"] = mapping;

                        if (add)
                        {
                            venue.RoomDetails.Add(venueroom);
                        }
                    }
                }            


                var queryresult = await InsertDataToDB(
                    venue,
                    new KeyValuePair<string, IEnumerable<MomentusRoom>>(
                        momentusroomgroup,
                        momentusrooms
                    )
                );

                newcounter = newcounter + queryresult.created ?? 0;
                updatecounter = updatecounter + queryresult.updated ?? 0;

                WriteLog.LogToConsole(
                    idtoreturn,
                    "dataimport",
                    "single.venue",
                    new ImportLog()
                    {
                        sourceid = idtoreturn,
                        sourceinterface = "momentus.venue",
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
                    "single.venue",
                    new ImportLog()
                    {
                        sourceid = idtoreturn,
                        sourceinterface = "momentus.venue",
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
            VenueV2 venue,
            KeyValuePair<string, IEnumerable<MomentusRoom>> momentusrooms
        )
        {
            try
            {
                //Setting LicenseInfo
                //eventshort.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<VenueV2>(
                //    eventshort,
                //    Helper.LicenseHelper.GetLicenseforVenue
                //);
                //Check Languages
                //eventshort.CheckMyInsertedLanguages();

                //Remove Set PublishedOn not set automatically
                //eventshort.CreatePublishedOnList();

                var rawdataid = await InsertInRawDataDB(momentusrooms);

                return await QueryFactory.UpsertData<VenueV2>(
                    venue,
                    new DataInfo("venues", Helper.Generic.CRUDOperation.CreateAndUpdate, true),
                    new EditInfo("momentus.venue.import", importerURL),
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

        private async Task<int> InsertInRawDataDB(KeyValuePair<string, IEnumerable<MomentusRoom>> momentusroom)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "eurac",
                    importdate = DateTime.Now,
                    raw = JsonConvert.SerializeObject(momentusroom.Value),
                    sourceinterface = "momentus",
                    sourceid = momentusroom.Key,
                    sourceurl = "https://api.eu-venueops.com/v1/general-setup/rooms",
                    type = "venue",
                    license = "open",
                    rawformat = "json",
                }
            );
        }

        private static List<string>? AssignTechnologyfieldsautomatically(
            string companyname,
            List<string>? technologyfields
        )
        {
            if (technologyfields == null)
                technologyfields = new List<string>();

            //Digital, Alpine, Automotive/Automation, Food, Green

            AssignTechnologyFields(companyname, "Digital", "Digital", technologyfields);
            AssignTechnologyFields(companyname, "Alpine", "Alpine", technologyfields);
            AssignTechnologyFields(
                companyname,
                "Automotive",
                "Automotive/Automation",
                technologyfields
            );
            AssignTechnologyFields(companyname, "Food", "Food", technologyfields);
            AssignTechnologyFields(companyname, "Green", "Green", technologyfields);

            if (technologyfields.Count == 0)
                return null;
            else
                return technologyfields;
        }

        private static void AssignTechnologyFields(
            string companyname,
            string tocheck,
            string toassign,
            List<string> automatictechnologyfields
        )
        {
            if (companyname.Contains(tocheck))
                if (!automatictechnologyfields.Contains(toassign))
                    automatictechnologyfields.Add(toassign);
        }
      
        #endregion
    }
}
