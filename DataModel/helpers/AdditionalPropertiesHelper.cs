// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            //TODO Fill the properties here           

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
}
