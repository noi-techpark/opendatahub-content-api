For discovery operations you can specify a filter over timeseries' datatypes to get sensors satisfying that condition.

Filter the result with filter-triples, like timeseries.operator.value_or_list
For json measurements you can ckeck nested values by timeseries.nested1.nested2.operator.value_or_list

values_or_list

    value: Whatever you want, also a regular expression. However, you need to escape ,'" with a \. Use url-encoded values, if your tool does not support certain characters.
    list: (value,value,value)

operator

    eq: Equal
    neq: Not Equal
    lt: Less Than
    gt: Greater Than
    lteq: Less Than Or Equal
    gteq: Greater Than Or Equal
    re: Regular Expression
    ire: Insensitive Regular Expression
    nre: Negated Regular Expression
    nire: Negated Insensitive Regular Expression
    bbi: Bounding box intersecting objects (ex., a street that is only partially covered by the box). Syntax? See below.
    bbc: Bounding box containing objects (ex., a station or street, that is completely covered by the box). Syntax? See below.
    dlt: Within distance (in meters) from point. Learn more and see examples
    in: True, if the value of the alias can be found within the given list. Example: name.in.(Peter,Patrick,Rudi)
    nin: False, if the value of the alias can be found within the given list. Example: name.nin.(Peter,Patrick,Rudi)

logical operations

    and(alias.operator.value_or_list,...): Conjunction of filters (can be nested)
    or(alias.operator.value_or_list,...): Disjunction of filters (can be nested)

Multiple conditions possible as comma-separated-values. values will be casted to Double precision or null, if possible. Put them inside double quotes, if you want to prevent that.

Example-syntax for bbi/bbc could be coordinate.bbi.(11,46,12,47,4326), where the ordering inside the list is left-x, left-y, right-x, right-y and SRID (optional, default 4326).

Example-syntax for dlt could be coordinate.dlt.(5000,11.2,46.7,4326), where the ordering inside the list is distance (in meters), point-x, point-y and SRID (optional, default 4326).