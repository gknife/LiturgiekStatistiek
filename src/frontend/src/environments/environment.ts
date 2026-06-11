export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api',
  msalConfig: {
    auth: {
      clientId: '00000000-0000-0000-0000-000000000000',
      authority: 'https://login.microsoftonline.com/common',
      redirectUri: 'http://localhost:4200',
    },
    scopes: ['api://liturgiek-statistiek/access'],
  },
};
