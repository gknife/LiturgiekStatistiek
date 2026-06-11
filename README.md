# Liturgiek Statistiek

A research platform for studying liturgy and church music across Dutch congregations. Built for academic research in Theology and Liturgy.

## 🏗️ Architecture

```
Angular 21 (SPA) → .NET 10 Web API → Azure SQL Database
      ↓                   ↓
Azure Static Web Apps  Azure Container Apps (Consumption)
```

**Stack:**
- **Frontend:** Angular 21 + Angular Material + Chart.js + Plotly + Leaflet
- **Backend:** .NET 10 + EF Core + Clean Architecture
- **Database:** Azure SQL (Basic tier)
- **Auth:** Microsoft Entra ID
- **AI:** Azure OpenAI GPT-4o-mini (natural language queries, data parsing)
- **Hosting:** Azure Static Web Apps (free) + Azure Container Apps (consumption)
- **CI/CD:** GitHub Actions (auto-deploy on push to main)
- **IaC:** Bicep

## 📁 Project Structure

```
liturgiek-statistiek/
├── src/
│   ├── frontend/          → Angular 21 application
│   └── backend/           → .NET 10 Web API (Clean Architecture)
├── tests/
│   ├── LiturgiekStatistiek.UnitTests/
│   └── LiturgiekStatistiek.IntegrationTests/
├── infrastructure/        → Bicep templates for Azure
├── .github/workflows/     → CI/CD pipelines
├── seed-data/             → Initial data (psalms, lists, examples)
└── docs/                  → Documentation
```

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dot.net/download)
- [Node.js 24+](https://nodejs.org/)
- [Angular CLI](https://angular.dev/): `npm install -g @angular/cli`
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (for deployment)

### Local Development

**Backend:**
```bash
cd src/backend
dotnet restore
dotnet run --project LiturgiekStatistiek.Api
```
API will be available at `https://localhost:7001`

**Frontend:**
```bash
cd src/frontend
npm install
ng serve
```
App will be available at `http://localhost:4200`

### Running Tests

```bash
# Backend unit tests
dotnet test tests/LiturgiekStatistiek.UnitTests

# Backend integration tests
dotnet test tests/LiturgiekStatistiek.IntegrationTests

# Frontend tests
cd src/frontend && npm test
```

## 🔧 Configuration

### Backend (`appsettings.json`)
- `ConnectionStrings:DefaultConnection` — SQL Server connection string
- `AzureAd:*` — Microsoft Entra ID configuration
- `AllowedOrigins` — CORS origins

### Frontend (`environments/environment.ts`)
- `apiUrl` — Backend API URL
- `msalConfig` — Entra ID client configuration

## 🌐 Azure Deployment

### Automated (CI/CD)
Push to `main` branch triggers automatic deployment:
- Frontend → Azure Static Web Apps
- Backend → Azure Container Apps via ACR

### Manual Infrastructure Setup
```bash
az login
az group create --name rg-liturgiek-statistiek --location westeurope
az deployment group create \
  --resource-group rg-liturgiek-statistiek \
  --template-file infrastructure/main.bicep \
  --parameters @infrastructure/parameters/production.bicepparam
```

### Required GitHub Secrets
| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Service principal credentials |
| `ACR_LOGIN_SERVER` | Container Registry login server |
| `ACR_USERNAME` | Container Registry username |
| `ACR_PASSWORD` | Container Registry password |
| `SQL_CONNECTION_STRING` | Azure SQL connection string |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Static Web Apps deploy token |

## 💰 Estimated Monthly Cost

| Service | Cost |
|---------|------|
| Azure SQL (Basic) | ~€5 |
| Container Apps (Consumption) | ~€0-5 |
| Container Registry (Basic) | ~€5 |
| Static Web Apps (Free) | €0 |
| Azure OpenAI (GPT-4o-mini) | ~€1-3 |
| **Total** | **~€11-18/month** |

Budget alert configured at €25/month.

## 📖 Features

- **Query Engine:** Predefined templates, natural language (Dutch), advanced filter builder
- **Data Entry:** Manual forms, paste-to-parse, URL import, bulk operations
- **Visualization:** Charts (Chart.js/Plotly), maps (Leaflet), tables
- **Export:** Excel, PDF, chart images
- **Song Catalog:** Manageable per bundle (Psalms pre-seeded)
- **Predefined Lists:** Configurable abbreviations, denominations, bundles, etc.
- **Authentication:** Microsoft Entra ID (Admin/Researcher roles)
- **Responsive:** Full mobile support
- **Dark Mode:** User-configurable theme

## 📝 License

MIT

## 🤝 Contributing

Contributions are welcome! Please open an issue or submit a pull request.
