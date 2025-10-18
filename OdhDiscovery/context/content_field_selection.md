# Common parameters 

Common parameters are available on the most endpoints of ODH Tourism

## fields

Select the fields to be included in the output. More fields can be selected, simply comma separate it.
the Id field is always included.
It is possible to navigate trough Objects  
Also to display n-th items of Arrays  
Selecting items of Arrays is also possible.

examples for (https://tourism.api.opendatahub.bz.it/v1/ODHActivityPoi?fields=):
- `Shortname` : Includes the field Shortname  
- `Detail` : Includes the object Detail
- `Detail,ImageGallery,Shortname` : Includes Object Detail, Array ImageGallery and Property Shortname
- `Detail.en.Title` : Include Title in English of Detail Object
- `ODHTags` : Include ODHTags Array
- `ODHTags.[0]` : Include ODHTags Array first Element
- `ODHTags.[0].Id` : Include ODHTags Array first Elements Id
- `ODHTags.[*].Id` : Includes ODHTags Array Element Id, returning an array

## language

Language Selector.
The language selector crops the json to show only the selected language in the json Response.  
Passing only one language is supported.  

## langfilter

Language Filter.
Only Content available in the filtered Language is returned.  
The langfilter filters the data by its `HasLanguage` field.

## searchfilter

Search trough **title fields** of the Dataset.  
If no language is passed, search is done trough all available languages.  
This query can take some time to be processed, it is recommended to add a language parameter if a search is done. Also it may show up to many results since the Title field in each language is searched.    
  
It is also possible to insert an ID in the searchfilter the api automatically searches all matching ID's.

## removenullvalues

Remove all `Null` values from json output. Useful for reducing json size.  
By default set to false, the whole object is shown.

## updatefrom

Get Data which was updated after the passed date. Format yyyy-MM-dd
