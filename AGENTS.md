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

Codex runtime environment:

```bash
ASPNETCORE_ENVIRONMENT=Codex
DOTNET_ENVIRONMENT=Codex
ASPNETCORE_URLS=http://127.0.0.1:5194
Signer__RequireCertificate=false
```

## NuGet

Local NuGet packages are stored here:

```bash
/workspace/kbki_api/.codex-nuget
```

NuGet config file:

```bash
/workspace/kbki_api/NuGet.Codex.Config.xml
```

Package restore is performed during the Codex setup or maintenance script.

Do not run restore during the agent phase.

Do not run:

```bash
/workspace/.dotnet/dotnet restore QBCH_api.sln
```

Do not run:

```bash
/workspace/.dotnet/dotnet restore QBCH_api.sln --configfile /workspace/kbki_api/NuGet.Codex.Config.xml
```

If build fails because project assets are missing or broken, report that setup restore did not prepare the environment.

## Build

Main solution:

```bash
QBCH_api.sln
```

Main API project:

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

Verification must be done through build and, when possible, runtime HTTP checks.

## Runtime verification

If API behavior needs to be verified, start the API with:

```bash
ASPNETCORE_ENVIRONMENT=Codex \
DOTNET_ENVIRONMENT=Codex \
ASPNETCORE_URLS=http://127.0.0.1:5194 \
Signer__RequireCertificate=false \
/workspace/.dotnet/dotnet run --project QBCH_api/QBCH_api.csproj --no-build --no-launch-profile > /tmp/qbch_api.log 2>&1 &
```

Then check that the process is running and send HTTP requests with `curl`.

Preferred smoke check:

```bash
curl -i http://127.0.0.1:5194/v2/healthz
```

If the task changes a specific endpoint, verify that endpoint directly with `curl`.

If the API does not start, print the log:

```bash
cat /tmp/qbch_api.log
```

Do not claim that runtime verification passed unless the API actually started and the HTTP request was actually sent.

If the API cannot start because Redis, Kafka, certificates, CryptoPro, internal services, config files, or secrets are unavailable, report the exact error.

## Route verification

When checking a new or changed endpoint, verify the actual controller route before guessing URLs.

Inspect controller attributes such as:

```csharp
[Route(...)]
[ApiVersion(...)]
[MapToApiVersion(...)]
[HttpGet(...)]
[HttpPost(...)]
```

For versioned controllers using:

```csharp
[Route("v{version:apiVersion}")]
```

the route does not include the controller name unless explicitly configured.

## Linux

Linux paths are case-sensitive.

If a project reference uses incorrect filename casing, fix the reference to match the real filename.

Do not rename project files unless explicitly required.

## Code changes

Make minimal changes needed for the task.

Do not change unrelated files.

Do not change Production or Development configuration unless explicitly required.

Codex-specific runtime changes should be isolated to:

```bash
QBCH_api/appsettings.Codex.json
```

or guarded by the `Codex` environment.

## Final response

In the final response, include:

- summary of what changed;
- changed files;
- commands run;
- build result;
- whether the API was started;
- HTTP requests sent;
- HTTP status codes and response bodies;
- what could not be verified;
- confirmation that restore was not run during the agent phase;
- confirmation that unit tests were not run.