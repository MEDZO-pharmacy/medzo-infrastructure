# medzo-infrastructure
Medzo infrastructure, Docker, Kafka, MySQL and observability configuration

## SE3112 unit testing pipeline

After the Unit Testing & Mocking changes are merged into each service repository,
run the complete test suite from this repository:

```powershell
.\scripts\Run-UnitTests.ps1
```

This runs xUnit with Coverlet coverage collection for the four .NET services and
Vitest unit tests for the frontend. Generated .NET coverage reports are placed
in `test-results/unit-coverage/`.

## GitHub Actions automation

The `Medzo Unit Testing Pipeline` workflow checks out the five application
repositories, installs the required SDKs, runs this script, and uploads the
test and coverage evidence as a GitHub Actions artifact.

Before running it, add a repository secret named `MEDZO_REPO_TOKEN` in the
Infrastructure repository. It must be a fine-grained GitHub token with
**Contents: Read** permission for the other private `MEDZO-pharmacy`
repositories. In GitHub, use **Actions → Medzo Unit Testing Pipeline → Run
workflow** to start a live run, then open the job log and download the
`unit-test-evidence` artifact.
