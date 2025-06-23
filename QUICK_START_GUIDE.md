# 🚀 OneTime Dating App - Quick Start Guide

## Prerequisites Checklist
- [ ] Azure account (free tier works)
- [ ] Azure CLI installed
- [ ] .NET 8.0 SDK installed
- [ ] Xcode for iOS development
- [ ] Apple Developer account (for push notifications)

## Step-by-Step Execution

### 1. 💳 **Azure Account Setup** (5 minutes)
```bash
# Go to portal.azure.com and create free account
# You get $200 credit for 30 days - more than enough for testing

# Install Azure CLI on macOS
brew install azure-cli

# Login
az login
```

### 2. 🏗️ **Create All Azure Resources** (15 minutes)
```bash
# Navigate to your backend folder
cd /Users/ashishbhardwaj/Documents/Data/OneTime-Backend

# Make the setup script executable
chmod +x setup-azure.sh

# Run the setup (this creates everything automatically)
./setup-azure.sh
```

**What this creates:**
- Resource Group
- SQL Server + Database
- Storage Account (for photos/media)
- SignalR Service (real-time messaging)
- Notification Hub (push notifications)
- Redis Cache (performance)
- Key Vault (secrets)
- Web App Service (API hosting)
- Cognitive Services (AI features)

**Expected output:**
```
✅ Azure setup complete!
📋 SUMMARY:
Resource Group: onetime-resources
Web App URL: https://onetime-api.azurewebsites.net
Storage Account: onetimestorage1234567890
SQL Server: onetime-sql-server.database.windows.net
```

### 3. 🗄️ **Setup Database** (5 minutes)
```bash
# Connect to your Azure SQL Database
# Use the credentials from the setup script output

# Run the migration script
sqlcmd -S onetime-sql-server.database.windows.net -d OneTimeDb -U onetimeadmin -P OneTime123!@# -i OneTime.API/Migrations/001_InitialCreate.sql
```

**Alternative: Use Azure Portal**
1. Go to portal.azure.com
2. Find your SQL Database "OneTimeDb"
3. Click "Query editor"
4. Login with: onetimeadmin / OneTime123!@#
5. Copy-paste the content from `OneTime.API/Migrations/001_InitialCreate.sql`
6. Run the script

### 4. 🚀 **Deploy Your API** (10 minutes)
```bash
# Make sure you have .NET 8.0 installed
dotnet --version

# Make deploy script executable
chmod +x deploy-api.sh

# Deploy your API to Azure
./deploy-api.sh
```

**Expected output:**
```
✅ Deployment complete!
🌐 Your API is now available at: https://onetime-api.azurewebsites.net
📊 Health check: https://onetime-api.azurewebsites.net/health
📖 API documentation: https://onetime-api.azurewebsites.net/swagger
```

### 5. ✅ **Test Your API** (5 minutes)
```bash
# Test health endpoint
curl https://onetime-api.azurewebsites.net/health

# Should return:
# {"status":"Healthy","timestamp":"2024-01-01T00:00:00Z",...}
```

**Open in browser:**
- Health Check: `https://onetime-api.azurewebsites.net/health`
- API Docs: `https://onetime-api.azurewebsites.net/swagger`

### 6. 📱 **Connect iOS App** (15 minutes)

1. **Update API Configuration in iOS:**
```swift
// In your iOS project, update APIConfiguration.swift
static let baseURL = "https://onetime-api.azurewebsites.net"
```

2. **Add required iOS packages:**
```
// Add via Swift Package Manager:
- SignalR Client: https://github.com/moozzyk/SignalR-Client-Swift
```

3. **Test iOS Integration:**
```swift
// Use the APITestView from configure-ios-integration.md
// This will test all your API endpoints
```

### 7. 🔔 **Setup Push Notifications** (10 minutes)

1. **In Apple Developer Console:**
   - Create App ID
   - Enable Push Notifications
   - Download certificates

2. **In Azure Portal:**
   - Go to your Notification Hub
   - Add Apple (APNS) configuration
   - Upload your certificates

3. **Test push notifications from Azure**

## 📊 **Cost Estimation**

**Monthly costs for development/testing:**
- App Service (B1): ~$13/month
- SQL Database (Basic): ~$5/month
- Storage Account: ~$1/month
- SignalR Service: ~$1/month
- Notification Hub: ~$1/month
- Redis Cache: ~$15/month
- Cognitive Services: ~$0 (free tier)

**Total: ~$35-50/month for development**

## 🔧 **Troubleshooting**

### Common Issues:

1. **"dotnet command not found"**
```bash
# Install .NET 8.0 SDK from microsoft.com/net/download
```

2. **"Azure CLI not found"**
```bash
brew install azure-cli
```

3. **Database connection issues**
```bash
# Check firewall rules in Azure Portal
# SQL Server → Firewalls and virtual networks
# Add your IP address
```

4. **API deployment fails**
```bash
# Check logs
az webapp log tail --resource-group onetime-resources --name onetime-api
```

5. **iOS app can't connect**
```swift
// Make sure you're using HTTPS URL
// Check Info.plist for network security settings
```

## 🎯 **What You'll Have After This**

✅ **Complete backend API** running on Azure
✅ **Database** with all tables and relationships
✅ **Real-time messaging** via SignalR
✅ **File storage** for photos and media
✅ **Push notifications** ready
✅ **AI services** for matching and moderation
✅ **Authentication** with JWT tokens
✅ **Monitoring** and health checks

## 📱 **iOS App Integration Status**

After completing these steps, your iOS app will have:
- ✅ User registration and login
- ✅ Profile management with photo uploads
- ✅ Swiping and matching functionality
- ✅ Real-time messaging
- ✅ Push notifications
- ✅ Gamification system
- ✅ Premium features

## 🚀 **Ready for Production!**

Once you complete these steps, you'll have a fully functional dating app backend ready for:
- App Store submission
- Real user testing
- Scaling to thousands of users
- Adding advanced features

**Total setup time: ~1 hour** 🕐

---

**Need help?** Check the detailed guides:
- `configure-ios-integration.md` - Complete iOS setup
- `README.md` - Full documentation
- `DEPLOYMENT_SUMMARY.md` - What was built

**Happy coding! 🎉**