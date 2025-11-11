// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers
{
    public class SkiAreasToODHActivityPoisImportHelper : ImportHelper, IImportHelper
    {
        public SkiAreasToODHActivityPoisImportHelper(
             ISettings settings,
             QueryFactory queryfactory,
             string table,
             string importerURL
         )
             : base(settings, queryfactory, table, importerURL) { }

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
            
            //loop trough outdooractive items
            foreach (var skiarea in skiareas)
            {
                var importresult = await ImportDataSingle(skiarea, cancellationToken);

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
        public async Task<UpdateDetail> ImportDataSingle(SkiAreaLinked skiarea, CancellationToken cancellationToken)
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

        private ODHActivityPoiLinked ParseSkiAreaToODHActivityPoi(SkiArea skiarea, ODHActivityPoiLinked odhactivitypoi)
        {
            if (odhactivitypoi == null)
            {
                //Neu
                odhactivitypoi = new ODHActivityPoiLinked();
                odhactivitypoi.FirstImport = DateTime.Now;
                odhactivitypoi.LastChange = DateTime.Now;



                odhactivitypoi.Source = "Common";
                odhactivitypoi.SyncSourceInterface = "Common";
                odhactivitypoi.SyncUpdateMode = "Full";

                odhactivitypoi.Active = skigebiet.Active;

                odhactivitypoi.AltitudeDifference = 0;
                odhactivitypoi.AltitudeHighestPoint = 0;
                odhactivitypoi.AltitudeLowestPoint = 0;
                odhactivitypoi.AltitudeSumDown = 0;
                odhactivitypoi.AltitudeSumUp = 0;
                odhactivitypoi.AreaId = skigebiet.AreaId;

                odhactivitypoi.ContactInfos = skigebiet.ContactInfos;
                odhactivitypoi.Detail = skigebiet.Detail;
                odhactivitypoi.Difficulty = "";
                odhactivitypoi.DistanceDuration = 0;
                odhactivitypoi.DistanceLength = 0;
                odhactivitypoi.Exposition = null;
                odhactivitypoi.FeetClimb = false;
                odhactivitypoi.GpsInfo = new List<GpsInfo>() { new GpsInfo() { Altitude = skigebiet.Altitude, AltitudeUnitofMeasure = skigebiet.AltitudeUnitofMeasure, Gpstype = "position", Latitude = skigebiet.Latitude, Longitude = skigebiet.Longitude } };
                odhactivitypoi.HasFreeEntrance = false;
                odhactivitypoi.HasLanguage = skigebiet.HasLanguage;
                odhactivitypoi.HasRentals = true;
                odhactivitypoi.Highlight = true;
                odhactivitypoi.Id = "smgpoi" + skigebiet.Id;
                odhactivitypoi.ImageGallery = skigebiet.ImageGallery;
                odhactivitypoi.IsOpen = true;
                odhactivitypoi.IsPrepared = true;
                odhactivitypoi.IsWithLigth = false;
                odhactivitypoi.LiftAvailable = true;
                odhactivitypoi.LocationInfo = skigebiet.LocationInfo;

                //Mapping
                odhactivitypoi.Mapping = skigebiet.Mapping;

                odhactivitypoi.MaxSeatingCapacity = 0;
                odhactivitypoi.OperationSchedule = skigebiet.OperationSchedule;

                PoiProperty totalkm = new PoiProperty() { Name = "TotalSlopeKm", Value = skigebiet.TotalSlopeKm };
                PoiProperty SlopeKmBlue = new PoiProperty() { Name = "SlopeKmBlue", Value = skigebiet.SlopeKmBlue };
                PoiProperty SlopeKmRed = new PoiProperty() { Name = "SlopeKmRed", Value = skigebiet.SlopeKmRed };
                PoiProperty SlopeKmBlack = new PoiProperty() { Name = "SlopeKmBlack", Value = skigebiet.SlopeKmBlack };
                PoiProperty SkiRegionId = new PoiProperty() { Name = "SkiRegionId", Value = skigebiet.SkiRegionId };
                PoiProperty SkiAreaMapURL = new PoiProperty() { Name = "SkiAreaMapURL", Value = skigebiet.SkiAreaMapURL };

                Dictionary<string, List<PoiProperty>> mypoipropertylistdict = new Dictionary<string, List<PoiProperty>>();

                foreach (var language in languagelistcategories)
                {
                    PoiProperty SkiRegionName = new PoiProperty() { Name = "SkiRegionName", Value = skigebiet.SkiRegionName[language] };

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

                odhactivitypoi.Webcam = skigebiet.Webcam;

                //odhactivitypoi.PoiServices;

                odhactivitypoi.Type = null;
                odhactivitypoi.SubType = null;

                odhactivitypoi.PoiType = GetTheRightSkiregion(skigebiet.SkiRegionName["de"]);

                odhactivitypoi.Ratings = null;
                odhactivitypoi.RelatedContent = null;
                odhactivitypoi.RunToValley = true;
                odhactivitypoi.Shortname = skigebiet.Shortname;
                odhactivitypoi.SmgTags = skigebiet.SmgTags;
                odhactivitypoi.SmgTags.Add("Winter");

                odhactivitypoi.TourismorganizationId = skigebiet.TourismvereinIds.FirstOrDefault();
                
                foreach (var language in languagelistcategories)
                {
                    AdditionalPoiInfos additional = new AdditionalPoiInfos();
                    additional.Language = language;
                    additional.MainType = null;
                    additional.SubType = null;
                    additional.PoiType = null;
                    odhactivitypoi.AdditionalPoiInfos.TryAddOrUpdate(language, additional);
                }

                //Setting Categorization by Valid Tags
                var currentcategories = validtagsforcategories.Where(x => x.Id.ToLower().In(odhactivitypoi.SmgTags.Select(y => y.ToLower())));

                foreach (var smgtagtotranslate in currentcategories)
                {
                    foreach (var languagecategory in languagelistcategories)
                    {
                        if (odhactivitypoi.AdditionalPoiInfos[languagecategory].Categories == null)
                            odhactivitypoi.AdditionalPoiInfos[languagecategory].Categories = new List<string>();

                        if (smgtagtotranslate.TagName.ContainsKey(languagecategory) && !odhactivitypoi.AdditionalPoiInfos[languagecategory].Categories.Contains(smgtagtotranslate.TagName[languagecategory].Trim()))
                            odhactivitypoi.AdditionalPoiInfos[languagecategory].Categories.Add(smgtagtotranslate.TagName[languagecategory].Trim());
                    }
                }

                //Calculate GPS Distance to District and Municipality
                if (odhactivitypoi.LocationInfo != null)
                {
                    if (odhactivitypoi.LocationInfo.DistrictInfo != null)
                    {
                        var districtreduced = districtreducedinfo.Where(x => x.Id == odhactivitypoi.LocationInfo.DistrictInfo.Id).FirstOrDefault();
                        if (districtreduced != null)
                        {
                            odhactivitypoi.ExtendGpsInfoToDistanceCalculationList("district", districtreduced.Latitude, districtreduced.Longitude);
                        }
                    }
                    if (odhactivitypoi.LocationInfo.MunicipalityInfo != null)
                    {
                        var municipalityreduced = municipalityreducedinfo.Where(x => x.Id == odhactivitypoi.LocationInfo.MunicipalityInfo.Id).FirstOrDefault();
                        if (municipalityreduced != null)
                        {
                            odhactivitypoi.ExtendGpsInfoToDistanceCalculationList("municipality", municipalityreduced.Latitude, municipalityreduced.Longitude);
                        }
                    }
                }

                //Set Main Type as Activity/Poi/Gastronomy
                SmgPoiHelper.SetMainCategorizationForODHActivityPoi(odhactivitypoi);

                //Setting LicenseInfo
                odhactivitypoi.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<SmgPoi>(odhactivitypoi, Helper.LicenseHelper.GetLicenseforOdhActivityPoi);                
            }
            else
            {
                odhactivitypoi.LastChange = DateTime.Now;
                odhactivitypoi.CustomId = skigebiet.Id;
                odhactivitypoi.Source = "Common";
                odhactivitypoi.SyncSourceInterface = "Common";
                odhactivitypoi.SyncUpdateMode = "Full";

                odhactivitypoi.Active = skigebiet.Active;
                odhactivitypoi.SmgActive = skigebiet.SmgActive;

                odhactivitypoi.AltitudeDifference = 0;
                odhactivitypoi.AltitudeHighestPoint = 0;
                odhactivitypoi.AltitudeLowestPoint = 0;
                odhactivitypoi.AltitudeSumDown = 0;
                odhactivitypoi.AltitudeSumUp = 0;
                odhactivitypoi.AreaId = skigebiet.AreaId;

                odhactivitypoi.ContactInfos = skigebiet.ContactInfos;
                odhactivitypoi.Detail = skigebiet.Detail;
                odhactivitypoi.Difficulty = "";
                odhactivitypoi.DistanceDuration = 0;
                odhactivitypoi.DistanceLength = 0;
                odhactivitypoi.Exposition = null;
                odhactivitypoi.FeetClimb = false;
                odhactivitypoi.GpsInfo = new List<GpsInfo>() { new GpsInfo() { Altitude = skigebiet.Altitude, AltitudeUnitofMeasure = skigebiet.AltitudeUnitofMeasure, Gpstype = "position", Latitude = skigebiet.Latitude, Longitude = skigebiet.Longitude } };
                odhactivitypoi.HasFreeEntrance = false;
                odhactivitypoi.HasLanguage = skigebiet.HasLanguage;
                odhactivitypoi.HasRentals = true;
                odhactivitypoi.Highlight = true;
                odhactivitypoi.ImageGallery = skigebiet.ImageGallery;
                odhactivitypoi.IsOpen = true;
                odhactivitypoi.IsPrepared = true;
                odhactivitypoi.IsWithLigth = false;
                odhactivitypoi.LiftAvailable = true;
                odhactivitypoi.LocationInfo = skigebiet.LocationInfo;

                odhactivitypoi.MaxSeatingCapacity = 0;
                odhactivitypoi.OperationSchedule = skigebiet.OperationSchedule;

                PoiProperty totalkm = new PoiProperty() { Name = "TotalSlopeKm", Value = skigebiet.TotalSlopeKm };
                PoiProperty SlopeKmBlue = new PoiProperty() { Name = "SlopeKmBlue", Value = skigebiet.SlopeKmBlue };
                PoiProperty SlopeKmRed = new PoiProperty() { Name = "SlopeKmRed", Value = skigebiet.SlopeKmRed };
                PoiProperty SlopeKmBlack = new PoiProperty() { Name = "SlopeKmBlack", Value = skigebiet.SlopeKmBlack };
                PoiProperty SkiRegionId = new PoiProperty() { Name = "SkiRegionId", Value = skigebiet.SkiRegionId };
                PoiProperty SkiAreaMapURL = new PoiProperty() { Name = "SkiAreaMapURL", Value = skigebiet.SkiAreaMapURL };


                Dictionary<string, List<PoiProperty>> mypoipropertylistdict = new Dictionary<string, List<PoiProperty>>();

                foreach (var language in languagelistcategories)
                {
                    PoiProperty SkiRegionName = new PoiProperty() { Name = "SkiRegionName", Value = skigebiet.SkiRegionName[language] };

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

                odhactivitypoi.Webcam = skigebiet.Webcam;

                //Mapping
                odhactivitypoi.Mapping = skigebiet.Mapping;

                odhactivitypoi.Type = "Winter";
                odhactivitypoi.SubType = "Skigebiete";

                odhactivitypoi.PoiType = GetTheRightSkiregion(skigebiet.SkiRegionName["de"]);

                odhactivitypoi.Ratings = null;
                odhactivitypoi.RelatedContent = null;
                odhactivitypoi.RunToValley = true;
                odhactivitypoi.Shortname = skigebiet.Shortname;
               
                foreach (var language in languagelistcategories)
                {
                    AdditionalPoiInfos additional = new AdditionalPoiInfos();
                    additional.Language = language;
                    additional.MainType = null;
                    additional.SubType = null;
                    additional.PoiType = null;
                    odhactivitypoi.AdditionalPoiInfos.TryAddOrUpdate(language, additional);
                }

                //Calculate GPS Distance to District and Municipality
                if (odhactivitypoi.LocationInfo != null)
                {
                    if (odhactivitypoi.LocationInfo.DistrictInfo != null)
                    {
                        var districtreduced = districtreducedinfo.Where(x => x.Id == odhactivitypoi.LocationInfo.DistrictInfo.Id).FirstOrDefault();
                        if (districtreduced != null)
                        {
                            odhactivitypoi.ExtendGpsInfoToDistanceCalculationList("district", districtreduced.Latitude, districtreduced.Longitude);
                        }
                    }
                    if (odhactivitypoi.LocationInfo.MunicipalityInfo != null)
                    {
                        var municipalityreduced = municipalityreducedinfo.Where(x => x.Id == odhactivitypoi.LocationInfo.MunicipalityInfo.Id).FirstOrDefault();
                        if (municipalityreduced != null)
                        {
                            odhactivitypoi.ExtendGpsInfoToDistanceCalculationList("municipality", municipalityreduced.Latitude, municipalityreduced.Longitude);
                        }
                    }
                }

                //Setting LicenseInfo
                odhactivitypoi.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<SmgPoi>(odhactivitypoi, Helper.LicenseHelper.GetLicenseforOdhActivityPoi);

                //Setting Categorization by Valid Tags
                var currentcategories = validtagsforcategories.Where(x => x.Id.ToLower().In(odhactivitypoi.SmgTags.Select(y => y.ToLower())));

                //Resetting Categories
                foreach (var languagecategory in languagelistcategories)
                {
                    if (odhactivitypoi.AdditionalPoiInfos[languagecategory].Categories == null)
                        odhactivitypoi.AdditionalPoiInfos[languagecategory].Categories = new List<string>();
                    else
                        odhactivitypoi.AdditionalPoiInfos[languagecategory].Categories.Clear();
                }
                //Reassigning Categories
                foreach (var smgtagtotranslate in currentcategories)
                {
                    foreach (var languagecategory in languagelistcategories)
                    {
                        if (smgtagtotranslate.TagName.ContainsKey(languagecategory) && !odhactivitypoi.AdditionalPoiInfos[languagecategory].Categories.Contains(smgtagtotranslate.TagName[languagecategory].Trim()))
                            odhactivitypoi.AdditionalPoiInfos[languagecategory].Categories.Add(smgtagtotranslate.TagName[languagecategory].Trim());
                    }
                }

                //Set Main Type as Activity/Poi/Gastronomy
                SmgPoiHelper.SetMainCategorizationForODHActivityPoi(odhactivitypoi);
                
            }
        }
    }
}
