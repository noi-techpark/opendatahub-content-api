// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;

namespace MSS
{
    public class GetMssRoomlist
    {
        public static async Task<XElement> GetMssRoomlistAsync(
            HttpClient httpClient,
            string lang,
            string hotelid,
            string hotelidofchannel,
            XElement roomdetails,
            XDocument roomamenities,
            string source,
            string version,
            string serviceurl,
            string mssuser,
            string msspswd
        )
        {
            try
            {
                XDocument myrequest = MssRequest.BuildRoomlistPostData(
                    roomdetails,
                    hotelid,
                    hotelidofchannel,
                    lang,
                    source,
                    version,
                    mssuser,
                    msspswd
                );
                var myresponses = MssRequest.RequestRoomAsync(serviceurl, httpClient, myrequest);

                await Task.WhenAll(myresponses);

                Task<string> roomresponsecontent = myresponses.Result.Content.ReadAsStringAsync();

                await Task.WhenAll(roomresponsecontent);

                XElement fullresponse = XElement.Parse(roomresponsecontent.Result);

                return fullresponse;                
            }
            catch (Exception)
            {
                return null;
            }
        }       
    }
}
