targetScope = 'resourceGroup'

@description('Short, lowercase project name used for Azure resource names.')
param projectName string = 'captiongen'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Globally unique Azure Container Registry name. Use only lowercase letters and numbers.')
param containerRegistryName string = 'captiongenacr'

@description('Container image name and tag in ACR for the .NET API, for example captiongen-api:abc123.')
param apiImage string = 'captiongen-api:latest'

@description('Container image name and tag in ACR for the Python AI service.')
param aiImage string = 'captiongen-ai:latest'

@description('Container image name and tag in ACR for the React frontend.')
param frontendImage string = 'captiongen-frontend:latest'

@description('PostgreSQL administrator username.')
param postgresAdminLogin string = 'captionadmin'

@secure()
@description('PostgreSQL administrator password.')
param postgresAdminPassword string

@secure()
@description('JWT signing key. Use at least 32 characters.')
param jwtKey string

@secure()
@description('Base64-encoded 32-byte key used to encrypt stored OAuth tokens.')
param tokenEncryptionKey string

@secure()
@description('OpenAI API key used by the Python AI service.')
param openAiApiKey string

@secure()
@description('Optional Stripe secret key.')
param stripeSecretKey string = ''

@secure()
@description('Optional Stripe webhook secret.')
param stripeWebhookSecret string = ''

@description('Stripe publishable key returned to the frontend when payments are enabled.')
param stripePublishableKey string = ''

@description('Optional Stripe Basic price ID.')
param stripeBasicPriceId string = ''

@description('Optional Stripe Freelancer price ID.')
param stripeFreelancerPriceId string = ''

@description('Optional Stripe Influencer price ID.')
param stripeInfluencerPriceId string = ''

@description('Optional Stripe Agency price ID.')
param stripeAgencyPriceId string = ''

var safeProjectName = toLower(replace(projectName, '-', ''))
var unique = uniqueString(resourceGroup().id, projectName)
var acrName = toLower(containerRegistryName)
var logAnalyticsName = '${projectName}-logs-${unique}'
var envName = '${projectName}-env'
var postgresName = '${projectName}-pg-${unique}'
var databaseName = 'captiongen'
var storageName = take('${safeProjectName}st${unique}', 24)
var mediaContainerName = 'captiongen-media'
var apiAppName = '${projectName}-api'
var aiAppName = '${projectName}-ai'
var frontendAppName = '${projectName}-frontend'
var containerPullIdentityName = '${projectName}-acr-pull'
var appDomain = managedEnvironment.properties.defaultDomain
var apiOrigin = 'https://${apiAppName}.${appDomain}'
var aiOrigin = 'https://${aiAppName}.${appDomain}'
var frontendOrigin = 'https://${frontendAppName}.${appDomain}'
var storageKey = listKeys(storage.id, '2023-01-01').keys[0].value
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storageKey};EndpointSuffix=${environment().suffixes.storage}'
var mediaPublicBaseUrl = 'https://${storage.name}.blob.${environment().suffixes.storage}/${mediaContainerName}'
var dbConnectionString = 'Host=${postgres.name}.postgres.database.azure.com;Port=5432;Database=${databaseName};Username=${postgresAdminLogin};Password=${postgresAdminPassword};Ssl Mode=Require;Trust Server Certificate=true'

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

resource containerPullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: containerPullIdentityName
  location: location
}

resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: acr
  name: guid(acr.id, containerPullIdentity.id, 'AcrPull')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: containerPullIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: postgresName
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    version: '16'
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

resource postgresAllowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: postgres
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgres
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storage
  name: 'default'
}

resource mediaContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: mediaContainerName
  properties: {
    publicAccess: 'Blob'
  }
}

resource aiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: aiAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${containerPullIdentity.id}': {}
    }
  }
  dependsOn: [
    acrPullAssignment
  ]
  properties: {
    managedEnvironmentId: managedEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        clientCertificateMode: 'require'
        external: true
        targetPort: 8001
        transport: 'auto'
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: containerPullIdentity.id
        }
      ]
      secrets: [
        {
          name: 'openai-api-key'
          value: openAiApiKey
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'ai'
          image: '${acr.properties.loginServer}/${aiImage}'
          env: [
            {
              name: 'OPENAI_API_KEY'
              secretRef: 'openai-api-key'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${containerPullIdentity.id}': {}
    }
  }
  dependsOn: [
    database
    mediaContainer
    aiApp
    postgresAllowAzure
  ]
  properties: {
    managedEnvironmentId: managedEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        clientCertificateMode: 'require'
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: containerPullIdentity.id
        }
      ]
      secrets: concat([
        {
          name: 'db-connection-string'
          value: dbConnectionString
        }
        {
          name: 'jwt-key'
          value: jwtKey
        }
        {
          name: 'token-encryption-key'
          value: tokenEncryptionKey
        }
        {
          name: 'storage-connection-string'
          value: storageConnectionString
        }
      ], empty(stripeSecretKey) ? [] : [
        {
          name: 'stripe-secret-key'
          value: stripeSecretKey
        }
      ], empty(stripeWebhookSecret) ? [] : [
        {
          name: 'stripe-webhook-secret'
          value: stripeWebhookSecret
        }
      ])
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${acr.properties.loginServer}/${apiImage}'
          env: concat([
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ConnectionStrings__Db'
              secretRef: 'db-connection-string'
            }
            {
              name: 'Database__AutoMigrate'
              value: 'true'
            }
            {
              name: 'Seed__Enabled'
              value: 'false'
            }
            {
              name: 'AiService__BaseUrl'
              value: aiOrigin
            }
            {
              name: 'AiService__HealthPath'
              value: 'health'
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: frontendOrigin
            }
            {
              name: 'MediaStorage__Provider'
              value: 'AzureBlob'
            }
            {
              name: 'MediaStorage__AzureBlob__ConnectionString'
              secretRef: 'storage-connection-string'
            }
            {
              name: 'MediaStorage__AzureBlob__ContainerName'
              value: mediaContainerName
            }
            {
              name: 'MediaStorage__AzureBlob__PublicBaseUrl'
              value: mediaPublicBaseUrl
            }
            {
              name: 'Jwt__Issuer'
              value: 'CaptionGen'
            }
            {
              name: 'Jwt__Audience'
              value: 'CaptionGen'
            }
            {
              name: 'Jwt__Key'
              secretRef: 'jwt-key'
            }
            {
              name: 'TokenEncryption__Key'
              secretRef: 'token-encryption-key'
            }
            {
              name: 'Jwt__AccessMinutes'
              value: '60'
            }
            {
              name: 'Jwt__CookieName'
              value: 'cg_at'
            }
            {
              name: 'Jwt__AllowInsecureCookieOnHttp'
              value: 'false'
            }
            {
              name: 'Stripe__PublishableKey'
              value: stripePublishableKey
            }
            {
              name: 'Stripe__SuccessUrl'
              value: '${frontendOrigin}/checkout/success?session_id={CHECKOUT_SESSION_ID}'
            }
            {
              name: 'Stripe__CancelUrl'
              value: '${frontendOrigin}/checkout/cancel'
            }
            {
              name: 'Stripe__PriceIds__basic'
              value: stripeBasicPriceId
            }
            {
              name: 'Stripe__PriceIds__freelancer'
              value: stripeFreelancerPriceId
            }
            {
              name: 'Stripe__PriceIds__influencer'
              value: stripeInfluencerPriceId
            }
            {
              name: 'Stripe__PriceIds__agency'
              value: stripeAgencyPriceId
            }
          ], empty(stripeSecretKey) ? [] : [
            {
              name: 'Stripe__SecretKey'
              secretRef: 'stripe-secret-key'
            }
          ], empty(stripeWebhookSecret) ? [] : [
            {
              name: 'Stripe__WebhookSecret'
              secretRef: 'stripe-webhook-secret'
            }
          ])
          resources: {
            cpu: json('0.75')
            memory: '1.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

resource frontendApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: frontendAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${containerPullIdentity.id}': {}
    }
  }
  dependsOn: [
    apiApp
  ]
  properties: {
    managedEnvironmentId: managedEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        clientCertificateMode: 'require'
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: containerPullIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'frontend'
          image: '${acr.properties.loginServer}/${frontendImage}'
          env: [
            {
              name: 'API_UPSTREAM'
              value: apiOrigin
            }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

output acrLoginServer string = acr.properties.loginServer
output apiUrl string = apiOrigin
output aiUrl string = aiOrigin
output frontendUrl string = frontendOrigin
output mediaContainerUrl string = mediaPublicBaseUrl
