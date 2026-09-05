RentFlow Backend Development Roadmap
Project Overview
Project: RentFlow
Backend repository: rentflow-api
Frontend repository: rentflow-client
Backend technology: ASP.NET Core 10, PostgreSQL, EF Core
Frontend technology: Angular 21
Currency: ETB only
Authentication: ASP.NET Core Identity and JWT
Architecture: Clean Architecture
The backend solution will contain:

rentflow-api/
├── RentFlow.slnx
├── RentFlow.Api/
├── RentFlow.Application/
├── RentFlow.Domain/
├── RentFlow.Infrastructure/
├── tests/
├── .gitignore
└── README.md

Dependency Direction

RentFlow.Api
├── RentFlow.Application
└── RentFlow.Infrastructure
├── RentFlow.Application
└── RentFlow.Domain

RentFlow.Application
└── RentFlow.Domain

RentFlow.Domain
└── no dependencies

The Domain project must not reference EF Core, ASP.NET Core, Identity, or Infrastructure.
Step 5: Create the Core Domain Entities
Goal
Create the entities that represent the main rental workflow.

Property
└── Unit
├── RentalApplication
└── Tenancy

Initial Entities
Property
Represents a property owned by an Owner.
Important fields:

Id
OwnerId
Name
Description
PropertyType
Address
City
Amenities
Status
CreatedAtUtc
UpdatedAtUtc

Unit
Represents a rentable item inside a property.
Important fields:

Id
PropertyId
NameOrNumber
Bedrooms
Bathrooms
MonthlyRent
SecurityDeposit
Status

RentalApplication
Represents a Tenant’s request to rent a Unit.
Important fields:

Id
TenantId
UnitId
EmploymentInformation
NumberOfOccupants
PreferredMoveInDate
Message
Status
SubmittedAtUtc
ReviewedAtUtc
DecisionReason

Tenancy
Represents the rental agreement created after approval.
Important fields:

Id
ApplicationId
UnitId
TenantId
OwnerId
StartDate
EndDate
MonthlyRent
SecurityDeposit
Status
CreatedAtUtc
ActivatedAtUtc
EndedAtUtc

Initial Statuses

Property:
Draft
Published
Suspended
Archived

Unit:
Available
Occupied
Unavailable
Archived

RentalApplication:
Submitted
UnderReview
Approved
Rejected
Withdrawn

Tenancy:
Active
Expired
Terminated

Because approval immediately activates the tenancy, the first version does not need:

PendingAcceptance
Reserved
Cancelled

Domain Rules
Every rentable item must be a Unit.
A Property belongs to one Owner.
A Unit belongs to one Property.
A RentalApplication belongs to one Tenant and one Unit.
A Tenancy is created from an approved application.
A Unit with an active tenancy must be Occupied.
An occupied Unit cannot receive another active tenancy.
StartDate must be before EndDate.
Money must use decimal.
IDs should use Guid.
Timestamps should use UTC.
Historical properties, units, applications, and tenancies should be archived rather than hard-deleted.
Verification

dotnet build RentFlow.slnx

Commit

git add RentFlow.Domain
git commit -m "feat: add core rental domain entities"
git push

Open a pull request and review the entity model before continuing.
Step 6: Create the EF Core DbContext
Goal
Create the database context in Infrastructure and expose the domain entities to EF Core.
Create:

RentFlow.Infrastructure/
└── Persistence/
└── RentFlowDbContext.cs

The context should inherit from the appropriate Identity DbContext once Identity is introduced.
Initially, it will eventually contain:

DbSet<Property>
DbSet<Unit>
DbSet<RentalApplication>
DbSet<Tenancy>

Entity Configurations
Create separate configuration classes:

RentFlow.Infrastructure/
└── Persistence/
└── Configurations/
├── PropertyConfiguration.cs
├── UnitConfiguration.cs
├── RentalApplicationConfiguration.cs
└── TenancyConfiguration.cs

Configure:
Primary keys.
Required properties.
Maximum string lengths.
Decimal precision.
Relationships.
Foreign keys.
Delete behavior.
Enum storage.
Database indexes.
Unique constraints.
Important Relationships

Property 1 ──── many Unit
Unit 1 ──── many RentalApplication
Unit 1 ──── many Tenancy
RentalApplication 1 ──── zero/one Tenancy

Do not cascade-delete historical rental records accidentally.
Important Indexes
Create indexes for:

Property.OwnerId
Property.Status
Property.City
Unit.PropertyId
Unit.Status
RentalApplication.TenantId
RentalApplication.UnitId
RentalApplication.Status
Tenancy.UnitId
Tenancy.TenantId
Tenancy.OwnerId
Tenancy.Status

Add a unique rule to help prevent duplicate open applications for the same Tenant and Unit. The exact implementation can be finalized after the application status model is complete.
Verification

dotnet build RentFlow.slnx

Commit

git add RentFlow.Infrastructure
git commit -m "feat: configure rental persistence model"
git push

Step 7: Configure PostgreSQL
Goal
Connect the API to a local PostgreSQL database.
PostgreSQL must be installed and running on every developer’s computer. The application can create the database and tables, but it cannot install PostgreSQL itself.
Local Connection String
Use User Secrets for the real password:

dotnet user-secrets init --project RentFlow.Api/RentFlow.Api.csproj

dotnet user-secrets set "ConnectionStrings:RentFlowDatabase" "Host=localhost;Port=5432;Database=rentflow_dev;Username=postgres;Password=YOUR_PASSWORD" --project RentFlow.Api/RentFlow.Api.csproj

Commit only an example configuration:

RentFlow.Api/appsettings.Development.example.json

{
"ConnectionStrings": {
"RentFlowDatabase": "Host=localhost;Port=5432;Database=rentflow_dev;Username=postgres;Password=CHANGE_ME"
}
}

Never commit:
PostgreSQL passwords.
JWT signing keys.
Refresh tokens.
Production connection strings.
Register DbContext
Register RentFlowDbContext in RentFlow.Api/Program.cs using the RentFlowDatabase connection string.
Automatic Database Migration
During local development, the API can apply pending migrations on startup:

await dbContext.Database.MigrateAsync();

This allows a collaborator to clone the repository, configure their local PostgreSQL password, start the API, and have the database schema created automatically.
The PostgreSQL server and login must still exist locally.
For production, migrations should normally run as a deployment step instead of every application instance starting simultaneously.
Commit

git add .
git commit -m "chore: configure local PostgreSQL database"
git push

Step 8: Create the Initial EF Core Migration
Goal
Create the first version-controlled database schema.
Install or verify the EF CLI:

dotnet tool install --global dotnet-ef

Create the migration:

dotnet ef migrations add InitialCreate --project RentFlow.Infrastructure/RentFlow.Infrastructure.csproj --startup-project RentFlow.Api/RentFlow.Api.csproj --output-dir Persistence/Migrations

Apply it manually once to verify PostgreSQL:

dotnet ef database update --project RentFlow.Infrastructure/RentFlow.Infrastructure.csproj --startup-project RentFlow.Api/RentFlow.Api.csproj

Start the API:

dotnet run --project RentFlow.Api/RentFlow.Api.csproj

Verify that:
The database exists.
Tables are created.
The API starts successfully.
OpenAPI is available.
No secrets are committed.
Commit

git add RentFlow.Infrastructure/Persistence/Migrations
git commit -m "chore: add initial rental database migration"
git push

Step 9: Implement Identity and Account Activation
Goal
Implement secure authentication before protected rental features.
User Model
Create an Identity user in Infrastructure with:

Id
Email
PhoneNumber
FirstName
LastName
AccountStatus
CreatedAtUtc
UpdatedAtUtc

Account Statuses

Pending
Active
Suspended
Deactivated

Roles

Admin
Owner
Tenant

Registration Rules
Public registration may create only:

Owner
Tenant

The client must never be allowed to create an Admin account.
New accounts begin as:

AccountStatus = Pending

Authentication Features
Implement:

POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET /api/v1/auth/me

Implement:
Password validation.
JWT access tokens.
Refresh-token rotation.
Token revocation.
Account status validation.
Login rate limiting.
Locked-account handling.
Pending, Suspended, and Deactivated users must not be able to log in.
Admin Account
Seed one Admin account during development.
Admin registration must not be public.
Admin Account Management
Implement:

GET /api/v1/admin/users
POST /api/v1/admin/users/{id}/activate
POST /api/v1/admin/users/{id}/suspend
POST /api/v1/admin/users/{id}/deactivate

Only Admin users can access these endpoints.
Migration
Identity adds database tables, so create a new migration:

dotnet ef migrations add AddIdentity --project RentFlow.Infrastructure/RentFlow.Infrastructure.csproj --startup-project RentFlow.Api/RentFlow.Api.csproj --output-dir Persistence/Migrations

Tests
Test:
Owner registration.
Tenant registration.
Admin registration rejection.
Pending user login rejection.
Activated user login success.
Suspended user login rejection.
Role authorization.
Refresh-token rotation.
Refresh-token reuse detection.
Commit

git add .
git commit -m "feat: add identity roles and account activation"
git push

Step 10: Add Authorization Policies
Goal
Protect endpoints according to roles and resource ownership.
Role Policies
Create policies for:

AdminOnly
OwnerOnly
TenantOnly
ActiveUser

Ownership Rules
An Owner may access a Property only when:

Property.OwnerId == CurrentUser.Id

An Owner may access a Unit only when the Unit belongs to one of their Properties.
An Owner may access an Application only when the Application’s Unit belongs to one of their Properties.
A Tenant may access only:

Their own applications
Their own tenancies
Their own notifications
Their own documents

Important Security Rule
Do not rely only on Angular route guards or hidden buttons. Every rule must be enforced in the API.
Tests
Add tests for:
Owner accessing their own property.
Owner accessing another Owner’s property.
Tenant accessing their own application.
Tenant accessing another Tenant’s application.
Admin access.
Anonymous access to public listings.
Commit

git add .
git commit -m "feat: enforce role and ownership authorization"
git push

Step 11: Implement Owner Property Management
Goal
Allow active Owners to manage properties.
Endpoints

GET /api/v1/owner/properties
POST /api/v1/owner/properties
GET /api/v1/owner/properties/{id}
PUT /api/v1/owner/properties/{id}
POST /api/v1/owner/properties/{id}/publish
POST /api/v1/owner/properties/{id}/archive

Creation Rules
Only active Owners can create properties.
New properties begin as:

Draft

Publishing Rules
Publishing requires:
The property belongs to the current Owner.
Required fields are valid.
At least one unit exists.
At least one unit is available.
The property is not archived.
DTOs
Use separate request and response DTOs:

CreatePropertyRequest
UpdatePropertyRequest
PropertyResponse
PropertyDetailResponse

Do not expose Identity entities directly through API responses.
Tests
Test:
Property creation.
Property update.
Property ownership.
Publishing without units.
Publishing with an unavailable unit.
Publishing a valid property.
Archiving a property.
Commit

git add .
git commit -m "feat: add owner property management"
git push

Step 12: Implement Unit Management
Goal
Allow Owners to manage rentable units.
Endpoints

GET /api/v1/owner/properties/{propertyId}/units
POST /api/v1/owner/properties/{propertyId}/units
GET /api/v1/owner/units/{id}
PUT /api/v1/owner/units/{id}
POST /api/v1/owner/units/{id}/mark-unavailable
POST /api/v1/owner/units/{id}/archive

Validation
Monthly rent must be greater than zero.
Security deposit cannot be negative.
Bedrooms cannot be negative.
Bathrooms cannot be negative.
Unit name or number is required.
The parent property must belong to the current Owner.
Occupied units cannot be archived or deleted.
Units with historical records should be archived instead of deleted.
Commit

git add .
git commit -m "feat: add owner unit management"
git push

Step 13: Implement Anonymous Property Discovery
Goal
Allow anyone to browse published properties without logging in.
Public Endpoints

GET /api/v1/properties
GET /api/v1/properties/{id}

Public Listing Rules
Return only:
Published properties.
Available units.
Public property fields.
Current unit rental information.
Do not expose:
Owner private information.
Tenant information.
Applications.
Tenancies.
Internal database fields.
Filters
Implement server-side filters for:

City
PropertyType
MinimumRent
MaximumRent
Bedrooms
Page
PageSize

Pagination
Use a standard response:

Items
TotalCount
Page
PageSize
TotalPages
HasPrevious
HasNext

Tests
Test:
Anonymous listing access.
Draft properties excluded.
Suspended properties excluded.
Archived properties excluded.
Occupied units excluded.
Filtering.
Pagination.
Property details.
Commit

git add .
git commit -m "feat: add public property discovery"
git push

Step 14: Implement Rental Applications
Goal
Allow active Tenants to apply for available Units.
Endpoints

POST /api/v1/units/{unitId}/applications
GET /api/v1/tenant/applications
GET /api/v1/tenant/applications/{id}
POST /api/v1/tenant/applications/{id}/withdraw
GET /api/v1/owner/applications
GET /api/v1/owner/applications/{id}
POST /api/v1/owner/applications/{id}/start-review
POST /api/v1/owner/applications/{id}/reject

Application Rules
Only active Tenants can apply.
The Unit must exist.
The Unit must be available.
The Property must be published.
The Tenant cannot have another open application for the same Unit.
A Tenant can view only their own applications.
An Owner can view only applications for their Units.
Only Submitted applications can move to UnderReview.
Only Submitted or UnderReview applications can be withdrawn.
Rejected and withdrawn applications are closed.
Validation
Number of occupants must be greater than zero.
Preferred move-in date must be valid.
Required applicant information must be provided.
Message length must be limited.
Employment information must be validated.
Tests
Test:
Successful application.
Application to an unavailable Unit.
Duplicate application.
Tenant privacy.
Owner application access.
Invalid state transitions.
Withdrawal.
Commit

git add .
git commit -m "feat: add tenant rental applications"
git push

Step 15: Implement Application Approval and Tenancy Creation
Goal
Complete the central business workflow.

Owner approves application
→ Application = Approved
→ Tenancy = Active
→ Unit = Occupied

Approval Endpoint

POST /api/v1/owner/applications/{id}/approve

Approval Transaction
The operation must execute inside one database transaction:
Load the application.
Verify the current Owner owns the unit.
Verify the application is still open.
Verify the Unit is still available.
Verify no active tenancy exists for the Unit.
Validate tenancy dates.
Mark the application as Approved.
Create an Active Tenancy.
Set the Unit to Occupied.
Reject other open applications for that Unit.
Create notification records.
Commit the transaction.
If any step fails:

Rollback all changes.

Tenancy Dates
For the first version:

StartDate < EndDate

The Owner may provide the dates during approval.
Competing Applications
When one application is approved, other open applications for the same Unit should become:

Rejected

Use a clear decision reason such as:

The unit was rented to another applicant.

Tenancy Endpoints

GET /api/v1/owner/tenancies
GET /api/v1/owner/tenancies/{id}
GET /api/v1/tenant/tenancy
GET /api/v1/tenant/tenancy/{id}
POST /api/v1/owner/tenancies/{id}/terminate

Tests
Test:
Successful approval.
Tenancy creation.
Unit becomes occupied.
Competing applications are rejected.
Approval by the wrong Owner.
Approval after another tenancy exists.
Invalid tenancy dates.
Transaction rollback.
Tenant and Owner tenancy privacy.
Commit

git add .
git commit -m "feat: add application approval and tenancy lifecycle"
git push

Step 16: Implement Tenancy Termination
Goal
Allow a valid Owner to end an active tenancy.
Termination Rules
Only the owning Owner can terminate the tenancy.
Only an active tenancy can be terminated.
The tenancy becomes Terminated.
The Unit becomes Available.
The operation must be transactional.
Relevant notifications should be created.
Transaction

Tenancy.Status = Terminated
Unit.Status = Available
Create notifications
Commit

Future Expiration
Automatic expiration will be added later through a background worker.
Tests
Test:
Successful termination.
Wrong Owner rejection.
Terminating an already ended tenancy.
Unit becomes available.
Transaction rollback.
Commit

git add .
git commit -m "feat: add tenancy termination"
git push

Step 17: Add Notifications
Goal
Create persistent in-app notifications for important events.
Notification Entity
Add later:

Id
RecipientUserId
Type
Title
Message
RelatedEntityType
RelatedEntityId
IsRead
CreatedAtUtc

Notification Events
Create notifications when:
Application is submitted.
Application is approved.
Application is rejected.
Tenancy is activated.
Tenancy is terminated.
Property is suspended.
Tenancy is approaching expiration.
Endpoints

GET /api/v1/notifications
POST /api/v1/notifications/{id}/read
POST /api/v1/notifications/read-all

Start with database-backed notifications. Add SignalR only after this works.
Commit

git add .
git commit -m "feat: add in-app notifications"
git push

Step 18: Add Admin Governance
Goal
Give Admin platform-wide visibility and control.
Admin Features
Implement:
User list and filtering.
Account activation.
Account suspension.
Account deactivation.
Property moderation.
Property suspension.
Property restoration.
Application monitoring.
Tenancy monitoring.
Platform statistics.
Admin Endpoints

GET /api/v1/admin/dashboard
GET /api/v1/admin/users
GET /api/v1/admin/properties
POST /api/v1/admin/properties/{id}/suspend
POST /api/v1/admin/properties/{id}/restore
GET /api/v1/admin/applications
GET /api/v1/admin/tenancies

Admin normally observes applications and tenancies but does not approve or reject applications.
Commit

git add .
git commit -m "feat: add admin governance features"
git push

Step 19: Add Background Tenancy Expiration
Goal
Automatically expire completed tenancies.
Create a hosted background service that periodically:
Finds active tenancies whose end date has passed.
Changes the tenancy to Expired.
Changes the Unit to Available.
Creates notifications.
Logs the operation.
The update must be transactional.
Later, it can also:
Notify users 30 days before expiration.
Handle failed jobs.
Prevent duplicate processing.
Record execution logs.
Commit

git add .
git commit -m "feat: add automatic tenancy expiration"
git push

Step 20: Add Audit Logging
Goal
Record important business events for Admin review.
Audit Log Fields

Id
UserId
Action
EntityType
EntityId
TimestampUtc
Metadata

Events to Log
User activated.
User suspended.
User deactivated.
Property created.
Property published.
Property suspended.
Application submitted.
Application approved.
Application rejected.
Tenancy created.
Tenancy terminated.
Tenancy expired.
Do not log every request. Focus on business events.
Commit

git add .
git commit -m "feat: add business audit logging"
git push

Step 21: Add Concurrency Protection
Goal
Prevent two Owners or requests from changing the same Unit simultaneously.
Add an EF Core concurrency token to Unit.
The approval workflow should handle concurrency conflicts and return:

409 Conflict

Example scenario:

Two Tenants apply for the same Unit
Two Owners attempt approval
Only one approval succeeds
The other receives a conflict response

Tests
Test concurrent approval attempts and verify:
Only one tenancy is created.
The Unit becomes occupied once.
The losing request receives 409 Conflict.
No partial data remains.
Commit

git add .
git commit -m "feat: protect unit approval with concurrency control"
git push

Step 22: Testing Strategy
Unit Tests
Test domain and application logic:
Status transitions.
Date validation.
Duplicate application rules.
Ownership rules.
Unit state changes.
Tenancy termination.
Approval behavior.
Integration Tests
Test the full API pipeline:
Registration.
Login.
Account activation.
Unauthorized access.
Forbidden access.
Property creation.
Property publishing.
Public discovery.
Application submission.
Application approval.
Tenancy creation.
Unit occupancy.
Tenancy termination.
Test Project Structure

tests/
├── RentFlow.Tests/
└── RentFlow.IntegrationTests/

Minimum MVP Tests
Before the instructor demonstration, complete tests for:
Pending account cannot log in.
Admin can activate accounts.
Owner can create a property.
Owner cannot access another Owner’s property.
Anonymous user can browse published properties.
Tenant can apply to an available Unit.
Duplicate applications are rejected.
Owner can approve an application.
Approval creates an active tenancy.
Approval changes the Unit to occupied.
A second active tenancy cannot be created.
Step 23: Backend MVP Definition
The backend MVP is complete when this API flow works:

Owner registration
→ Tenant registration
→ Admin activation
→ Owner login
→ Owner creates Property
→ Owner creates Unit
→ Owner publishes Property
→ Anonymous user views Property
→ Tenant login
→ Tenant submits Application
→ Owner views Application
→ Owner approves Application
→ Tenancy becomes Active
→ Unit becomes Occupied

The following are not required for the first demonstration:

Property images
Documents
SignalR
Automatic expiration
Audit-log interface
Caching
Docker
Payment processing
Maintenance management
Maps
AI recommendations

Step 24: Frontend Coordination
The backend and frontend repositories are separate, but features should be coordinated by API contract.
Before frontend implementation begins for a feature, agree on:
Endpoint URL.
HTTP method.
Request DTO.
Response DTO.
Validation errors.
Authorization requirements.
Success status code.
Error status codes.
Pagination format.
Recommended frontend sequence:

1. Angular shell and routing
2. Authentication
3. Admin account activation
4. Owner property management
5. Owner unit management
6. Public property discovery
7. Tenant applications
8. Owner application review
9. Tenancy views
10. Notifications

Step 25: Git and Pull Request Workflow
Branch Creation
Always create branches from updated main:

git switch main
git pull origin main
git switch -c feature/branch-name

Example Branches

feature/2-domain-model
feature/3-persistence-foundation
feature/4-identity
feature/5-authorization
feature/6-property-management
feature/7-unit-management
feature/8-property-discovery
feature/9-applications
feature/10-tenancy-lifecycle

Commit Style
Use focused commits:

feat: add core rental domain entities
feat: configure rental persistence model
chore: add initial rental database migration
feat: add identity roles and account activation
feat: enforce role and ownership authorization
feat: add owner property management
feat: add public property discovery
feat: add tenant rental applications
feat: add application approval and tenancy lifecycle
test: add core rental workflow tests

Pull Request Process
Create a feature branch.
Implement one focused change.
Run the relevant tests.
Run dotnet build.
Commit the changes.
Push the branch.
Open a pull request into main.
Ask your collaborator to review it.
Address review comments.
Merge only after approval and passing checks.
Delete the remote branch.
Update local main.
Synchronize After Merge

git switch main
git pull origin main

Create the next branch:

git switch -c feature/next-feature
