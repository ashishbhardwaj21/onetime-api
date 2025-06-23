# 🤖 Azure Copilot Prompt for OneTime Dating App

## Copy and paste this EXACT prompt into Azure Copilot:

```
I need to create a complete Azure infrastructure for a dating app called "OneTime" built with ASP.NET Core 8.0. Please create and configure all the following resources in the East US region within a resource group called "onetime-resources":

1. **App Service Plan** (Basic B1) and **Web App** named "onetime-api-[random]" for hosting a .NET 8.0 API with:
   - Runtime: .NET 8 (LTS)
   - Operating System: Linux
   - Enable Application Insights
   - Configure for production deployment

2. **SQL Server** named "onetime-sql-[random]" and **SQL Database** named "OneTimeDb" with:
   - Admin username: onetimeadmin
   - Basic pricing tier
   - Enable firewall rule for Azure services
   - Connection string configured for Entity Framework

3. **Storage Account** named "onetimestorage[random]" with:
   - Standard performance, LRS redundancy
   - Create blob containers: "profile-photos", "message-media", "verification-photos"
   - Enable public blob access for these containers
   - Configure CORS for web access

4. **SignalR Service** named "onetime-signalr" with:
   - Standard tier
   - Configure for real-time messaging

5. **Azure Cache for Redis** named "onetime-redis" with:
   - Basic tier, C0 size
   - Configure for session and data caching

6. **Notification Hub Namespace** named "onetime-notifications" with:
   - Standard tier
   - Create notification hub named "onetime-hub"
   - Ready for iOS and Android push notifications

7. **Cognitive Services** multi-service account named "onetime-cognitive" with:
   - Standard S0 tier
   - For content moderation and AI features

8. **Key Vault** named "onetime-keyvault-[random]" with:
   - Store all connection strings and secrets securely
   - Configure access for the web app

9. **Application Insights** for monitoring and analytics

Please also:
- Configure all connection strings and app settings for the web app
- Set up managed identity for secure access to Key Vault
- Enable diagnostic logging
- Configure health checks endpoint
- Set up proper CORS policies
- Create deployment slots for staging/production

After creation, provide me with:
- All connection strings
- API endpoint URLs
- Configuration steps for the ASP.NET Core app
- Cost estimation for monthly usage

This is for a production-ready dating app that needs to handle real-time messaging, file uploads, push notifications, and user authentication with JWT tokens.
```

## Alternative Shorter Prompt (if the above is too long):

```
Create Azure infrastructure for a .NET 8.0 dating app in East US region:
- Resource group: "onetime-resources"  
- Web App (Basic B1) for .NET 8 API
- SQL Database (Basic tier) 
- Storage Account with blob containers for photos
- SignalR Service for real-time messaging
- Redis Cache for performance
- Notification Hub for push notifications
- Key Vault for secrets
- Application Insights for monitoring

Configure all connection strings, enable managed identity, and provide deployment instructions.
```

## What Azure Copilot Will Do Automatically:

✅ Create all 15+ Azure resources
✅ Configure networking and security
✅ Set up connection strings 
✅ Configure authentication and access
✅ Enable monitoring and logging
✅ Provide deployment instructions
✅ Give you cost estimates
✅ Set up best practices automatically

## Expected Response Time: 2-5 minutes

Azure Copilot will:
1. Create all resources
2. Configure them properly
3. Provide you with a deployment guide
4. Give you all the URLs and connection strings you need
5. Show estimated monthly costs

## After Copilot Completes:

1. **Deploy your API** - Copilot will give you instructions
2. **Update iOS app** - Use the provided API URLs
3. **Test everything** - Copilot will provide test URLs
4. **Monitor usage** - Through Application Insights

This approach will save you 90% of the manual work! 🚀