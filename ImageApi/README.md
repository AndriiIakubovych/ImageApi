# ImageApi — Test Assignment (Senior Backend Engineer)

## Task Description

The task implements an image upload service with on-demand image variations (resizing), based on provided test assignment PDF.

Technology stack:

- ASP.NET Core 8
- GraphQL (HotChocolate)
- PostgreSQL (EF Core)
- ImageSharp (image processing)
- xUnit + Moq (unit tests)
- InMemory EF (for tests)
- ILogger (logging)
- Docker-ready architecture

---

## Architecture Overview

The solution follows clean separation:

- `Application` — business logic layer (services, interfaces)
- `Domain` — domain entities
- `Infrastructure` — persistence layer (EF Core DbContext)
- `GraphQL` — GraphQL API layer (Query/Mutation definitions)
- `Shared` — DTOs and exceptions

No REST controllers are used — pure GraphQL API based on HotChocolate.

---

## Features

- Image uploading with physical file storage
- Automatic thumbnail generation (predefined 160px height)
- On-demand image variation generation by height (cached after first request)
- Deletion of image with all variations
- Error handling with proper GraphQL error codes
- Full logging of business operations
- Full unit test coverage for business layer

---

## Quick Start (Local)

### Prerequisites

- .NET 8 SDK
- PostgreSQL installed locally
- Visual Studio 2022 or Rider

### Setup

1️. Clone repository  
2️. Configure database connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=ImageApiDb;Username=your_user;Password=your_password"
}
```

3. Apply migrations:

```bash
dotnet ef database update
```

4. Run the project:

```bash
dotnet run --project ImageApi
```

---

## Testing

Unit tests implemented via xUnit:

```bash
dotnet test
```

All tests are fully independent, using embedded resource image for integration tests.

---

## GraphQL Playground

Once running:

https://localhost:7125/graphql/

Use Playground to execute queries and mutations directly.

---

## Deployment

The solution can be easily deployed via Docker or hosted on Azure/AWS with minor configuration adjustments (connection strings, volumes for file storage, etc).

---

## Author

Test task completed by Andrii Iakubovych
This string is added to test CI/CD