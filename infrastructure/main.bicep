targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (e.g., prod, staging)')
param environmentName string = 'prod'

@description('SQL Server admin login')
@secure()
param sqlAdminLogin string

@description('SQL Server admin password')
@secure()
param sqlAdminPassword string

@description('Azure AD tenant ID for authentication')
param aadTenantId string

@description('Azure AD client ID for the API')
param aadClientId string

var prefix = 'liturgiek'
var uniqueSuffix = uniqueString(resourceGroup().id)

// Container Registry
module acr 'modules/container-registry.bicep' = {
  name: 'deploy-acr'
  params: {
    name: '${prefix}acr${uniqueSuffix}'
    location: location
  }
}

// SQL Database
module sql 'modules/sql-database.bicep' = {
  name: 'deploy-sql'
  params: {
    serverName: '${prefix}-sql-${uniqueSuffix}'
    databaseName: 'LiturgiekStatistiek'
    location: location
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
  }
}

// Container Apps Environment + App
module containerApp 'modules/container-app.bicep' = {
  name: 'deploy-container-app'
  params: {
    name: 'ca-liturgiek-api'
    location: location
    containerRegistryName: acr.outputs.name
    containerRegistryLoginServer: acr.outputs.loginServer
    containerRegistryPassword: acr.outputs.adminPassword
    sqlConnectionString: sql.outputs.connectionString
    aadTenantId: aadTenantId
    aadClientId: aadClientId
  }
}

// Static Web App (frontend)
module staticWebApp 'modules/static-web-app.bicep' = {
  name: 'deploy-static-web-app'
  params: {
    name: '${prefix}-web-${environmentName}'
    location: location
  }
}

// Monitoring
module monitoring 'modules/monitoring.bicep' = {
  name: 'deploy-monitoring'
  params: {
    name: '${prefix}-monitor-${environmentName}'
    location: location
    monthlyBudget: 25
    alertEmail: 'admin@liturgiekstatistiek.nl'
  }
}

output staticWebAppUrl string = staticWebApp.outputs.defaultHostname
output apiUrl string = containerApp.outputs.fqdn
output sqlServerFqdn string = sql.outputs.serverFqdn
