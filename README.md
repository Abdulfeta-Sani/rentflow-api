# RentFlow API

ASP.NET Core API for RentFlow, a property rental and tenancy management platform. The API owns authentication, authorization, rental workflow rules, persistence, and tenancy creation.

## Current Scope

The API currently supports:

- ASP.NET Core Identity with Admin, Owner, and Tenant roles.
- JWT access tokens and rotating refresh tokens.
- Public property and unit discovery.
- Owner property and unit management.
- Tenant rental-application submission, listing, and withdrawal.
- Owner application review, approval, and rejection.
- Automatic tenancy creation when an application is approved.
- Tenant and owner tenancy read endpoints.
- Administrator user-status and property moderation.
- PostgreSQL persistence through Entity Framework Core migrations.
- Development OpenAPI document generation.

The API is the final authority for authentication, authorization, ownership checks, status transitions, and data validation.

## Technology

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL
- ASP.NET Core Identity
- JWT Bearer authentication
- MediatR
- Clean Architecture project structure

## Solution Structure

```text
RentFlow.Api/             HTTP pipeline, controllers, authorization
RentFlow.Application/     Use cases, commands, queries, DTOs, interfaces
RentFlow.Domain/          Entities, enums, and domain state
RentFlow.Infrastructure/ EF Core, Identity, migrations, persistence services
```

Dependency direction:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application -> Domain
```

## Prerequisites

- .NET 10 SDK
- PostgreSQL running locally
- A PostgreSQL user with permission to create or update the `rentflow_dev` database

## Configuration

The API requires a PostgreSQL connection string and JWT settings. Development configuration is loaded from `appsettings.Development.json` and User Secrets.

The recommended approach is to keep passwords and signing keys out of tracked files:

```bash
dotnet user-secrets init --project RentFlow.Api/RentFlow.Api.csproj

dotnet user-secrets set "ConnectionStrings:RentFlowDatabase" "Host=localhost;Port=5432;Database=rentflow_dev;Username=postgres;Password=YOUR_POSTGRES_PASSWORD" --project RentFlow.Api/RentFlow.Api.csproj

dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_DEVELOPMENT_KEY" --project RentFlow.Api/RentFlow.Api.csproj
```

The non-secret JWT values are:

```text
Jwt:Issuer = RentFlow.Api
Jwt:Audience = RentFlow.Client
```

Never commit PostgreSQL passwords, JWT signing keys, access tokens, refresh tokens, or User Secrets.

## Database Initialization

On startup, the API applies pending EF Core migrations and seeds the required roles. It also creates the development administrator if the account does not already exist.

Seeded development administrator:

```text
Email: admin@rentflow.local
Password: Admin@12345
```

Change or replace this credential before using the system outside local development.

## Running Locally

From the `rentflow-api` directory:

```bash
dotnet restore RentFlow.slnx
dotnet build RentFlow.slnx
dotnet run --project RentFlow.Api/RentFlow.Api.csproj --launch-profile https
```

The development URLs are:

```text
HTTPS: https://localhost:7249
HTTP:  http://localhost:5118
```

If the local HTTPS certificate is not trusted:

```bash
dotnet dev-certs https --trust
```

The Angular client runs on `http://localhost:4200`. The API CORS policy allows both `http://localhost:4200` and `http://127.0.0.1:4200`.

## OpenAPI

In the Development environment, the API exposes its OpenAPI document at:

```text
https://localhost:7249/openapi/v1.json
```

The current project exposes the OpenAPI document directly. A Swagger UI package is not currently included.

## Endpoint Overview

### Authentication

```text
POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
PUT  /api/v1/auth/me
```

### Public properties

```text
GET /api/v1/properties/published
GET /api/v1/properties/{id}
```

### Owner properties

```text
GET    /api/v1/owner/properties
POST   /api/v1/owner/properties
GET    /api/v1/owner/properties/{id}
PUT    /api/v1/owner/properties/{id}
POST   /api/v1/owner/properties/{id}/publish
POST   /api/v1/owner/properties/{id}/archive
DELETE /api/v1/owner/properties/{id}
```

### Owner units

```text
GET  /api/v1/owner/properties/{propertyId}/units
POST /api/v1/owner/properties/{propertyId}/units
GET  /api/v1/owner/units/{id}
PUT  /api/v1/owner/units/{id}
POST /api/v1/owner/units/{id}/mark-unavailable
POST /api/v1/owner/units/{id}/archive
```

### Rental applications

```text
GET  /api/v1/rental-applications/mine
GET  /api/v1/rental-applications/owner
POST /api/v1/rental-applications
POST /api/v1/rental-applications/{id}/withdraw
POST /api/v1/rental-applications/{id}/approve
POST /api/v1/rental-applications/{id}/reject
GET  /api/v1/rental-applications/{id}/owner-view
GET  /api/v1/rental-applications/{id}/tenant-view
```

Approving an application creates an active tenancy and marks the unit occupied when the unit is still available.

### Tenancies

```text
GET /api/v1/tenancies/mine
GET /api/v1/tenancies/owner
GET /api/v1/tenancies/{id}
```

### Administration

```text
GET    /api/v1/admin/users
POST   /api/v1/admin/users/{id}/activate
POST   /api/v1/admin/users/{id}/suspend
POST   /api/v1/admin/users/{id}/deactivate
GET    /api/v1/admin/properties
POST   /api/v1/admin/properties/{id}/suspend
DELETE /api/v1/admin/properties/{id}
GET    /api/v1/admin/applications
```

All protected endpoints require a bearer token and the appropriate role or ownership relationship.

## Verification

Build the complete solution:

```bash
dotnet build RentFlow.slnx
```

Apply migrations manually when needed:

```bash
dotnet ef database update \
  --project RentFlow.Infrastructure/RentFlow.Infrastructure.csproj \
  --startup-project RentFlow.Api/RentFlow.Api.csproj
```

Check the working tree before committing:

```bash
git status
```

## Core Business Flow

```text
Owner registers
  -> Admin activates owner
  -> Owner creates property and units
  -> Owner publishes property
  -> Tenant registers and is activated
  -> Tenant submits application
  -> Owner approves or rejects application
  -> Approval creates a tenancy and occupies the unit
```

## Known Limitations

- Online payments and maintenance management are not implemented.
- Application and tenancy history is exposed through the implemented read endpoints, but there is no payment, document, or notification subsystem.
- Tenancy creation currently occurs during application approval; tenancy termination and renewal workflows are not implemented.
- The API currently exposes OpenAPI JSON rather than a Swagger UI.

## Security Notes

- Use User Secrets for local credentials.
- Use a unique, long random JWT signing key per environment.
- Do not reuse the development administrator password outside local development.
- Do not commit `appsettings.Development.json` values containing real credentials.
- JWT access tokens are short-lived; refresh tokens are rotated and persisted as hashes.
