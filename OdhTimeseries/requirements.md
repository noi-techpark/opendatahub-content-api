Timeseries Database is ment to store timeseries and datasets metadata.  
The database and application layer is designed to limit the filter and search capability since timeseries should be retrieved once the user knows in which sensor is interested.

## Glossary

### Timeseries
A timeseries is a sequence of measurement over time referring to the same sensor.  
Timeseries enriches the measurements with some metdata about the timeseries itself such as:

- Oldest measurement timestamp
- Newest measurement timestamp
- Is-Up-To-Date flag (?)
- Is-Deprecated flag (?)
- Unit (informations about the scale/semantic of the value)
- Data type (numeric, string, json, geo-pos, geo-shape)
- Sensor ID

#### Measurement
A measurement is the the registered value in a specific time.
Supported values:

- Numbers
- Strings
- Geo Position
- Geo Shapes
- JSON

### Dataset
A dataset is a Timeseries collection describing the semantic, expected types

- Name
- Description
- Timeseries Schema (mondatory timeseries the sensor needs to provide to be part of the dataset)

## Expected capabilities

Mutations:
1. Process single or batched data tree and insert them in batches
2. Delete measurements for a (set) of sensors for a (set) of datasets in a given time range
3. Handle table partition for measurements
4. Ensure idempotency when writing data (no double records)


Query:
1. Given a sensor id, get latest measurement for a set of Timeseries.
2. Given a set of sensor ids, get latest measurement for a set of Timeseries.
3. Given a sensor id, get latest+historical measurement for a set of Timeseries.
4. Given a set of sensor ids, get latest+historical measurement for a set of Timeseries.
5. Find all sensors part of a Dataset
6. find all sensors which measurement's value satisfy a condition for a "type" given an optional time range
    EG: "give me sensors which geo_position intersected with POLYGON during period 01-01-2025 to 02-01-2025"

## Constraints

- Application layer must return measurements from single endpoints, regardless the underlying datatype. The datatype must retuns as well (eiter attached to each measurement or as "head" of the timeseries)