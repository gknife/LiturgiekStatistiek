export const environment = {
  production: true,
  apiUrl: 'https://api.liturgiekstatistiek.nl/api',
  msalConfig: {
    auth: {
      clientId: '00000000-0000-0000-0000-000000000000',
      authority: 'https://login.microsoftonline.com/common',
      redirectUri: 'https://liturgiekstatistiek.nl',
    },
    scopes: ['api://liturgiek-statistiek/access'],
  },
};
