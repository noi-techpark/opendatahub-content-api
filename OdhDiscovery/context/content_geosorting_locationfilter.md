# Filtering ODH by Location

## Geosorting Functionality

`latitude`: GeoFilter Latitude Format ex. '46.624975'  
`longitude`: GeoFilter Longitude Format ex. '11.369909'  
`radius`: Radius to Search in Meters. Only Object withhin the given point and radius are returned and sorted by distance. 

example call  #
https://tourism.opendatahub.com/v1/ODHActivityPoi?latitude=46.624975&longitude=11.369909&radius=2000

Note: An active geofilter overwrites a rawsort Parameter

## Polygon Filter Functionality

`polygon`: Pass a valid GPS Polygon to filter data. Available options WKT Format/Custom Format/Using the Open Data Hub GeoShapes api

examples #

1. By using the shapes api  
Check out https://tourism.api.opendatahub.com/v1/GeoShapes  
Pass the desired polygon in format Country.Type.Id OR Country.Type.Name  
'polygon=it.municipality.Bolzano/Bozen'

Example: https://tourism.api.opendatahub.com/v1/ODHActivityPoi?polygon=it.municipality.Bolzano/Bozen

2. By using WKT Format  
`polygon=POLYGON((11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285))`  
`polygon=LINESTRING(11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285)`  
`polygon=MULTIPOLYGON(((11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285)),((11.483016 46.537154,11.582580 46.517785,11.557174 46.481863,11.483016 46.537154)))`    
It is possible to add a custom SRID by adding ;SRID=4326 at the end of the passed string  
`polygon=POLYGON((11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.026805 46.688285));SRID=4326`
  
Example: https://tourism.api.opendatahub.com/v1/ODHActivityPoi?polygon=POLYGON((11.026805%2046.688285,11.083110%2046.690169,11.081394%2046.660723,11.035045%2046.655775,11.026805%2046.688285))
  
Information: The Size of the passed polygon Parameter could hit the GET Parameter Max Length if the passed polygon is too large
  
3. By bbc/bbi syntax (known from the timeseries api)  
`polygon=bbc(11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285,11.026805 46.688285)`    It is possible to add a custom SRID by adding ;SRID=4326 at the end of the passed string  
`polygon=bbc(11.026805 46.688285,11.083110 46.690169,11.081394 46.660723,11.035045 46.655775,11.026805 46.688285,11.026805 46.688285);SRID=4326`
  
Example: https://tourism.api.opendatahub.com/v1/ODHActivityPoi?polygon=bbc(11.026805%2046.688285,11.083110%2046.690169,11.081394%2046.660723,11.035045%2046.655775,11.026805%2046.688285,11.026805%2046.688285)  
  
Information: The Size of the passed polygon Parameter could hit the GET Parameter Max Length if the passed polygon is too large

## Location Filter locfilter

Get all available locations  

https://tourism.opendatahub.com/v1/Location?showall=true

Use `typ`+ `id` and pass as `locfilter` 

example call  
Get all ODHActivityPois in Tourismarea Ritten  
`https://tourism.opendatahub.com/v1/ODHActivityPoi?locfilter=tvs522822D451CA11D18F1400A02427D15E`
  
The Location Filter supports multiple values of different types, if inserted the location ids are combined with 'OR' logic.  
Example  
`https://tourism.opendatahub.com/v1/Accommodation?locfilter=tvs5228229B51CA11D18F1400A02427D15E,mun99A8B1D4A8D64303B1B965AA7C20FA60,fra79CBD63151C911D18F1400A02427D15E`

## Area Filter ODHActivity Poi 

Special Case on ODHActivityPoi it is also possible to filter on areas and skiareas

