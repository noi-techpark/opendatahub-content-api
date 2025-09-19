(
  select 'geoshape' as ty, s.* from 
  measurements_geoshape m,
  timeseries t,
  sensors s 
  where t.sensor_id = s.id 
  and m.timeseries_id = t.id 
  limit 1
)
union all 
(
  select 'geoposition' as ty, s.* from 
  measurements_geoposition m,
  timeseries t,
  sensors s 
  where t.sensor_id = s.id 
  and m.timeseries_id = t.id 
  limit 1
)
union all 
(
  select 'boolean' as ty, s.* from 
  measurements_boolean m,
  timeseries t,
  sensors s 
  where t.sensor_id = s.id 
  and m.timeseries_id = t.id 
  limit 1
)
union all 
(
  select 'json' as ty, s.* from 
  measurements_json m,
  timeseries t,
  sensors s 
  where t.sensor_id = s.id 
  and m.timeseries_id = t.id 
  limit 1
)
union all 
(
  select 'numeric' as ty, s.* from 
  measurements_numeric m,
  timeseries t,
  sensors s 
  where t.sensor_id = s.id 
  and m.timeseries_id = t.id 
  limit 1
)
union all 
(
  select 'string' as ty, s.* from 
  measurements_string m,
  timeseries t,
  sensors s 
  where t.sensor_id = s.id 
  and m.timeseries_id = t.id 
  limit 1
);