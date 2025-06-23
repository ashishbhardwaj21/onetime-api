#!/bin/bash

# OneTime Dating App - API Deployment Script
# This script deploys your ASP.NET Core API to Azure

set -e  # Exit on any error

# Configuration (update these with your Azure resource names)
RESOURCE_GROUP="onetime-resources"
APP_NAME="onetime-api"
PROJECT_PATH="./OneTime.API"

echo "🚀 Deploying OneTime API to Azure..."

# 1. Load Azure configuration
if [ ! -f "azure-config.json" ]; then
    echo "❌ Azure configuration not found. Please run setup-azure.sh first."
    exit 1
fi

# 2. Build the application
echo "🔨 Building application..."
dotnet publish $PROJECT_PATH \
    --configuration Release \
    --output ./publish \
    --self-contained false \
    --runtime linux-x64

# 3. Create deployment package
echo "📦 Creating deployment package..."
cd publish
zip -r ../deployment.zip .
cd ..

# 4. Deploy to Azure App Service
echo "🌐 Deploying to Azure App Service..."
az webapp deployment source config-zip \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --src deployment.zip

# 5. Configure app settings from Key Vault
echo "⚙️ Configuring application settings..."

# Get Key Vault name from config
KEY_VAULT=$(cat azure-config.json | jq -r '.services.keyVault')

# Set connection strings
az webapp config connection-string set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --connection-string-type SQLAzure \
    --settings DefaultConnection="@Microsoft.KeyVault(VaultName=$KEY_VAULT;SecretName=SqlConnection)"

az webapp config connection-string set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --connection-string-type Custom \
    --settings AzureStorage="@Microsoft.KeyVault(VaultName=$KEY_VAULT;SecretName=StorageConnection)"

az webapp config connection-string set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --connection-string-type Custom \
    --settings AzureSignalR="@Microsoft.KeyVault(VaultName=$KEY_VAULT;SecretName=SignalRConnection)"

az webapp config connection-string set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --connection-string-type Custom \
    --settings Redis="@Microsoft.KeyVault(VaultName=$KEY_VAULT;SecretName=RedisConnection)"

# Set app settings
az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    JWT__Key="@Microsoft.KeyVault(VaultName=$KEY_VAULT;SecretName=JWTKey)" \
    JWT__Issuer="OneTimeAPI" \
    JWT__Audience="OneTimeApp" \
    Azure__CognitiveServices__ComputerVision__SubscriptionKey="@Microsoft.KeyVault(VaultName=$KEY_VAULT;SecretName=CognitiveKey)" \
    Azure__NotificationHubs__ConnectionString="@Microsoft.KeyVault(VaultName=$KEY_VAULT;SecretName=NotificationConnection)"

# 6. Enable system-assigned managed identity
echo "🔐 Configuring managed identity..."
az webapp identity assign \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --query principalId \
    --output tsv)

# Grant Key Vault access
az keyvault set-policy \
    --name $KEY_VAULT \
    --object-id $PRINCIPAL_ID \
    --secret-permissions get list

# 7. Run database migration
echo "🗄️ Running database migration..."
# Note: You'll need to run the migration script manually or set up a deployment slot

# 8. Restart the app
echo "🔄 Restarting application..."
az webapp restart \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME

# 9. Get the application URL
APP_URL=$(az webapp show \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --query defaultHostName \
    --output tsv)

echo "✅ Deployment complete!"
echo ""
echo "🌐 Your API is now available at: https://$APP_URL"
echo "📊 Health check: https://$APP_URL/health"
echo "📖 API documentation: https://$APP_URL/swagger"
echo ""
echo "🔍 To monitor your application:"
echo "az webapp log tail --resource-group $RESOURCE_GROUP --name $APP_NAME"
echo ""
echo "📝 Next steps:"
echo "1. Test your API endpoints"
echo "2. Run database migration if needed"
echo "3. Configure your iOS app with the new API URL"
echo "4. Set up custom domain and SSL if desired"

# Clean up
rm -f deployment.zip
rm -rf publish