<!--
SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>

SPDX-License-Identifier: CC0-1.0
-->

# Future Optimizations

This folder collects analyses of possible performance optimizations. None of them are implemented yet.

- [JSON Response Streaming](#json-response-streaming)

---

## JSON Response Streaming

**Status:** analysed, not implemented (2026-09-29)
**Scope:** `OdhApiCore` list endpoints

**Question:** can the API stream JSON responses instead of building the whole response in memory first?

**Short answer:** yes. Two parts are needed:

- The output side can stop buffering through a config change, but that change has a trade-off.
- Real end-to-end streaming needs a small custom writer for list endpoints.

Moving the whole API to System.Text.Json is not recommended.

### How a list request works today

Example: `ODHActivityPoiController.GetFiltered`.

1. `query.PaginateAsync<JsonRaw>()` runs a count query and a data query. Dapper buffers the whole page into a `List<JsonRaw>`, with each row held as a JSON string.
2. `.Select(raw => raw.TransformRawData(...))` runs lazily. Each item is parsed with `JToken.Parse`, filtered by language, fields and self link, and turned back into a string (`Helper/JsonTransformer/JsonTransformer.cs`).
3. `ResponseHelpers.GetResult` wraps the items in a `JsonResult<JsonRaw>`. `Ok(...)` then hands it to the Newtonsoft output formatter (`AddNewtonsoftJson` in `OdhApiCore/Startup.cs`).
4. **The Newtonsoft output formatter buffers the entire serialized response before sending it.** The buffer is in memory, and spills to a temp file above 32 KB. Newtonsoft only writes synchronously, so ASP.NET Core buffers it by default.
5. The response is gzipped by `UseResponseCompression`.

**Result:** nothing is streamed. The client receives nothing until the whole response is built.

**Where this hurts most:**

- `pagesize=-1` or `pagesize=0`, which `OdhApiCore/Binders/PageSize.cs` maps to `int.MaxValue`.
- The unpaged `GetAsync<JsonRaw>` branches, for example the type lists.

In both cases the whole result is held in memory three times: as raw strings, as transformed strings, and as the buffered output.

No middleware replaces `Response.Body` or caches responses, so nothing else in the pipeline blocks streaming.

### Option A: stop buffering the output only (small change)

Set `options.SuppressOutputFormatterBuffering = true` in `services.AddMvc(...)` (`OdhApiCore/Startup.cs`).

- **Gain:** the first byte goes out earlier, and there is no second full copy or temp file.
- **Catch 1:** Newtonsoft then writes to Kestrel synchronously. That requires `AllowSynchronousIO = true`, which blocks thread-pool threads under load. Microsoft advises against it.
- **Catch 2:** rows are still loaded from the DB in full.
- **Verdict:** a quick win, but only enable it after load-testing.

### Option B: real streaming for list endpoints (recommended, opt-in)

Write the response directly instead of going through the output formatter:

1. Run the count query first. The envelope needs `TotalResults`, `TotalPages`, and the `NextPage`/`PreviousPage` links.
2. Compile the SqlKata query and read the rows with Dapper `QueryUnbufferedAsync<JsonRaw>`. It is available in Dapper 2.1.35 and returns an `IAsyncEnumerable`.
3. Write `{"TotalResults":…,"TotalPages":…,…,"Items":[` to the response.
4. Write each transformed item as a raw value. Items are already JSON strings, so no serializer is needed.
5. Flush every N items, then close with `]}`.
6. Wrap this in a reusable helper, for example a `StreamJsonResult` `IActionResult`, so each controller only changes a few lines.

**Call sites in OdhApiCore:** there are 36 `PaginateAsync` and 18 `GetAsync<JsonRaw>` calls. Only the endpoints where it matters need converting.

**Benefit:** memory stays at about one row at a time, whatever the page size.

**Trade-offs to handle:**

- **Errors mid-stream:** once `200` and part of the body are sent, a `500` can no longer be returned, and the client receives truncated JSON. Log the error and abort the connection, so that clients see a failure rather than silent data loss.
- **Other output formats:** `CsvOutputFormatter`, `JsonLdOutputFormatter` and `RawdataOutputFormatter` expect `IResponse<JsonRaw>` or `IEnumerable<JsonRaw>`. Only stream `application/json`, and keep the current path for the other formats.
- **DB connections:** a Postgres connection stays open while a slow client downloads. Large unpaged requests could exhaust the pool, so add a cap or a timeout.
- **Endpoints that need the count first cannot stream.** For example, the weather "compatibility hack" returns a single object instead of an array when there is exactly one result.
- **CPU does not drop.** `TransformRawData` still parses each item into a `JToken`. Only memory and time-to-first-byte improve.
- **Gzip:** `UseResponseCompression` keeps working with streamed output.

### Option C: switch to System.Text.Json (not recommended)

System.Text.Json streams `IAsyncEnumerable` natively. The codebase, however, depends on Newtonsoft throughout:

- `JsonRawConverter`
- `DefaultContractResolver` (PascalCase property names)
- `StringEnumConverter`
- `[JsonProperty]` attributes
- `Swashbuckle.AspNetCore.Newtonsoft`

The migration would be large and risky, for a benefit Option B already provides.

### Recommendation

1. Implement Option B as an opt-in helper.
2. Start with the endpoints that return the most data: `ODHActivityPoi`, `Accommodation` and `Event` with `pagesize=-1`, plus the unpaged type-list branches.
3. Measure memory and time-to-first-byte before and after.
4. Leave paged requests with the default `pagesize=10` on the current path. Streaming brings almost nothing there.
