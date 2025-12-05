FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0.407 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_NOLOGO=1
COPY DataModel/DataModel.csproj DataModel/
COPY RawQueryParser/RawQueryParser.fsproj RawQueryParser/
COPY GeoConverter/GeoConverter.csproj GeoConverter/
COPY Helper/Helper.csproj Helper/
COPY EBMS/EBMS.csproj EBMS/
COPY DSS/DSS.csproj DSS/
COPY MSS/MSS.csproj MSS/
COPY LCS/LCS.csproj LCS/
COPY SIAG/SIAG.csproj SIAG/
COPY SuedtirolWein/SuedtirolWein.csproj SuedtirolWein/
COPY NINJA/NINJA.csproj NINJA/
COPY RAVEN/RAVEN.csproj RAVEN/
COPY STA/STA.csproj STA/
COPY HDS/HDS.csproj HDS/
COPY OUTDOORACTIVE/OUTDOORACTIVE.csproj OUTDOORACTIVE/
COPY PANOMAX/PANOMAX.csproj PANOMAX/
COPY PANOCLOUD/PANOCLOUD.csproj PANOCLOUD/
COPY FERATEL/FERATEL.csproj FERATEL/
COPY A22/A22.csproj A22/
COPY DIGIWAY/DIGIWAY.csproj DIGIWAY/
COPY GTFSAPI/GTFSAPI.csproj GTFSAPI/
COPY LOOPTEC/LOOPTEC.csproj LOOPTEC/
COPY PushServer/PushServer.csproj PushServer/
COPY OdhNotifier/OdhNotifier.csproj OdhNotifier/
COPY OdhApiImporter/OdhApiImporter.csproj OdhApiImporter/
RUN dotnet restore OdhApiImporter/OdhApiImporter.csproj
COPY . .
WORKDIR /src/OdhApiImporter
RUN dotnet build OdhApiImporter.csproj -c Release -o /app --no-restore --no-incremental --verbosity diagnostic

FROM build AS publish
RUN dotnet publish OdhApiImporter.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY OdhApiImporter/xml/Weather.xml xml/
COPY OdhApiImporter/xml/STACategories.xml xml/
COPY OdhApiImporter/xml/BezirkWeather.xml xml/

ENTRYPOINT ["dotnet", "OdhApiImporter.dll"]
