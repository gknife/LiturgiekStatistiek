@description('Resource name prefix')
param name string

@description('Location')
param location string

@description('Monthly budget in EUR')
param monthlyBudget int

@description('Alert email')
param alertEmail string

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${name}-workspace'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${name}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource budget 'Microsoft.Consumption/budgets@2023-03-01' = {
  name: '${name}-budget'
  properties: {
    category: 'Cost'
    amount: monthlyBudget
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: '2026-06-01'
      endDate: '2027-06-01'
    }
    notifications: {
      alert50: {
        enabled: true
        threshold: 50
        operator: 'GreaterThan'
        contactEmails: [alertEmail]
        thresholdType: 'Actual'
      }
      alert80: {
        enabled: true
        threshold: 80
        operator: 'GreaterThan'
        contactEmails: [alertEmail]
        thresholdType: 'Actual'
      }
      alert100: {
        enabled: true
        threshold: 100
        operator: 'GreaterThan'
        contactEmails: [alertEmail]
        thresholdType: 'Actual'
      }
    }
  }
}

output appInsightsConnectionString string = appInsights.properties.ConnectionString
