#!/bin/bash

# OneTime Dating App - Azure Setup Script
# This script creates all necessary Azure resources for the backend

set -e  # Exit on any error

# Configuration variables
RESOURCE_GROUP="onetime-resources"
LOCATION="eastus"
APP_NAME="onetime-api"
STORAGE_ACCOUNT="onetimestorage$(date +%s)"  # Must be globally unique
SQL_SERVER="onetime-sql-server"
SQL_DATABASE="OneTimeDb"
SIGNALR_SERVICE="onetime-signalr"
NOTIFICATION_HUB_NAMESPACE="onetime-notifications"
NOTIFICATION_HUB="onetime-hub"
COGNITIVE_SERVICE="onetime-cognitive"
APP_SERVICE_PLAN="onetime-plan"
KEY_VAULT="onetime-keyvault"
REDIS_CACHE="onetime-redis"

echo "🚀 Starting OneTime Azure Setup..."
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"

# 1. Create Resource Group
echo "📁 Creating Resource Group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# 2. Create Storage Account
echo "💾 Creating Storage Account..."
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2

# Get storage account key
STORAGE_KEY=$(az storage account keys list \
  --resource-group $RESOURCE_GROUP \
  --account-name $STORAGE_ACCOUNT \
  --query '[0].value' \
  --output tsv)

# Create blob containers
echo "📦 Creating Blob Containers..."
az storage container create \
  --name "profile-photos" \
  --account-name $STORAGE_ACCOUNT \
  --account-key $STORAGE_KEY \
  --public-access blob

az storage container create \
  --name "message-media" \
  --account-name $STORAGE_ACCOUNT \
  --account-key $STORAGE_KEY \
  --public-access blob

az storage container create \
  --name "verification-photos" \
  --account-name $STORAGE_ACCOUNT \
  --account-key $STORAGE_KEY \
  --public-access blob

# 3. Create SQL Server and Database
echo "🗄️ Creating SQL Server and Database..."
SQL_ADMIN_PASSWORD="OneTime123!@#"

az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user "onetimeadmin" \
  --admin-password $SQL_ADMIN_PASSWORD

# Allow Azure services to access SQL Server
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Create SQL Database
az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DATABASE \
  --service-objective Basic

# 4. Create SignalR Service
echo "📡 Creating SignalR Service..."
az signalr create \
  --name $SIGNALR_SERVICE \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_S1

# 5. Create Notification Hub
echo "🔔 Creating Notification Hub..."
az notification-hub namespace create \
  --resource-group $RESOURCE_GROUP \
  --name $NOTIFICATION_HUB_NAMESPACE \
  --location $LOCATION \
  --sku Standard

az notification-hub create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NOTIFICATION_HUB_NAMESPACE \
  --name $NOTIFICATION_HUB

# 6. Create Cognitive Services
echo "🧠 Creating Cognitive Services..."
az cognitiveservices account create \
  --name $COGNITIVE_SERVICE \
  --resource-group $RESOURCE_GROUP \
  --kind CognitiveServices \
  --sku S0 \
  --location $LOCATION

# 7. Create Redis Cache
echo "⚡ Creating Redis Cache..."
az redis create \
  --name $REDIS_CACHE \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Basic \
  --vm-size c0

# 8. Create Key Vault
echo "🔐 Creating Key Vault..."
az keyvault create \
  --name $KEY_VAULT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# 9. Create App Service Plan
echo "🖥️ Creating App Service Plan..."
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku B1 \
  --is-linux

# 10. Create Web App
echo "🌐 Creating Web App..."
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:8.0"

# 11. Get connection strings and keys
echo "🔑 Retrieving connection strings and keys..."

# Storage connection string
STORAGE_CONNECTION="DefaultEndpointsProtocol=https;AccountName=$STORAGE_ACCOUNT;AccountKey=$STORAGE_KEY;EndpointSuffix=core.windows.net"

# SQL connection string
SQL_CONNECTION="Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DATABASE;Persist Security Info=False;User ID=onetimeadmin;Password=$SQL_ADMIN_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# SignalR connection string
SIGNALR_CONNECTION=$(az signalr key list \
  --name $SIGNALR_SERVICE \
  --resource-group $RESOURCE_GROUP \
  --query primaryConnectionString \
  --output tsv)

# Notification Hub connection string
NOTIFICATION_CONNECTION=$(az notification-hub authorization-rule list-keys \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NOTIFICATION_HUB_NAMESPACE \
  --notification-hub-name $NOTIFICATION_HUB \
  --name DefaultFullSharedAccessSignature \
  --query primaryConnectionString \
  --output tsv)

# Cognitive Services key
COGNITIVE_KEY=$(az cognitiveservices account keys list \
  --name $COGNITIVE_SERVICE \
  --resource-group $RESOURCE_GROUP \
  --query key1 \
  --output tsv)

# Redis connection string
REDIS_CONNECTION=$(az redis list-keys \
  --name $REDIS_CACHE \
  --resource-group $RESOURCE_GROUP \
  --query primaryKey \
  --output tsv)

# 12. Store secrets in Key Vault
echo "💾 Storing secrets in Key Vault..."
az keyvault secret set --vault-name $KEY_VAULT --name "StorageConnection" --value "$STORAGE_CONNECTION"
az keyvault secret set --vault-name $KEY_VAULT --name "SqlConnection" --value "$SQL_CONNECTION"
az keyvault secret set --vault-name $KEY_VAULT --name "SignalRConnection" --value "$SIGNALR_CONNECTION"
az keyvault secret set --vault-name $KEY_VAULT --name "NotificationConnection" --value "$NOTIFICATION_CONNECTION"
az keyvault secret set --vault-name $KEY_VAULT --name "CognitiveKey" --value "$COGNITIVE_KEY"
az keyvault secret set --vault-name $KEY_VAULT --name "RedisConnection" --value "$REDIS_CONNECTION"

# 13. Generate configuration file
echo "📝 Generating configuration file..."
cat > azure-config.json << EOF
{
  "resourceGroup": "$RESOURCE_GROUP",
  "location": "$LOCATION",
  "services": {
    "webApp": "$APP_NAME",
    "sqlServer": "$SQL_SERVER",
    "sqlDatabase": "$SQL_DATABASE",
    "storageAccount": "$STORAGE_ACCOUNT",
    "signalRService": "$SIGNALR_SERVICE",
    "notificationHub": "$NOTIFICATION_HUB",
    "cognitiveService": "$COGNITIVE_SERVICE",
    "redisCache": "$REDIS_CACHE",
    "keyVault": "$KEY_VAULT"
  },
  "connectionStrings": {
    "storage": "$STORAGE_CONNECTION",
    "sql": "$SQL_CONNECTION",
    "signalR": "$SIGNALR_CONNECTION",
    "notification": "$NOTIFICATION_CONNECTION",
    "redis": "$REDIS_CACHE.redis.cache.windows.net:6380,password=$REDIS_CONNECTION,ssl=True,abortConnect=False"
  },
  "keys": {
    "cognitiveServices": "$COGNITIVE_KEY",
    "storageKey": "$STORAGE_KEY"
  },
  "endpoints": {
    "webApp": "https://$APP_NAME.azurewebsites.net",
    "storage": "https://$STORAGE_ACCOUNT.blob.core.windows.net",
    "cognitive": "https://$LOCATION.api.cognitive.microsoft.com"
  }
}
EOF

echo "✅ Azure setup complete!"
echo ""
echo "📋 SUMMARY:"
echo "Resource Group: $RESOURCE_GROUP"
echo "Web App URL: https://$APP_NAME.azurewebsites.net"
echo "Storage Account: $STORAGE_ACCOUNT"
echo "SQL Server: $SQL_SERVER.database.windows.net"
echo ""
echo "🔑 Configuration saved to: azure-config.json"
echo "📁 All secrets stored in Key Vault: $KEY_VAULT"
echo ""
echo "🚀 Next steps:"
echo "1. Run the database migration script"
echo "2. Deploy your API to the web app"
echo "3. Configure your iOS app with the endpoints"
echo ""
echo "💡 Estimated monthly cost: $50-100 for development/testing"