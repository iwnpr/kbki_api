# AGENTS.md

## Environment

Codex Cloud runs on Linux.

Use .NET only by absolute path:

```bash
/workspace/.dotnet/dotnet
```

Do not assume that the `dotnet` command is available through `PATH`.

## NuGet

Use only this config file for package restore:

```bash
/workspace/kbki_api/NuGet.Codex.Config.xml
```

Restore command:

```bash
/workspace/.dotnet/dotnet restore QBCH_api.sln --configfile /workspace/kbki_api/NuGet.Codex.Config.xml
```

Do not run restore without `NuGet.Codex.Config.xml`.

## Build

Main project:

```bash
QBCH_api/QBCH_api.csproj
```

Build command:

```bash
/workspace/.dotnet/dotnet build QBCH_api/QBCH_api.csproj -nologo --no-restore
```

After code changes, run:

```bash
/workspace/.dotnet/dotnet restore QBCH_api.sln --configfile /workspace/kbki_api/NuGet.Codex.Config.xml
/workspace/.dotnet/dotnet build QBCH_api/QBCH_api.csproj -nologo --no-restore
```

If the build fails, fix the compilation errors and run the build again.

## Tests

Do not run unit tests.

Do not run:

```bash
dotnet test
```

Do not run:

```bash
/workspace/.dotnet/dotnet test
```

## HTTP verification

If API behavior needs to be verified, start the project and send HTTP requests with `curl`.

Example start command:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://127.0.0.1:5194 \
/workspace/.dotnet/dotnet run --project QBCH_api/QBCH_api.csproj --no-build
```

Example request:

```bash
curl -i http://127.0.0.1:5194/swagger/index.html
```

If the API cannot start because Redis, Kafka, certificates, CryptoPro, internal services, config files, or secrets are unavailable, report the exact error.

## Linux

Linux paths are case-sensitive.

If a project reference uses incorrect filename casing, fix the reference to match the real filename.

## Final response

In the final response, include:

- what was changed;
- which files were changed;
- which commands were run;
- restore result;
- build result;
- which HTTP requests were sent;
- what could not be verified;
- confirmation that unit tests were not run.