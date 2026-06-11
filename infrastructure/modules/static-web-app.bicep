@description('Static Web App name')
param name string

@description('Location')
param location string

resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: name
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

output defaultHostname string = staticWebApp.properties.defaultHostname
output id string = staticWebApp.id
