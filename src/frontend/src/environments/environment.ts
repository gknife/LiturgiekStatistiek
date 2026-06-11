export const environment = {
  production: false,
  apiUrl: 'http://localhost:5001/api',
  msalConfig: {
    auth: {
      clientId: '00000000-0000-0000-0000-000000000000',
      authority: 'https://login.microsoftonline.com/common',
      redirectUri: 'http://localhost:4200',
    },
    scopes: ['api://00000000-0000-0000-0000-000000000000/access'],  // Replace with your Client ID
  },
};
