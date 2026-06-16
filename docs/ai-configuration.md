# Azure OpenAI Configuration

The AI features (natural-language Dutch queries and paste-to-parse liturgy import) use
Azure OpenAI. They are **optional**: if not configured, the rest of the application
works normally and the UI shows a clear "AI niet geconfigureerd" banner.

## Required settings

All **three** must be present:

| Setting | Description |
|---------|-------------|
| `AzureOpenAI:Endpoint` | e.g. `https://<resource>.openai.azure.com/` |
| `AzureOpenAI:ApiKey` | A key from the Azure OpenAI resource |
| `AzureOpenAI:DeploymentName` | The **deployment** name (not the model name), e.g. `gpt-4o-mini` |

> Common mistake: setting `DeploymentName` to the model id while the Azure deployment is
> named differently. The deployment name must match exactly what is shown under
> **Deployments** in your Azure OpenAI resource.

## Configure with User Secrets (development)

```bash
cd src/backend/LiturgiekStatistiek.Api
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
```

Environment variables also work (e.g. `AzureOpenAI__Endpoint`), which is the recommended
approach in container/production environments.

## Checking the status

`LlmService` logs at startup exactly which value is missing, and exposes a status
endpoint:

```
GET /api/queries/ai-status
```

Response (`LlmStatus`, camelCased in JSON):

```json
{
  "isConfigured": false,
  "hasEndpoint": false,
  "hasApiKey": true,
  "deploymentName": "gpt-4o-mini",
  "message": "Azure OpenAI is niet geconfigureerd. Ontbrekend: Endpoint. Stel deze in via user-secrets of omgevingsvariabelen."
}
```

The frontend reads this and displays the message as a non-blocking warning banner on the
search page.

## Troubleshooting

- **`isConfigured: false` but key is set** — you are likely missing `Endpoint` and/or
  `DeploymentName`. Both are required in addition to the key.
- **401 / authentication errors at call time** — the key or endpoint is wrong, or the key
  is from a different resource than the endpoint.
- **404 / deployment not found** — `DeploymentName` does not match an existing deployment.
- **Model mismatch** — ensure the deployment uses the intended model (`gpt-4o-mini`);
  an earlier default referenced `gpt-4.1-mini` which does not match the documented setup.
