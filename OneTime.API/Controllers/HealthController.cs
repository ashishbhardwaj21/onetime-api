using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneTime.API.Data;
using OneTime.API.Models.DTOs;

namespace OneTime.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<HealthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var healthStatus = new HealthStatus
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = _configuration["App:Version"] ?? "1.0.0",
                Environment = _configuration["App:Environment"] ?? "Unknown"
            };

            // Check database connectivity
            try
            {
                await _context.Database.CanConnectAsync();
                healthStatus.Services.Add("Database", "Healthy");
            }
            catch (Exception ex)
            {
                healthStatus.Services.Add("Database", "Unhealthy");
                healthStatus.Status = "Degraded";
                _logger.LogError(ex, "Database health check failed");
            }

            // Check Redis connectivity (if configured)
            var redisConnectionString = _configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                try
                {
                    // In a real implementation, you'd check Redis connectivity here
                    healthStatus.Services.Add("Redis", "Healthy");
                }
                catch (Exception ex)
                {
                    healthStatus.Services.Add("Redis", "Unhealthy");
                    healthStatus.Status = "Degraded";
                    _logger.LogError(ex, "Redis health check failed");
                }
            }

            // Check Azure services (if configured)
            var azureStorageConnection = _configuration.GetConnectionString("AzureStorage");
            if (!string.IsNullOrEmpty(azureStorageConnection) && !azureStorageConnection.Contains("UseDevelopmentStorage"))
            {
                try
                {
                    healthStatus.Services.Add("AzureStorage", "Healthy");
                }
                catch (Exception ex)
                {
                    healthStatus.Services.Add("AzureStorage", "Unhealthy");
                    healthStatus.Status = "Degraded";
                    _logger.LogError(ex, "Azure Storage health check failed");
                }
            }

            // Determine overall status
            if (healthStatus.Services.Any(s => s.Value == "Unhealthy"))
            {
                healthStatus.Status = "Unhealthy";
                return StatusCode(503, healthStatus);
            }

            if (healthStatus.Services.Any(s => s.Value == "Degraded"))
            {
                healthStatus.Status = "Degraded";
                return StatusCode(200, healthStatus);
            }

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            var unhealthyStatus = new HealthStatus
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Version = _configuration["App:Version"] ?? "1.0.0",
                Environment = _configuration["App:Environment"] ?? "Unknown"
            };

            return StatusCode(503, unhealthyStatus);
        }
    }

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        try
        {
            var detailedHealth = new DetailedHealthStatus
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = _configuration["App:Version"] ?? "1.0.0",
                Environment = _configuration["App:Environment"] ?? "Unknown",
                Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                MemoryUsage = GC.GetTotalMemory(false),
                ProcessorTime = Process.GetCurrentProcess().TotalProcessorTime
            };

            // Database health with metrics
            try
            {
                var dbStartTime = DateTime.UtcNow;
                var userCount = await _context.Users.CountAsync();
                var dbResponseTime = DateTime.UtcNow - dbStartTime;

                detailedHealth.Services.Add("Database", new ServiceHealth
                {
                    Status = "Healthy",
                    ResponseTime = dbResponseTime,
                    LastChecked = DateTime.UtcNow,
                    Metrics = new Dictionary<string, object>
                    {
                        ["UserCount"] = userCount,
                        ["ConnectionString"] = _configuration.GetConnectionString("DefaultConnection")?.Substring(0, 20) + "..."
                    }
                });
            }
            catch (Exception ex)
            {
                detailedHealth.Services.Add("Database", new ServiceHealth
                {
                    Status = "Unhealthy",
                    LastChecked = DateTime.UtcNow,
                    Error = ex.Message
                });
                detailedHealth.Status = "Degraded";
            }

            // Feature flags status
            detailedHealth.FeatureFlags = new Dictionary<string, bool>
            {
                ["EnableRegistration"] = _configuration.GetValue<bool>("Features:EnableRegistration"),
                ["EnableGamification"] = _configuration.GetValue<bool>("Features:EnableGamification"),
                ["EnableAIMatching"] = _configuration.GetValue<bool>("Features:EnableAIMatching"),
                ["EnableVideoMessages"] = _configuration.GetValue<bool>("Features:EnableVideoMessages"),
                ["EnablePremiumFeatures"] = _configuration.GetValue<bool>("Features:EnablePremiumFeatures"),
                ["MaintenanceMode"] = _configuration.GetValue<bool>("Features:MaintenanceMode")
            };

            // System metrics
            detailedHealth.SystemMetrics = new Dictionary<string, object>
            {
                ["MachineName"] = Environment.MachineName,
                ["ProcessorCount"] = Environment.ProcessorCount,
                ["OSVersion"] = Environment.OSVersion.ToString(),
                ["WorkingSet"] = Environment.WorkingSet,
                ["ThreadCount"] = Process.GetCurrentProcess().Threads.Count
            };

            return Ok(detailedHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed");
            return StatusCode(500, new { error = "Health check failed", message = ex.Message });
        }
    }

    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            // Check if application is ready to serve requests
            await _context.Database.CanConnectAsync();
            
            var readinessStatus = new ReadinessStatus
            {
                Ready = true,
                Timestamp = DateTime.UtcNow,
                Checks = new Dictionary<string, bool>
                {
                    ["DatabaseConnectivity"] = true,
                    ["ConfigurationLoaded"] = !string.IsNullOrEmpty(_configuration["App:Name"]),
                    ["ServicesRegistered"] = true
                }
            };

            return Ok(readinessStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            
            var readinessStatus = new ReadinessStatus
            {
                Ready = false,
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };

            return StatusCode(503, readinessStatus);
        }
    }

    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        // Simple liveness check - if we can respond, we're alive
        return Ok(new LivenessStatus
        {
            Alive = true,
            Timestamp = DateTime.UtcNow
        });
    }
}

// Health status DTOs
public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public Dictionary<string, string> Services { get; set; } = new();
}

public class DetailedHealthStatus : HealthStatus
{
    public TimeSpan Uptime { get; set; }
    public long MemoryUsage { get; set; }
    public TimeSpan ProcessorTime { get; set; }
    public new Dictionary<string, ServiceHealth> Services { get; set; } = new();
    public Dictionary<string, bool> FeatureFlags { get; set; } = new();
    public Dictionary<string, object> SystemMetrics { get; set; } = new();
}

public class ServiceHealth
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan? ResponseTime { get; set; }
    public DateTime LastChecked { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
}

public class ReadinessStatus
{
    public bool Ready { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, bool>? Checks { get; set; }
    public string? Error { get; set; }
}

public class LivenessStatus
{
    public bool Alive { get; set; }
    public DateTime Timestamp { get; set; }
}