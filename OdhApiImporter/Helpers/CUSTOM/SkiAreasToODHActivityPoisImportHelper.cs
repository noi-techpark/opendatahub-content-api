// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Helper.Location;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers
{
    public class SkiAreasToODHActivityPoisImportHelper : ImportHelper, IImportHelper
    {
        public SkiAreasToODHActivityPoisImportHelper(
             ISettings settings,
             QueryFactory queryfactory,
             string table,
             string importerURL,
            IOdhPushNotifier odhpushnotifier
         )
             : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {            
            //GET Data
            var data = await GetData(cancellationToken);

            //UPDATE all data
            var updateresult = await ImportData(data, cancellationToken);
            
            return GenericResultsHelper.MergeUpdateDetail(
                new List<UpdateDetail>() { updateresult }
            );
        }

        //Get Data from Source
        private async Task<IEnumerable<SkiAreaLinked>> GetData(CancellationToken cancellationToken)
        {
            var skiareasquery = QueryFactory.Query().From("skiareas").Select("data");
            return await skiareasquery.GetObjectListAsync<SkiAreaLinked>();
        }
        
        //Import the Data
        public async Task<UpdateDetail> ImportData(
            IEnumerable<SkiAreaLinked> skiareas,
            CancellationToken cancellationToken
        )
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            //Load the json Data
            IDictionary<string, JArray> jsondata = default(Dictionary<string, JArray>);

            jsondata = await LTSAPIImportHelper.LoadJsonFiles(
            settings.JsonConfig.Jsondir,
            new List<string>()
                {
                        "ActivityPoiDisplayAsCategory",
                        "GenericTags",
                }
            );

            //loop trough outdooractive items
            foreach (var skiarea in skiareas)
            {
                var importresult = await ImportDataSingle(skiarea, jsondata, cancellationToken);

                newcounter = newcounter + importresult.created ?? newcounter;
                updatecounter = updatecounter + importresult.updated ?? updatecounter;
                errorcounter = errorcounter + importresult.error ?? errorcounter;
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
        public async Task<UpdateDetail> ImportDataSingle(SkiAreaLinked skiarea, IDictionary<string, JArray> jsonfiles, CancellationToken cancellationToken)
        {
            int updatecounter = 0;
            int newcounter = 0;
            int deletecounter = 0;
            int errorcounter = 0;

            //id
            string returnid = "";

            try
            {
                var odhactivitypoi = new ODHActivityPoiLinked();

                //Load from DB
                var skiareasquery = QueryFactory.Query().From("smgpois").Select("data").Where("id", "smgpoi" + skiarea.Id);
                odhactivitypoi = await skiareasquery.GetObjectSingleAsync<ODHActivityPoiLinked>();

                odhactivitypoi = ParseSkiAreaToODHActivityPoi(skiarea, odhactivitypoi);

                //TODO Remove all automatically assigned Tags

                //Traduce all Tags with Source IDM to english tags, CONSIDER TagId "poi" is added here
                await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                    odhactivitypoi,
                    jsonfiles != null && jsonfiles["GenericTags"] != null ? jsonfiles["GenericTags"].ToObject<List<TagLinked>>() : null
                );

                //DistanceCalculation
                await odhactivitypoi.UpdateDistanceCalculation(QueryFactory);

                //AdditionalInfos
                SetAdditionalInfosCategoriesByODHTags(odhactivitypoi, jsonfiles);

                //Save parsedobject to DB + Save Rawdata to DB
                var pgcrudresult = await InsertDataToDB(
                    odhactivitypoi,
                    skiarea
                );

                newcounter = newcounter + pgcrudresult.created ?? 0;
                updatecounter = updatecounter + pgcrudresult.updated ?? 0;

                WriteLog.LogToConsole(
                    odhactivitypoi.Id,
                    "dataimport",
                    "single.skiarea",
                    new ImportLog()
                    {
                        sourceid = odhactivitypoi.Id,
                        sourceinterface = "common.skiarea",
                        success = true,
                        error = "",
                    }
                );


            }
            catch (Exception ex)
            {
                WriteLog.LogToConsole(
                    returnid,
                    "dataimport",
                    "single.skiarea",
                    new ImportLog()
                    {
                        sourceid = returnid,
                        sourceinterface = "common.skiarea",
                        success = false,
                        error = "skiarea could not be parsed",
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

        //Inserting into DB
        private async Task<PGCRUDResult> InsertDataToDB(
            ODHActivityPoiLinked odhactivitypoi,
            SkiAreaLinked skiarea
        )
        {
            var rawdataid = await InsertInRawDataDB(skiarea);
                        
            var pgcrudresult = await QueryFactory.UpsertData<ODHActivityPoiLinked>(
                odhactivitypoi,
                new DataInfo(table, Helper.Generic.CRUDOperation.CreateAndUpdate),
                new EditInfo("common.skiarea.import", importerURL),
                new CRUDConstraints(),
                new CompareConfig(true, false),
                rawdataid
            );

            return pgcrudresult;
        }

        private async Task<int> InsertInRawDataDB(SkiAreaLinked data)
        {
            return await QueryFactory.InsertInRawtableAndGetIdAsync(
                new RawDataStore()
                {
                    datasource = "common",
                    rawformat = "xml",
                    importdate = DateTime.Now,
                    license = "open",
                    sourceinterface = "skiareas",
                    sourceurl = settings.OutdooractiveConfig.ServiceUrl,
                    type = "odhactivitypoi",
                    sourceid = data.Id,
                    raw = JsonConvert.SerializeObject(data),
                }
            );
        }          

        private ODHActivityPoiLinked ParseSkiAreaToODHActivityPoi(SkiAreaLinked skiarea, ODHActivityPoiLinked odhactivitypoi)
        {
            List<string> languagelistcategories = new List<string>() { "de", "it", "en", "nl", "cs", "pl", "fr", "ru" };

            if (odhactivitypoi == null)
            {
                //Neu
                odhactivitypoi = new ODHActivityPoiLinked();
                odhactivitypoi.FirstImport = DateTime.Now;
                odhactivitypoi.LastChange = DateTime.Now;

                odhactivitypoi.Source = "Common";
                odhactivitypoi.SyncSourceInterface = "Common";
                odhactivitypoi.SyncUpdateMode = "Full";

                odhactivitypoi.Active = skiarea.Active;

                odhactivitypoi.AltitudeDifference = 0;
                odhactivitypoi.AltitudeHighestPoint = 0;
                odhactivitypoi.AltitudeLowestPoint = 0;
                odhactivitypoi.AltitudeSumDown = 0;
                odhactivitypoi.AltitudeSumUp = 0;
                odhactivitypoi.AreaId = skiarea.AreaId;

                odhactivitypoi.ContactInfos = skiarea.ContactInfos;
                odhactivitypoi.Detail = skiarea.Detail;
                odhactivitypoi.Difficulty = "";
                odhactivitypoi.DistanceDuration = 0;
                odhactivitypoi.DistanceLength = 0;
                odhactivitypoi.Exposition = null;
                odhactivitypoi.FeetClimb = false;
                odhactivitypoi.GpsInfo = new List<GpsInfo>() { new GpsInfo() { Altitude = skiarea.Altitude, AltitudeUnitofMeasure = skiarea.AltitudeUnitofMeasure, Gpstype = "position", Latitude = skiarea.Latitude, Longitude = skiarea.Longitude } };
                odhactivitypoi.HasFreeEntrance = false;
                odhactivitypoi.HasLanguage = skiarea.HasLanguage;
                odhactivitypoi.HasRentals = true;
                odhactivitypoi.Highlight = true;
                odhactivitypoi.Id = "smgpoi" + skiarea.Id;
                odhactivitypoi.ImageGallery = skiarea.ImageGallery;
                odhactivitypoi.IsOpen = true;
                odhactivitypoi.IsPrepared = true;
                odhactivitypoi.IsWithLigth = false;
                odhactivitypoi.LiftAvailable = true;
                odhactivitypoi.LocationInfo = skiarea.LocationInfo;

                //Mapping
                odhactivitypoi.Mapping = skiarea.Mapping;

                odhactivitypoi.MaxSeatingCapacity = 0;
                odhactivitypoi.OperationSchedule = skiarea.OperationSchedule;

                PoiProperty totalkm = new PoiProperty() { Name = "TotalSlopeKm", Value = skiarea.TotalSlopeKm };
                PoiProperty SlopeKmBlue = new PoiProperty() { Name = "SlopeKmBlue", Value = skiarea.SlopeKmBlue };
                PoiProperty SlopeKmRed = new PoiProperty() { Name = "SlopeKmRed", Value = skiarea.SlopeKmRed };
                PoiProperty SlopeKmBlack = new PoiProperty() { Name = "SlopeKmBlack", Value = skiarea.SlopeKmBlack };
                PoiProperty SkiRegionId = new PoiProperty() { Name = "SkiRegionId", Value = skiarea.SkiRegionId };
                PoiProperty SkiAreaMapURL = new PoiProperty() { Name = "SkiAreaMapURL", Value = skiarea.SkiAreaMapURL };

                Dictionary<string, List<PoiProperty>> mypoipropertylistdict = new Dictionary<string, List<PoiProperty>>();

                foreach (var language in languagelistcategories)
                {
                    PoiProperty SkiRegionName = new PoiProperty() { Name = "SkiRegionName", Value = skiarea.SkiRegionName[language] };

                    List<PoiProperty> mypoipropertylist = new List<PoiProperty>();
                    mypoipropertylist.Add(totalkm);
                    mypoipropertylist.Add(SlopeKmBlue);
                    mypoipropertylist.Add(SlopeKmRed);
                    mypoipropertylist.Add(SlopeKmBlack);
                    mypoipropertylist.Add(SkiRegionId);
                    mypoipropertylist.Add(SkiRegionName);
                    mypoipropertylist.Add(SkiAreaMapURL);

                    mypoipropertylistdict.TryAddOrUpdate(language, mypoipropertylist);
                }

                odhactivitypoi.PoiProperty = mypoipropertylistdict;

                //TODO ADD WEBCAM TO RELATED CONTENT
                //odhactivitypoi.Webcam = skiarea.Webcam;

                //odhactivitypoi.PoiServices;

                odhactivitypoi.Type = null;
                odhactivitypoi.SubType = null;

                odhactivitypoi.PoiType = GetTheRightSkiregion(skiarea.SkiRegionName["de"]);

                odhactivitypoi.Ratings = null;
                odhactivitypoi.RelatedContent = null;
                odhactivitypoi.RunToValley = true;
                odhactivitypoi.Shortname = skiarea.Shortname;
                odhactivitypoi.SmgTags = skiarea.SmgTags;
                odhactivitypoi.SmgTags.Add("Winter");

                odhactivitypoi.TourismorganizationId = skiarea.TourismvereinIds.FirstOrDefault();

                foreach (var language in languagelistcategories)
                {
                    AdditionalPoiInfos additional = new AdditionalPoiInfos();
                    additional.Language = language;
                    additional.MainType = null;
                    additional.SubType = null;
                    additional.PoiType = null;
                    odhactivitypoi.AdditionalPoiInfos.TryAddOrUpdate(language, additional);
                }                     
            }
            else
            {
                odhactivitypoi.LastChange = DateTime.Now;
                odhactivitypoi.CustomId = skiarea.Id;
                odhactivitypoi.Source = "Common";
                odhactivitypoi.SyncSourceInterface = "Common";
                odhactivitypoi.SyncUpdateMode = "Full";

                odhactivitypoi.Active = skiarea.Active;
                odhactivitypoi.SmgActive = skiarea.SmgActive;

                odhactivitypoi.AltitudeDifference = 0;
                odhactivitypoi.AltitudeHighestPoint = 0;
                odhactivitypoi.AltitudeLowestPoint = 0;
                odhactivitypoi.AltitudeSumDown = 0;
                odhactivitypoi.AltitudeSumUp = 0;
                odhactivitypoi.AreaId = skiarea.AreaId;

                odhactivitypoi.ContactInfos = skiarea.ContactInfos;
                odhactivitypoi.Detail = skiarea.Detail;
                odhactivitypoi.Difficulty = "";
                odhactivitypoi.DistanceDuration = 0;
                odhactivitypoi.DistanceLength = 0;
                odhactivitypoi.Exposition = null;
                odhactivitypoi.FeetClimb = false;
                odhactivitypoi.GpsInfo = new List<GpsInfo>() { new GpsInfo() { Altitude = skiarea.Altitude, AltitudeUnitofMeasure = skiarea.AltitudeUnitofMeasure, Gpstype = "position", Latitude = skiarea.Latitude, Longitude = skiarea.Longitude } };
                odhactivitypoi.HasFreeEntrance = false;
                odhactivitypoi.HasLanguage = skiarea.HasLanguage;
                odhactivitypoi.HasRentals = true;
                odhactivitypoi.Highlight = true;
                odhactivitypoi.ImageGallery = skiarea.ImageGallery;
                odhactivitypoi.IsOpen = true;
                odhactivitypoi.IsPrepared = true;
                odhactivitypoi.IsWithLigth = false;
                odhactivitypoi.LiftAvailable = true;
                odhactivitypoi.LocationInfo = skiarea.LocationInfo;

                odhactivitypoi.MaxSeatingCapacity = 0;
                odhactivitypoi.OperationSchedule = skiarea.OperationSchedule;

                PoiProperty totalkm = new PoiProperty() { Name = "TotalSlopeKm", Value = skiarea.TotalSlopeKm };
                PoiProperty SlopeKmBlue = new PoiProperty() { Name = "SlopeKmBlue", Value = skiarea.SlopeKmBlue };
                PoiProperty SlopeKmRed = new PoiProperty() { Name = "SlopeKmRed", Value = skiarea.SlopeKmRed };
                PoiProperty SlopeKmBlack = new PoiProperty() { Name = "SlopeKmBlack", Value = skiarea.SlopeKmBlack };
                PoiProperty SkiRegionId = new PoiProperty() { Name = "SkiRegionId", Value = skiarea.SkiRegionId };
                PoiProperty SkiAreaMapURL = new PoiProperty() { Name = "SkiAreaMapURL", Value = skiarea.SkiAreaMapURL };


                Dictionary<string, List<PoiProperty>> mypoipropertylistdict = new Dictionary<string, List<PoiProperty>>();

                foreach (var language in languagelistcategories)
                {
                    PoiProperty SkiRegionName = new PoiProperty() { Name = "SkiRegionName", Value = skiarea.SkiRegionName[language] };

                    List<PoiProperty> mypoipropertylist = new List<PoiProperty>();
                    mypoipropertylist.Add(totalkm);
                    mypoipropertylist.Add(SlopeKmBlue);
                    mypoipropertylist.Add(SlopeKmRed);
                    mypoipropertylist.Add(SlopeKmBlack);
                    mypoipropertylist.Add(SkiRegionId);
                    mypoipropertylist.Add(SkiRegionName);
                    mypoipropertylist.Add(SkiAreaMapURL);

                    mypoipropertylistdict.TryAddOrUpdate(language, mypoipropertylist);
                }

                odhactivitypoi.PoiProperty = mypoipropertylistdict;

                //TODO add as RElated content
                //odhactivitypoi.Webcam = skiarea.Webcam;

                //Mapping
                odhactivitypoi.Mapping = skiarea.Mapping;

                odhactivitypoi.Type = "Winter";
                odhactivitypoi.SubType = "Skigebiete";

                odhactivitypoi.PoiType = GetTheRightSkiregion(skiarea.SkiRegionName["de"]);

                odhactivitypoi.Ratings = null;
                odhactivitypoi.RelatedContent = null;
                odhactivitypoi.RunToValley = true;
                odhactivitypoi.Shortname = skiarea.Shortname;

                foreach (var language in languagelistcategories)
                {
                    AdditionalPoiInfos additional = new AdditionalPoiInfos();
                    additional.Language = language;
                    additional.MainType = null;
                    additional.SubType = null;
                    additional.PoiType = null;
                    odhactivitypoi.AdditionalPoiInfos.TryAddOrUpdate(language, additional);
                }           
            }

            if (!odhactivitypoi.SmgTags.Contains("poi"))
                odhactivitypoi.SmgTags.Add("poi");

            return odhactivitypoi;
        }

        private static string GetTheRightSkiregion(string skiregion)
        {
            switch (skiregion)
            {
                case "Dolomiti Superski":
                    return "Dolomiti Superski";
                case "Ortler Skiarena":
                    return "Ortler Skiarena";
                case "Skiverbund Eisacktaler Wipptal":
                    return "Skiverbund Eisacktal-Wipptal";
                case "Tauferer Ahrntal":
                    return "Skiregion Tauferer Ahrntal";
                default:
                    return "";
            }
        }

        private static void SetAdditionalInfosCategoriesByODHTags(ODHActivityPoiLinked activityNew, IDictionary<string, JArray>? jsonfiles)
        {
            //TO CHECK
            //SET ADDITIONALINFOS
            //Setting Categorization by Valid Tags
            var validcategorylist = jsonfiles != null && jsonfiles["ActivityPoiDisplayAsCategory"] != null ? jsonfiles["ActivityPoiDisplayAsCategory"].ToObject<List<CategoriesTags>>() : null;

            if (validcategorylist != null && activityNew.SmgTags != null)
            {
                var currentcategories = validcategorylist.Where(x => activityNew.SmgTags.Select(y => y.ToLower()).Contains(x.Id.ToLower())).ToList();

                if (currentcategories != null)
                {
                    if (activityNew.AdditionalPoiInfos == null)
                        activityNew.AdditionalPoiInfos = new Dictionary<string, AdditionalPoiInfos>();

                    foreach (var languagecategory in new List<string>() { "de", "it", "en", "nl", "cs", "pl", "fr", "ru" })
                    {
                        //Do not overwrite Novelty
                        string? novelty = null;
                        if (activityNew.AdditionalPoiInfos.ContainsKey(languagecategory) && !String.IsNullOrEmpty(activityNew.AdditionalPoiInfos[languagecategory].Novelty))
                            novelty = activityNew.AdditionalPoiInfos[languagecategory].Novelty;


                        AdditionalPoiInfos additionalPoiInfos = new AdditionalPoiInfos() { Language = languagecategory, Categories = new List<string>(), Novelty = novelty };

                        //Reassigning Categories
                        foreach (var smgtagtotranslate in currentcategories)
                        {
                            if (smgtagtotranslate.TagName.ContainsKey(languagecategory))
                            {
                                if (!additionalPoiInfos.Categories.Contains(smgtagtotranslate.TagName[languagecategory].Trim()))
                                    additionalPoiInfos.Categories.Add(smgtagtotranslate.TagName[languagecategory].Trim());
                            }
                        }

                        activityNew.AdditionalPoiInfos.TryAddOrUpdate(languagecategory, additionalPoiInfos);
                    }
                }
            }
        }

    }
}
