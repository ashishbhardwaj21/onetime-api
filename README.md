# OneTime Dating App - Backend API

A comprehensive ASP.NET Core 8.0 Web API for the OneTime dating application, featuring real-time messaging, advanced matching algorithms, gamification, and Azure cloud integration.

## 🏗️ Architecture

- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server with Entity Framework Core
- **Caching**: Redis
- **Real-time Communication**: SignalR
- **Cloud Services**: Azure (Blob Storage, Notification Hubs, Cognitive Services)
- **Authentication**: JWT with ASP.NET Core Identity
- **API Documentation**: Swagger/OpenAPI

## 🚀 Features

### Core Features
- **User Authentication & Authorization**: JWT-based auth with 2FA support
- **Profile Management**: Photo uploads, interests, preferences
- **Advanced Matching**: AI-powered compatibility scoring with location filtering
- **Real-time Messaging**: Text, voice, video, and media messages with SignalR
- **Gamification System**: XP, badges, achievements, leaderboards
- **Premium Subscriptions**: Stripe integration for premium features
- **Push Notifications**: Azure Notification Hubs integration
- **Content Moderation**: Azure Cognitive Services integration
- **Analytics & Monitoring**: Application Insights and custom metrics

### API Endpoints

#### Authentication (`/api/auth`)
- `POST /signup` - User registration
- `POST /signin` - User login
- `POST /refresh` - Refresh JWT token
- `POST /forgot-password` - Password reset request
- `POST /reset-password` - Reset password
- `POST /verify-email` - Email verification
- `POST /2fa/setup` - Setup two-factor authentication
- `POST /2fa/verify` - Verify 2FA code

#### User Management (`/api/user`)
- `GET /profile` - Get user profile
- `PUT /profile` - Update profile
- `POST /photos` - Upload photos
- `DELETE /photos/{id}` - Delete photo
- `PUT /location` - Update location
- `POST /verification/phone` - Phone verification
- `POST /verification/photo` - Photo verification
- `GET /interests` - Get available interests
- `PUT /interests` - Update user interests
- `POST /premium/subscribe` - Subscribe to premium
- `DELETE` - Delete account

#### Matching (`/api/matching`)
- `GET /discover` - Discover potential matches
- `POST /like` - Like a profile
- `POST /pass` - Pass on a profile
- `POST /super-like` - Super like a profile
- `GET /matches` - Get user matches
- `DELETE /matches/{id}` - Unmatch
- `GET /preferences` - Get matching preferences
- `PUT /preferences` - Update preferences
- `POST /boost` - Activate boost
- `POST /block` - Block user
- `POST /report` - Report user

#### Messaging (`/api/messaging`)
- `GET /conversations` - Get conversations
- `GET /conversations/{id}/messages` - Get messages
- `POST /conversations/{id}/messages` - Send message
- `PUT /messages/{id}` - Update message
- `DELETE /messages/{id}` - Delete message
- `POST /messages/{id}/read` - Mark as read
- `POST /messages/{id}/reactions` - Add reaction
- `POST /upload` - Upload media

#### Gamification (`/api/gamification`)
- `GET /profile` - Get gamification profile
- `GET /leaderboard` - Get leaderboard
- `GET /achievements` - Get achievements
- `GET /badges` - Get badges
- `POST /daily-check-in` - Daily check-in
- `POST /claim-reward` - Claim reward
- `GET /challenges` - Get active challenges

## 🛠️ Setup & Installation

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB for development)
- Redis
- Docker (optional)

### Local Development

1. **Clone the repository**
```bash
git clone <repository-url>
cd OneTime-Backend
```

2. **Install dependencies**
```bash
dotnet restore
```

3. **Update connection strings**
Edit `appsettings.Development.json` with your local database and Redis connections.

4. **Run database migrations**
```bash
dotnet ef database update
```

5. **Start the application**
```bash
dotnet run --project OneTime.API
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

### Docker Deployment

1. **Build and run with Docker Compose**
```bash
docker-compose up -d
```

This will start:
- OneTime API
- SQL Server
- Redis
- Nginx (reverse proxy)
- Elasticsearch & Kibana (logging)
- Prometheus & Grafana (monitoring)
- Additional services (MinIO, RabbitMQ, etc.)

2. **Access services**
- API: `http://localhost:8080`
- Grafana: `http://localhost:3000`
- Kibana: `http://localhost:5601`
- Prometheus: `http://localhost:9090`

## 🔧 Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection="Server=...;Database=...;..."
ConnectionStrings__Redis="localhost:6379"

# JWT
JWT__Key="your-secret-key"
JWT__Issuer="OneTimeAPI"
JWT__Audience="OneTimeApp"

# Azure Services
Azure__Storage__AccountName="your-storage-account"
Azure__NotificationHubs__ConnectionString="your-connection-string"
Azure__CognitiveServices__ComputerVision__SubscriptionKey="your-key"

# Email & SMS
Email__SendGrid__ApiKey="your-sendgrid-key"
SMS__Twilio__AccountSid="your-twilio-sid"

# Payments
Payment__Stripe__SecretKey="your-stripe-key"
```

### Feature Flags

Control feature availability through configuration:

```json
{
  "Features": {
    "EnableRegistration": true,
    "EnableGamification": true,
    "EnableAIMatching": true,
    "EnableVideoMessages": true,
    "EnablePremiumFeatures": true,
    "EnableContentModeration": true,
    "MaintenanceMode": false
  }
}
```

## 📊 Database Schema

The application uses Entity Framework Core with the following main entities:

- **Users**: Core user information and authentication
- **UserProfiles**: Extended profile data
- **Photos**: User photo storage and management
- **Matches**: Match relationships between users
- **Messages**: Chat messages and media
- **GamificationProfiles**: User gamification data
- **Subscriptions**: Premium subscription management

See `Migrations/001_InitialCreate.sql` for the complete schema.

## 🔐 Security

- **Authentication**: JWT tokens with refresh token rotation
- **Authorization**: Role-based and policy-based authorization
- **Rate Limiting**: Configurable rate limits per endpoint
- **Data Protection**: Encrypted sensitive data
- **CORS**: Configurable allowed origins
- **Input Validation**: FluentValidation for request validation
- **Security Headers**: Comprehensive security headers

## 📱 Real-time Features

### SignalR Hubs

#### MessageHub (`/messageHub`)
- Real-time messaging
- Typing indicators
- Online status
- Message reactions
- Read receipts

**Client Events:**
- `MessageReceived` - New message
- `UserStartedTyping` - Typing indicator
- `UserOnline/Offline` - Online status
- `MessageRead` - Read receipt
- `MessageReactionAdded` - Reaction added

## 🎮 Gamification System

### Experience Points (XP)
- Profile completion: 50 XP
- First match: 25 XP
- Daily login: 10 XP
- Send message: 2 XP
- Like profile: 2 XP
- Super like: 15 XP

### Badges & Achievements
- **First Match**: Get your first match
- **Social Butterfly**: Match with 10 people
- **Conversation Starter**: Send your first message
- **Verified**: Complete profile verification
- **Weekly Warrior**: 7-day login streak

### Leaderboards
- Weekly XP leaderboard
- Monthly XP leaderboard
- All-time rankings

## 🌐 Azure Integration

### Services Used
- **Blob Storage**: Photo and media storage
- **Notification Hubs**: Push notifications
- **SignalR Service**: Scalable real-time messaging
- **Cognitive Services**: Content moderation and AI features
- **Application Insights**: Monitoring and analytics
- **Key Vault**: Secure configuration management

## 📈 Monitoring & Analytics

### Health Checks
- Database connectivity
- Redis connectivity
- Azure services status
- Custom health checks

Available at: `/health`

### Metrics & Logging
- Structured logging with Serilog
- Application Insights integration
- Custom metrics tracking
- Performance monitoring
- Error tracking and alerting

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📋 API Documentation

Interactive API documentation is available via Swagger UI when running the application:
- Development: `https://localhost:5001/swagger`
- Production: `https://api.onetime.app/swagger`

## 🚀 Deployment

### Azure App Service

1. **Create Azure resources**
```bash
# Create resource group
az group create --name onetime-resources --location eastus

# Create App Service plan
az appservice plan create --name onetime-plan --resource-group onetime-resources --sku B1

# Create web app
az webapp create --name onetime-api --resource-group onetime-resources --plan onetime-plan
```

2. **Deploy application**
```bash
dotnet publish -c Release
az webapp deployment source config-zip --resource-group onetime-resources --name onetime-api --src publish.zip
```

### Docker Production

```bash
# Build production image
docker build -t onetime-api:latest .

# Push to registry
docker tag onetime-api:latest your-registry/onetime-api:latest
docker push your-registry/onetime-api:latest

# Deploy with docker-compose
docker-compose -f docker-compose.prod.yml up -d
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For support, email support@onetime.app or join our Slack channel.

---

**OneTime Dating App Backend** - Built with ❤️ using ASP.NET Core 8.0