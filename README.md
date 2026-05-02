# task-manager

Task management app with pagination, built using ASP.NET Core and JavaScript.

## Configuration

This repository does not store database credentials or JWT secrets.

### Local development

Use `dotnet user-secrets` for local secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<db>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" --project .\TaskManagerApi\TaskManagerApi.csproj
dotnet user-secrets set "Jwt:Key" "<your-jwt-key>" --project .\TaskManagerApi\TaskManagerApi.csproj
```

Optional JWT overrides:

```powershell
dotnet user-secrets set "Jwt:Issuer" "taskmanager-api" --project .\TaskManagerApi\TaskManagerApi.csproj
dotnet user-secrets set "Jwt:Audience" "taskmanager-client" --project .\TaskManagerApi\TaskManagerApi.csproj
dotnet user-secrets set "Jwt:AccessTokenMinutes" "15" --project .\TaskManagerApi\TaskManagerApi.csproj
dotnet user-secrets set "Jwt:RefreshTokenDays" "7" --project .\TaskManagerApi\TaskManagerApi.csproj
```

You can inspect local secrets with:

```powershell
dotnet user-secrets list --project .\TaskManagerApi\TaskManagerApi.csproj
```

### Azure App Service

Store production values in App Service Configuration, not in the repository.

Recommended keys:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__AccessTokenMinutes`
- `Jwt__RefreshTokenDays`

### Migrations

To add a migration locally:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:Jwt__Key='design_time_jwt_key_for_migrations_12345'
dotnet ef migrations add <MigrationName> --project .\TaskManagerApi\TaskManagerApi.csproj --startup-project .\TaskManagerApi\TaskManagerApi.csproj
```

To apply migrations to the configured database:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
$env:Jwt__Key='design_time_jwt_key_for_migrations_12345'
dotnet ef database update --project .\TaskManagerApi\TaskManagerApi.csproj --startup-project .\TaskManagerApi\TaskManagerApi.csproj
```
