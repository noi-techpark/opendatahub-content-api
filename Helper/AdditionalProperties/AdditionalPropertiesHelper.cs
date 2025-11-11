// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;
using Helper.Location;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlKata.Execution;

namespace Helper.AdditionalProperties
{
    public static class AdditionalPropertiesHelper
    {
        /// <summary>
        /// Extension Method to check Additionalproperties Model TODO MAKE DYNAMIC
        /// </summary>
        /// <param name="oldlocationinfo"></param>
        /// <param name="queryFactory"></param>
        /// <returns></returns>
        public static async Task<IDictionary<string, string>> CheckAdditionalProperties<T>(
            this T data
        )
            where T : IHasAdditionalProperties
        {
            Dictionary<string, string> errorlist = new Dictionary<string, string>();

            bool success = false;

            foreach (var kvp in data.AdditionalProperties)
            {
                switch (kvp.Key)
                {
                    case "EchargingDataProperties":

                        var resultecharging = CastAs<EchargingDataProperties>(kvp.Value);
                        success = resultecharging.Item1;
                        if (!success)
                            errorlist.TryAddOrUpdate("error", (string)resultecharging.Item2);
                        else
                        {
                            //Assign the Casted model
                            if (resultecharging.Item3 != null)
                            {
                                data.AdditionalProperties.TryAddOrUpdate(
                                    "EchargingDataProperties",
                                    (EchargingDataProperties)resultecharging.Item3
                                );
                            }
                        }

                        break;
                    case "ActivityLtsDataProperties":

                        var resultactivitylts = CastAs<ActivityLtsDataProperties>(kvp.Value);
                        success = resultactivitylts.Item1;
                        if (!success)
                            errorlist.TryAddOrUpdate("error", (string)resultactivitylts.Item2);
                        else
                        {
                            //Assign the Casted model
                            if (resultactivitylts.Item3 != null)
                            {
                                data.AdditionalProperties.TryAddOrUpdate(
                                    "ActivityLtsDataProperties",
                                    (ActivityLtsDataProperties)resultactivitylts.Item3
                                );
                            }
                        }

                        break;
                    case "PoiLtsDataProperties":

                        var resultpoilts = CastAs<PoiLtsDataProperties>(kvp.Value);
                        success = resultpoilts.Item1;
                        if (!success)
                            errorlist.TryAddOrUpdate("error", (string)resultpoilts.Item2);
                        else
                        {
                            //Assign the Casted model
                            if (resultpoilts.Item3 != null)
                            {
                                data.AdditionalProperties.TryAddOrUpdate(
                                    "PoiLtsDataProperties",
                                    (PoiLtsDataProperties)resultpoilts.Item3
                                );
                            }
                        }

                        break;
                    case "GastronomyLtsDataProperties":

                        var resultgastronomylts = CastAs<GastronomyLtsDataProperties>(kvp.Value);
                        success = resultgastronomylts.Item1;
                        if (!success)
                            errorlist.TryAddOrUpdate("error", (string)resultgastronomylts.Item2);
                        else
                        {
                            //Assign the Casted model
                            if (resultgastronomylts.Item3 != null)
                            {
                                data.AdditionalProperties.TryAddOrUpdate(
                                    "GastronomyLtsDataProperties",
                                    (GastronomyLtsDataProperties)resultgastronomylts.Item3
                                );
                            }
                        }

                        break;
                    case "PoiAgeDataProperties":

                        var resultpoiage = CastAs<PoiAgeDataProperties>(kvp.Value);
                        success = resultpoiage.Item1;
                        if (!success)
                            errorlist.TryAddOrUpdate("error", (string)resultpoiage.Item2);
                        else
                        {
                            //Assign the Casted model
                            if (resultpoiage.Item3 != null)
                            {
                                data.AdditionalProperties.TryAddOrUpdate(
                                    "PoiAgeDataProperties",
                                    (PoiAgeDataProperties)resultpoiage.Item3
                                );
                            }
                        }

                        break;
                    case "SuedtirolWeinCompanyDataProperties":

                        var resultsuedtirolweincompany = CastAs<SuedtirolWeinCompanyDataProperties>(kvp.Value);
                        success = resultsuedtirolweincompany.Item1;
                        if (!success)
                            errorlist.TryAddOrUpdate("error", (string)resultsuedtirolweincompany.Item2);
                        else
                        {
                            //Assign the Casted model
                            if (resultsuedtirolweincompany.Item3 != null)
                            {
                                data.AdditionalProperties.TryAddOrUpdate(
                                    "SuedtirolWeinCompanyDataProperties",
                                    (SuedtirolWeinCompanyDataProperties)resultsuedtirolweincompany.Item3
                                );
                            }
                        }

                        break;
                    case "SiagMuseumDataProperties":

                        var resultsiagmuseumdata = CastAs<SiagMuseumDataProperties>(kvp.Value);
                        success = resultsiagmuseumdata.Item1;
                        if (!success)
                            errorlist.TryAddOrUpdate("error", (string)resultsiagmuseumdata.Item2);
                        else
                        {
                            //Assign the Casted model
                            if (resultsiagmuseumdata.Item3 != null)
                            {
                                data.AdditionalProperties.TryAddOrUpdate(
                                    "SiagMuseumDataProperties",
                                    (SiagMuseumDataProperties)resultsiagmuseumdata.Item3
                                );
                            }
                        }

                        break;

                    default:
                        errorlist.Add("unknown error", "The Type " + kvp.Key + " is not known");
                        break;
                }
            }

            return errorlist;
        }

        public static (bool, string, T) CastAs<T>(dynamic data)
        {
            try
            {
                T info = ((JObject)data).ToObject<T>();

                return (true, "", info);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, default(T));
            }
        }
    }
}
