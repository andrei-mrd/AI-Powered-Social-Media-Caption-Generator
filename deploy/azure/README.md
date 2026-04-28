# Azure deployment

This deployment uses:

- Azure Container Registry for the three images.
- Azure Container Apps for the React frontend, .NET API, and Python AI service.
- Azure Database for PostgreSQL Flexible Server for the application database.
- Azure Blob Storage for uploaded images and video clips.
- Log Analytics for Container Apps logs.

## Required GitHub secrets

Set these repository secrets before running the workflow:

- `AZURE_CREDENTIALS`: JSON from an Azure service principal with contributor access.
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION`, for example `westeurope`.
- `AZURE_PROJECT_NAME`, for example `captiongen`.
- `AZURE_ACR_NAME`, globally unique, lowercase letters and numbers only.
- `POSTGRES_ADMIN_PASSWORD`
- `JWT_KEY`, at least 32 characters.
- `OPENAI_API_KEY`
- `SONAR_TOKEN`
- `SONAR_HOST_URL`
- `SONAR_ORGANIZATION`, your SonarCloud organization key.

Optional secrets:

- `STRIPE_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET`

Optional variables:

- `STRIPE_PUBLISHABLE_KEY`
- `STRIPE_BASIC_PRICE_ID`
- `STRIPE_FREELANCER_PRICE_ID`
- `STRIPE_INFLUENCER_PRICE_ID`
- `STRIPE_AGENCY_PRICE_ID`

## Manual deployment

Create the resource group and ACR first:

```bash
az group create --name rg-captiongen --location westeurope
az acr create --resource-group rg-captiongen --name captiongenacr123 --sku Basic --admin-enabled true
```

Build and push images:

```bash
az acr login --name captiongenacr123
docker build -f src/CaptionGen.Api/Dockerfile -t captiongenacr123.azurecr.io/captiongen-api:latest .
docker build -f ai-service/Dockerfile -t captiongenacr123.azurecr.io/captiongen-ai:latest ai-service
docker build -f src/frontend/Dockerfile \
  --build-arg VITE_STRIPE_PUBLISHABLE_KEY='<publishable-key-if-used>' \
  -t captiongenacr123.azurecr.io/captiongen-frontend:latest .
docker push captiongenacr123.azurecr.io/captiongen-api:latest
docker push captiongenacr123.azurecr.io/captiongen-ai:latest
docker push captiongenacr123.azurecr.io/captiongen-frontend:latest
```

Deploy resources:

```bash
az deployment group create \
  --resource-group rg-captiongen \
  --template-file deploy/azure/main.bicep \
  --parameters projectName=captiongen containerRegistryName=captiongenacr123 \
  --parameters apiImage=captiongen-api:latest aiImage=captiongen-ai:latest frontendImage=captiongen-frontend:latest \
  --parameters postgresAdminPassword='<strong-password>' jwtKey='<32-char-min-key>' openAiApiKey='<openai-key>'
```

The deployment outputs the public frontend, API, AI, and media container URLs.
