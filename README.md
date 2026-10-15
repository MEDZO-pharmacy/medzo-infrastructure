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
