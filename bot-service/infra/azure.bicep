@maxLength(20)
@minLength(4)
@description('Used to generate names for all resources in this file')
param resourceBaseName string

param webAppSKU string

@maxLength(42)
param botDisplayName string

param botId string
param tenantId string
@secure()
param botPassword string
param apiServerUrl string
param frontEndUrl string

param serverfarmsName string = resourceBaseName
param webAppName string = resourceBaseName
param location string = resourceGroup().location

// resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
//   location: location
//   name: identityName
// }

// Compute resources for your Web App
resource serverfarm 'Microsoft.Web/serverfarms@2021-02-01' = {
  kind: 'app'
  location: location
  name: serverfarmsName
  sku: {
    name: webAppSKU
  }
}

// Web App that hosts your bot
resource webApp 'Microsoft.Web/sites@2021-02-01' = {
  kind: 'app'
  location: location
  name: webAppName
  properties: {
    serverFarmId: serverfarm.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'BOT_ID'
          value: botId
        }
        {
          name: 'BOT_TENANT_ID'
          value: tenantId
        }
        {
          name: 'BOT_PASSWORD'
          value: botPassword
        }
        {
          name: 'BOT_TYPE'
          value: 'MultiTenant'
        }
        {
          name: 'API_SERVER_URL'
          value: apiServerUrl
        }
        {
          name: 'FRONTEND_URL'
          value: frontEndUrl
        }
      ]
      ftpsState: 'FtpsOnly'
    }
  }
}

// Register your web service as a bot with the Bot Framework
module azureBotRegistration './botRegistration/azurebot.bicep' = {
  name: 'Azure-Bot-registration'
  params: {
    resourceBaseName: resourceBaseName
    clientId: botId
    botAppDomain: webApp.properties.defaultHostName
    botDisplayName: botDisplayName
  }
}

// The output will be persisted in .env.{envName}. Visit https://aka.ms/teamsfx-actions/arm-deploy for more details.
output BOT_AZURE_APP_SERVICE_RESOURCE_ID string = webApp.id
output BOT_DOMAIN string = webApp.properties.defaultHostName
