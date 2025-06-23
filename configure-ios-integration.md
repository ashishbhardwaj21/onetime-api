# iOS Integration Configuration Guide

## 🍎 Connecting Your iOS App to Azure Backend

### Step 1: Update iOS App Configuration

#### 1.1 Create API Configuration File

Create a new file in your iOS project: `APIConfiguration.swift`

```swift
import Foundation

struct APIConfiguration {
    // Replace with your actual Azure App Service URL
    static let baseURL = "https://onetime-api.azurewebsites.net"
    
    // API Endpoints
    struct Endpoints {
        static let auth = "\(baseURL)/api/auth"
        static let user = "\(baseURL)/api/user"
        static let matching = "\(baseURL)/api/matching"
        static let messaging = "\(baseURL)/api/messaging"
        static let gamification = "\(baseURL)/api/gamification"
        static let health = "\(baseURL)/health"
    }
    
    // SignalR Hub URL
    static let signalRHubURL = "\(baseURL)/messageHub"
    
    // Request timeout
    static let timeoutInterval: TimeInterval = 30
}
```

#### 1.2 Update NetworkManager

Update your existing `NetworkManager.swift` to use the Azure endpoints:

```swift
import Foundation
import Combine

class NetworkManager: ObservableObject {
    static let shared = NetworkManager()
    private let session = URLSession.shared
    
    // MARK: - Authentication
    func signUp(email: String, password: String, firstName: String, lastName: String, dateOfBirth: Date, gender: String) -> AnyPublisher<AuthResponse, Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.auth)/signup")!
        
        let signUpData = SignUpRequest(
            email: email,
            password: password,
            firstName: firstName,
            lastName: lastName,
            dateOfBirth: dateOfBirth,
            gender: gender
        )
        
        return makeRequest(url: url, method: "POST", body: signUpData)
    }
    
    func signIn(email: String, password: String) -> AnyPublisher<AuthResponse, Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.auth)/signin")!
        
        let signInData = SignInRequest(email: email, password: password)
        
        return makeRequest(url: url, method: "POST", body: signInData)
    }
    
    // MARK: - User Profile
    func getUserProfile() -> AnyPublisher<UserProfileResponse, Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.user)/profile")!
        return makeAuthenticatedRequest(url: url, method: "GET")
    }
    
    func updateProfile(_ profile: UpdateProfileRequest) -> AnyPublisher<UserProfileResponse, Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.user)/profile")!
        return makeAuthenticatedRequest(url: url, method: "PUT", body: profile)
    }
    
    // MARK: - Matching
    func discoverProfiles(count: Int = 10) -> AnyPublisher<[UserProfileResponse], Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.matching)/discover?count=\(count)")!
        return makeAuthenticatedRequest(url: url, method: "GET")
    }
    
    func likeProfile(targetUserId: String) -> AnyPublisher<MatchResponse, Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.matching)/like")!
        let likeData = LikeProfileRequest(targetUserId: targetUserId)
        return makeAuthenticatedRequest(url: url, method: "POST", body: likeData)
    }
    
    func passProfile(targetUserId: String) -> AnyPublisher<Bool, Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.matching)/pass")!
        let passData = PassProfileRequest(targetUserId: targetUserId)
        return makeAuthenticatedRequest(url: url, method: "POST", body: passData)
    }
    
    // MARK: - Messaging
    func getConversations() -> AnyPublisher<[ConversationResponse], Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.messaging)/conversations")!
        return makeAuthenticatedRequest(url: url, method: "GET")
    }
    
    func getMessages(conversationId: String, page: Int = 1) -> AnyPublisher<PaginatedResponse<MessageResponse>, Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.messaging)/conversations/\(conversationId)/messages?page=\(page)")!
        return makeAuthenticatedRequest(url: url, method: "GET")
    }
    
    // MARK: - Photo Upload
    func uploadPhoto(_ imageData: Data, isMain: Bool = false, order: Int = 0) -> AnyPublisher<PhotoResponse, Error> {
        let url = URL(string: "\(APIConfiguration.Endpoints.user)/photos")!
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("Bearer \(AuthManager.shared.accessToken ?? "")", forHTTPHeaderField: "Authorization")
        
        let boundary = UUID().uuidString
        request.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
        
        let httpBody = NSMutableData()
        
        // Add image data
        httpBody.append("--\(boundary)\r\n".data(using: .utf8)!)
        httpBody.append("Content-Disposition: form-data; name=\"Photo\"; filename=\"photo.jpg\"\r\n".data(using: .utf8)!)
        httpBody.append("Content-Type: image/jpeg\r\n\r\n".data(using: .utf8)!)
        httpBody.append(imageData)
        httpBody.append("\r\n".data(using: .utf8)!)
        
        // Add order field
        httpBody.append("--\(boundary)\r\n".data(using: .utf8)!)
        httpBody.append("Content-Disposition: form-data; name=\"Order\"\r\n\r\n".data(using: .utf8)!)
        httpBody.append("\(order)".data(using: .utf8)!)
        httpBody.append("\r\n".data(using: .utf8)!)
        
        // Add isMain field
        httpBody.append("--\(boundary)\r\n".data(using: .utf8)!)
        httpBody.append("Content-Disposition: form-data; name=\"IsMain\"\r\n\r\n".data(using: .utf8)!)
        httpBody.append("\(isMain)".data(using: .utf8)!)
        httpBody.append("\r\n".data(using: .utf8)!)
        
        httpBody.append("--\(boundary)--\r\n".data(using: .utf8)!)
        
        request.httpBody = httpBody as Data
        
        return session.dataTaskPublisher(for: request)
            .map(\.data)
            .decode(type: ApiResponse<PhotoResponse>.self, decoder: JSONDecoder())
            .map(\.data)
            .compactMap { $0 }
            .eraseToAnyPublisher()
    }
    
    // MARK: - Helper Methods
    private func makeRequest<T: Codable, R: Codable>(url: URL, method: String, body: T? = nil) -> AnyPublisher<R, Error> {
        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.timeoutInterval = APIConfiguration.timeoutInterval
        
        if let body = body {
            do {
                request.httpBody = try JSONEncoder().encode(body)
            } catch {
                return Fail(error: error).eraseToAnyPublisher()
            }
        }
        
        return session.dataTaskPublisher(for: request)
            .map(\.data)
            .decode(type: ApiResponse<R>.self, decoder: JSONDecoder())
            .map(\.data)
            .compactMap { $0 }
            .eraseToAnyPublisher()
    }
    
    private func makeAuthenticatedRequest<T: Codable, R: Codable>(url: URL, method: String, body: T? = nil) -> AnyPublisher<R, Error> {
        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("Bearer \(AuthManager.shared.accessToken ?? "")", forHTTPHeaderField: "Authorization")
        request.timeoutInterval = APIConfiguration.timeoutInterval
        
        if let body = body {
            do {
                request.httpBody = try JSONEncoder().encode(body)
            } catch {
                return Fail(error: error).eraseToAnyPublisher()
            }
        }
        
        return session.dataTaskPublisher(for: request)
            .map(\.data)
            .decode(type: ApiResponse<R>.self, decoder: JSONDecoder())
            .map(\.data)
            .compactMap { $0 }
            .eraseToAnyPublisher()
    }
}
```

#### 1.3 Create Data Models

Create `APIModels.swift` to match your backend DTOs:

```swift
import Foundation

// MARK: - API Response Wrapper
struct ApiResponse<T: Codable>: Codable {
    let success: Bool
    let data: T?
    let message: String?
    let errors: [String]?
    let timestamp: Date
}

// MARK: - Authentication Models
struct SignUpRequest: Codable {
    let email: String
    let password: String
    let firstName: String
    let lastName: String
    let dateOfBirth: Date
    let gender: String
    let acceptedTerms: Bool = true
    let acceptedPrivacyPolicy: Bool = true
}

struct SignInRequest: Codable {
    let email: String
    let password: String
    let rememberMe: Bool = false
}

struct AuthResponse: Codable {
    let success: Bool
    let accessToken: String?
    let refreshToken: String?
    let expiresAt: Date?
    let user: UserProfileResponse?
    let requiresEmailVerification: Bool
    let requires2FA: Bool
    let message: String?
}

// MARK: - User Models
struct UserProfileResponse: Codable, Identifiable {
    let id: String
    let name: String
    let firstName: String?
    let lastName: String?
    let age: Int
    let bio: String?
    let occupation: String?
    let education: String?
    let height: Int?
    let drinking: String?
    let smoking: String?
    let children: String?
    let photos: [PhotoResponse]
    let interests: [String]
    let distance: Double?
    let isVerified: Bool
    let lastActive: Date?
    let isPremium: Bool
    let isOnline: Bool
}

struct PhotoResponse: Codable, Identifiable {
    let id: String
    let url: String
    let thumbnailUrl: String?
    let order: Int
    let isMain: Bool
    let createdAt: Date
}

struct UpdateProfileRequest: Codable {
    let firstName: String?
    let lastName: String?
    let bio: String?
    let occupation: String?
    let education: String?
    let height: Int?
    let drinking: String?
    let smoking: String?
    let children: String?
}

// MARK: - Matching Models
struct LikeProfileRequest: Codable {
    let targetUserId: String
}

struct PassProfileRequest: Codable {
    let targetUserId: String
}

struct MatchResponse: Codable {
    let isMatch: Bool
    let matchId: String?
    let matchedAt: Date?
    let expiresAt: Date?
    let conversationId: String?
    let userProfile: UserProfileResponse?
    let lastMessage: MessageResponse?
    let unreadCount: Int
}

// MARK: - Messaging Models
struct ConversationResponse: Codable, Identifiable {
    let id: String
    let matchId: String
    let otherUser: UserProfileResponse
    let lastMessage: MessageResponse?
    let unreadCount: Int
    let createdAt: Date
    let isActive: Bool
}

struct MessageResponse: Codable, Identifiable {
    let id: String
    let conversationId: String
    let senderId: String
    let content: String?
    let type: String
    let mediaUrl: String?
    let thumbnailUrl: String?
    let duration: Int?
    let createdAt: Date
    let updatedAt: Date?
    let isRead: Bool
    let isEdited: Bool
    let reactions: [MessageReactionResponse]
}

struct MessageReactionResponse: Codable {
    let reaction: String
    let count: Int
    let userIds: [String]
}

struct PaginatedResponse<T: Codable>: Codable {
    let items: [T]
    let page: Int
    let pageSize: Int
    let totalItems: Int
    let totalPages: Int
    let hasNextPage: Bool
    let hasPreviousPage: Bool
}
```

### Step 2: Configure Push Notifications

#### 2.1 Add Push Notification Capability

1. In Xcode, select your project target
2. Go to "Signing & Capabilities"
3. Click "+ Capability"
4. Add "Push Notifications"

#### 2.2 Update AppDelegate for Push Notifications

```swift
import UIKit
import UserNotifications

class AppDelegate: NSObject, UIApplicationDelegate {
    func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey : Any]? = nil) -> Bool {
        
        // Request notification permissions
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound, .badge]) { granted, error in
            if granted {
                DispatchQueue.main.async {
                    application.registerForRemoteNotifications()
                }
            }
        }
        
        return true
    }
    
    func application(_ application: UIApplication, didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data) {
        let tokenString = deviceToken.map { String(format: "%02.2hhx", $0) }.joined()
        print("Device token: \(tokenString)")
        
        // Send token to your backend
        NotificationManager.shared.registerDevice(token: tokenString)
    }
    
    func application(_ application: UIApplication, didFailToRegisterForRemoteNotificationsWithError error: Error) {
        print("Failed to register for remote notifications: \(error)")
    }
}
```

#### 2.3 Create NotificationManager

```swift
import Foundation
import UserNotifications
import Combine

class NotificationManager: ObservableObject {
    static let shared = NotificationManager()
    
    func registerDevice(token: String) {
        let url = URL(string: "\(APIConfiguration.baseURL)/api/notifications/register")!
        
        let registrationData = DeviceRegistrationRequest(
            deviceToken: token,
            platform: "ios",
            deviceModel: UIDevice.current.model,
            appVersion: Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String
        )
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("Bearer \(AuthManager.shared.accessToken ?? "")", forHTTPHeaderField: "Authorization")
        
        do {
            request.httpBody = try JSONEncoder().encode(registrationData)
            
            URLSession.shared.dataTask(with: request) { data, response, error in
                if let error = error {
                    print("Failed to register device: \(error)")
                    return
                }
                
                print("Device registered successfully")
            }.resume()
        } catch {
            print("Failed to encode registration data: \(error)")
        }
    }
}

struct DeviceRegistrationRequest: Codable {
    let deviceToken: String
    let platform: String
    let deviceModel: String?
    let appVersion: String?
}
```

### Step 3: Set up Real-time Messaging

#### 3.1 Add SignalR Package

Add SignalR to your iOS project via Swift Package Manager:
1. File → Add Package Dependencies
2. Enter: `https://github.com/moozzyk/SignalR-Client-Swift`
3. Click "Add Package"

#### 3.2 Create SignalRManager

```swift
import Foundation
import SignalRClient
import Combine

class SignalRManager: ObservableObject {
    static let shared = SignalRManager()
    
    private var connection: HubConnection?
    @Published var isConnected = false
    @Published var newMessage: MessageResponse?
    
    private init() {}
    
    func connect() {
        guard let accessToken = AuthManager.shared.accessToken else {
            print("No access token available")
            return
        }
        
        connection = HubConnectionBuilder(url: URL(string: APIConfiguration.signalRHubURL)!)
            .withLogging(logLevel: .debug)
            .withHttpConnectionOptions { httpOptions in
                httpOptions.accessTokenProvider = { return accessToken }
            }
            .build()
        
        // Handle connection events
        connection?.on(method: "MessageReceived") { [weak self] (message: MessageResponse) in
            DispatchQueue.main.async {
                self?.newMessage = message
            }
        }
        
        connection?.on(method: "UserStartedTyping") { (userId: String, conversationId: String) in
            print("User \(userId) started typing in \(conversationId)")
            // Handle typing indicator
        }
        
        connection?.on(method: "UserStoppedTyping") { (userId: String, conversationId: String) in
            print("User \(userId) stopped typing in \(conversationId)")
            // Handle typing indicator
        }
        
        // Start connection
        connection?.start()
        
        connection?.connectionDidOpen { [weak self] in
            DispatchQueue.main.async {
                self?.isConnected = true
                print("SignalR connected")
            }
        }
        
        connection?.connectionDidClose { [weak self] error in
            DispatchQueue.main.async {
                self?.isConnected = false
                print("SignalR disconnected: \(error?.localizedDescription ?? "Unknown error")")
            }
        }
    }
    
    func disconnect() {
        connection?.stop()
        isConnected = false
    }
    
    func joinConversation(_ conversationId: String) {
        connection?.invoke(method: "JoinConversation", conversationId) { error in
            if let error = error {
                print("Failed to join conversation: \(error)")
            }
        }
    }
    
    func sendMessage(conversationId: String, content: String, type: String = "text") {
        let request = SendMessageRequest(
            conversationId: conversationId,
            content: content,
            type: type
        )
        
        connection?.invoke(method: "SendMessage", request) { error in
            if let error = error {
                print("Failed to send message: \(error)")
            }
        }
    }
    
    func startTyping(in conversationId: String) {
        connection?.invoke(method: "StartTyping", conversationId) { error in
            if let error = error {
                print("Failed to start typing: \(error)")
            }
        }
    }
    
    func stopTyping(in conversationId: String) {
        connection?.invoke(method: "StopTyping", conversationId) { error in
            if let error = error {
                print("Failed to stop typing: \(error)")
            }
        }
    }
}

struct SendMessageRequest: Codable {
    let conversationId: String
    let content: String?
    let type: String
    let mediaUrl: String?
    let thumbnailUrl: String?
    let duration: Int?
    
    init(conversationId: String, content: String? = nil, type: String = "text", mediaUrl: String? = nil, thumbnailUrl: String? = nil, duration: Int? = nil) {
        self.conversationId = conversationId
        self.content = content
        self.type = type
        self.mediaUrl = mediaUrl
        self.thumbnailUrl = thumbnailUrl
        self.duration = duration
    }
}
```

### Step 4: Test Your Integration

#### 4.1 Create a Test View

```swift
import SwiftUI

struct APITestView: View {
    @StateObject private var networkManager = NetworkManager.shared
    @StateObject private var signalRManager = SignalRManager.shared
    @State private var testResults: [String] = []
    
    var body: some View {
        NavigationView {
            List {
                Section("API Tests") {
                    Button("Test Health Check") {
                        testHealthCheck()
                    }
                    
                    Button("Test Authentication") {
                        testAuthentication()
                    }
                    
                    Button("Test Profile API") {
                        testProfileAPI()
                    }
                    
                    Button("Connect SignalR") {
                        signalRManager.connect()
                    }
                }
                
                Section("Results") {
                    ForEach(testResults, id: \.self) { result in
                        Text(result)
                            .font(.caption)
                    }
                }
            }
            .navigationTitle("API Integration Test")
        }
        .onReceive(signalRManager.$isConnected) { isConnected in
            testResults.append("SignalR: \(isConnected ? "Connected" : "Disconnected")")
        }
    }
    
    private func testHealthCheck() {
        guard let url = URL(string: "\(APIConfiguration.baseURL)/health") else { return }
        
        URLSession.shared.dataTask(with: url) { data, response, error in
            DispatchQueue.main.async {
                if let error = error {
                    testResults.append("Health Check: ❌ \(error.localizedDescription)")
                } else if let httpResponse = response as? HTTPURLResponse {
                    testResults.append("Health Check: ✅ Status \(httpResponse.statusCode)")
                }
            }
        }.resume()
    }
    
    private func testAuthentication() {
        // Test with dummy credentials
        networkManager.signUp(
            email: "test@example.com",
            password: "Test123!@#",
            firstName: "Test",
            lastName: "User",
            dateOfBirth: Calendar.current.date(byAdding: .year, value: -25, to: Date()) ?? Date(),
            gender: "Other"
        )
        .sink(
            receiveCompletion: { completion in
                switch completion {
                case .finished:
                    break
                case .failure(let error):
                    testResults.append("Auth Test: ❌ \(error.localizedDescription)")
                }
            },
            receiveValue: { response in
                testResults.append("Auth Test: ✅ Success")
            }
        )
        .store(in: &cancellables)
    }
    
    private func testProfileAPI() {
        networkManager.getUserProfile()
            .sink(
                receiveCompletion: { completion in
                    switch completion {
                    case .finished:
                        break
                    case .failure(let error):
                        testResults.append("Profile Test: ❌ \(error.localizedDescription)")
                    }
                },
                receiveValue: { profile in
                    testResults.append("Profile Test: ✅ Got profile for \(profile.name)")
                }
            )
            .store(in: &cancellables)
    }
    
    @State private var cancellables = Set<AnyCancellable>()
}
```

### Step 5: Final Configuration

1. **Update your main App file**:
```swift
@main
struct OneTimeApp: App {
    @UIApplicationDelegateAdaptor(AppDelegate.self) var delegate
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .onAppear {
                    // Connect to SignalR when app launches
                    if AuthManager.shared.isAuthenticated {
                        SignalRManager.shared.connect()
                    }
                }
        }
    }
}
```

2. **Update Info.plist** to allow network requests:
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
</dict>
```

## 🚀 Next Steps

1. **Run the Azure setup script**: `./setup-azure.sh`
2. **Deploy your API**: `./deploy-api.sh`
3. **Update the API URL in your iOS app** with your actual Azure URL
4. **Test the integration** using the test view above
5. **Configure push notifications** in Azure Notification Hubs

Your iOS app should now be fully connected to your Azure backend! 🎉