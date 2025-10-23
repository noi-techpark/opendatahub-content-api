// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataModel.helpers
{
    public static class AdditionalPropertiesHelper
    {
        public static void FillLTSActivityAdditionalProperties(this ODHActivityPoi odhActivityPoi)
        {
            ActivityLtsDataProperties addprop = new ActivityLtsDataProperties();
            addprop.AltitudeDifference = odhActivityPoi.AltitudeDifference;
            addprop.WayNumber = odhActivityPoi.WayNumber;
            addprop.Number = odhActivityPoi.Number;
            addprop.AltitudeHighestPoint = odhActivityPoi.AltitudeHighestPoint;
            addprop.AltitudeLowestPoint = odhActivityPoi.AltitudeLowestPoint;
            addprop.AltitudeSumDown = odhActivityPoi.AltitudeSumDown;
            addprop.AltitudeSumUp = odhActivityPoi.AltitudeSumUp;
            addprop.DistanceDuration = odhActivityPoi.DistanceDuration;
            addprop.DistanceLength = odhActivityPoi.DistanceLength;
            addprop.Exposition = odhActivityPoi.Exposition;
            addprop.FeetClimb = odhActivityPoi.FeetClimb;
            addprop.HasRentals = odhActivityPoi.HasRentals;
            addprop.IsOpen = odhActivityPoi.IsOpen;
            addprop.IsPrepared = odhActivityPoi.IsPrepared;
            addprop.RunToValley = odhActivityPoi.RunToValley;
            addprop.IsWithLigth = odhActivityPoi.IsWithLigth;
            addprop.HasRentals = odhActivityPoi.HasRentals;
            addprop.LiftAvailable = odhActivityPoi.LiftAvailable;
            addprop.FeetClimb = odhActivityPoi.FeetClimb;
            addprop.BikeTransport = odhActivityPoi.BikeTransport;
            addprop.Exposition = odhActivityPoi.Exposition;
            addprop.MountainBikePermitted = odhActivityPoi.Mapping["lts"].ContainsKey("mountainBikePermitted") ? Convert.ToBoolean(odhActivityPoi.Mapping["lts"]["mountainBikePermitted"]) : null; 
            addprop.LiftType = odhActivityPoi.Mapping["lts"].ContainsKey("liftType") ? odhActivityPoi.Mapping["lts"]["liftType"].ToString() : null;
            addprop.LiftCapacityType = odhActivityPoi.Mapping["lts"].ContainsKey("liftCapacityType") ? odhActivityPoi.Mapping["lts"]["liftCapacityType"].ToString() : null;
            
            var additionalpropertieskey = typeof(ActivityLtsDataProperties).Name;

            if (odhActivityPoi.AdditionalProperties == null )
            {
                odhActivityPoi.AdditionalProperties = new Dictionary<string, dynamic>();
                odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }                
            else
            {
                if(odhActivityPoi.AdditionalProperties.ContainsKey(additionalpropertieskey))
                    odhActivityPoi.AdditionalProperties[additionalpropertieskey] = addprop;
                else
                    odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }

        }

        public static void FillLTSPoiAdditionalProperties(this ODHActivityPoi odhActivityPoi)
        {
            PoiLtsDataProperties addprop = new PoiLtsDataProperties();            
            addprop.IsOpen = odhActivityPoi.IsOpen;
            addprop.HasFreeEntrance = odhActivityPoi.HasFreeEntrance;
            
            var additionalpropertieskey = typeof(PoiLtsDataProperties).Name;

            if (odhActivityPoi.AdditionalProperties == null)
            {
                odhActivityPoi.AdditionalProperties = new Dictionary<string, dynamic>();
                odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }
            else
            {
                if (odhActivityPoi.AdditionalProperties.ContainsKey(additionalpropertieskey))
                    odhActivityPoi.AdditionalProperties[additionalpropertieskey] = addprop;
                else
                    odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }

        }

        public static void FillLTSGastronomyAdditionalProperties(this ODHActivityPoi odhActivityPoi)
        {
            GastronomyLtsDataProperties addprop = new GastronomyLtsDataProperties();
            addprop.MaxSeatingCapacity = odhActivityPoi.MaxSeatingCapacity;
            addprop.CategoryCodes = odhActivityPoi.CategoryCodes;
            addprop.DishRates = odhActivityPoi.DishRates;
            addprop.CapacityCeremony = odhActivityPoi.CapacityCeremony;
            addprop.Facilities = odhActivityPoi.Facilities;
            
            var additionalpropertieskey = typeof(GastronomyLtsDataProperties).Name;

            if (odhActivityPoi.AdditionalProperties == null)
            {
                odhActivityPoi.AdditionalProperties = new Dictionary<string, dynamic>();
                odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }
            else
            {
                if (odhActivityPoi.AdditionalProperties.ContainsKey(additionalpropertieskey))
                    odhActivityPoi.AdditionalProperties[additionalpropertieskey] = addprop;
                else
                    odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }

        }

        public static void FillIDMPoiAdditionalProperties(this ODHActivityPoi odhActivityPoi)
        {
            PoiAgeDataProperties addprop = new PoiAgeDataProperties();
            addprop.AgeFrom = odhActivityPoi.AgeFrom;
            addprop.AgeTo = odhActivityPoi.AgeTo;
            
            var additionalpropertieskey = typeof(PoiAgeDataProperties).Name;

            if (odhActivityPoi.AdditionalProperties == null)
            {
                odhActivityPoi.AdditionalProperties = new Dictionary<string, dynamic>();
                odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }
            else
            {
                if (odhActivityPoi.AdditionalProperties.ContainsKey(additionalpropertieskey))
                    odhActivityPoi.AdditionalProperties[additionalpropertieskey] = addprop;
                else
                    odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }

        }

        public static void FillSuedtirolWeinCompanyAdditionalProperties(this ODHActivityPoi odhActivityPoi)
        {
            SuedtirolWeinCompanyDataProperties addprop = new SuedtirolWeinCompanyDataProperties();
            //Filling properties
            //boolean
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasvisits").Count() > 0)
                addprop.HasVisits = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasvisits").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasovernights").Count() > 0)
                addprop.HasOvernights = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasovernights").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasbiowine").Count() > 0)
                addprop.HasBiowine = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasbiowine").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasaccommodation").Count() > 0)
                addprop.HasAccommodation = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasaccommodation").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "isvinumhotel").Count() > 0)
                addprop.IsVinumHotel = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "isvinumhotel").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "isanteprima").Count() > 0)
                addprop.IsAnteprima = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "isanteprima").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "iswinestories").Count() > 0)
                addprop.IsWineStories = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "iswinestories").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "iswinesummit").Count() > 0)
                addprop.IsWineSummit = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "iswinesummit").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "issparklingwineassociation").Count() > 0)
                addprop.IsSparklingWineassociation = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "issparklingwineassociation").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "iswinery").Count() > 0)
                addprop.IsWinery = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "iswinery").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasonlineshop").Count() > 0)
                addprop.HasOnlineshop = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasonlineshop").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasdeliveryservice").Count() > 0)
                addprop.HasDeliveryservice = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasdeliveryservice").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasdirectsales").Count() > 0)
                addprop.HasDirectSales = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "hasdirectsales").FirstOrDefault().Value);
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "isskyalpspartner").Count() > 0)
                addprop.IsSkyalpsPartner = BoolExtensions.TrySetBool(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "isskyalpspartner").FirstOrDefault().Value);
            //string
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "imagesparklingwineproducer").Count() > 0)
                addprop.ImageSparklingWineproducer = odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "imagesparklingwineproducer").FirstOrDefault().Value;
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialsfacebook").Count() > 0)
                addprop.SocialsFacebook = odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialsfacebook").FirstOrDefault().Value;
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialsinstagram").Count() > 0)
                addprop.SocialsInstagram = odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialsinstagram").FirstOrDefault().Value;
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialslinkedIn").Count() > 0)
                addprop.SocialsLinkedIn = odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialslinkedIn").FirstOrDefault().Value;
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialspinterest").Count() > 0)
                addprop.SocialsPinterest = odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialspinterest").FirstOrDefault().Value;
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialstiktok").Count() > 0)
                addprop.SocialsTiktok = odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialstiktok").FirstOrDefault().Value;
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialsyoutube").Count() > 0)
                addprop.SocialsYoutube = odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialsyoutube").FirstOrDefault().Value;
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialslinkedIn").Count() > 0)
                addprop.SocialsTwitter = odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "socialslinkedIn").FirstOrDefault().Value;

            //ICollection
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.ContainsKey("de") && odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "wines").Count() > 0)
                addprop.Wines = !String.IsNullOrEmpty(odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "wines").FirstOrDefault().Value) ? odhActivityPoi.PoiProperty["de"].Where(x => x.Name == "wines").FirstOrDefault().Value.Split(",").ToList() : null;

            //Dictionary
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "openingtimeswineshop")).Count() > 0)
                addprop.OpeningtimesWineshop = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "openingtimeswineshop");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "openingtimesguides")).Count() > 0)
                addprop.OpeningtimesGuides = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "openingtimesguides");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "openingtimesgastronomie")).Count() > 0)
                addprop.OpeningtimesGastronomie = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "openingtimesgastronomie");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "companyholiday")).Count() > 0)
                addprop.CompanyHoliday = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "companyholiday");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "onlineshopurl")).Count() > 0)
                addprop.OnlineShopurl = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "onlineshopurl");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "deliveryserviceurl")).Count() > 0)
                addprop.DeliveryServiceUrl = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "deliveryserviceurl");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "h1")).Count() > 0)
                addprop.H1 = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "h1");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "h2")).Count() > 0)
                addprop.H2 = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "h2");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "quote")).Count() > 0)
                addprop.Quote = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "quote");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "quoteauthor")).Count() > 0)
                addprop.QuoteAuthor = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "quoteauthor");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "descriptionsparklingwineproducer")).Count() > 0)
                addprop.DescriptionSparklingWineproducer = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "descriptionsparklingwineproducer");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "h1sparklingwineproducer")).Count() > 0)
                addprop.H1SparklingWineproducer = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "h1sparklingwineproducer");
            if (odhActivityPoi.PoiProperty != null && odhActivityPoi.PoiProperty.Where(x => x.Value.Any(y => y.Name == "h2sparklingwineproducer")).Count() > 0)
                addprop.H2SparklingWineproducer = ExtractFromPoiProperty.ExtractValuesFromPoiProperty(odhActivityPoi.PoiProperty, "h2sparklingwineproducer");


            var additionalpropertieskey = typeof(SuedtirolWeinCompanyDataProperties).Name;

            if (odhActivityPoi.AdditionalProperties == null)
            {
                odhActivityPoi.AdditionalProperties = new Dictionary<string, dynamic>();
                odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }
            else
            {
                if (odhActivityPoi.AdditionalProperties.ContainsKey(additionalpropertieskey))
                    odhActivityPoi.AdditionalProperties[additionalpropertieskey] = addprop;
                else
                    odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }
        }

        public static void FillSiagMuseumAdditionalProperties(this ODHActivityPoi odhActivityPoi)
        {
            SiagMuseumDataProperties addprop = new SiagMuseumDataProperties();
            //TODO Fill the properties here

            var additionalpropertieskey = typeof(SiagMuseumDataProperties).Name;

            if (odhActivityPoi.AdditionalProperties == null)
            {
                odhActivityPoi.AdditionalProperties = new Dictionary<string, dynamic>();
                odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }
            else
            {
                if (odhActivityPoi.AdditionalProperties.ContainsKey(additionalpropertieskey))
                    odhActivityPoi.AdditionalProperties[additionalpropertieskey] = addprop;
                else
                    odhActivityPoi.AdditionalProperties.Add(additionalpropertieskey, addprop);
            }

        }
    }

    public static class BoolExtensions
    {
        public static bool? TrySetBool(string? value)
        {
            if (bool.TryParse(value, out var result))
            {
                return result;                
            }

            return null;
        }
    }

    public static class ExtractFromPoiProperty
    {
        public static IDictionary<string,string>? ExtractValuesFromPoiProperty(IDictionary<string, List<PoiProperty>> poiproperty, string name)
        {
            var result = poiproperty
                .Where(kvp => kvp.Value.Any(p =>
                    string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                        .Select(p => p.Value)
                        .FirstOrDefault()
                );

            return result;
        }        
    }
}
