// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Amazon.Runtime.Internal.Transform;
using Amazon.S3.Model;
using DataModel;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.AccommodationRoomsExtension
{
    public static class FillAccoRoomInfosObject
    {
      
        /// <summary>
        /// Extension Method to update the AccoRoomInfo
        /// </summary>
        /// <param name="queryFactory"></param>
        /// <returns></returns>
        public static async Task UpdateAccoRoomInfosExtension(this AccommodationV2 data, QueryFactory queryFactory, List<string> sourcestosync, Dictionary<string, List<string>>? roomdict = null)
        {
            //If roomdict is null, query the DB for the actual Rooms for each source
            if (roomdict == null)
            {
                roomdict = new Dictionary<string, List<string>>();

                if (sourcestosync.Contains("lts"))
                {
                    var querylts = queryFactory
                        .Query("accommodationrooms")
                        .Select("id")
                        .Where("gen_a0rid", data.Id)
                        .Where("gen_active", true)
                        .SourceFilter_GeneratedColumn(new List<string>() { "lts" });

                    var accommodationroomslts = await querylts.GetAsync<string>();

                    roomdict.Add("lts", accommodationroomslts.ToList());
                }

                if (sourcestosync.Contains("hgv"))
                {
                    var queryhgv = queryFactory
                    .Query("accommodationrooms")
                    .Select("id")
                    .Where("gen_a0rid", data.Id)
                    .Where("gen_active", true)
                    .SourceFilter_GeneratedColumn(new List<string>() { "hgv" });

                    var accommodationroomshgv = await queryhgv.GetAsync<string>();

                    roomdict.Add("hgv", accommodationroomshgv.ToList());
                }
            }

            //Check if all Ids of the source are listed and remove the ids that are only on AccoRooms
            if (sourcestosync.Contains("lts"))
            {
                foreach (var ltsroom in roomdict["lts"])
                {
                    if (data.AccoRoomInfo == null)
                        data.AccoRoomInfo = new List<AccoRoomInfoLinked>();

                    if (data.AccoRoomInfo.Where(x => x.Id == ltsroom).Count() == 0)
                        data.AccoRoomInfo.Add(new AccoRoomInfoLinked() { Id = ltsroom, Source = "lts" });
                }
            }
            if (sourcestosync.Contains("hgv"))
            {
                foreach (var hgvroom in roomdict["hgv"])
                {
                    if (data.AccoRoomInfo == null)
                        data.AccoRoomInfo = new List<AccoRoomInfoLinked>();

                    if (data.AccoRoomInfo.Where(x => x.Id == hgvroom).Count() == 0)
                        data.AccoRoomInfo.Add(new AccoRoomInfoLinked() { Id = hgvroom, Source = "hgv" });
                }
            }

            //To check
            var roomids = roomdict.SelectMany(x => x.Value).ToList();

            var roomidstoremove = data.AccoRoomInfo.Select(x => x.Id).Except(roomids);

            foreach (var roomidtoremove in roomidstoremove)
            {
                var acccoroominfolinkedtoremoe = data.AccoRoomInfo.Where(x => x.Id == roomidtoremove).FirstOrDefault();
                if (acccoroominfolinkedtoremoe != null)
                    data.AccoRoomInfo.Remove(acccoroominfolinkedtoremoe);
            }            
        }
    }
}
