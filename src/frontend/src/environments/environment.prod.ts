export const environment = {
  production: true,
  apiUrl: 'https://REPLACE_WITH_CONTAINER_APP_FQDN/api',  // e.g. https://ca-liturgiek-api.nicegrass-abc123.westeurope.azurecontainerapps.io/api
  msalConfig: {
    auth: {
      clientId: 'REPLACE_WITH_ENTRA_CLIENT_ID',        // From App Registration
      authority: 'https://login.microsoftonline.com/REPLACE_WITH_TENANT_ID',
      redirectUri: 'https://REPLACE_WITH_SWA_URL',     // e.g. https://liturgiek-web-prod.azurestaticapps.net
    },
    scopes: ['api://REPLACE_WITH_ENTRA_CLIENT_ID/access'],  // Use same Client ID as above
  },
};
