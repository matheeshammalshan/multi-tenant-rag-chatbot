# SQL database migrations

EF Core migrations own the MySQL schema. `Api/Program.cs` no longer creates tables or changes the schema on startup. Apply migrations explicitly before running the API. MongoDB and Qdrant continue to use their existing initialization paths.

The initial migration is `20261013134851_InitialCreate` and lives in `Infrastructure/Migrations`, alongside its designer and model snapshot. It creates Organizations, ChatBots, Analysis, Document, and LlmConfig with their indexes and relationships. Commit migrations and the model snapshot with the entity changes they represent.

## First setup: empty database

From the chatbot solution directory, with MySQL running:

```powershell
dotnet tool restore
dotnet ef database update --project Infrastructure --startup-project Api --context AppDb
```

This creates the schema and records the applied migration in `__EFMigrationsHistory`. Re-running the command applies only outstanding migrations. Start the API afterwards.

If port 3306 is occupied by a locally installed MySQL, choose which server to use. The Compose MySQL container cannot publish that same port while the local server is listening. Do not stop or remove a database that contains data without deciding where that data should live.

## Connection configuration

Runtime and design-time tooling use the same MySQL provider configuration. The default connection matches the local Compose database. To use a different database or credentials, configure `ConnectionStrings:MySql` in Api user secrets:

```powershell
dotnet user-secrets set 'ConnectionStrings:MySql' 'server=localhost;port=3306;user=root;password=YOUR_PASSWORD;database=ragPipeline' --project Api
```

The environment variable `ConnectionStrings__MySql` also works. Use the same environment for the API and migration commands. Design-time commands default to Development when no environment is set, load Api configuration and Development user secrets, then apply environment overrides. Generating migrations does not require running MySQL, MongoDB, Qdrant, Ollama, or the API.

## Existing database created by EnsureCreated

`EnsureCreated()` does not record migration history. Do not apply InitialCreate directly to a populated schema with no migration history: its CREATE TABLE statements will conflict with existing tables. An idempotent migration script does not solve this because it checks migration history, not whether the tables already exist.

Two paths are available:

1. **New database:** change `ConnectionStrings:MySql` to use a new database name, then run `database update`. This preserves the old database, but starts the API with empty SQL data. Any MongoDB/Qdrant records belong to the old SQL chatbot IDs; keep those stores coordinated if you intend to reuse existing data.
2. **Preserve existing data:** back up the database, compare the actual schema against `scripts/migrations.sql`, and baseline InitialCreate only after confirming the schema matches. Compare columns, types, lengths, nullability, auto-increment keys, character sets, foreign keys, delete behavior, and unique/ordinary indexes. Resolve schema differences before recording history.

For a verified matching schema, a DBA can run this SQL against that database to record the baseline without recreating tables:

```sql
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20261013134851_InitialCreate', '9.0.9'
WHERE NOT EXISTS (
    SELECT 1 FROM `__EFMigrationsHistory`
    WHERE `MigrationId` = '20261013134851_InitialCreate'
);
```

Run `database update` afterwards. Recording a baseline without checking the schema would incorrectly tell EF that the migration has already been applied. No baselining or changes to an existing application database were performed by this code change.

## Future schema changes

Change an entity or its EF mapping, then run:

```powershell
dotnet ef migrations add DescribeYourChange --project Infrastructure --startup-project Api --context AppDb --output-dir Migrations
dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project Api --context AppDb
dotnet ef migrations script --idempotent --project Infrastructure --startup-project Api --context AppDb --output scripts/migrations.sql
# Review the generated migration and SQL before applying:
dotnet ef database update --project Infrastructure --startup-project Api --context AppDb
```

`has-pending-model-changes` checks whether the current model differs from the latest migration snapshot; it does not check the target database's migration state. To list migrations and their applied state, use `dotnet ef migrations list --project Infrastructure --startup-project Api --context AppDb` with MySQL running.

## Visual Studio Package Manager Console

The EF Tools package is included. The equivalent commands are:

```powershell
Add-Migration DescribeYourChange -Project Infrastructure -StartupProject Api -Context AppDb -OutputDir Migrations
Update-Database -Project Infrastructure -StartupProject Api -Context AppDb
Script-Migration -Idempotent -Project Infrastructure -StartupProject Api -Context AppDb -Output scripts/migrations.sql
```

## Deployment

Generate and review an idempotent SQL script (as above) or an EF migration bundle, then apply it as a separate deployment step before starting the API. Keep credentials out of committed scripts. MySQL schema changes can implicitly commit, so prepare backups and review destructive operations before deployment.

Microsoft guidance: [Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/), [applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying), and [EnsureCreated limitations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/ensure-created).
