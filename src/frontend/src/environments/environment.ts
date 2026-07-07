export const environment = {
  production: false,
  apiUrl: 'http://localhost:5001/api',
  devBypass: true,
  msalConfig: {
    auth: {
      clientId: 'ee00b27d-ca8e-4c09-aff2-5d632f42691e',
      authority: 'https://login.microsoftonline.com/ec105fef-67f8-4b7a-b298-3a50eb06538c',
      redirectUri: 'http://localhost:4200',
    },
    scopes: ['api://ee00b27d-ca8e-4c09-aff2-5d632f42691e/access'],
  },
};
