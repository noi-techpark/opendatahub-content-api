# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build
- Build entire solution: `dotnet build OdhApiCore.sln`
- Build API: `dotnet build OdhApiCore/OdhApiCore.csproj`
- Build Importer: `dotnet build OdhApiImporter/OdhApiImporter.csproj`

### Run/Development
- Run API: `dotnet run --project OdhApiCore` (https://localhost:5001, http://localhost:5000)
- Run Importer: `dotnet run --project OdhApiImporter`

### Docker
- API: `cd OdhApiCore && docker-compose up` (http://localhost:8001/)
- Importer: `cd OdhApiImporter && docker-compose up` (http://localhost:8002/)

### Tests
- Run all tests: `dotnet test`
- Run specific test project: `dotnet test OdhApiCoreTests/OdhApiCoreTests.csproj`
- Run F# parser tests: `dotnet test RawQueryParserTests/RawQueryParserTests.fsproj`

### REUSE Compliance
- Install pre-commit: `pip install pre-commit && pre-commit install`
- Check REUSE compliance: `reuse lint`

## Architecture

### Core Components

**OdhApiCore** - Main REST API (.NET 8, ASP.NET Core)
- Controllers in `/Controllers/` organized by domain (api/, generic/, helper/)
- Uses PostgreSQL with JSON storage and generated columns for performance
- Swagger/OpenAPI support with custom filters
- Identity Server integration (Keycloak)
- Implements advanced filtering (rawfilter/rawsort) via RawQueryParser
- Custom output formatters (CSV, JSON-LD, Raw data)

**OdhApiImporter** - Background service for data import (.NET 8)
- Worker service using `IBackgroundTaskQueue` pattern
- Import helpers organized by data source (A22/, CDB/, DSS/, etc.)
- Scheduled tasks for importing data from various tourism/mobility APIs

**DataModel** - Shared data models (available as NuGet package)
- Contains ODH Tourism domain models
- Interfaces and compatibility helpers
- Available at: https://www.nuget.org/packages/opendatahub-datamodel-tourism

### Data Collectors (Source-Specific Libraries)
Each handles data retrieval and transformation to ODH format:
- **A22** - A22 highway data
- **CDB** - Content Database 
- **DSS** - Destination South Tyrol data
- **EBMS** - Event Booking Management System
- **FERATEL** - Feratel tourism data
- **GTFSAPI** - GTFS timetables
- **LCS** - Local Content System
- **MSS** - Hotel availability system  
- **NINJA** - Mobility/sensor data (Big Data Platform)
- **RAVEN** - Tourism data aggregator
- **SIAG** - Weather and museum data
- **STA** - Public transport data
- **SuedtirolWein** - Wine producer data

### Utility Libraries

**Helper** - Core utilities and extensions
- PostgreSQL query builders and helpers (`/Postgres/`)
- Generic helpers (`/Generic/`)
- Identity and authorization (`/Identity/`)
- Location and tagging helpers

**GeoConverter** - Geographic data conversion (KML/GPX → GeoJSON)
**JsonLDTransformer** - Convert ODH objects to schema.org JSON-LD
**RawQueryParser** - F# library for advanced query parsing
**OdhNotifier** - Push notifications for data changes
**PushServer** - Firebase Cloud Messaging integration

### Database Architecture

**PostgreSQL 15** with JSON storage pattern:
- Main data stored as JSONB in single column
- Generated columns for performance (extracted from JSON)
- Custom PostgreSQL functions for JSON processing
- Extensions: earthdistance, cube, pg_trgm, postgis

Key generated column patterns:
- `gen_bool` - Boolean fields
- `gen_string` - Text fields  
- `gen_array` - Array fields
- `gen_date` - Timestamp fields
- `gen_position` - PostGIS geometry for location search
- `gen_access_role` - Access control arrays

## Project Structure Notes

- Solution file: `OdhApiCore.sln` contains all projects
- Uses .NET 8 (upgraded from .NET Core 5)
- Mixed C# and F# codebase (RawQueryParser in F#)
- Docker support with separate compose files per service
- REUSE compliant for licensing
- Test projects use xUnit framework
- Environment variables required - see README.md for full list

## Development Notes

- The API follows async/await patterns throughout
- Custom middleware for rate limiting, logging, and request interception
- Swagger generation includes custom schema filters and operation filters  
- Advanced query capabilities via `rawfilter` and `rawsort` parameters
- Multi-format output support (JSON, CSV, JSON-LD)
- Generated columns and indices optimize PostgreSQL performance for large datasets