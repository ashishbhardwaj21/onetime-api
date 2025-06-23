using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using OneTime.API.Models.DTOs;

namespace OneTime.API.Services;

public interface IAnalyticsService
{
    Task<ServiceResult<bool>> TrackEventAsync(string userId, string eventName, Dictionary<string, object> properties);
    Task<ServiceResult<bool>> TrackUserActionAsync(string userId, string action, string? target = null);
    Task<ServiceResult<bool>> TrackScreenViewAsync(string userId, string screenName, TimeSpan? duration = null);
    Task<ServiceResult<bool>> TrackErrorAsync(string userId, Exception exception, Dictionary<string, object>? properties = null);
    Task<ServiceResult<bool>> TrackPerformanceAsync(string operation, TimeSpan duration, bool success);
    Task<ServiceResult<bool>> TrackConversionAsync(string userId, string conversionType, decimal? value = null);
    Task<ServiceResult<AnalyticsReport>> GetUserAnalyticsAsync(string userId, DateTime startDate, DateTime endDate);
    Task<ServiceResult<AnalyticsReport>> GetAppAnalyticsAsync(DateTime startDate, DateTime endDate);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly TelemetryClient? _telemetryClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly bool _analyticsEnabled;

    public AnalyticsService(
        TelemetryClient? telemetryClient,
        IConfiguration configuration,
        ILogger<AnalyticsService> logger)
    {
        _telemetryClient = telemetryClient;
        _configuration = configuration;
        _logger = logger;
        _analyticsEnabled = _configuration.GetValue<bool>("Features:EnableAnalytics", false);
    }

    public async Task<ServiceResult<bool>> TrackEventAsync(string userId, string eventName, Dictionary<string, object> properties)
    {
        try
        {
            if (!_analyticsEnabled)
            {
                return ServiceResult<bool>.Success(true);
            }

            // Convert properties to string dictionary for Application Insights
            var stringProperties = properties.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty
            );

            // Add common properties
            stringProperties["UserId"] = userId;
            stringProperties["Timestamp"] = DateTime.UtcNow.ToString("O");
            stringProperties["Environment"] = _configuration.GetValue<string>("App:Environment", "Unknown");

            // Track with Application Insights
            _telemetryClient?.TrackEvent(eventName, stringProperties);

            // Log for debugging in development
            if (_configuration.GetValue<string>("App:Environment") == "Development")
            {
                _logger.LogInformation("Analytics Event: {EventName} for User: {UserId} with Properties: {@Properties}",
                    eventName, userId, stringProperties);
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking analytics event {EventName} for user {UserId}", eventName, userId);
            return ServiceResult<bool>.Failure("Failed to track analytics event");
        }
    }

    public async Task<ServiceResult<bool>> TrackUserActionAsync(string userId, string action, string? target = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["Action"] = action,
            ["ActionTime"] = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(target))
        {
            properties["Target"] = target;
        }

        return await TrackEventAsync(userId, "UserAction", properties);
    }

    public async Task<ServiceResult<bool>> TrackScreenViewAsync(string userId, string screenName, TimeSpan? duration = null)
    {
        try
        {
            if (!_analyticsEnabled)
            {
                return ServiceResult<bool>.Success(true);
            }

            var properties = new Dictionary<string, string>
            {
                ["UserId"] = userId,
                ["ScreenName"] = screenName,
                ["ViewTime"] = DateTime.UtcNow.ToString("O")
            };

            if (duration.HasValue)
            {
                properties["Duration"] = duration.Value.TotalSeconds.ToString("F2");
            }

            // Track page view with Application Insights
            var pageViewTelemetry = new PageViewTelemetry(screenName)
            {
                UserId = userId,
                Duration = duration ?? TimeSpan.Zero
            };

            foreach (var prop in properties)
            {
                pageViewTelemetry.Properties[prop.Key] = prop.Value;
            }

            _telemetryClient?.TrackPageView(pageViewTelemetry);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking screen view {ScreenName} for user {UserId}", screenName, userId);
            return ServiceResult<bool>.Failure("Failed to track screen view");
        }
    }

    public async Task<ServiceResult<bool>> TrackErrorAsync(string userId, Exception exception, Dictionary<string, object>? properties = null)
    {
        try
        {
            if (!_analyticsEnabled)
            {
                return ServiceResult<bool>.Success(true);
            }

            var stringProperties = new Dictionary<string, string>
            {
                ["UserId"] = userId,
                ["ErrorTime"] = DateTime.UtcNow.ToString("O"),
                ["ExceptionType"] = exception.GetType().Name,
                ["ExceptionMessage"] = exception.Message
            };

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    stringProperties[prop.Key] = prop.Value?.ToString() ?? string.Empty;
                }
            }

            // Track exception with Application Insights
            var exceptionTelemetry = new ExceptionTelemetry(exception)
            {
                UserId = userId
            };

            foreach (var prop in stringProperties)
            {
                exceptionTelemetry.Properties[prop.Key] = prop.Value;
            }

            _telemetryClient?.TrackException(exceptionTelemetry);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking exception for user {UserId}", userId);
            return ServiceResult<bool>.Failure("Failed to track error");
        }
    }

    public async Task<ServiceResult<bool>> TrackPerformanceAsync(string operation, TimeSpan duration, bool success)
    {
        try
        {
            if (!_analyticsEnabled)
            {
                return ServiceResult<bool>.Success(true);
            }

            var properties = new Dictionary<string, string>
            {
                ["Operation"] = operation,
                ["Duration"] = duration.TotalMilliseconds.ToString("F2"),
                ["Success"] = success.ToString(),
                ["Timestamp"] = DateTime.UtcNow.ToString("O")
            };

            // Track dependency with Application Insights
            var dependencyTelemetry = new DependencyTelemetry(
                "Operation",
                operation,
                operation,
                success,
                DateTimeOffset.UtcNow.Subtract(duration),
                duration,
                success ? "200" : "500");

            foreach (var prop in properties)
            {
                dependencyTelemetry.Properties[prop.Key] = prop.Value;
            }

            _telemetryClient?.TrackDependency(dependencyTelemetry);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking performance for operation {Operation}", operation);
            return ServiceResult<bool>.Failure("Failed to track performance");
        }
    }

    public async Task<ServiceResult<bool>> TrackConversionAsync(string userId, string conversionType, decimal? value = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["ConversionType"] = conversionType,
            ["ConversionTime"] = DateTime.UtcNow
        };

        if (value.HasValue)
        {
            properties["Value"] = value.Value;
        }

        return await TrackEventAsync(userId, "Conversion", properties);
    }

    public async Task<ServiceResult<AnalyticsReport>> GetUserAnalyticsAsync(string userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            // In a real implementation, this would query Application Insights or your analytics database
            // For now, we'll return a mock report
            var report = new AnalyticsReport
            {
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                TotalEvents = 0,
                UniqueScreenViews = 0,
                SessionCount = 0,
                AverageSessionDuration = TimeSpan.Zero,
                TopEvents = new List<EventSummary>(),
                TopScreens = new List<ScreenSummary>(),
                ConversionFunnel = new List<ConversionStep>()
            };

            _logger.LogWarning("GetUserAnalyticsAsync not fully implemented - returning mock data");
            return ServiceResult<AnalyticsReport>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user analytics for {UserId}", userId);
            return ServiceResult<AnalyticsReport>.Failure("Failed to get user analytics");
        }
    }

    public async Task<ServiceResult<AnalyticsReport>> GetAppAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            // In a real implementation, this would query Application Insights or your analytics database
            // For now, we'll return a mock report
            var report = new AnalyticsReport
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalEvents = 0,
                UniqueUsers = 0,
                SessionCount = 0,
                AverageSessionDuration = TimeSpan.Zero,
                TopEvents = new List<EventSummary>(),
                TopScreens = new List<ScreenSummary>(),
                ConversionFunnel = new List<ConversionStep>()
            };

            _logger.LogWarning("GetAppAnalyticsAsync not fully implemented - returning mock data");
            return ServiceResult<AnalyticsReport>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting app analytics");
            return ServiceResult<AnalyticsReport>.Failure("Failed to get app analytics");
        }
    }
}

// Analytics DTOs
public class AnalyticsReport
{
    public string? UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEvents { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueScreenViews { get; set; }
    public int SessionCount { get; set; }
    public TimeSpan AverageSessionDuration { get; set; }
    public List<EventSummary> TopEvents { get; set; } = new();
    public List<ScreenSummary> TopScreens { get; set; } = new();
    public List<ConversionStep> ConversionFunnel { get; set; } = new();
}

public class EventSummary
{
    public string EventName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class ScreenSummary
{
    public string ScreenName { get; set; } = string.Empty;
    public int Views { get; set; }
    public TimeSpan AverageTimeSpent { get; set; }
    public double BounceRate { get; set; }
}

public class ConversionStep
{
    public string StepName { get; set; } = string.Empty;
    public int Users { get; set; }
    public double ConversionRate { get; set; }
    public double DropOffRate { get; set; }
}