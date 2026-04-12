param location string = resourceGroup().location
param appServicePlanName string = 'your-existing-plan-name'
param appServicePlanResourceGroup string = 'your-existing-plan-resource-group'
param webAppName string = 'taskmanagerapi-${uniqueString(resourceGroup().id)}'

// Reference existing App Service Plan
resource existingPlan 'Microsoft.Web/serverfarms@2023-12-01' existing = {
  name: appServicePlanName
  scope: resourceGroup(appServicePlanResourceGroup)
}

// Create Web App using existing plan
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: existingPlan.id
    httpsOnly: true
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output webAppName string = webApp.name
