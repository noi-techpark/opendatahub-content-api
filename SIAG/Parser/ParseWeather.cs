// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Amazon.Auth.AccessControlPolicy;
using DataModel;
using Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIAG.Model;

namespace SIAG
{
    public class ParseWeather
    {
        //string urlfrom indicates 'siag' old parsing, 'opendata' new url

        public static WeatherLinked ParsemyWeatherResponse(
            string lang,
            XDocument weatherdataxml,
            XDocument weatherresponse,
            string source
        )
        {
            try
            {
                WeatherLinked myweather = new WeatherLinked();

                myweather.Id = Convert.ToInt32(weatherresponse.Root.Element("Id").Value);
                myweather.Date = Convert.ToDateTime(
                    weatherresponse
                        .Root.Element("date")
                        .Value.Replace(
                            "00:00:00",
                            weatherresponse.Root.Element("hour").Value + ":00"
                        )
                );
                myweather.Evolution =
                    weatherresponse.Root.Element("evolution") != null
                        ? weatherresponse.Root.Element("evolution").Value
                        : null;
                myweather.EvolutionTitle =
                    weatherresponse.Root.Element("evolutionTitle") != null
                        ? weatherresponse.Root.Element("evolutionTitle").Value
                        : null;
                myweather.Language = lang;

                myweather.LicenseInfo = Helper.LicenseHelper.GetLicenseforWeather(source);

                var mydayforecasts = weatherresponse.Root.Elements("dayForecast");
                var mountaintoday = weatherresponse.Root.Element("mountainToday");
                var mountaintomorrow = weatherresponse.Root.Element("mountainTomorrow");
                var today = weatherresponse.Root.Element("today");
                var tomorrow = weatherresponse.Root.Element("tomorrow");

                //Forecast info
                foreach (XElement forecast in mydayforecasts)
                {
                    Forecast myforecast = new Forecast();

                    myforecast.Date = Convert.ToDateTime(forecast.Element("date").Value);

                    if (forecast.Element("reliability") != null)
                    {
                        myforecast.Reliability = forecast.Element("reliability").Value;

                        myforecast.WeatherCode = forecast.Element("symbol").Element("code").Value;
                        myforecast.WeatherDesc = forecast
                            .Element("symbol")
                            .Element("description")
                            .Value;
                        myforecast.WeatherImgUrl = forecast
                            .Element("symbol")
                            .Element("imageURL")
                            .Value;

                        myforecast.TempMaxmax = Convert.ToInt32(
                            forecast.Element("tempMax").Element("max").Value
                        );
                        myforecast.TempMaxmin = Convert.ToInt32(
                            forecast.Element("tempMax").Element("min").Value
                        );

                        myforecast.TempMinmax = Convert.ToInt32(
                            forecast.Element("tempMin").Element("max").Value
                        );
                        myforecast.TempMinmin = Convert.ToInt32(
                            forecast.Element("tempMin").Element("min").Value
                        );

                        myweather.Forecast.Add(myforecast);
                    }
                }

                // mountain Info

                if (mountaintoday != null)
                {
                    Mountain mymountaintoday = new Mountain();
                    mymountaintoday.Date = Convert.ToDateTime(mountaintoday.Element("date").Value);
                    mymountaintoday.Title =
                        mountaintoday.Element("title") != null
                            ? mountaintoday.Element("title").Value
                            : "";
                    mymountaintoday.Zerolimit =
                        mountaintoday.Element("zerolimit") != null
                            ? mountaintoday.Element("zerolimit").Value
                            : "";
                    mymountaintoday.WeatherDesc =
                        mountaintoday.Element("weather") != null
                            ? mountaintoday.Element("weather").Value
                            : "";

                    mymountaintoday.Conditions =
                        mountaintoday.Element("conditions") != null
                            ? mountaintoday.Element("conditions").Value
                            : "";
                    mymountaintoday.MountainImgUrl = mountaintoday.Element("imageURL").Value;
                    mymountaintoday.Moonrise =
                        mountaintoday.Elements("moonRise").Count() > 0
                            ? mountaintoday.Element("moonRise").Value
                            : "";
                    mymountaintoday.Moonset =
                        mountaintoday.Elements("moonSet").Count() > 0
                            ? mountaintoday.Element("moonSet").Value
                            : "";
                    mymountaintoday.Sunrise =
                        mountaintoday.Elements("sunRise").Count() > 0
                            ? mountaintoday.Element("sunRise").Value
                            : "";
                    mymountaintoday.Sunset =
                        mountaintoday.Elements("sunSet").Count() > 0
                            ? mountaintoday.Element("sunSet").Value
                            : "";
                    mymountaintoday.Reliability = mountaintoday.Element("reliability").Value;
                    mymountaintoday.Temp1000 = Convert.ToInt32(
                        mountaintoday.Element("temp1000").Value
                    );
                    mymountaintoday.Temp2000 = Convert.ToInt32(
                        mountaintoday.Element("temp2000").Value
                    );
                    mymountaintoday.Temp3000 = Convert.ToInt32(
                        mountaintoday.Element("temp3000").Value
                    );
                    mymountaintoday.Temp4000 = Convert.ToInt32(
                        mountaintoday.Element("temp4000").Value
                    );

                    mymountaintoday.NorthCode = mountaintoday
                        .Element("north")
                        .Element("code")
                        .Value;
                    mymountaintoday.NorthDesc = mountaintoday
                        .Element("north")
                        .Element("description")
                        .Value;
                    mymountaintoday.NorthImgUrl = mountaintoday
                        .Element("north")
                        .Element("imageURL")
                        .Value;
                    mymountaintoday.SouthCode = mountaintoday
                        .Element("south")
                        .Element("code")
                        .Value;
                    mymountaintoday.SouthDesc = mountaintoday
                        .Element("south")
                        .Element("description")
                        .Value;
                    mymountaintoday.SouthImgUrl = mountaintoday
                        .Element("south")
                        .Element("imageURL")
                        .Value;
                    mymountaintoday.WindCode = mountaintoday.Element("wind").Element("code").Value;
                    mymountaintoday.WindDesc = mountaintoday
                        .Element("wind")
                        .Element("description")
                        .Value;
                    mymountaintoday.WindImgUrl = mountaintoday
                        .Element("wind")
                        .Element("imageURL")
                        .Value;

                    myweather.Mountain.Add(mymountaintoday);
                }

                // mountain Info Tomorrow
                if (mountaintomorrow != null)
                {
                    if (mountaintomorrow.HasElements)
                    {
                        Mountain mymountaintomorrow = new Mountain();
                        mymountaintomorrow.Date = Convert.ToDateTime(
                            mountaintomorrow.Element("date").Value
                        );
                        mymountaintomorrow.Title =
                            mountaintomorrow.Element("title") != null
                                ? mountaintomorrow.Element("title").Value
                                : "";
                        mymountaintomorrow.Zerolimit =
                            mountaintomorrow.Element("zerolimit") != null
                                ? mountaintomorrow.Element("zerolimit").Value
                                : "";
                        mymountaintomorrow.WeatherDesc =
                            mountaintomorrow.Element("weather") != null
                                ? mountaintomorrow.Element("weather").Value
                                : "";

                        mymountaintomorrow.Conditions =
                            mountaintomorrow.Element("conditions") != null
                                ? mountaintomorrow.Element("conditions").Value
                                : "";
                        mymountaintomorrow.MountainImgUrl =
                            mountaintomorrow.Element("imageURL") != null
                                ? mountaintomorrow.Element("imageURL").Value
                                : "";
                        mymountaintomorrow.Moonrise =
                            mountaintomorrow.Elements("moonRise").Count() > 0
                                ? mountaintomorrow.Element("moonRise").Value
                                : "";
                        mymountaintomorrow.Moonset =
                            mountaintomorrow.Elements("moonSet").Count() > 0
                                ? mountaintomorrow.Element("moonSet").Value
                                : "";
                        mymountaintomorrow.Sunrise =
                            mountaintomorrow.Elements("sunRise").Count() > 0
                                ? mountaintomorrow.Element("sunRise").Value
                                : "";
                        mymountaintomorrow.Sunset =
                            mountaintomorrow.Elements("sunSet").Count() > 0
                                ? mountaintomorrow.Element("sunSet").Value
                                : "";
                        mymountaintomorrow.Reliability =
                            mountaintomorrow.Element("reliability") != null
                                ? mountaintomorrow.Element("reliability").Value
                                : "";
                        mymountaintomorrow.Temp1000 =
                            mountaintomorrow.Element("temp1000") != null
                                ? Convert.ToInt32(mountaintomorrow.Element("temp1000").Value)
                                : 0;
                        mymountaintomorrow.Temp2000 =
                            mountaintomorrow.Element("temp2000") != null
                                ? Convert.ToInt32(mountaintomorrow.Element("temp2000").Value)
                                : 0;
                        mymountaintomorrow.Temp3000 =
                            mountaintomorrow.Element("temp3000") != null
                                ? Convert.ToInt32(mountaintomorrow.Element("temp3000").Value)
                                : 0;
                        mymountaintomorrow.Temp4000 =
                            mountaintomorrow.Element("temp4000") != null
                                ? Convert.ToInt32(mountaintomorrow.Element("temp4000").Value)
                                : 0;

                        mymountaintomorrow.NorthCode = mountaintomorrow
                            .Element("north")
                            .Element("code")
                            .Value;
                        mymountaintomorrow.NorthDesc = mountaintomorrow
                            .Element("north")
                            .Element("description")
                            .Value;
                        mymountaintomorrow.NorthImgUrl = mountaintomorrow
                            .Element("north")
                            .Element("imageURL")
                            .Value;
                        mymountaintomorrow.SouthCode = mountaintomorrow
                            .Element("south")
                            .Element("code")
                            .Value;
                        mymountaintomorrow.SouthDesc = mountaintomorrow
                            .Element("south")
                            .Element("description")
                            .Value;
                        mymountaintomorrow.SouthImgUrl = mountaintomorrow
                            .Element("south")
                            .Element("imageURL")
                            .Value;
                        mymountaintomorrow.WindCode = mountaintomorrow
                            .Element("wind")
                            .Element("code")
                            .Value;
                        mymountaintomorrow.WindDesc = mountaintomorrow
                            .Element("wind")
                            .Element("description")
                            .Value;
                        mymountaintomorrow.WindImgUrl = mountaintomorrow
                            .Element("wind")
                            .Element("imageURL")
                            .Value;

                        myweather.Mountain.Add(mymountaintomorrow);
                    }
                }

                //Today Info
                if (today != null)
                {
                    Conditions myconditiontoday = new Conditions();

                    myconditiontoday.Date = Convert.ToDateTime(today.Element("date").Value);
                    myconditiontoday.WeatherCondition =
                        today.Element("conditions") != null
                            ? today.Element("conditions").Value
                            : "";
                    myconditiontoday.WeatherImgUrl =
                        today.Element("imageURL") != null ? today.Element("imageURL").Value : "";
                    myconditiontoday.WeatherDesc =
                        today.Element("weather") != null ? today.Element("weather").Value : "";
                    myconditiontoday.Title =
                        today.Element("title") != null ? today.Element("title").Value : "";
                    myconditiontoday.Temperatures =
                        today.Element("temperatures") != null
                            ? today.Element("temperatures").Value
                            : "";

                    myweather.Conditions.Add(myconditiontoday);
                }
                //Stationdata today
                if (today != null)
                {
                    var stationstoday = today.Elements("stationData");

                    if (stationstoday != null)
                    {
                        foreach (XElement stationtoday in stationstoday)
                        {
                            Stationdata mystationdatatoday = new Stationdata();

                            mystationdatatoday.Date = Convert.ToDateTime(
                                today.Element("date").Value
                            );

                            mystationdatatoday.Id = Convert.ToInt32(
                                stationtoday.Element("Id").Value
                            );
                            var mystationdatacity = weatherdataxml
                                .Root.Elements("Station")
                                .Where(x =>
                                    x.Attribute("Id").Value.Equals(stationtoday.Element("Id").Value)
                                )
                                .FirstOrDefault();
                            mystationdatatoday.CityName = mystationdatacity
                                .Attribute("Name" + lang.ToUpper())
                                .Value;

                            if (lang == "en")
                            {
                                if (mystationdatatoday.CityName.Contains("/"))
                                {
                                    mystationdatatoday.CityName =
                                        mystationdatatoday.CityName.Replace("/", " / ");
                                }
                            }
                            //mystationdatatoday.stationdatacityrid = mystationdatacity.Attribute("RID").Value;

                            mystationdatatoday.WeatherCode = stationtoday
                                .Element("symbol")
                                .Element("code")
                                .Value;
                            mystationdatatoday.WeatherDesc = stationtoday
                                .Element("symbol")
                                .Element("description")
                                .Value;
                            mystationdatatoday.WeatherImgUrl = stationtoday
                                .Element("symbol")
                                .Element("imageURL")
                                .Value;

                            if (source == "siag")
                            {
                                mystationdatatoday.MaxTemp = Convert.ToInt32(
                                    stationtoday.Element("temperature").Element("max").Value
                                );
                                mystationdatatoday.MinTemp = Convert.ToInt32(
                                    stationtoday.Element("temperature").Element("min").Value
                                );
                            }
                            else
                            {
                                mystationdatatoday.MaxTemp = Convert.ToInt32(
                                    stationtoday.Element("max").Value
                                );
                                mystationdatatoday.MinTemp = Convert.ToInt32(
                                    stationtoday.Element("min").Value
                                );
                            }

                            myweather.Stationdata.Add(mystationdatatoday);
                        }
                    }
                }

                //Tomorrow info
                if (tomorrow != null)
                {
                    if (tomorrow.HasElements)
                    {
                        Conditions myconditiontomorrow = new Conditions();

                        myconditiontomorrow.Date = Convert.ToDateTime(
                            tomorrow.Element("date").Value
                        );
                        myconditiontomorrow.WeatherCondition =
                            tomorrow.Element("conditions") != null
                                ? tomorrow.Element("conditions").Value
                                : "";
                        myconditiontomorrow.WeatherImgUrl =
                            tomorrow.Element("imageURL") != null
                                ? tomorrow.Element("imageURL").Value
                                : "";
                        myconditiontomorrow.WeatherDesc =
                            tomorrow.Element("weather") != null
                                ? tomorrow.Element("weather").Value
                                : "";
                        myconditiontomorrow.Title =
                            tomorrow.Element("title") != null
                                ? tomorrow.Element("title").Value
                                : "";
                        myconditiontomorrow.Temperatures =
                            tomorrow.Element("temperatures") != null
                                ? tomorrow.Element("temperatures").Value
                                : "";

                        myweather.Conditions.Add(myconditiontomorrow);
                    }
                }

                //Stationdata today
                if (tomorrow != null)
                {
                    if (tomorrow.HasElements)
                    {
                        var stationstomorrow = tomorrow.Elements("stationData");
                        if (stationstomorrow != null)
                        {
                            foreach (XElement stationtomorrow in stationstomorrow)
                            {
                                Stationdata mystationdatatomorrow = new Stationdata();

                                mystationdatatomorrow.Date = Convert.ToDateTime(
                                    tomorrow.Element("date").Value
                                );

                                mystationdatatomorrow.Id = Convert.ToInt32(
                                    stationtomorrow.Element("Id").Value
                                );
                                var mystationdatacity = weatherdataxml
                                    .Root.Elements("Station")
                                    .Where(x =>
                                        x.Attribute("Id")
                                            .Value.Equals(stationtomorrow.Element("Id").Value)
                                    )
                                    .FirstOrDefault();
                                mystationdatatomorrow.CityName = mystationdatacity
                                    .Attribute("Name" + lang.ToUpper())
                                    .Value;
                                //mystationdatatomorrow.stationdatacityrid = mystationdatacity.Attribute("RID").Value;

                                if (lang == "en")
                                {
                                    if (mystationdatatomorrow.CityName.Contains("/"))
                                    {
                                        mystationdatatomorrow.CityName =
                                            mystationdatatomorrow.CityName.Replace("/", " / ");
                                    }
                                }

                                mystationdatatomorrow.WeatherCode = stationtomorrow
                                    .Element("symbol")
                                    .Element("code")
                                    .Value;
                                mystationdatatomorrow.WeatherDesc = stationtomorrow
                                    .Element("symbol")
                                    .Element("description")
                                    .Value;
                                mystationdatatomorrow.WeatherImgUrl = stationtomorrow
                                    .Element("symbol")
                                    .Element("imageURL")
                                    .Value;

                                if (source == "siag")
                                {
                                    mystationdatatomorrow.MaxTemp = Convert.ToInt32(
                                        stationtomorrow.Element("temperature").Element("max").Value
                                    );
                                    mystationdatatomorrow.MinTemp = Convert.ToInt32(
                                        stationtomorrow.Element("temperature").Element("min").Value
                                    );
                                }
                                else
                                {
                                    mystationdatatomorrow.MaxTemp = Convert.ToInt32(
                                        stationtomorrow.Element("max").Value
                                    );
                                    mystationdatatomorrow.MinTemp = Convert.ToInt32(
                                        stationtomorrow.Element("min").Value
                                    );
                                }

                                myweather.Stationdata.Add(mystationdatatomorrow);
                            }
                        }
                    }
                }

                return myweather;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static WeatherLinked ParsemyStationWeatherResponse(
            string lang,
            XDocument weatherdataxml,
            XDocument weatherresponse,
            string stationid,
            string source
        )
        {
            WeatherLinked myweather = new WeatherLinked();

            myweather.Id = Convert.ToInt32(weatherresponse.Root.Element("Id").Value);
            myweather.Date = Convert.ToDateTime(
                weatherresponse
                    .Root.Element("date")
                    .Value.Replace("00:00:00", weatherresponse.Root.Element("hour").Value + ":00")
            );
            myweather.LicenseInfo = Helper.LicenseHelper.GetLicenseforWeather(source);

            var today = weatherresponse.Root.Element("today");
            var tomorrow = weatherresponse.Root.Element("tomorrow");

            //Stationdata today
            if (today != null)
            {
                var stationstoday = today.Elements("stationData");

                if (stationstoday != null)
                {
                    foreach (
                        XElement stationtoday in stationstoday.Where(x =>
                            x.Element("Id").Value == stationid
                        )
                    )
                    {
                        Stationdata mystationdatatoday = new Stationdata();

                        mystationdatatoday.Date = Convert.ToDateTime(today.Element("date").Value);

                        mystationdatatoday.Id = Convert.ToInt32(stationtoday.Element("Id").Value);
                        var mystationdatacity = weatherdataxml
                            .Root.Elements("Station")
                            .Where(x =>
                                x.Attribute("Id").Value.Equals(stationtoday.Element("Id").Value)
                            )
                            .FirstOrDefault();
                        mystationdatatoday.CityName = mystationdatacity
                            .Attribute("Name" + lang.ToUpper())
                            .Value;

                        if (lang == "en")
                        {
                            if (mystationdatatoday.CityName.Contains("/"))
                            {
                                mystationdatatoday.CityName = mystationdatatoday.CityName.Replace(
                                    "/",
                                    " / "
                                );
                            }
                        }

                        //mystationdatatoday.stationdatacityrid = mystationdatacity.Attribute("RID").Value;

                        mystationdatatoday.WeatherCode = stationtoday
                            .Element("symbol")
                            .Element("code")
                            .Value;
                        mystationdatatoday.WeatherDesc = stationtoday
                            .Element("symbol")
                            .Element("description")
                            .Value;
                        mystationdatatoday.WeatherImgUrl = stationtoday
                            .Element("symbol")
                            .Element("imageURL")
                            .Value;

                        if (source == "siag")
                        {
                            mystationdatatoday.MaxTemp = Convert.ToInt32(
                                stationtoday.Element("temperature").Element("max").Value
                            );
                            mystationdatatoday.MinTemp = Convert.ToInt32(
                                stationtoday.Element("temperature").Element("min").Value
                            );
                        }
                        else
                        {
                            mystationdatatoday.MaxTemp = Convert.ToInt32(
                                stationtoday.Element("max").Value
                            );
                            mystationdatatoday.MinTemp = Convert.ToInt32(
                                stationtoday.Element("min").Value
                            );
                        }

                        myweather.Stationdata.Add(mystationdatatoday);
                    }
                }
            }

            //Stationdata today
            if (tomorrow != null)
            {
                var stationstomorrow = tomorrow.Elements("stationData");
                if (stationstomorrow != null)
                {
                    foreach (
                        XElement stationtomorrow in stationstomorrow.Where(x =>
                            x.Element("Id").Value == stationid
                        )
                    )
                    {
                        Stationdata mystationdatatomorrow = new Stationdata();

                        mystationdatatomorrow.Date = Convert.ToDateTime(
                            tomorrow.Element("date").Value
                        );

                        mystationdatatomorrow.Id = Convert.ToInt32(
                            stationtomorrow.Element("Id").Value
                        );
                        var mystationdatacity = weatherdataxml
                            .Root.Elements("Station")
                            .Where(x =>
                                x.Attribute("Id").Value.Equals(stationtomorrow.Element("Id").Value)
                            )
                            .FirstOrDefault();
                        mystationdatatomorrow.CityName = mystationdatacity
                            .Attribute("Name" + lang.ToUpper())
                            .Value;

                        if (lang == "en")
                        {
                            if (mystationdatatomorrow.CityName.Contains("/"))
                            {
                                mystationdatatomorrow.CityName =
                                    mystationdatatomorrow.CityName.Replace("/", " / ");
                            }
                        }
                        //mystationdatatomorrow.stationdatacityrid = mystationdatacity.Attribute("RID").Value;

                        mystationdatatomorrow.WeatherCode = stationtomorrow
                            .Element("symbol")
                            .Element("code")
                            .Value;
                        mystationdatatomorrow.WeatherDesc = stationtomorrow
                            .Element("symbol")
                            .Element("description")
                            .Value;
                        mystationdatatomorrow.WeatherImgUrl = stationtomorrow
                            .Element("symbol")
                            .Element("imageURL")
                            .Value;

                        if (source == "siag")
                        {
                            mystationdatatomorrow.MaxTemp = Convert.ToInt32(
                                stationtomorrow.Element("temperature").Element("max").Value
                            );
                            mystationdatatomorrow.MinTemp = Convert.ToInt32(
                                stationtomorrow.Element("temperature").Element("min").Value
                            );
                        }
                        else
                        {
                            mystationdatatomorrow.MaxTemp = Convert.ToInt32(
                                stationtomorrow.Element("max").Value
                            );
                            mystationdatatomorrow.MinTemp = Convert.ToInt32(
                                stationtomorrow.Element("min").Value
                            );
                        }

                        myweather.Stationdata.Add(mystationdatatomorrow);
                    }
                }
            }

            return myweather;
        }

        public static WeatherDistrictLinked ParsemyBezirksWeatherResponse(
            string lang,
            XDocument weatherresponse,
            string source
        )
        {
            WeatherDistrictLinked myweather = new WeatherDistrictLinked();

            myweather.Id = Convert.ToInt32(
                weatherresponse.Root.Element("district").Element("Id").Value
            );
            myweather.DistrictName = weatherresponse.Root.Element("district").Element("name").Value;
            myweather.LicenseInfo = Helper.LicenseHelper.GetLicenseforWeather(source);

            myweather.Date = Convert.ToDateTime(
                weatherresponse
                    .Root.Element("date")
                    .Value.Replace("00:00:00", weatherresponse.Root.Element("hour").Value + ":00")
            );

            var mybezirkforecasts = weatherresponse.Root.Elements("forecast");

            List<BezirksForecast> mybezirksforecastlist = new List<BezirksForecast>();

            //Tomorrow info
            foreach (var mybezirkforecast in mybezirkforecasts)
            {
                BezirksForecast bezirksforecast = new BezirksForecast();

                bezirksforecast.Date = Convert.ToDateTime(mybezirkforecast.Element("date").Value);

                bezirksforecast.WeatherCode = mybezirkforecast
                    .Element("symbol")
                    .Element("code")
                    .Value;
                bezirksforecast.WeatherDesc = mybezirkforecast
                    .Element("symbol")
                    .Element("description")
                    .Value;
                bezirksforecast.WeatherImgUrl = mybezirkforecast
                    .Element("symbol")
                    .Element("imageURL")
                    .Value;

                bezirksforecast.Freeze = Convert.ToInt16(mybezirkforecast.Element("freeze").Value);
                bezirksforecast.RainFrom = Convert.ToInt16(
                    mybezirkforecast.Element("rainFrom").Value
                );
                bezirksforecast.RainTo = Convert.ToInt16(mybezirkforecast.Element("rainTo").Value);

                if (source == "siag")
                {
                    bezirksforecast.MaxTemp = Convert.ToInt16(
                        mybezirkforecast.Element("temperature").Element("max").Value
                    );
                    bezirksforecast.MinTemp = Convert.ToInt16(
                        mybezirkforecast.Element("temperature").Element("min").Value
                    );

                    bezirksforecast.Part1 =
                        mybezirkforecast.Element("part1") != null
                            ? Convert.ToInt16(mybezirkforecast.Element("part1").Value)
                            : 0;
                    bezirksforecast.Part2 =
                        mybezirkforecast.Element("part2") != null
                            ? Convert.ToInt16(mybezirkforecast.Element("part2").Value)
                            : 0;
                    bezirksforecast.Part3 =
                        mybezirkforecast.Element("part3") != null
                            ? Convert.ToInt16(mybezirkforecast.Element("part3").Value)
                            : 0;
                    bezirksforecast.Part4 =
                        mybezirkforecast.Element("part4") != null
                            ? Convert.ToInt16(mybezirkforecast.Element("part4").Value)
                            : 0;

                    if (
                        mybezirkforecast.Element("part3") == null
                        && mybezirkforecast.Element("part4") == null
                    )
                    {
                        bezirksforecast.Part1 =
                            mybezirkforecast.Element("part1") != null
                                ? Convert.ToInt16(mybezirkforecast.Element("part1").Value)
                                : 0;
                        bezirksforecast.Part2 =
                            mybezirkforecast.Element("part1") != null
                                ? Convert.ToInt16(mybezirkforecast.Element("part1").Value)
                                : 0;
                        bezirksforecast.Part3 =
                            mybezirkforecast.Element("part2") != null
                                ? Convert.ToInt16(mybezirkforecast.Element("part2").Value)
                                : 0;
                        bezirksforecast.Part4 =
                            mybezirkforecast.Element("part2") != null
                                ? Convert.ToInt16(mybezirkforecast.Element("part2").Value)
                                : 0;
                    }

                    bezirksforecast.Thunderstorm = Convert.ToInt16(
                        mybezirkforecast.Element("thunderStorm").Value
                    );
                }
                else
                {
                    bezirksforecast.MaxTemp = Convert.ToInt16(
                        mybezirkforecast.Element("temperatureMax").Value
                    );
                    bezirksforecast.MinTemp = Convert.ToInt16(
                        mybezirkforecast.Element("temperatureMin").Value
                    );

                    bezirksforecast.Part1 =
                        mybezirkforecast.Element("rainTimespan1") != null
                            ? Convert.ToInt16(mybezirkforecast.Element("rainTimespan1").Value)
                            : 0;
                    bezirksforecast.Part2 =
                        mybezirkforecast.Element("rainTimespan2") != null
                            ? Convert.ToInt16(mybezirkforecast.Element("rainTimespan2").Value)
                            : 0;
                    bezirksforecast.Part3 =
                        mybezirkforecast.Element("rainTimespan3") != null
                            ? Convert.ToInt16(mybezirkforecast.Element("rainTimespan3").Value)
                            : 0;
                    bezirksforecast.Part4 =
                        mybezirkforecast.Element("rainTimespan4") != null
                            ? Convert.ToInt16(mybezirkforecast.Element("rainTimespan4").Value)
                            : 0;

                    if (
                        mybezirkforecast.Element("rainTimespan3") == null
                        && mybezirkforecast.Element("rainTimespan4") == null
                    )
                    {
                        bezirksforecast.Part1 =
                            mybezirkforecast.Element("rainTimespan1") != null
                                ? Convert.ToInt16(mybezirkforecast.Element("rainTimespan1").Value)
                                : 0;
                        bezirksforecast.Part2 =
                            mybezirkforecast.Element("rainTimespan1") != null
                                ? Convert.ToInt16(mybezirkforecast.Element("rainTimespan1").Value)
                                : 0;
                        bezirksforecast.Part3 =
                            mybezirkforecast.Element("rainTimespan2") != null
                                ? Convert.ToInt16(mybezirkforecast.Element("rainTimespan2").Value)
                                : 0;
                        bezirksforecast.Part4 =
                            mybezirkforecast.Element("rainTimespan2") != null
                                ? Convert.ToInt16(mybezirkforecast.Element("rainTimespan2").Value)
                                : 0;
                    }

                    bezirksforecast.Thunderstorm = Convert.ToInt16(
                        mybezirkforecast.Element("storms").Value
                    );
                }

                //TODO no


                mybezirksforecastlist.Add(bezirksforecast);
            }

            myweather.BezirksForecast = mybezirksforecastlist.ToList();

            return myweather;
        }

        //JSON Response Parser

        public static WeatherLinked ParsemyWeatherJsonResponse(
            string lang,
            XDocument weatherdataxml,
            string weatherresponsejson,
            string source
        )
        {
            var siagweather = JsonConvert.DeserializeObject<WeatherModel.SiagWeather>(
                weatherresponsejson
            );

            try
            {
                WeatherLinked myweather = new WeatherLinked();

                myweather.Id = Convert.ToInt32(siagweather.id);
                myweather.Date = Convert.ToDateTime(
                    siagweather.date.ToShortDateString() + " " + siagweather.hour
                );
                myweather.Evolution = siagweather.evolution;
                myweather.EvolutionTitle = siagweather.evolutionTitle;
                myweather.Language = lang;
                myweather.LicenseInfo = Helper.LicenseHelper.GetLicenseforWeather(source);

                //Forecast info
                foreach (var forecast in siagweather.dayForecasts)
                {
                    DataModel.Forecast myforecast = new DataModel.Forecast();

                    myforecast.Date = forecast.date;
                    myforecast.Reliability = forecast.reliability.ToString();
                    myforecast.WeatherCode = forecast.symbol.code;
                    myforecast.WeatherDesc = forecast.symbol.description;
                    myforecast.WeatherImgUrl = forecast.symbol.imageUrl;

                    myforecast.TempMaxmax = Convert.ToInt32(forecast.tempMax.max);
                    myforecast.TempMaxmin = Convert.ToInt32(forecast.tempMax.min);

                    myforecast.TempMinmax = Convert.ToInt32(forecast.tempMin.max);
                    myforecast.TempMinmin = Convert.ToInt32(forecast.tempMin.min);

                    myweather.Forecast.Add(myforecast);
                }

                // mountain Info

                if (siagweather.mountainToday != null)
                {
                    Mountain mymountaintoday = new Mountain();
                    mymountaintoday.Date = siagweather.mountainToday.date;
                    mymountaintoday.Title = siagweather.mountainToday.title;
                    mymountaintoday.Zerolimit = siagweather.mountainToday.zeroLimit.ToString();
                    mymountaintoday.Snowlimit = siagweather
                        .mountainToday.snowLimit.Select(x => x.ToString())
                        .ToList<string>();

                    mymountaintoday.WeatherDesc = siagweather.mountainToday.weather;

                    mymountaintoday.Conditions = siagweather.mountainToday.conditions;
                    mymountaintoday.MountainImgUrl = siagweather.mountainToday.imageUrl;
                    mymountaintoday.Moonrise = siagweather.mountainToday.moonRise;
                    mymountaintoday.Moonset = siagweather.mountainToday.moonSet;
                    mymountaintoday.Sunrise = siagweather.mountainToday.sunRise;
                    mymountaintoday.Sunset = siagweather.mountainToday.sunSet;

                    mymountaintoday.Reliability = siagweather.mountainToday.reliability.ToString();
                    mymountaintoday.Temp1000 = Convert.ToInt32(siagweather.mountainToday.temp1000);
                    mymountaintoday.Temp2000 = Convert.ToInt32(siagweather.mountainToday.temp2000);
                    mymountaintoday.Temp3000 = Convert.ToInt32(siagweather.mountainToday.temp3000);
                    mymountaintoday.Temp4000 = Convert.ToInt32(siagweather.mountainToday.temp4000);

                    mymountaintoday.NorthCode = siagweather.mountainToday.north.code;
                    mymountaintoday.NorthDesc = siagweather.mountainToday.north.description;
                    mymountaintoday.NorthImgUrl = siagweather.mountainToday.north.imageUrl;
                    mymountaintoday.SouthCode = siagweather.mountainToday.south.code;
                    mymountaintoday.SouthDesc = siagweather.mountainToday.south.description;
                    mymountaintoday.SouthImgUrl = siagweather.mountainToday.south.imageUrl;
                    mymountaintoday.WindCode = siagweather.mountainToday.wind.code;
                    mymountaintoday.WindDesc = siagweather.mountainToday.wind.description;
                    mymountaintoday.WindImgUrl = siagweather.mountainToday.wind.imageUrl;

                    myweather.Mountain.Add(mymountaintoday);
                }

                // mountain Info Tomorrow
                if (siagweather.mountainTomorrow != null)
                {
                    Mountain mymountaintomorrow = new Mountain();
                    mymountaintomorrow.Date = siagweather.mountainTomorrow.date;
                    mymountaintomorrow.Title = siagweather.mountainTomorrow.title;
                    mymountaintomorrow.Zerolimit =
                        siagweather.mountainTomorrow.zeroLimit.ToString();
                    mymountaintomorrow.Snowlimit = siagweather
                        .mountainTomorrow.snowLimit.Select(x => x.ToString())
                        .ToList<string>();

                    mymountaintomorrow.WeatherDesc = siagweather.mountainTomorrow.weather;

                    mymountaintomorrow.Conditions = siagweather.mountainTomorrow.conditions;
                    mymountaintomorrow.MountainImgUrl = siagweather.mountainTomorrow.imageUrl;
                    mymountaintomorrow.Moonrise = siagweather.mountainTomorrow.moonRise;
                    mymountaintomorrow.Moonset = siagweather.mountainTomorrow.moonSet;
                    mymountaintomorrow.Sunrise = siagweather.mountainTomorrow.sunRise;
                    mymountaintomorrow.Sunset = siagweather.mountainTomorrow.sunSet;
                    mymountaintomorrow.Reliability =
                        siagweather.mountainTomorrow.reliability.ToString();
                    mymountaintomorrow.Temp1000 = Convert.ToInt32(
                        siagweather.mountainTomorrow.temp1000
                    );
                    mymountaintomorrow.Temp2000 = Convert.ToInt32(
                        siagweather.mountainTomorrow.temp2000
                    );
                    mymountaintomorrow.Temp3000 = Convert.ToInt32(
                        siagweather.mountainTomorrow.temp3000
                    );
                    mymountaintomorrow.Temp4000 = Convert.ToInt32(
                        siagweather.mountainTomorrow.temp4000
                    );

                    mymountaintomorrow.NorthCode = siagweather.mountainTomorrow.north.code;
                    mymountaintomorrow.NorthDesc = siagweather.mountainTomorrow.north.description;
                    mymountaintomorrow.NorthImgUrl = siagweather.mountainTomorrow.north.imageUrl;
                    mymountaintomorrow.SouthCode = siagweather.mountainTomorrow.south.code;
                    mymountaintomorrow.SouthDesc = siagweather.mountainTomorrow.south.description;
                    mymountaintomorrow.SouthImgUrl = siagweather.mountainTomorrow.south.imageUrl;
                    mymountaintomorrow.WindCode = siagweather.mountainTomorrow.wind.code;
                    mymountaintomorrow.WindDesc = siagweather.mountainTomorrow.wind.description;
                    mymountaintomorrow.WindImgUrl = siagweather.mountainTomorrow.wind.imageUrl;

                    myweather.Mountain.Add(mymountaintomorrow);
                }

                //Today Info
                if (siagweather.today != null)
                {
                    Conditions myconditiontoday = new Conditions();

                    myconditiontoday.Date = Convert.ToDateTime(
                        siagweather.today.date.ToShortDateString() + " " + siagweather.today.hour
                    ); //TODO CHeck
                    myconditiontoday.WeatherCondition = siagweather.today.conditions;
                    myconditiontoday.WeatherImgUrl = siagweather.today.imageUrl;
                    myconditiontoday.WeatherDesc = siagweather.today.weather;
                    myconditiontoday.Title = siagweather.today.title;
                    myconditiontoday.Temperatures = siagweather.today.temperatures;

                    myconditiontoday.BulletinStatus = siagweather.today.bulletinStatus;
                    myconditiontoday.Reliability = siagweather.today.reliability.ToString();
                    myconditiontoday.TempMaxmax = Convert.ToInt32(siagweather.today.tMaxMax);
                    myconditiontoday.TempMaxmin = Convert.ToInt32(siagweather.today.tMaxMin);
                    myconditiontoday.TempMinmax = Convert.ToInt32(siagweather.today.tMinMax);
                    myconditiontoday.TempMinmin = Convert.ToInt32(siagweather.today.tMinMin);

                    myweather.Conditions.Add(myconditiontoday);
                }
                //Stationdata today
                if (siagweather.today != null)
                {
                    var stationstoday = siagweather.today.stationData;

                    if (stationstoday != null)
                    {
                        int i = 1;

                        foreach (var stationtoday in stationstoday)
                        {
                            DataModel.Stationdata mystationdatatoday = new DataModel.Stationdata();

                            mystationdatatoday.Date = Convert.ToDateTime(
                                siagweather.today.date.ToShortDateString()
                                    + " "
                                    + siagweather.today.hour
                            ); //TODO CHeck

                            mystationdatatoday.Id = i;
                            var mystationdatacity = weatherdataxml
                                .Root.Elements("Station")
                                .Where(x => x.Attribute("Id").Value.Equals(i.ToString()))
                                .FirstOrDefault();
                            mystationdatatoday.CityName = mystationdatacity
                                .Attribute("Name" + lang.ToUpper())
                                .Value;

                            if (lang == "en")
                            {
                                if (mystationdatatoday.CityName.Contains("/"))
                                {
                                    mystationdatatoday.CityName =
                                        mystationdatatoday.CityName.Replace("/", " / ");
                                }
                            }
                            //mystationdatatoday.stationdatacityrid = mystationdatacity.Attribute("RID").Value;

                            mystationdatatoday.WeatherCode = stationtoday.symbol.code;
                            mystationdatatoday.WeatherDesc = stationtoday.symbol.description;
                            mystationdatatoday.WeatherImgUrl = stationtoday.symbol.imageUrl;
                            mystationdatatoday.MaxTemp = Convert.ToInt32(stationtoday.max);
                            mystationdatatoday.MinTemp = Convert.ToInt32(stationtoday.min);

                            myweather.Stationdata.Add(mystationdatatoday);

                            i++;
                        }
                    }
                }

                //Tomorrow info
                if (siagweather.tomorrow != null)
                {
                    Conditions myconditiontomorrow = new Conditions();

                    myconditiontomorrow.Date = Convert.ToDateTime(
                        siagweather.tomorrow.date.ToShortDateString()
                            + " "
                            + siagweather.tomorrow.hour
                    ); //TODO CHeck
                    myconditiontomorrow.WeatherCondition = siagweather.tomorrow.conditions;
                    myconditiontomorrow.WeatherImgUrl = siagweather.tomorrow.imageUrl;
                    myconditiontomorrow.WeatherDesc = siagweather.tomorrow.weather;
                    myconditiontomorrow.Title = siagweather.tomorrow.title;
                    myconditiontomorrow.Temperatures = siagweather.tomorrow.temperatures;

                    myconditiontomorrow.BulletinStatus = siagweather.tomorrow.bulletinStatus;
                    myconditiontomorrow.Reliability = siagweather.tomorrow.reliability.ToString();
                    myconditiontomorrow.TempMaxmax = Convert.ToInt32(siagweather.tomorrow.tMaxMax);
                    myconditiontomorrow.TempMaxmin = Convert.ToInt32(siagweather.tomorrow.tMaxMin);
                    myconditiontomorrow.TempMinmax = Convert.ToInt32(siagweather.tomorrow.tMinMax);
                    myconditiontomorrow.TempMinmin = Convert.ToInt32(siagweather.tomorrow.tMinMin);

                    myweather.Conditions.Add(myconditiontomorrow);
                }

                //Stationdata today
                if (siagweather.tomorrow != null)
                {
                    var stationstomorrow = siagweather.tomorrow.stationData;

                    if (stationstomorrow != null)
                    {
                        int j = 1;

                        foreach (var stationtomorrow in stationstomorrow)
                        {
                            DataModel.Stationdata mystationdatatomorrow =
                                new DataModel.Stationdata();

                            mystationdatatomorrow.Date = Convert.ToDateTime(
                                siagweather.tomorrow.date.ToShortDateString()
                                    + " "
                                    + siagweather.tomorrow.hour
                            );

                            mystationdatatomorrow.Id = j;
                            var mystationdatacity = weatherdataxml
                                .Root.Elements("Station")
                                .Where(x => x.Attribute("Id").Value.Equals(j.ToString()))
                                .FirstOrDefault();
                            mystationdatatomorrow.CityName = mystationdatacity
                                .Attribute("Name" + lang.ToUpper())
                                .Value;
                            //mystationdatatomorrow.stationdatacityrid = mystationdatacity.Attribute("RID").Value;

                            if (lang == "en")
                            {
                                if (mystationdatatomorrow.CityName.Contains("/"))
                                {
                                    mystationdatatomorrow.CityName =
                                        mystationdatatomorrow.CityName.Replace("/", " / ");
                                }
                            }

                            mystationdatatomorrow.WeatherCode = stationtomorrow.symbol.code;
                            mystationdatatomorrow.WeatherDesc = stationtomorrow.symbol.description;
                            mystationdatatomorrow.WeatherImgUrl = stationtomorrow.symbol.imageUrl;

                            mystationdatatomorrow.MaxTemp = Convert.ToInt32(stationtomorrow.max);
                            mystationdatatomorrow.MinTemp = Convert.ToInt32(stationtomorrow.min);

                            myweather.Stationdata.Add(mystationdatatomorrow);

                            j++;
                        }
                    }
                }

                return myweather;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static WeatherLinked ParsemyStationWeatherJsonResponse(
            string lang,
            XDocument weatherdataxml,
            string weatherresponsejson,
            string source,
            string stationid
        )
        {
            var siagweather = JsonConvert.DeserializeObject<WeatherModel.SiagWeather>(
                weatherresponsejson
            );

            try
            {
                WeatherLinked myweather = new WeatherLinked();

                myweather.Id = Convert.ToInt32(siagweather.id);
                myweather.Date = Convert.ToDateTime(
                    siagweather.date.ToShortDateString() + " " + siagweather.hour
                );
                myweather.Language = lang;
                myweather.LicenseInfo = Helper.LicenseHelper.GetLicenseforWeather(source);

                var stationindex = Convert.ToInt32(stationid);

                //Stationdata today
                if (siagweather.today != null)
                {
                    var stationstoday = siagweather.today.stationData;

                    if (stationstoday != null)
                    {
                        var stationtoday = stationstoday[stationindex - 1];

                        DataModel.Stationdata mystationdatatoday = new DataModel.Stationdata();

                        mystationdatatoday.Date = Convert.ToDateTime(
                            siagweather.today.date.ToShortDateString()
                                + " "
                                + siagweather.today.hour
                        ); //TODO CHeck

                        mystationdatatoday.Id = stationindex;
                        var mystationdatacity = weatherdataxml
                            .Root.Elements("Station")
                            .Where(x => x.Attribute("Id").Value.Equals(stationindex))
                            .FirstOrDefault();
                        mystationdatatoday.CityName = mystationdatacity
                            .Attribute("Name" + lang.ToUpper())
                            .Value;

                        if (lang == "en")
                        {
                            if (mystationdatatoday.CityName.Contains("/"))
                            {
                                mystationdatatoday.CityName = mystationdatatoday.CityName.Replace(
                                    "/",
                                    " / "
                                );
                            }
                        }
                        //mystationdatatoday.stationdatacityrid = mystationdatacity.Attribute("RID").Value;

                        mystationdatatoday.WeatherCode = stationtoday.symbol.code;
                        mystationdatatoday.WeatherDesc = stationtoday.symbol.description;
                        mystationdatatoday.WeatherImgUrl = stationtoday.symbol.imageUrl;
                        mystationdatatoday.MaxTemp = Convert.ToInt32(stationtoday.max);
                        mystationdatatoday.MinTemp = Convert.ToInt32(stationtoday.min);

                        myweather.Stationdata.Add(mystationdatatoday);
                    }
                }
                //Stationdata today
                if (siagweather.tomorrow != null)
                {
                    var stationstomorrow = siagweather.tomorrow.stationData;

                    if (stationstomorrow != null)
                    {
                        var stationtomorrow = stationstomorrow[stationindex - 1];

                        DataModel.Stationdata mystationdatatomorrow = new DataModel.Stationdata();

                        mystationdatatomorrow.Date = Convert.ToDateTime(
                            siagweather.tomorrow.date.ToShortDateString()
                                + " "
                                + siagweather.tomorrow.hour
                        );

                        mystationdatatomorrow.Id = stationindex;
                        var mystationdatacity = weatherdataxml
                            .Root.Elements("Station")
                            .Where(x => x.Attribute("Id").Value.Equals(stationindex.ToString()))
                            .FirstOrDefault();
                        mystationdatatomorrow.CityName = mystationdatacity
                            .Attribute("Name" + lang.ToUpper())
                            .Value;
                        //mystationdatatomorrow.stationdatacityrid = mystationdatacity.Attribute("RID").Value;

                        if (lang == "en")
                        {
                            if (mystationdatatomorrow.CityName.Contains("/"))
                            {
                                mystationdatatomorrow.CityName =
                                    mystationdatatomorrow.CityName.Replace("/", " / ");
                            }
                        }

                        mystationdatatomorrow.WeatherCode = stationtomorrow.symbol.code;
                        mystationdatatomorrow.WeatherDesc = stationtomorrow.symbol.description;
                        mystationdatatomorrow.WeatherImgUrl = stationtomorrow.symbol.imageUrl;

                        mystationdatatomorrow.MaxTemp = Convert.ToInt32(stationtomorrow.max);
                        mystationdatatomorrow.MinTemp = Convert.ToInt32(stationtomorrow.min);

                        myweather.Stationdata.Add(mystationdatatomorrow);
                    }
                }

                return myweather;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static WeatherDistrictLinked ParsemyBezirksWeatherJsonResponse(
            string lang,
            string weatherresponsejson,
            string source
        )
        {
            var siagdistrictweather =
                JsonConvert.DeserializeObject<WeatherModel.SiagWeatherDistrict>(
                    weatherresponsejson
                );

            try
            {
                WeatherDistrictLinked myweather = new WeatherDistrictLinked();

                myweather.Id = siagdistrictweather.district.id;
                myweather.DistrictName = siagdistrictweather.district.name;
                myweather.LicenseInfo = Helper.LicenseHelper.GetLicenseforWeather(source);
                myweather.Date = Convert.ToDateTime(
                    siagdistrictweather.date.ToShortDateString() + " " + siagdistrictweather.hour
                );
                myweather.Language = lang;

                List<BezirksForecast> mybezirksforecastlist = new List<BezirksForecast>();

                //Tomorrow info
                foreach (var mybezirkforecast in siagdistrictweather.forecasts)
                {
                    BezirksForecast bezirksforecast = new BezirksForecast();

                    bezirksforecast.Date = mybezirkforecast.date;

                    bezirksforecast.WeatherCode = mybezirkforecast.symbol.code;
                    bezirksforecast.WeatherDesc = mybezirkforecast.symbol.description;
                    bezirksforecast.WeatherImgUrl = mybezirkforecast.symbol.imageUrl;

                    bezirksforecast.Freeze = mybezirkforecast.freeze;
                    bezirksforecast.RainFrom = Convert.ToInt32(mybezirkforecast.rainFrom);
                    bezirksforecast.RainTo = Convert.ToInt32(mybezirkforecast.rainTo);

                    bezirksforecast.MaxTemp = Convert.ToInt32(mybezirkforecast.temperatureMax);
                    bezirksforecast.MinTemp = Convert.ToInt32(mybezirkforecast.temperatureMin);

                    bezirksforecast.Part1 = Convert.ToInt32(mybezirkforecast.rainTimespan1);
                    bezirksforecast.Part2 = Convert.ToInt32(mybezirkforecast.rainTimespan2);
                    bezirksforecast.Part3 = Convert.ToInt32(mybezirkforecast.rainTimespan3);
                    bezirksforecast.Part4 = Convert.ToInt32(mybezirkforecast.rainTimespan4);

                    //TO CHECK WHAT THIS IS FOR
                    //HACK
                    if (
                        mybezirkforecast.rainTimespan3 == null
                        && mybezirkforecast.rainTimespan4 == null
                    )
                    {
                        bezirksforecast.Part1 =
                            mybezirkforecast.rainTimespan1 != null
                                ? Convert.ToInt32(mybezirkforecast.rainTimespan1)
                                : 0;
                        bezirksforecast.Part2 =
                            mybezirkforecast.rainTimespan1 != null
                                ? Convert.ToInt32(mybezirkforecast.rainTimespan1)
                                : 0;
                        bezirksforecast.Part3 =
                            mybezirkforecast.rainTimespan2 != null
                                ? Convert.ToInt32(mybezirkforecast.rainTimespan2)
                                : 0;
                        bezirksforecast.Part4 =
                            mybezirkforecast.rainTimespan2 != null
                                ? Convert.ToInt32(mybezirkforecast.rainTimespan2)
                                : 0;
                    }

                    bezirksforecast.Thunderstorm = Convert.ToInt32(mybezirkforecast.freeze);

                    bezirksforecast.Reliability = mybezirkforecast.reliability;
                    bezirksforecast.SymbolId = mybezirkforecast.symbolId;

                    mybezirksforecastlist.Add(bezirksforecast);
                }

                myweather.BezirksForecast = mybezirksforecastlist.ToList();

                return myweather;
            }
            catch (Exception)
            {
                return null;
            }
        }

        //public static WeatherDistrictLinked MergeBezirksWetterIntoOne(IEnumerable<WeatherDistrictLinked> weatherDistrictlist)
        //{
        //    var weatherDistrictmerged = weatherDistrictlist.FirstOrDefault();
        //    weatherDistrictmerged.District = new Dictionary<string, string>();

        //    //District name
        //    foreach (var weatherDistrict in weatherDistrictlist)
        //    {
        //        weatherDistrictmerged.District.TryAddOrUpdate(weatherDistrict.Language, weatherDistrict.DistrictName);

        //        for(int i = 0; i <= weatherDistrict.BezirksForecast.Count; i++)
        //        {
        //            if (weatherDistrictmerged.BezirksForecast.ElementAt(i).WeatherDescription == null)
        //                weatherDistrictmerged.BezirksForecast.ElementAt(i).WeatherDescription = new Dictionary<string, string>();

        //            weatherDistrictmerged.BezirksForecast.ElementAt(i).WeatherDescription.TryAddOrUpdate(weatherDistrict.Language, weatherDistrict.BezirksForecast.ElementAt(i).WeatherDesc);
        //        }
        //    }

        //    //Forecast

        //    return weatherDistrictmerged;
        //}

        public static WeatherForecastLinked ParseWeatherForecastFromJsonFile(
            string lang,
            SiagMunicipality siagforecast,
            SiagForecastInfo siagforecastinfo
        )
        {
            WeatherForecastLinked weatherforecast = new WeatherForecastLinked();

            weatherforecast.Date = siagforecastinfo.currentModelRun;
            weatherforecast.FirstImport = siagforecastinfo.fileCreationDate;
            weatherforecast.LastChange = siagforecastinfo.fileCreationDate;
            weatherforecast.Language = lang;
            weatherforecast.LicenseInfo = Helper.LicenseHelper.GetLicenseforWeather("province");

            //Municipalityinfo merge?
            //weatherforecast.GpsInfo
            //weatherforecast.LocationInfo

            weatherforecast.Id = "forecast_" + siagforecast.code;

            weatherforecast.Shortname = GetNameInLanguage(lang, siagforecast);
            weatherforecast.MunicipalityIstatCode = siagforecast.code;

            weatherforecast.MunicipalityName = new Dictionary<string, string>()
            {
                { "de", siagforecast.nameDe },
                { "it", siagforecast.nameIt },
                { "en", siagforecast.nameEn },
                { "ld", siagforecast.nameRm },
            };

            weatherforecast.ForeCastDaily = new List<Forecast24Hours>();

            //Forecast24Hours
            foreach (var measurement24 in GetAllPossible24hDates(siagforecast))
            {
                Forecast24Hours forecast24Hours = new Forecast24Hours();

                forecast24Hours.Date = measurement24.Key;

                foreach (var grouped in measurement24)
                {
                    if (grouped.forecastdata.ContainsKey("ssd24"))
                        forecast24Hours.SunshineDuration = grouped.forecastdata["ssd24"];
                    if (grouped.forecastdata.ContainsKey("tempMin24"))
                        forecast24Hours.MinTemp = grouped.forecastdata["tempMin24"];
                    if (grouped.forecastdata.ContainsKey("tempMax24"))
                        forecast24Hours.MaxTemp = grouped.forecastdata["tempMax24"];
                    if (grouped.forecastdata.ContainsKey("precProb24"))
                        forecast24Hours.PrecipitationProbability = grouped.forecastdata[
                            "precProb24"
                        ];
                    if (grouped.forecastdata.ContainsKey("precSum24"))
                        forecast24Hours.Precipitation = grouped.forecastdata["precSum24"];
                    if (grouped.forecastdatastr.ContainsKey("symbols24"))
                        forecast24Hours.WeatherCode = GetWeatherCodeTrimmed(
                            grouped.forecastdatastr["symbols24"]
                        );
                    if (grouped.forecastdatastr.ContainsKey("symbols24"))
                        forecast24Hours.WeatherImgUrl = String.Format(
                            "https://daten.buergernetz.bz.it/services/weather/graphics/icons/imgsource/wetter/icon_{0}.png",
                            GetWeatherCodeAsInteger(grouped.forecastdatastr["symbols24"])
                        );
                    if (grouped.forecastdatastr.ContainsKey("symbols24"))
                        forecast24Hours.WeatherDesc = GetMappedWeatherDesc(
                            lang,
                            grouped.forecastdatastr["symbols24"]
                        );
                    if (grouped.forecastdatastr.ContainsKey("symbols24"))
                        forecast24Hours.WeatherDescription = new Dictionary<string, string>()
                        {
                            {
                                "de",
                                GetMappedWeatherDesc("de", grouped.forecastdatastr["symbols24"])
                            },
                            {
                                "it",
                                GetMappedWeatherDesc("it", grouped.forecastdatastr["symbols24"])
                            },
                            {
                                "en",
                                GetMappedWeatherDesc("en", grouped.forecastdatastr["symbols24"])
                            },
                        };
                    ;
                }

                weatherforecast.ForeCastDaily.Add(forecast24Hours);
            }

            weatherforecast.Forecast3HoursInterval = new List<Forecast3Hours>();
            //Forecast3Hours
            foreach (var measurement3 in GetAllPossible3hDates(siagforecast))
            {
                Forecast3Hours forecast3Hours = new Forecast3Hours();

                forecast3Hours.Date = measurement3.Key;

                foreach (var grouped in measurement3)
                {
                    if (grouped.forecastdatafloat.ContainsKey("precSum3"))
                        forecast3Hours.Precipitation = grouped.forecastdatafloat["precSum3"];
                    if (grouped.forecastdata.ContainsKey("precProb3"))
                        forecast3Hours.PrecipitationProbability = grouped.forecastdata["precProb3"];
                    if (grouped.forecastdatafloat.ContainsKey("temp3"))
                        forecast3Hours.Temperature = grouped.forecastdatafloat["temp3"];
                    if (grouped.forecastdata.ContainsKey("windDir3"))
                        forecast3Hours.WindDirection = grouped.forecastdata["windDir3"];
                    if (grouped.forecastdata.ContainsKey("windSpd3"))
                        forecast3Hours.WindSpeed = grouped.forecastdata["windSpd3"];
                    if (grouped.forecastdatastr.ContainsKey("symbols3"))
                        forecast3Hours.WeatherCode = GetWeatherCodeTrimmed(
                            grouped.forecastdatastr["symbols3"]
                        );
                    if (grouped.forecastdatastr.ContainsKey("symbols3"))
                        forecast3Hours.WeatherImgUrl = String.Format(
                            "https://daten.buergernetz.bz.it/services/weather/graphics/icons/imgsource/wetter/icon_{0}.png",
                            GetWeatherCodeAsInteger(grouped.forecastdatastr["symbols3"])
                        );
                    if (grouped.forecastdatastr.ContainsKey("symbols3"))
                        forecast3Hours.WeatherDesc = GetMappedWeatherDesc(
                            lang,
                            grouped.forecastdatastr["symbols3"]
                        );
                    if (grouped.forecastdatastr.ContainsKey("symbols24"))
                        forecast3Hours.WeatherDescription = new Dictionary<string, string>()
                        {
                            {
                                "de",
                                GetMappedWeatherDesc("de", grouped.forecastdatastr["symbols24"])
                            },
                            {
                                "it",
                                GetMappedWeatherDesc("it", grouped.forecastdatastr["symbols24"])
                            },
                            {
                                "en",
                                GetMappedWeatherDesc("en", grouped.forecastdatastr["symbols24"])
                            },
                        };
                }

                weatherforecast.Forecast3HoursInterval.Add(forecast3Hours);
            }

            return weatherforecast;
        }

        #region Forecast Helpers
        public static string GetNameInLanguage(string lang, SiagMunicipality siagforecast)
        {
            if (lang == "de")
                return siagforecast.nameDe;
            else if (lang == "it")
                return siagforecast.nameIt;
            else if (lang == "en")
                return siagforecast.nameEn;
            else if (lang == "ld")
                return siagforecast.nameRm;
            else
                return siagforecast.nameEn;
        }

        public static string GetWeatherCodeTrimmed(string weathercode)
        {
            if (weathercode.Contains("_n"))
                return weathercode.Replace("_n", "");
            if (weathercode.Contains("_d"))
                return weathercode.Replace("_d", "");
            else
                return weathercode;
        }

        public static string GetWeatherCodeAsInteger(string weathercode)
        {
            var weathercodetoconvert = GetWeatherCodeTrimmed(weathercode);

            if (!String.IsNullOrEmpty(weathercodetoconvert) && weathercodetoconvert.Length == 1)
            {
                var charr = weathercodetoconvert.ToCharArray();

                int index = (int)(charr.FirstOrDefault()) % 32;
                //int index = char.ToUpper(c) - 64;

                return index.ToString();
            }

            return "";
        }

        public static IEnumerable<
            IGrouping<DateTime, SiagForecastMeasurement>
        > GetAllPossible24hDates(SiagMunicipality siagforecast)
        {
            var mylist = new List<SiagForecastMeasurement>();

            foreach (var data in siagforecast.tempMin24?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "tempMin24", data.value));
            }
            foreach (var data in siagforecast.tempMax24?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "tempMax24", data.value));
            }
            foreach (var data in siagforecast.ssd24?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "ssd24", data.value));
            }
            foreach (var data in siagforecast.precProb24?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "precProb24", data.value));
            }
            foreach (var data in siagforecast.precSum24?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "precSum24", data.value));
            }
            foreach (var data in siagforecast.symbols24?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(
                    new SiagForecastMeasurement(data.date, "symbols24", null, null, data.value)
                );
            }

            return mylist.OrderBy(x => x.forecastdate).GroupBy(x => x.forecastdate).ToList();
        }

        public static IEnumerable<
            IGrouping<DateTime, SiagForecastMeasurement>
        > GetAllPossible3hDates(SiagMunicipality siagforecast)
        {
            var mylist = new List<SiagForecastMeasurement>();

            foreach (var data in siagforecast.temp3?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "temp3", null, data.value));
            }
            foreach (var data in siagforecast.precProb3?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "precProb3", data.value));
            }
            foreach (var data in siagforecast.precSum3?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "precSum3", null, data.value));
            }
            foreach (var data in siagforecast.windSpd3?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "windSpd3", data.value));
            }
            foreach (var data in siagforecast.windDir3?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(new SiagForecastMeasurement(data.date, "windDir3", data.value));
            }
            foreach (var data in siagforecast.symbols3?.data ?? Enumerable.Empty<dynamic>())
            {
                mylist.Add(
                    new SiagForecastMeasurement(data.date, "symbols3", null, null, data.value)
                );
            }

            return mylist.OrderBy(x => x.forecastdate).GroupBy(x => x.forecastdate).ToList();
        }

        public static string GetMappedWeatherDesc(string lang, string code)
        {
            return (GetWeatherCodeTrimmed(code), lang) switch
            {
                ("a", "de") => "Wolkenlos",
                ("a", "it") => "Sereno",
                ("a", "en") => "sunny",

                ("b", "de") => "Heiter",
                ("b", "it") => "Poco Nuvoloso",
                ("b", "en") => "partly cloudy",

                ("c", "de") => "Wolkig",
                ("c", "it") => "Nuvoloso",
                ("c", "en") => "cloudy",

                ("d", "de") => "Stark bewölkt",
                ("d", "it") => "Molto nuvoloso",
                ("d", "en") => "very cloudy",

                ("e", "de") => "Bedeckt",
                ("e", "it") => "Coperto",
                ("e", "en") => "overcast",

                ("f", "de") => "Wolkig, mäßiger Regen",
                ("f", "it") => "Nuvoloso, piogge moderate",
                ("f", "en") => "cloudy with moderate rain",

                ("g", "de") => "Wolkig, starker Regen",
                ("g", "it") => "Nuvoloso, piogge intense",
                ("g", "en") => "cloudy with intense rain",

                ("h", "de") => "Bedeckt, mäßiger Regen",
                ("h", "it") => "Coperto, piogge moderate",
                ("h", "en") => "overcast with moderate rain",

                ("i", "de") => "Bedeckt, starker Regen",
                ("i", "it") => "Coperto, piogge intense",
                ("i", "en") => "overcast with intense rain",

                ("j", "de") => "Bedeckt, leichter Regen",
                ("j", "it") => "Coperto, piogge deboli",
                ("j", "en") => "overcast with light rain",

                ("k", "de") => "Durchscheinende Bewölkung",
                ("k", "it") => "Nuvolositá translucida",
                ("k", "en") => "translucent cloudy",

                ("l", "de") => "Wolkig, leichter Schneefall",
                ("l", "it") => "Nuvoloso, nevicate deboli",
                ("l", "en") => "cloudy with light snow",

                ("m", "de") => "Wolkig, starker Schneefall",
                ("m", "it") => "Nuvoloso, nevicate intense",
                ("m", "en") => "cloudy with heavy snow",

                ("n", "de") => "Bedeckt, leichter Schneefall",
                ("n", "it") => "Coperto, nevicate deboli",
                ("n", "en") => "overcast with light snow",

                ("o", "de") => "Bedeckt, mäßiger Schneefall",
                ("o", "it") => "Coperto, nevicate moderate",
                ("o", "en") => "overcast with moderate snow",

                ("p", "de") => "Bedeckt, starker Schneefall",
                ("p", "it") => "Coperto, nevicate intense",
                ("p", "en") => "overcast with intense snow",

                ("q", "de") => "Wolkig, Schneeregen",
                ("q", "it") => "Nuvoloso, piogga e neve",
                ("q", "en") => "cloudy with rain and snow",

                ("r", "de") => "Bedeckt, Schneeregen",
                ("r", "it") => "Coperto, pioggia e neve",
                ("r", "en") => "overcast with rain and snow",

                ("s", "de") => "Hochnebel",
                ("s", "it") => "Nuvolositá bassa",
                ("s", "en") => "low cloudiness",

                ("t", "de") => "Nebel",
                ("t", "it") => "Nebbia",
                ("t", "en") => "Fog",

                ("u", "de") => "Wolkig, Gewitter mit mäßigen Schauern",
                ("u", "it") => "Nuvoloso, temporali con moderati rovesci",
                ("u", "en") => "cloudy, thunderstorms with moderate showers",

                ("v", "de") => "Bedeckt, Gewitter mit starken Schauern",
                ("v", "it") => "Coperto, temporali con rovesci intensi",
                ("v", "en") => "cloudy, thunderstorms with intense showers",

                ("w", "de") => "Wolkig, Gewitter mit mäßigen Schneeregenschauern",
                ("w", "it") => "Nuvoloso, temporali con moderati rovesci nevosi e piovosi",
                ("w", "en") => "cloudy, thunderstorms with moderate snowy and rainy showers",

                ("x", "de") => "Bedeckt, Gewitter mit starken Schneeregenschauern",
                ("x", "it") => "Coperto, temporali con intensi rovesci nevosi e piovosi",
                ("x", "en") => "cloudy, thunderstorms with intense snowy and rainy showers",

                ("y", "de") => "Wolkig, Gewitter mit mäßigen Schneeschauern",
                ("y", "it") => "Nuvoloso, temporali con moderati rovesci nevosi",
                ("y", "en") => "cloudy, thunderstorms with moderate snowy showers",

                _ => "",
            };
        }

        #endregion
    }

    public class SiagForecastMeasurement
    {
        public SiagForecastMeasurement(
            DateTime _forecastdate,
            string _type,
            int? _forecastvalue,
            float? _forecastvaluefloat = null,
            string? _forecastvaluestr = null
        )
        {
            forecastdate = _forecastdate;
            if (_forecastvalue != null)
                forecastdata = new Dictionary<string, int> { { _type, _forecastvalue.Value } };
            else
                forecastdata = new Dictionary<string, int>();

            if (_forecastvaluestr != null)
                forecastdatastr = new Dictionary<string, string> { { _type, _forecastvaluestr } };
            else
                forecastdatastr = new Dictionary<string, string>();

            if (_forecastvaluefloat != null)
                forecastdatafloat = new Dictionary<string, float>
                {
                    { _type, _forecastvaluefloat.Value },
                };
            else
                forecastdatafloat = new Dictionary<string, float>();
        }

        public DateTime forecastdate { get; set; }

        public Dictionary<string, int> forecastdata { get; set; }

        public Dictionary<string, float> forecastdatafloat { get; set; }
        public Dictionary<string, string> forecastdatastr { get; set; }
    }
}
