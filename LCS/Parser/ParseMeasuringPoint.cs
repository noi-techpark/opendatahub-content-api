// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DataModel;

namespace LCS
{
    public class ParseMeasuringPoint
    {
        public static CultureInfo myculture = new CultureInfo("en");

        public static Measuringpoint ParseMyMeasuringPoint(
            ServiceReferenceLCS.WeatherSnowObservationDetailRS measuringpoints,
            bool newmeasuringpoint,
            Measuringpoint mymeasuringpoint
        )
        {
            var measuringpoint = measuringpoints.MeasuringPoints.MeasuringPoint.FirstOrDefault();

            mymeasuringpoint.Id = measuringpoint.RID;
            mymeasuringpoint.Shortname = measuringpoint.Name.FirstOrDefault().InnerText;

            mymeasuringpoint.SmgActive =
                measuringpoint.News.Status.IsActive != null
                    ? Convert.ToBoolean(measuringpoint.News.Status.IsActive)
                    : false;
            mymeasuringpoint.Active =
                measuringpoint.News.Status.IsActive != null
                    ? Convert.ToBoolean(measuringpoint.News.Status.IsActive)
                    : false;

            if (measuringpoint.GeoDatas.GeoData != null)
            {
                mymeasuringpoint.Latitude =
                    measuringpoint.GeoDatas.GeoData.FirstOrDefault().Positions.Position != null
                        ? Convert.ToDouble(
                            measuringpoint
                                .GeoDatas.GeoData.FirstOrDefault()
                                .Positions.Position.FirstOrDefault()
                                .Latitude,
                            myculture
                        )
                        : 0;
                mymeasuringpoint.Longitude =
                    measuringpoint.GeoDatas.GeoData.FirstOrDefault().Positions.Position != null
                        ? Convert.ToDouble(
                            measuringpoint
                                .GeoDatas.GeoData.FirstOrDefault()
                                .Positions.Position.FirstOrDefault()
                                .Longitude,
                            myculture
                        )
                        : 0;
                mymeasuringpoint.Altitude =
                    measuringpoint.GeoDatas.GeoData.FirstOrDefault().Positions.Position != null
                        ? Convert.ToDouble(
                            measuringpoint
                                .GeoDatas.GeoData.FirstOrDefault()
                                .Positions.Position.FirstOrDefault()
                                .Altitude,
                            myculture
                        )
                        : 0;
                mymeasuringpoint.AltitudeUnitofMeasure = "m";
                mymeasuringpoint.Gpstype = "center";
            }

            if (newmeasuringpoint)
                mymeasuringpoint.FirstImport = DateTime.Now;

            mymeasuringpoint.LastUpdate = Convert.ToDateTime(measuringpoint.News.Status.LastChange);

            mymeasuringpoint.LastChange = DateTime.Now;

            mymeasuringpoint.SnowHeight =
                measuringpoint.Observation.Snow.Height != null
                    ? measuringpoint.Observation.Snow.Height.ToString()
                    : "-";
            mymeasuringpoint.newSnowHeight =
                measuringpoint.Observation.Snow.NewHeight != null
                    ? measuringpoint.Observation.Snow.NewHeight.ToString()
                    : "-";
            mymeasuringpoint.Temperature =
                measuringpoint.Observation.Temperature != null
                    ? measuringpoint.Observation.Temperature.ToString() + " °"
                    : "-";
            mymeasuringpoint.LastSnowDate =
                measuringpoint.Observation.Snow.DateLastSnow != null
                    ? Convert.ToDateTime(measuringpoint.Observation.Snow.DateLastSnow)
                    : DateTime.MinValue;

            List<WeatherObservation> myweatherobservationlist = new List<WeatherObservation>();

            if (measuringpoint.Observation.WeatherForecasts != null)
            {
                if (measuringpoint.Observation.WeatherForecasts.WeatherForecast != null)
                {
                    foreach (
                        var weatherobservation in measuringpoint
                            .Observation
                            .WeatherForecasts
                            .WeatherForecast
                    )
                    {
                        WeatherObservation myobservation = new WeatherObservation();                        
                        myobservation.Id = measuringpoint.Observation.WeatherForecasts.ID;
                        myobservation.IconID = weatherobservation.IconID;
                        myobservation.Date = null;
                        if (!String.IsNullOrEmpty(weatherobservation.Date))
                            myobservation.Date = DateTime.Parse(weatherobservation.Date);

                        myobservation.WeatherStatus["de"] =
                            weatherobservation.Description != null
                                ? weatherobservation.Description.InnerText
                                : "";

                        myweatherobservationlist.Add(myobservation);
                    }
                }
            }
            mymeasuringpoint.WeatherObservation = myweatherobservationlist.ToList();

            mymeasuringpoint.OwnerId = measuringpoint.Owner.RID;

            List<string> arealist = new List<string>();

            foreach (var membership in measuringpoint.Memberships.Membership)
            {
                if (membership.Area != null)
                {
                    foreach (var myarea in membership.Area)
                    {
                        arealist.Add(myarea.RID);
                    }
                }
            }

            mymeasuringpoint.AreaIds = arealist.ToList();

            return mymeasuringpoint;
        }
    }
}
