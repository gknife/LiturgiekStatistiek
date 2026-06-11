@description('Name of the container registry')
param name string

@description('Location')
param location string

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

output name string = acr.name
output loginServer string = acr.properties.loginServer
