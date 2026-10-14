# Run the backend on Linux

Install Git, Docker Engine, and the Docker Compose plugin. The host does not need the .NET SDK.

```bash
git clone https://github.com/matheeshammalshan/multi-tenant-rag-chatbot.git
cd multi-tenant-rag-chatbot
git checkout develop
docker compose up -d --build
docker compose logs -f api
```

Clone the full repository: Compose builds the API from the source and Dockerfile.
First startup applies SQL migrations, downloads `nomic-embed-text`, and initializes
the Qdrant collection. This can take several minutes and needs internet access.
Existing collection dimensions are checked without deleting data. Use a fresh SQL
volume if an older database was created without migrations; see MIGRATIONS.md.

The API is available at http://localhost:5270, Swagger at
http://localhost:5270/swagger, and readiness at http://localhost:5270/health.
The health endpoint becomes available after initialization completes.
This stack runs in Development and binds published ports to the Linux host's
loopback interface. The frontend is separate.

For chat generation, create an ignored local environment file:

```bash
cp .env.example .env
# Edit .env and set GROQ_API_KEY to a new key from your Groq account.
docker compose up -d
```

The API starts without a Groq key, but chat generation requires one. Never commit
the real `.env`. Revoke any previously committed key in your Groq account.
JWT signing keys are randomly generated and stored in the `api_state` volume.
Database data, embedding models, signing keys, and uploaded documents persist
across normal `docker compose down` and subsequent startup. `down -v` deletes them.

To update:

```bash
git pull --ff-only
docker compose up -d --build
```

For Windows/Visual Studio debugging, start only the dependencies:
`docker compose up -d mysql mongodb qdrant ollama mailpit`, then follow RUNNING.md.
Configure JWT:Key through user secrets before running the API outside Compose.
