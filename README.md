# ImageApi — Test Assignment (Senior Backend Engineer)

## Task Description

The task implements an image upload and transformation service based on the provided Grip test assignment.

Technology stack:

- ASP.NET Core 8
- GraphQL (HotChocolate)
- PostgreSQL (EF Core)
- Azure Blob Storage
- ImageSharp (image processing)
- xUnit + Moq (unit tests)
- ILogger (logging)
- GitHub Actions (CI/CD)
- Azure Web App (deployment)

---

## Architecture Overview

The solution is built using a clean, modular architecture:

- `Application` — business logic (services, interfaces, background jobs)
- `Domain` — domain entities and value objects
- `Infrastructure` — data persistence layer (EF Core DbContext)
- `GraphQL` — API layer (queries, mutations, error filters)
- `Shared` — DTOs and exception classes
- `Middlewares` — error handling middleware
- `Migrations` — EF Core migrations

The solution uses pure GraphQL API built on top of HotChocolate, with no REST controllers.

---

## Features

- Image uploading with Azure Blob Storage
- Automatic thumbnail generation (predefined 160px height)
- On-demand image variation generation (resizing) by height
- Prevents upscaling (rejects requests where target height > original height)
- Caching of variations after first request for fast response times
- Full CRUD for images (upload, retrieve, delete with all variations)
- Interactive API documentation: instead of Swagger/OpenAPI (which is typical for REST APIs), the solution provides built-in GraphQL Playground (HotChocolate Nitro UI)
- Full unit test coverage for business logic layer
- Full error handling with appropriate GraphQL error codes
- CI/CD pipeline: build, test, migrations, deployment to Azure

---

## Cloud Deployment (Azure)

Deployment is fully automated using GitHub Actions.
- Each push to `main` branch triggers full CI/CD pipeline:
  - Build
  - Run unit tests
  - Apply EF Core database migrations
  - Deploy new build to Azure Web App (Windows instance)
- Deployment artifacts are published using Azure Web Apps Deploy action.
All secrets (connection strings, Azure credentials) are stored securely in GitHub repository secrets.

The project is successfully deployed and running on Azure Web App.
Live URL:
https://imageapi-service-b6grdca0fkfbeaeg.westeurope-01.azurewebsites.net/
Interactive GraphQL Playground available at:
https://imageapi-service-b6grdca0fkfbeaeg.westeurope-01.azurewebsites.net/graphql/

---

## Local Setup

### Prerequisites

- .NET 8 SDK
- PostgreSQL instance (locally or cloud)
- Azure Blob Storage account
- Visual Studio 2022 or Visual Studio Code

### Configuration

Configure database connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=ImageApiDb;Username=your_user;Password=your_password"
}
```
```json
"AzureBlobStorage": {
  "StorageKey": "DefaultEndpointsProtocol=https;AccountName=your_account;AccountKey=your_key;EndpointSuffix=core.windows.net"
}
```

Or use `dotnet user-secrets`.

### Apply migrations

```bash
dotnet ef database update --project ImageApi/ImageApi.csproj
```

### Run application

```bash
dotnet run --project ImageApi
```

---

## Testing

Unit tests implemented via xUnit and Moq:

```bash
dotnet test
```

Tests cover business layer logic and image processing workflows.

---

## GraphQL Playground

Once running locally:

```bash
https://localhost:7125/graphql/
```

Use Playground UI to execute queries and mutations directly.

---

## Usage Examples

### Upload Image (via Postman)

Since file uploads require multipart form-data, you can test image upload via Postman:
- Method: `POST`
- URL: `https://localhost:7125/graphql`
- Body → form-data:

| Key         | Value                                                                |
|-------------|----------------------------------------------------------------------|
| operations  | `{"query": "mutation ($file: Upload!) { uploadImage(file: $file) }",`|
|             | `"variables": {"file": null}}`                                       |
| map         | `{"0": ["variables.file"]}`                                          |
| 0           | File — (select your image file, e.g., `test.jpg`)                    |

### Query Image (Nitro UI)

You can execute the following GraphQL queries via Nitro UI (available at /graphql endpoint):

**Get full image with variations**
```graphql
query {
  image {
    imageWithVariations(id: "<GUID>") {
      id
      url
      createdAt
      variations {
        height
        url
      }
    }
  }
}
```

**Generate new variation on demand**
```graphql
query {
  image {
    imageVariation(id: "<GUID>", height: <HEIGHT>)
  }
}
```

**Check thumbnail generation job status**
```graphql
query {
  thumbnail {
    thumbnailJobStatus(id: "<GUID>") {
      id
      status
      errorMessage
      createdAt
    }
  }
}
```

**Delete image**
```graphql
mutation {
  deleteImage(id: "<GUID>")
}
```

---

## Deployment Pipeline

Full CI/CD pipeline implemented via GitHub Actions:

- Build on each push to `main`
- Full test run
- Automatic EF Core database migrations
- Automatic deployment to Azure Web App

No manual deployment steps are required.

---

## Solution Highlights

- Fully decoupled architecture (Domain-Driven Design principles)
- Async processing for thumbnail generation using background workers
- Clean dependency injection
- Centralized error handling
- Solid test coverage
- Fully automated deployment flow

---

## Challenges & Decisions

- Chose GraphQL (HotChocolate) for flexible API design
- Blob Storage integration to handle large files and scalability
- Decoupled on-demand variations for better performance & caching
- Background processing for thumbnails prevents long-running upload API requests
- Full automation pipeline eliminates human error during deployment

---

## Author

Test task completed by Andrii Iakubovych
