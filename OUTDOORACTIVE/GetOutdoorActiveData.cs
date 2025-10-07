// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Helper.GetData;

namespace OUTDOORACTIVE
{
    public class GetOutDooractiveData
    {
        public static async Task<XDocument> GetOutdooractiveDetail(string oaid, string lang, string oakey, string serviceurl)
        {
            try
            {
                string requesturl = serviceurl + oaid + "?key=" + oakey + "&lang=" + lang;

                GetData getdata = new GetData(
                    requesturl,
                    null,
                    null,
                    null,
                    GetDataAuthenticationOptions.None
                );

                return await getdata.GetDataAsXmlAsync();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task<XDocument> GetOutdooractiveData(string ltstype, string serviceurl)
        {
            try
            {
                string requesturl = serviceurl + ltstype;

                GetData getdata = new GetData(
                    requesturl,
                    null,
                    null,
                    null,
                    GetDataAuthenticationOptions.None
                );

                return await getdata.GetDataAsXmlAsync();
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
