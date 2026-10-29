// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IResponse<T>
    {
        public IEnumerable<T> Items { get; }
    }

    public class JsonResult<T> : IResponse<T>
    {
        public uint TotalResults { get; set; }
        public uint TotalPages { get; set; }
        public uint CurrentPage { get; set; }
        public string? PreviousPage { get; set; }
        public string? NextPage { get; set; }
        public string? Seed { get; set; }
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    }

    public class JsonResultWithOnlineResults<T> : JsonResult<T>
    {
        public int OnlineResults { get; set; }
    }

    public class JsonResultWithOnlineResultsAndResultId<T> : JsonResultWithOnlineResults<T>
    {
        public string? ResultId { get; set; }
    }

    public class JsonResultWithBookingInfo<T> : JsonResultWithOnlineResultsAndResultId<T>
    {
        public int AvailableOnline { get; set; }
        public int AvailableOnRequest { get; set; }
        public int AccommodationsRequested { get; set; }
    }

    //Standard paged result with additional search information
    public class SearchResult<T> : JsonResult<T>
    {
        public string? SearchTerm { get; set; }
        public Dictionary<string, uint> DetailedResults { get; set; } =
            new Dictionary<string, uint>();
    }

    //Item of the Search over all entities, same shape for every type
    public class FindResult
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Self { get; set; }

        //Additional fields requested with the fields parameter
        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, Newtonsoft.Json.Linq.JToken>? AdditionalFields { get; set; }
    }

    #region Resultset

    //Generic Result für Paging
    public class Result<T>
    {
        public int TotalResults { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public Nullable<int> OnlineResults { get; set; }
        public string? ResultId { get; set; }
        public string? Seed { get; set; }

        public ICollection<T>? Items { get; set; }
    }

    public class ResultAsync<T>
    {
        public int TotalResults { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public Nullable<int> OnlineResults { get; set; }

        public string? Seed { get; set; }

        public IList<T>? Items { get; set; }
    }

    #endregion
}
