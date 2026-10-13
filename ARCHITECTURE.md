# Architecture

The chatbot follows iDraw's root-level .NET solution layout. Open `multi-tenant-chatbot.sln` in Visual Studio or Rider.

```text
multi-tenant-rag-chatbot/
├── Api/
│   ├── Controllers/
│   ├── Filters/
│   ├── Requests/
│   ├── WebSockets/
│   ├── Properties/
│   ├── wwwroot/                  # Existing uploads (when present)
│   ├── Program.cs
│   └── Api.csproj
├── Application/
│   ├── Configuration/           # Mapping and dependency registration
│   ├── DTOs/
│   ├── Interfaces/
│   │   ├── Services/            # Application workflow contracts
│   │   ├── Repositories/        # Persistence and transaction contracts
│   │   ├── Integrations/        # Storage, readers, LLMs, notifications
│   │   └── Observers/           # Document analysis event contracts
│   ├── Observers/
│   ├── Services/                # Application workflows
│   └── Application.csproj
├── Domain/
│   ├── Entities/
│   ├── Enums/
│   └── Domain.csproj
├── Infrastructure/
│   ├── Configuration/           # Dependency registration
│   ├── DocumentReaders/
│   ├── DTOs/                    # Provider and storage data
│   ├── Embeddings/
│   ├── FileStorage/
│   ├── LlmModels/
│   ├── Observers/
│   ├── Persistence/             # EF Core DbContext
│   ├── Repositories/            # SQL and MongoDB adapters
│   └── Infrastructure.csproj
├── tests/Chatbot.RegressionChecks/
└── multi-tenant-chatbot.sln
```

Namespaces match folders, for example `Application.Interfaces.Services`, `Application.Interfaces.Repositories`, `Domain.Entities`, and `Infrastructure.Persistence`. Interfaces use the .NET `I` prefix; concrete implementations use names such as `DocumentService`, `FileService`, and `TextDocumentReader`, without `Impl` suffixes or implementation folders.

| Project | Responsibility | References |
| --- | --- | --- |
| Domain | Entities and model enum | None |
| Application | Workflows, DTOs, mapping, integration interfaces | Domain |
| Infrastructure | EF Core/MySQL, MongoDB, Qdrant, LLM providers, document readers, file storage | Application |
| Api | Controllers, HTTP requests, API key filters, WebSockets, startup | Application, Infrastructure |

Application defines `IApplicationRepository`, `IApplicationTransaction`, `IChatHistoryRepository`, and `IDocumentNotifier`. Infrastructure implements persistence; Api implements WebSocket notifications. Domain and Application do not depend on the external integrations or HTTP layer.

`Application.Configuration.DependencyInjection.AddApplication()` registers workflows. `Infrastructure.Configuration.DependencyInjection.AddInfrastructure()` registers adapters and persistence. `Api/Program.cs` connects these layers, initializes the database, and activates the document observer.

The API project and its existing configuration, launch profiles, and uploaded files move together to `Api`. Routes, request fields, database schema, MongoDB collection, and Qdrant chatbot filters are preserved. HTTP uploads bind to `DocumentRequest`; controllers pass stream-based `DocumentUpload` objects into Application.

## Build and verify

Run from the solution directory:

```powershell
dotnet build multi-tenant-chatbot.sln
dotnet run --project tests/Chatbot.RegressionChecks
dotnet run --project Api
```

The regression executable checks layer dependencies, dependency injection scopes, repeated upload consumption, API key rejection, chatbot scope propagation, and chat history persistence without external services. Starting the API requires the configured MySQL, MongoDB, Qdrant, Ollama, and LLM credentials. Live ingestion and chat integrations require those services for end-to-end verification.

## Adding features

Put entities and core rules in Domain, workflows and interfaces in Application, external implementations in Infrastructure, and HTTP/WebSocket concerns in Api. Infrastructure implements Application interfaces. Domain and Application must never reference Api or Infrastructure.

The layout mirrors iDraw where responsibilities correspond; chatbot-specific integrations retain their own folders. There is no `Api.Shared` project because the chatbot currently has no shared API component requiring a separate assembly. The existing .NET 9 target and package versions are retained.
