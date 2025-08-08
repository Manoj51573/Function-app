
var pfx = json(loadTextContent('../shared-env.json'))

@allowed([
  'dev'
  'tst'
  'uat'
  'prd'
])

param env string
@allowed([
  'aue'
  'aus'
  'auw'
])
param region string = 'aue'
param appServicesName string
param appServicesResourceGroupName string
param applicationInsightKey string
param FromEmail string
param ToEmail string
param SMTPHost string
param IsImpersonationAllowed string
param ReleaseTwo string
param TrelisAccessManagementEmail string
param BaseEformsUrl string

@secure()
param azureSqlConnectionString string

@description('Optional. Tags of the resource.')
var tags = {}

@description('Optional. Location for all resources.')
var location = resourceGroup().location

var functionAppName = '${pfx.clientPrefix}-az-${env}-${region}-fun-eforms'
var appServiceResourceId = resourceId(appServicesResourceGroupName, 'Microsoft.Web/serverfarms', appServicesName)
var storageAccountName = '${pfx.clientPrefix}az${env}${region}${pfx.storageAccountCode}eforms01'
var storageAccountResourceId = resourceId('Microsoft.Storage/storageAccounts', storageAccountName)

resource functionapp_resource 'Microsoft.Web/sites@2021-01-15' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    serverFarmId: appServiceResourceId
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listkeys(storageAccountResourceId, '2021-04-01').keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_VNET_ROUTE_ALL'
          value: '1'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listkeys(storageAccountResourceId, '2021-04-01').keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: '${functionAppName}-${uniqueString(resourceGroup().id)}'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsightKey
        }
        {
          name: 'Environment'
          value: env
        }
        {
          name: 'FromEmail'
          value: FromEmail
        }
        {
          name: 'ToEmail'
          value: ToEmail
        }
        {
          name: 'SMTPHost'
          value: SMTPHost
        }
        {
          name: 'IsImpersonationAllowed'
          value: IsImpersonationAllowed
        }
        {
          name: 'ReleaseTwo'
          value: ReleaseTwo
        }
        {
          name: 'TrelisAccessManagementEmail'
          value: TrelisAccessManagementEmail
        }
        {
          name: 'WEBSITE_TIME_ZONE'
          value: 'W. Australia Standard Time'
        }
        {
          name: 'BaseEformsUrl'
          value: BaseEformsUrl
        }
        {
          name: 'Blob:ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listkeys(storageAccountResourceId, '2021-04-01').keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'Blob:ContainerName'
          value: 'eforms-blob'
        }
      ]
      connectionStrings: [
        {
          name: 'AZURESQL-ConnectionString'
          connectionString: '${azureSqlConnectionString}'
          type: 'Custom'
        }
      ]
    }
  }
} 

