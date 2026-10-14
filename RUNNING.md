# Run locally on Windows with Visual Studio

For the full backend in Docker on Linux, follow [LINUX.md](LINUX.md).

Run MySQL, MongoDB, Qdrant, and Ollama in Docker; run the .NET API on Windows for Visual Studio debugging.

## 1. Prerequisites

- Docker Desktop running with Linux containers (WSL 2 backend recommended).
- Visual Studio with ASP.NET/web development tools and support for .NET 9, or the .NET 9 SDK for the CLI.
- A Groq API key for chat generation. Ollama is used for embeddings; the current chat implementations call Groq, not Ollama.

Docker installation: https://docs.docker.com/desktop/setup/install/windows-install/

## 2. Start the dependencies

Open PowerShell in the directory containing `compose.yaml`:

```powershell
Set-Location 'C:\Users\HP\Documents\rag-project\multi-tenant-rag-chatbot'
docker compose up -d --wait mysql mongodb qdrant ollama mailpit
docker compose ps
```

The first run downloads the images. These containers use named volumes, so their data survives container restarts and ordinary `docker compose down`. Fresh volumes start with empty databases; this does not import data from your Linux machine.

| Service | Windows connection | Data |
| --- | --- | --- |
| MySQL | `localhost:3306`, user `root`, password `1234` | `ragPipeline` database |
| MongoDB | `mongodb://localhost:27017/` | `Rag` database, `chatHistory` collection |
| Qdrant HTTP/dashboard | `http://localhost:6333/dashboard` | `rag-pipeline` collection |
| Qdrant gRPC (used by API) | `http://localhost:6334` | Same collection |
| Ollama | `http://localhost:11434` | `nomic-embed-text` model |

The addresses and MySQL credentials match the application's existing local connection settings. Ports are bound to localhost; this Compose file is for local development. Qdrant and Ollama use their latest image tags, so later image pulls can change versions.

If a port is already occupied by an installed database or Ollama, stop that conflicting local service before starting Compose. The current integration URLs are fixed to these ports.

## 3. Initialize embeddings and the vector collection

```powershell
docker compose exec ollama ollama pull nomic-embed-text
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Initialize-VectorDatabase.ps1
```

This downloads the embedding model, obtains an actual embedding to determine its dimension, and creates `rag-pipeline` with cosine distance. Re-running the script preserves existing collection data and checks its dimension. If Qdrant is still starting, wait until its dashboard opens and rerun the script.

MongoDB creates its database/collection on the first chat history write. SQL tables are managed through EF Core migrations. Before starting the API against an empty MySQL database, run:

```powershell
dotnet tool restore
dotnet ef database update --project Infrastructure --startup-project Api --context AppDb
```

If your database already has tables from the earlier `EnsureCreated()` startup, follow [MIGRATIONS.md](MIGRATIONS.md) to baseline the existing schema or use a new database. Do not apply the initial migration to existing tables blindly. The API no longer changes the SQL schema on startup.

## 4. Configure the Groq key

From the same solution directory:

```powershell
dotnet user-secrets set 'Groq:ApiKey' 'YOUR_GROQ_API_KEY' --project Api
```

Alternatively, right-click the **Api** project in Visual Studio, select **Manage User Secrets**, and add:

```json
{
  "Groq:ApiKey": "YOUR_GROQ_API_KEY"
}
```

The key is stored outside the repository. The API loads user secrets in Development. An environment variable named `Groq__ApiKey` also works; start Visual Studio from an environment where that variable is set if using this alternative.

The default chatbot configuration uses GPT-OSS 20B on Groq. A valid key is required, and the selected model must be available to your Groq account. If Groq reports an unavailable model, the provider/model mapping must be updated for a supported model before chat generation will work.

## 5. Run the API

In Visual Studio:

1. Open `multi-tenant-chatbot.sln`.
2. Right-click **Api** and choose **Set as Startup Project**.
3. Select the **http** launch profile and press **F5**.

Or run from PowerShell:

```powershell
dotnet run --project Api --launch-profile http
```

The API listens at `http://localhost:5270`. In Development, Swagger UI is at `http://localhost:5270/swagger` and its OpenAPI document is at `http://localhost:5270/openapi/v1.json`. Visual Studio launch profiles open Swagger automatically. Use Swagger's **Try it out**, Postman, or your HTTP client to create an organization/chatbot, generate a chatbot API key, upload a document, and ask a question. For protected semantic search endpoints, click Swagger's **Authorize** button and enter the chatbot's generated API key; Swagger sends it in the `x-api-key` header. This is separate from your Groq key. Swagger UI and the OpenAPI document are enabled only in Development.

## Stop and diagnose

```powershell
docker compose logs --tail 100
docker compose stop
# Start existing containers again:
docker compose start
# Or remove containers while retaining data volumes:
docker compose down
```

Avoid `docker compose down -v` unless you intend to delete the database and model volumes.

Offline checks (no Docker needed):

```powershell
dotnet build multi-tenant-chatbot.sln
dotnet run --project tests/Chatbot.RegressionChecks
```

Official references:

- MySQL Docker image: https://hub.docker.com/_/mysql/
- MongoDB Docker image: https://hub.docker.com/_/mongo/
- Qdrant setup: https://qdrant.tech/documentation/quickstart/
- Ollama Docker setup: https://docs.ollama.com/docker

## Frontend console

From `frontend`, run `npm.cmd run dev` and open http://localhost:5173. The development server proxies API requests to this API.

The signed-in playground streams through `POST /ChatBots/{id}/stream`; it checks chatbot ownership and CSRF without rotating shared API keys. Guest chat continues to use the API-key-protected `/SemanticSearch/stream` route. Document status is read through the existing document list/details endpoints; WebSocket notifications have been removed.
