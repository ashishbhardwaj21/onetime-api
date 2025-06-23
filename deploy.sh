#!/bin/bash

# Simple deployment script for OneTime API to Azure

echo "🚀 Starting deployment to Azure..."

APP_NAME="myapp-api-7vhw6fhdrelcs"
RESOURCE_GROUP="onetime-resources"

echo "📱 App Name: $APP_NAME"
echo "📦 Resource Group: $RESOURCE_GROUP"

# Check if we're in the right directory
if [ ! -f "OneTime.API/OneTime.API.csproj" ]; then
    echo "❌ Error: OneTime.API.csproj not found. Please run this script from the project root."
    exit 1
fi

echo ""
echo "🔧 Building the application..."

# Create publish directory
mkdir -p ./publish

# Use Docker to build and publish (since local .NET might not be installed)
echo "🐳 Using Docker to build the application..."

docker build -f OneTime.API/Dockerfile -t onetime-api:latest .

if [ $? -ne 0 ]; then
    echo "❌ Docker build failed!"
    exit 1
fi

# Extract the published files from Docker container
echo "📦 Extracting published files..."
docker create --name temp-container onetime-api:latest
docker cp temp-container:/app ./publish/app
docker rm temp-container

echo "✅ Build completed successfully!"

# Create deployment package
echo "📦 Creating deployment package..."
cd publish/app
zip -r ../deployment.zip . > /dev/null 2>&1
cd ../..

echo "✅ Deployment package created: publish/deployment.zip"

echo ""
echo "🌐 Deployment options:"
echo ""
echo "📋 Option 1: Azure Portal Deployment"
echo "   1. Go to https://portal.azure.com"
echo "   2. Navigate to your App Service: $APP_NAME"
echo "   3. Go to 'Advanced Tools' → 'Go'"
echo "   4. Click 'Tools' → 'Zip Push Deploy'"
echo "   5. Drag and drop: publish/deployment.zip"
echo ""
echo "📋 Option 2: GitHub Actions (Recommended)"
echo "   1. Push this code to GitHub"
echo "   2. In Azure Portal, go to your App Service"
echo "   3. Click 'Deployment Center'"
echo "   4. Choose 'GitHub' and connect your repository"
echo "   5. Azure will auto-deploy on every push"
echo ""
echo "📋 Option 3: VS Code Extension"
echo "   1. Install 'Azure App Service' extension in VS Code"
echo "   2. Right-click OneTime.API folder"
echo "   3. Select 'Deploy to Web App'"
echo "   4. Choose your app: $APP_NAME"
echo ""

echo "🔗 Your API will be available at:"
echo "   https://$APP_NAME.azurewebsites.net"
echo ""
echo "🧪 Test endpoints:"
echo "   https://$APP_NAME.azurewebsites.net/health"
echo "   https://$APP_NAME.azurewebsites.net/swagger"
echo ""
echo "✅ Deployment package ready!"