problem:
this time there are no particular problem to address. This is not a bugfix proposal is a transformative enhance.

--------------

goals:
simplify timeseries api to make the easier to use and learn for developers.
clear distinction about what to search in the content (sensors, poi, accomodation) and what to search in the timeseries (timeseries only), this way we have a consistent and unified flow the developers will learn.
bring on the table a technological UVP which gives Open Data Hub competitive advantage and apeal.

-----------------

goals (technical):
make timeseries database more maintainable, performant and lightweight.
resolve perfromance bottlenecks (writes, deletions).
start a federation between the 2 api services.
avoid duplication of data (echarging station as example).
naming convention

--------------

goals (operational):
instead of splitting the effort in maintain 2 system sharing a big chunk of BL, move all the content in the content and make timeseries more specialized.
having all SA sharing knowledge and actively maintaining both systems.
commit efforts in deliver content-specific and timeseries-specific features.

-----------------

Strategy:
redesign the timeseries api greatly limiting what an user can ask to the timeseries database. all the endpoints must be stricly related to the timeseries domain, no more station filtering.
Parallely simplify the underlying database schema removing everything concerning the sensors/stations and focus on timeseries:
- Timeseries table as timeseries header (mantaining metadata about timeseries itself)
- Dataset ontology to define groups of types carying a spcific semantic (Ontology "parking" has always "free" and occupied "types")
- Simplify the concept of timeseries from the triple <sensor-type-period> to an easier to maintain and use <sensor-type>; different periods becomes new types, if needed (like in the case of aggregation) enhancing clarity and maintainability.

Move the concept of station to the content api, exploit the existing battletested BL of the content API to manage stations (sensors). 
This way users always enter their journey in the content api and get to familiarize with it. The great challenge here is to federate the 2 apis to allow queries like "give me all sensors of type X, having timeseries 'humidity'".

Focus on UVPs to make Open Data Hub appealing for developers:
- Streaming subscription with filters GraphQL-like
(- Ability to request and download timeseries as CSV, zip, parquet files.)

Encode Station (sensor) names using the URN semantic, putting the sensor type directly in the name instead of as different property:
urn:odh:weathersensor:abc

This way we avoid collisions, sensor names are more explit, we have our own unique id instead of relying on the proider's.
urn semantic makes it very easy to filter by type as well, since we can use an ilike clause.

----------------

USP, why subscriptions?:

Timeseries represent a big part of the Open Data Hub.
We want to enhance how the user discover, requests and use the timeseries data.
Websocket for realtime updates inlines with our mission to deliver high quality real time iot data enhancing the developer experience without complicating how the data is stored, delivered and managed.
The streaming feature is built on top of the logical timeseries database, it is a plugable extension which to not changes how the ninja, the bdp and the underlying logical database behave and are designed.
Many Companies in the fin-tech and iot world are implementing / providing a websocket layer on top of their api:
- Polygon.io — realtime WebSocket market
- Binance — WebSocket market streams
- ThingsBoard — WebSocket API & MQTT-over-WebSocket
- Losant — added WebSocket support (platform update + WebSocket triggers and streaming endpoints)
- ...

realtime websockets can be sold as premium/freemium features.

subscription USECASEs:
We already have internal usecases for such as technology:

- Free parking elaborations currently poll parking data every 5 minutes and compute the free slots avalable with this 5 minute lag. With the websocket it could subscribe to all parking slots update and perform the elaboration as soon as the new measurement arrives
- Parking shim (used by bolzano parking totems) perform a call to ninja every time it gets hit. with websocket we could listen for update and keep a cache, liftig up loads from ninja


----------------

Implementation:

TS -------------
refactor bdp and ninja to be more specialized, remove all station mangament and focus on timeseries.
add measurement types:
- numeric
- string
- json
- bool
- geoshape
- geoposition

add dataset (ontology) and timeseries management.
focus on sensor discoverability given some timeseries contraint (dataset compliance, type, measurement filtering).

Bdp must be extremely good at inserting data 
- must accept a single batch insert which can potentially insert stations, types, measurements
- must provide api to delete data
- must provide api to force new timeseries creation / disable of existint timeseries
- basic cruds (not discovery)

Ninja must specialize on sensor discovery and timeseries deliver
- Discover sensors filtering about datasets/types constraint
- Discover sensor satisfying measurement constraint (latest & historical)
- Types/timeseries/dataset info
- Serve historical data with efficiency and in multiple formats (json, csv, parquet)
- Must answer fewer but more precise questions:
- - What sensors had measurement xy in this range last month?
- - What sensors have active timeseries for types xy, ab?
- - What sensors fully implement the dataset (ontology) bc?
- - Is this sensor compliant with these constraints?
- - Give me all types and all sensors-timeseries pairs
- - 

CONTENT ---------
use all the existing logic to implement the concept of sensor.
use free geometry (point,polygon,lines) as sensor position to be able to represent areas, munipalities, roads, ....

develop a federation helper to allow content to filter results based on sensor discoverability int he timeseries (dataset compliance, type, measurement filtering).

SUBSCRIPTIONS --------
relay layer which listens for timeseries updates and allows users to subscribe to the changes providing some filter capabilities.
this is done using Materialize which is a replicator and steam layer which can direclty connect to postgres. a service must be developed to manage websocket subscriptions and "materialize tails".

