# 🌐 Azure Portal Setup Guide (No CLI Required)

Since your Azure CLI is having issues, let's set up everything through the Azure Portal web interface. This is actually easier and more visual!

## Step 1: Login to Azure Portal

1. Go to **https://portal.azure.com**
2. Sign in with your Microsoft account
3. If you don't have one, create a free account (gets $200 credit)

## Step 2: Create Resource Group

1. Click **"Resource groups"** in the left menu
2. Click **"+ Create"**
3. Fill in:
   - **Subscription**: Your subscription
   - **Resource group name**: `onetime-resources`
   - **Region**: `East US`
4. Click **"Review + create"** → **"Create"**

## Step 3: Create App Service (for your API)

1. Click **"Create a resource"** (+ icon)
2. Search for **"Web App"**
3. Click **"Create"**
4. Fill in:
   - **Resource Group**: `onetime-resources`
   - **Name**: `onetime-api-[your-initials]` (must be globally unique)
   - **Runtime stack**: `.NET 8 (LTS)`
   - **Operating System**: `Linux`
   - **Region**: `East US`
   - **Pricing plan**: `Basic B1` (or Free F1 for testing)
5. Click **"Review + create"** → **"Create"**

**Save the URL**: `https://onetime-api-[your-initials].azurewebsites.net`

## Step 4: Create SQL Database

1. Click **"Create a resource"**
2. Search for **"SQL Database"**
3. Click **"Create"**
4. Fill in:
   - **Resource Group**: `onetime-resources`
   - **Database name**: `OneTimeDb`
   - **Server**: Click "Create new"
     - **Server name**: `onetime-sql-[your-initials]`
     - **Admin login**: `onetimeadmin`
     - **Password**: `OneTime123!@#`
     - **Location**: `East US`
   - **Compute + storage**: `Basic` (5 DTU, 2GB)
5. Click **"Review + create"** → **"Create"**

**Save the connection string**: `Server=tcp:onetime-sql-[your-initials].database.windows.net,1433;Initial Catalog=OneTimeDb;User ID=onetimeadmin;Password=OneTime123!@#;`

## Step 5: Create Storage Account (for photos)

1. Click **"Create a resource"**
2. Search for **"Storage account"**
3. Click **"Create"**
4. Fill in:
   - **Resource Group**: `onetime-resources`
   - **Storage account name**: `onetimestorage[random-numbers]` (must be unique)
   - **Region**: `East US`
   - **Performance**: `Standard`
   - **Redundancy**: `Locally-redundant storage (LRS)`
5. Click **"Review + create"** → **"Create"**

**After creation:**
1. Go to the storage account
2. Click **"Containers"** in the left menu
3. Create these containers (click **"+ Container"**):
   - `profile-photos` (Public access level: Blob)
   - `message-media` (Public access level: Blob)
   - `verification-photos` (Public access level: Blob)

## Step 6: Create SignalR Service (for real-time messaging)

1. Click **"Create a resource"**
2. Search for **"SignalR Service"**
3. Click **"Create"**
4. Fill in:
   - **Resource Group**: `onetime-resources`
   - **Resource name**: `onetime-signalr`
   - **Region**: `East US`
   - **Pricing tier**: `Standard`
5. Click **"Review + create"** → **"Create"**

## Step 7: Setup Database Schema

1. Go to your SQL Database in the portal
2. Click **"Query editor (preview)"**
3. Login with:
   - **SQL server authentication**
   - **Login**: `onetimeadmin`
   - **Password**: `OneTime123!@#`
4. Copy the content from `/OneTime-Backend/OneTime.API/Migrations/001_InitialCreate.sql`
5. Paste it in the query editor
6. Click **"Run"**

## Step 8: Update Your iOS App Configuration

Update your iOS app's `APIConfiguration.swift`:

```swift
struct APIConfiguration {
    // Replace with your actual App Service URL
    static let baseURL = "https://onetime-api-[your-initials].azurewebsites.net"
    
    // Your other endpoints stay the same...
}
```

## Step 9: Simple Deployment (Alternative to Scripts)

### Option A: Deploy via Visual Studio Code

1. Install **Azure App Service extension** in VS Code
2. Right-click your `OneTime.API` folder
3. Select **"Deploy to Web App"**
4. Choose your `onetime-api-[your-initials]` app
5. VS Code will build and deploy automatically

### Option B: Deploy via GitHub Actions

1. Push your code to GitHub
2. In Azure Portal, go to your App Service
3. Click **"Deployment Center"**
4. Choose **"GitHub"** as source
5. Connect your repository
6. Azure will auto-deploy on every push

### Option C: Deploy via ZIP file

1. Build your project:
```bash
cd OneTime.API
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deployment.zip .
cd ..
```

2. In Azure Portal, go to your App Service
3. Click **"Advanced Tools"** → **"Go"**
4. Click **"Tools"** → **"Zip Push Deploy"**
5. Drag your `deployment.zip` file

## Step 10: Configure App Settings

1. Go to your App Service in Azure Portal
2. Click **"Configuration"** in the left menu
3. Add these **Application settings**:

```
ASPNETCORE_ENVIRONMENT = Production
JWT__Key = ThisIsAVerySecureKeyThatIsAtLeast32CharactersLong!
JWT__Issuer = OneTimeAPI
JWT__Audience = OneTimeApp
```

4. Add these **Connection strings**:

```
DefaultConnection = [Your SQL connection string from Step 4]
AzureStorage = [Your storage connection string from Step 5]
AzureSignalR = [Your SignalR connection string from Step 6]
```

5. Click **"Save"**

## Step 11: Test Your Setup

1. Go to: `https://onetime-api-[your-initials].azurewebsites.net/health`
2. You should see: `{"status":"Healthy",...}`
3. Go to: `https://onetime-api-[your-initials].azurewebsites.net/swagger`
4. You should see the API documentation

## 🎉 You're Done!

Your backend is now running on Azure! 

**What you have:**
- ✅ API running on Azure App Service
- ✅ SQL Database with all tables
- ✅ Storage for photos and media
- ✅ SignalR for real-time messaging
- ✅ Ready for iOS app integration

**Total estimated cost**: ~$30-50/month for development

**Next steps:**
1. Update your iOS app with the new API URL
2. Test the API endpoints
3. Add push notifications (optional for now)
4. Deploy to App Store when ready!

This approach is actually more reliable than the CLI scripts and gives you better visibility into what's being created. 🚀