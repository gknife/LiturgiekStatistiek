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
API will be available at `http://localhost:5001` (Swagger UI at `/swagger`)

The development environment uses an in-memory database with sample data (3 services from Zutphen, Apeldoorn, and Putten) and disabled authentication for easy testing.

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
- `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:DeploymentName` — Azure OpenAI (see below)

### Azure OpenAI (natural language queries & paste-to-parse)

The AI features require **all three** of `Endpoint`, `ApiKey` and `DeploymentName`.
If any is missing the rest of the app keeps working and the UI shows a clear
"AI niet geconfigureerd" banner. Configure via User Secrets (recommended for dev):

```bash
cd src/backend/LiturgiekStatistiek.Api
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
```

`DeploymentName` must match the **deployment** name in your Azure OpenAI resource
(not the model name). The configured state can be checked at
`GET /api/queries/ai-status`. See [`docs/ai-configuration.md`](docs/ai-configuration.md).

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
- **Advanced Query Builder:** Block-based filters over services & songs, group-by aggregates, song-sequence operators (before/after/directly-before/directly-after), multi-query comparison, save/load (authenticated) — see [`docs/advanced-query.md`](docs/advanced-query.md)
- **Diensten (bulk view/edit):** Paginated/filtered grid, public view, inline & multi-select bulk edit for authenticated users, Admin bulk delete, create/edit dialog overlay — see [`docs/bulk-diensten.md`](docs/bulk-diensten.md)
- **Data Entry:** Manual forms with structured liturgy editor, paste-to-parse, URL import, bulk operations — see [`docs/data-entry.md`](docs/data-entry.md)
- **Orde van dienst:** Each service element is chosen from a fixed dropdown of 33 liturgical labels; song elements capture bundle, categorie/rubriek, number and verses
- **Leesdienst:** Reading services carry no voorganger (field disabled on entry, enforced server-side) and are badged in the diensten grid
- **Preektekst (sermon text):** Structured Bible references (book/chapter/verse, book names per translation) plus free-text fallback, queryable
- **Liturgy parsing:** Deterministic rule-based parser for paste & URL import (AI optional); recognises labels, song references and metadata from the title — see [`docs/data-entry.md`](docs/data-entry.md)
- **User settings:** Theme/font/accent preferences persisted per user in the database (extensible JSON blob) with localStorage fallback
- **Recent searches:** Last 10 natural-language questions saved per user; click to re-run or clear
- **Visualization:** Charts (Chart.js/Plotly), maps (Leaflet), tables
- **Export:** Excel, PDF, chart images
- **Song Catalog:** Manageable per bundle (Psalms pre-seeded), with editable per-bundle rubrieken (default rubriek pre-filled) and named verses such as a *Voorzang* before verse 1
- **Predefined Lists:** Configurable abbreviations, denominations, bundles, etc.
- **Authentication:** Microsoft Entra ID (Admin/Researcher roles)
- **Responsive:** Full mobile support
- **Theming:** Light/dark mode, configurable accent colour & font size — see [`docs/theming.md`](docs/theming.md)

## 📚 Technical Documentation

Detailed technical docs live under [`docs/`](docs/):
- [`architecture.md`](docs/architecture.md) — overall architecture & zoneless change detection
- [`advanced-query.md`](docs/advanced-query.md) — advanced query builder design
- [`bulk-diensten.md`](docs/bulk-diensten.md) — bulk view/edit Diensten
- [`theming.md`](docs/theming.md) — theming, logo & favicon
- [`ai-configuration.md`](docs/ai-configuration.md) — Azure OpenAI setup & troubleshooting
- [`data-entry.md`](docs/data-entry.md) — manual entry, orde-van-dienst dropdown, Preektekst editor, paste & URL parsing
- [`release-notes.md`](docs/release-notes.md) — user-facing release notes
- [`features/`](docs/features/) — per-feature delivery docs (brief, plan, review & QA, retro)

## 📝 License

MIT

## 🤝 Contributing

Contributions are welcome! Please open an issue or submit a pull request.
