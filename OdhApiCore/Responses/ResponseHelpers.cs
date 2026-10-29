// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using DataModel;
using Microsoft.AspNetCore.Mvc;

namespace OdhApiCore.Responses
{
    public static class ResponseHelpers
    {
        private static (string? previouspage, string? nextpage) GetPreviousAndNextPage(
            uint pagenumber,
            uint totalpages,
            IUrlHelper? url,
            string? seed
        )
        {
            string? previouspage = null;
            string? nextpage = null;
            if (url != null)
            {
                var query = url.ActionContext.HttpContext.Request.Query;
                var queryDict = query.ToDictionary(x => x.Key, x => x.Value.ToString());
                if (seed != null)
                {
                    queryDict["seed"] = seed;
                }
                if (pagenumber > 1 && pagenumber <= totalpages)
                {
                    queryDict["pagenumber"] = (pagenumber - 1).ToString();
                    previouspage = url.Link(null, queryDict);
                }
                if (pagenumber < totalpages)
                {
                    queryDict["pagenumber"] = (pagenumber + 1).ToString();
                    nextpage = url.Link(null, queryDict);
                }
            }
            return (previouspage, nextpage);
        }

        public static JsonResult<T> GetResult<T>(
            uint pagenumber,
            uint totalpages,
            uint totalcount,
            string? seed,
            IEnumerable<T> data,
            IUrlHelper? url
        )
        {
            var (previouspage, nextpage) = GetPreviousAndNextPage(
                pagenumber,
                totalpages,
                url,
                seed
            );
            return new JsonResult<T>
            {
                TotalResults = totalcount,
                TotalPages = totalpages,
                CurrentPage = pagenumber,
                PreviousPage = previouspage,
                NextPage = nextpage,
                Seed = seed,
                Items = data,
            };
        }

        public static JsonResultWithOnlineResults<T> GetResult<T>(
            uint pagenumber,
            uint totalpages,
            uint totalcount,
            int onlineresults,
            string? seed,
            IEnumerable<T> data,
            IUrlHelper url
        )
            where T : notnull
        {
            var (previouspage, nextpage) = GetPreviousAndNextPage(
                pagenumber,
                totalpages,
                url,
                seed
            );
            return new JsonResultWithOnlineResults<T>
            {
                TotalResults = totalcount,
                TotalPages = totalpages,
                CurrentPage = pagenumber,
                PreviousPage = previouspage,
                NextPage = nextpage,
                OnlineResults = onlineresults,
                Seed = seed,
                Items = data,
            };
        }

        public static JsonResultWithOnlineResultsAndResultId<T> GetResult<T>(
            uint pagenumber,
            uint totalpages,
            uint totalcount,
            int onlineresults,
            string resultid,
            string? seed,
            IEnumerable<T> data,
            IUrlHelper url
        )
            where T : notnull
        {
            var (previouspage, nextpage) = GetPreviousAndNextPage(
                pagenumber,
                totalpages,
                url,
                seed
            );
            return new JsonResultWithOnlineResultsAndResultId<T>
            {
                TotalResults = totalcount,
                TotalPages = totalpages,
                CurrentPage = pagenumber,
                PreviousPage = previouspage,
                NextPage = nextpage,
                OnlineResults = onlineresults,
                ResultId = resultid,
                Seed = seed,
                Items = data,
            };
        }

        public static JsonResultWithBookingInfo<T> GetResult<T>(
            uint pagenumber,
            uint totalpages,
            uint totalcount,
            int accosrequested,
            int availableonline,
            int availableonrequest,
            string resultid,
            string? seed,
            IEnumerable<T> data,
            IUrlHelper url
        )
        {
            var (previouspage, nextpage) = GetPreviousAndNextPage(
                pagenumber,
                totalpages,
                url,
                seed
            );
            return new JsonResultWithBookingInfo<T>
            {
                TotalResults = totalcount,
                TotalPages = totalpages,
                CurrentPage = pagenumber,
                PreviousPage = previouspage,
                NextPage = nextpage,
                OnlineResults = availableonline,
                AccommodationsRequested = accosrequested,
                AvailableOnline = availableonline,
                AvailableOnRequest = availableonrequest,
                ResultId = resultid,
                Seed = seed,
                Items = data,
            };
        }
    }
}
