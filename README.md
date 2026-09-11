# medzo-infrastructure

Medzo infrastructure, Docker, Kafka, MySQL and observability configuration.

## API gateway

The single MEDZO YARP gateway lives in `api-gateway/` within this repository.
It is not maintained as a separate `medzo-api-gateway` repository or sibling folder.

From this repository root:

```powershell
dotnet run --project api-gateway/src/Medzo.Gateway
dotnet test api-gateway/Medzo.Gateway.slnx -c Release
```

Use the .NET 10 SDK. Local development listens on http://localhost:5081.
The React frontend's existing Vite proxy on port 5173 points to that gateway.

| Routes | Local destination |
| --- | --- |
| `/api/auth/**`, `/api/users/**`, `/api/dashboard/**`, `/api/reviews`, `/api/contact` | User/auth service on port 5080 |
| `/health` | Gateway liveness; does not test downstream services |

Start the user/auth backend independently. The frontend is served by the separate `medzo-frontend` project.

See [gateway setup and deployment](api-gateway/README.md) for backend commands,
Docker, configuration, and verification. The Docker build context is `api-gateway`:

```powershell
docker build -f api-gateway/docker/Dockerfile -t medzo-api-gateway api-gateway
```

GitHub Actions is in `.github/workflows/gateway-deploy-azure.yml` at this
repository's root. CI covers gateway changes on `main` and `Dev`; deployment
retains its `Dev` branch and `DEPLOY_ENABLED` gate. No deployment is performed
by moving these files. Azure variables, secrets and OIDC federation must refer
to this infrastructure repository before deployment can be enabled.
