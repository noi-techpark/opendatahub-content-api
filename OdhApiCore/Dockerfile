FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src
COPY DataModel/DataModel.csproj DataModel/
COPY RawQueryParser/RawQueryParser.fsproj RawQueryParser/
COPY GeoConverter/GeoConverter.csproj GeoConverter/
COPY Helper/Helper.csproj Helper/
COPY EBMS/EBMS.csproj EBMS/
COPY MSS/MSS.csproj MSS/
COPY LCS/LCS.csproj LCS/
COPY SIAG/SIAG.csproj SIAG/
COPY NINJA/NINJA.csproj NINJA/
COPY RAVEN/RAVEN.csproj RAVEN/
COPY STA/STA.csproj STA/
COPY LOOPTEC/LOOPTEC.csproj LOOPTEC/
COPY OdhNotifier/OdhNotifier.csproj OdhNotifier/
COPY PushServer/PushServer.csproj PushServer/
COPY JsonLDTransformer/JsonLDTransformer.csproj JsonLDTransformer/
COPY OdhApiCore/OdhApiCore.csproj OdhApiCore/
COPY google-service-account.noicommunity.json OdhApiCore/google-service-account.noicommunity.json
RUN dotnet restore OdhApiCore/OdhApiCore.csproj
COPY . .
WORKDIR /src/OdhApiCore
RUN dotnet build OdhApiCore.csproj -c Release -o /app -v q

FROM build AS test
WORKDIR /src
RUN dotnet test --filter Category!=Integration OdhApiCore.sln

FROM build AS publish
RUN dotnet publish OdhApiCore.csproj -c Release -o /app/publish
# Install dotnet debug tools
RUN dotnet tool install --tool-path /tools dotnet-counters
RUN dotnet tool install --tool-path /tools dotnet-trace
RUN dotnet tool install --tool-path /tools dotnet-dump
RUN dotnet tool install --tool-path /tools dotnet-gcdump

FROM base AS final
# Copy dotnet-tools
WORKDIR /tools
COPY --from=publish /tools .
WORKDIR /app
COPY --from=publish /app/publish .
COPY OdhApiCore/xml/Weather.xml xml/
COPY OdhApiCore/xml/STACategories.xml xml/
COPY OdhApiCore/xml/BezirkWeather.xml xml/
COPY OdhApiCore/appsettings.Production.json appsettings.json

# Expose the API port
EXPOSE 80

ENTRYPOINT ["dotnet", "OdhApiCore.dll"]
