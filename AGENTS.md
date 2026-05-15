# AGENTS.md

## Environment

Codex Cloud runs on Linux.

Repository path:

```bash
/workspace/kbki_api
```

Use .NET only by absolute path:

```bash
/workspace/.dotnet/dotnet
```

Do not assume that `dotnet` is available through `PATH`.

## NuGet

Local NuGet packages are stored here:

```bash
/workspace/kbki_api/.codex-nuget
```

NuGet config file:

```bash
/workspace/kbki_api/NuGet.Codex.Config.xml
```

Package restore is performed during the Codex setup script.

Do not run restore during the agent phase.

Do not run:

```bash
/workspace/.dotnet/dotnet restore QBCH_api.sln
```

Do not run:

```bash
/workspace/.dotnet/dotnet restore QBCH_api.sln --configfile /workspace/kbki_api/NuGet.Codex.Config.xml
```

If build fails because project assets are missing or broken, report that the setup restore did not prepare the environment.

## Build

Main project:

```bash
QBCH_api/QBCH_api.csproj
```

After code changes, verify build only with:

```bash
/workspace/.dotnet/dotnet build QBCH_api/QBCH_api.csproj -nologo --no-restore
```

If build fails, inspect the compiler errors, fix the code, and run the same build command again.

Do not claim that build passed unless the command actually completed successfully.

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

## Runtime

Codex Cloud uses this runtime environment:

```bash
ASPNETCORE_ENVIRONMENT=Codex
ASPNETCORE_URLS=http://127.0.0.1:5194
```

If API behavior needs to be verified, start the API with:

```bash
ASPNETCORE_ENVIRONMENT=Codex \
ASPNETCORE_URLS=http://127.0.0.1:5194 \
/workspace/.dotnet/dotnet run --project QBCH_api/QBCH_api.csproj --no-build
```

Then send HTTP requests with `curl`.

Example:

```bash
curl -i http://127.0.0.1:5194/v2/healthz
```

If the API cannot start because Redis, Kafka, certificates, CryptoPro, internal services, config files, or secrets are unavailable, report the exact error.

Do not claim that runtime verification passed unless the API actually started and the HTTP request was actually sent.

## Linux

Linux paths are case-sensitive.

If a project reference uses incorrect filename casing, fix the reference to match the real filename.

Do not rename project files unless explicitly required.

## Final response

In the final response, include:

- what was changed;
- which files were changed;
- which commands were run;
- build result;
- whether the API was started;
- which HTTP requests were sent;
- what could not be verified;
- confirmation that restore was not run during the agent phase;
- confirmation that unit tests were not run.