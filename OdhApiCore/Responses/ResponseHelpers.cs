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

        /// <summary>
        /// Pages a list in memory, pagesize int.MaxValue returns all elements on one page
        /// </summary>
        public static (IEnumerable<T> items, uint totalpages) PageList<T>(
            IReadOnlyCollection<T> list,
            uint pagenumber,
            int pagesize
        )
        {
            uint page = System.Math.Max(pagenumber, 1);
            int size = System.Math.Max(pagesize, 1);
            uint totalpages = (uint)System.Math.Ceiling(list.Count / (double)size);
            long skip = (long)(page - 1) * size;

            var items = skip >= list.Count ? Enumerable.Empty<T>() : list.Skip((int)skip).Take(size);

            return (items, totalpages);
        }

        public static SearchResult<T> GetSearchResult<T>(
            uint pagenumber,
            uint totalpages,
            uint totalcount,
            string? searchterm,
            Dictionary<string, uint> detailedresults,
            IEnumerable<T> data,
            IUrlHelper? url
        )
        {
            var (previouspage, nextpage) = GetPreviousAndNextPage(
                pagenumber,
                totalpages,
                url,
                null
            );
            return new SearchResult<T>
            {
                TotalResults = totalcount,
                TotalPages = totalpages,
                CurrentPage = pagenumber,
                PreviousPage = previouspage,
                NextPage = nextpage,
                SearchTerm = searchterm,
                DetailedResults = detailedresults,
                Items = data,
            };
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
