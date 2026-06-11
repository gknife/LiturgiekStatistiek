export const environment = {
  production: true,
  apiUrl: 'https://ca-liturgiek-api.happypond-fc71b176.westeurope.azurecontainerapps.io/api',
  msalConfig: {
    auth: {
      clientId: 'ee00b27d-ca8e-4c09-aff2-5d632f42691e',        // From App Registration
      authority: 'https://login.microsoftonline.com/ec105fef-67f8-4b7a-b298-3a50eb06538c',
      redirectUri: 'https://gentle-plant-025c0e503.7.azurestaticapps.net',
    },
    scopes: ['api://ee00b27d-ca8e-4c09-aff2-5d632f42691e/access'],  // Use same Client ID as above
  },
};
