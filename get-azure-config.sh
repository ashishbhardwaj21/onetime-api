#!/bin/bash

# Script to get all connection strings and keys from your Azure resources

echo "🔑 Getting Azure configuration for your resources..."

RESOURCE_GROUP="onetime-resources"
STORAGE_ACCOUNT="myapp7vhw6fhdrelcs"
SQL_SERVER="myapp-sql-7vhw6fhdrelcs"
SIGNALR_SERVICE="myapp-signalr-7vhw6fhdrelcs"
REDIS_CACHE="myapp-redis-7vhw6fhdrelcs"
KEY_VAULT="myapp-7vhw6fhd"
NOTIFICATION_HUB_NAMESPACE="myapp-hub-7vhw6fhdrelcs"
NOTIFICATION_HUB="datingapp-notifications"

echo "📦 Getting Storage Account connection string..."
STORAGE_CONNECTION=$(az storage account show-connection-string \
    --name $STORAGE_ACCOUNT \
    --resource-group $RESOURCE_GROUP \
    --query connectionString \
    --output tsv)

echo "📡 Getting SignalR connection string..."
SIGNALR_CONNECTION=$(az signalr key list \
    --name $SIGNALR_SERVICE \
    --resource-group $RESOURCE_GROUP \
    --query primaryConnectionString \
    --output tsv)

echo "⚡ Getting Redis connection string..."
REDIS_KEY=$(az redis list-keys \
    --name $REDIS_CACHE \
    --resource-group $RESOURCE_GROUP \
    --query primaryKey \
    --output tsv)

REDIS_CONNECTION="$REDIS_CACHE.redis.cache.windows.net:6380,password=$REDIS_KEY,ssl=True,abortConnect=False"

echo "🔔 Getting Notification Hub connection string..."
NOTIFICATION_CONNECTION=$(az notification-hub authorization-rule list-keys \
    --resource-group $RESOURCE_GROUP \
    --namespace-name $NOTIFICATION_HUB_NAMESPACE \
    --notification-hub-name $NOTIFICATION_HUB \
    --name DefaultFullSharedAccessSignature \
    --query primaryConnectionString \
    --output tsv)

echo "📊 Getting Application Insights key..."
INSIGHTS_KEY=$(az monitor app-insights component show \
    --app myapp-insights-7vhw6fhdrelcs \
    --resource-group $RESOURCE_GROUP \
    --query instrumentationKey \
    --output tsv)

echo ""
echo "✅ Configuration retrieved! Here are your connection strings:"
echo ""
echo "🗄️ SQL Connection String:"
echo "Server=$SQL_SERVER.database.windows.net;Database=DatingAppDb;User=onetimedating;Password=onetime@2723;TrustServerCertificate=true;MultipleActiveResultSets=true;"
echo ""
echo "📦 Storage Connection String:"
echo "$STORAGE_CONNECTION"
echo ""
echo "📡 SignalR Connection String:"
echo "$SIGNALR_CONNECTION"
echo ""
echo "⚡ Redis Connection String:"
echo "$REDIS_CONNECTION"
echo ""
echo "🔔 Notification Hub Connection String:"
echo "$NOTIFICATION_CONNECTION"
echo ""
echo "📊 Application Insights Key:"
echo "$INSIGHTS_KEY"
echo ""
echo "🌐 Your API URL:"
echo "https://myapp-api-7vhw6fhdrelcs.azurewebsites.net"
echo ""

# Create production appsettings file
cat > appsettings.Production.json << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=$SQL_SERVER.database.windows.net;Database=DatingAppDb;User=onetimedating;Password=onetime@2723;TrustServerCertificate=true;MultipleActiveResultSets=true;",
    "AzureStorage": "$STORAGE_CONNECTION",
    "AzureSignalR": "$SIGNALR_CONNECTION",
    "Redis": "$REDIS_CONNECTION"
  },
  
  "App": {
    "Name": "OneTime",
    "Version": "1.0.0",
    "Environment": "Production",
    "BaseUrl": "https://myapp-api-7vhw6fhdrelcs.azurewebsites.net",
    "ClientUrl": "https://onetime.app",
    "SupportEmail": "support@onetime.app"
  },

  "Azure": {
    "ResourceGroup": "onetime-resources",
    "Storage": {
      "AccountName": "$STORAGE_ACCOUNT",
      "ContainerNames": {
        "ProfilePhotos": "profile-photos",
        "MessageMedia": "message-media", 
        "VerificationPhotos": "verification-photos"
      }
    },
    "NotificationHubs": {
      "ConnectionString": "$NOTIFICATION_CONNECTION",
      "HubName": "$NOTIFICATION_HUB"
    }
  },

  "Analytics": {
    "ApplicationInsights": {
      "ConnectionString": "InstrumentationKey=$INSIGHTS_KEY"
    }
  },

  "Features": {
    "EnableRegistration": true,
    "EnableGamification": true,
    "EnableAIMatching": false,
    "EnableVideoMessages": true,
    "EnableVoiceMessages": true,
    "EnableTimeBasedMatching": true,
    "EnablePremiumFeatures": true,
    "EnableContentModeration": false,
    "EnableAnalytics": true,
    "MaintenanceMode": false
  }
}
EOF

echo "📝 Created appsettings.Production.json with your Azure configuration!"
echo ""
echo "🚀 Next steps:"
echo "1. Deploy your API to Azure"
echo "2. Update your iOS app with the API URL"
echo "3. Test the endpoints"