using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using OneTime.API.Models.DTOs;

namespace OneTime.API.Services;

public interface IBlobStorageService
{
    Task<ServiceResult<FileUploadResponse>> UploadFileAsync(IFormFile file, string containerName, string userId);
    Task<ServiceResult<FileUploadResponse>> UploadImageAsync(IFormFile file, string containerName, string userId, bool generateThumbnail = true);
    Task<ServiceResult<bool>> DeleteFileAsync(string containerName, string fileName);
    Task<ServiceResult<string>> GetFileUrlAsync(string containerName, string fileName);
    Task<ServiceResult<List<string>>> GetFilesInContainerAsync(string containerName, string prefix = "");
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ServiceResult<FileUploadResponse>> UploadFileAsync(IFormFile file, string containerName, string userId)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return ServiceResult<FileUploadResponse>.Failure("No file provided");
            }

            // Validate file size (10MB limit)
            var maxFileSize = _configuration.GetValue<long>("Azure:Storage:MaxFileSize", 10485760);
            if (file.Length > maxFileSize)
            {
                return ServiceResult<FileUploadResponse>.Failure($"File size exceeds {maxFileSize / 1048576}MB limit");
            }

            // Validate file type
            var allowedTypes = _configuration.GetSection("Azure:Storage:AllowedFileTypes").Get<string[]>();
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (allowedTypes != null && !allowedTypes.Contains(fileExtension))
            {
                return ServiceResult<FileUploadResponse>.Failure("File type not allowed");
            }

            // Generate unique file name
            var fileName = $"{userId}/{Guid.NewGuid()}{fileExtension}";
            
            // Get container client
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Upload file
            var blobClient = containerClient.GetBlobClient(fileName);
            
            var headers = new BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, headers);

            var response = new FileUploadResponse
            {
                FileName = fileName,
                Url = blobClient.Uri.ToString(),
                ContentType = file.ContentType,
                Size = file.Length
            };

            _logger.LogInformation("File uploaded successfully: {FileName}", fileName);
            return ServiceResult<FileUploadResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for user {UserId}", userId);
            return ServiceResult<FileUploadResponse>.Failure("Failed to upload file");
        }
    }

    public async Task<ServiceResult<FileUploadResponse>> UploadImageAsync(IFormFile file, string containerName, string userId, bool generateThumbnail = true)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return ServiceResult<FileUploadResponse>.Failure("No file provided");
            }

            // Validate image file
            if (!IsImageFile(file))
            {
                return ServiceResult<FileUploadResponse>.Failure("File must be an image");
            }

            // Process and compress image
            var processedImage = await ProcessImageAsync(file);
            if (!processedImage.Success)
            {
                return processedImage;
            }

            // Upload main image
            var fileName = $"{userId}/{Guid.NewGuid()}.jpg";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(fileName);
            var headers = new BlobHttpHeaders { ContentType = "image/jpeg" };

            await blobClient.UploadAsync(processedImage.Data.ImageStream, headers);

            var response = new FileUploadResponse
            {
                FileName = fileName,
                Url = blobClient.Uri.ToString(),
                ContentType = "image/jpeg",
                Size = processedImage.Data.ImageStream.Length
            };

            // Generate thumbnail if requested
            if (generateThumbnail)
            {
                var thumbnailResult = await GenerateAndUploadThumbnailAsync(
                    processedImage.Data.OriginalImage, 
                    containerName, 
                    userId);
                
                if (thumbnailResult.Success)
                {
                    response.ThumbnailUrl = thumbnailResult.Data;
                }
            }

            processedImage.Data.ImageStream.Dispose();
            processedImage.Data.OriginalImage.Dispose();

            _logger.LogInformation("Image uploaded successfully: {FileName}", fileName);
            return ServiceResult<FileUploadResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for user {UserId}", userId);
            return ServiceResult<FileUploadResponse>.Failure("Failed to upload image");
        }
    }

    public async Task<ServiceResult<bool>> DeleteFileAsync(string containerName, string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var deleteResult = await blobClient.DeleteIfExistsAsync();
            
            if (deleteResult.Value)
            {
                _logger.LogInformation("File deleted successfully: {FileName}", fileName);
                return ServiceResult<bool>.Success(true);
            }

            return ServiceResult<bool>.Failure("File not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
            return ServiceResult<bool>.Failure("Failed to delete file");
        }
    }

    public async Task<ServiceResult<string>> GetFileUrlAsync(string containerName, string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var exists = await blobClient.ExistsAsync();
            if (!exists.Value)
            {
                return ServiceResult<string>.Failure("File not found");
            }

            return ServiceResult<string>.Success(blobClient.Uri.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file URL: {FileName}", fileName);
            return ServiceResult<string>.Failure("Failed to get file URL");
        }
    }

    public async Task<ServiceResult<List<string>>> GetFilesInContainerAsync(string containerName, string prefix = "")
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = new List<string>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                blobs.Add(blobItem.Name);
            }

            return ServiceResult<List<string>>.Success(blobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files in container: {ContainerName}", containerName);
            return ServiceResult<List<string>>.Failure("Failed to list files");
        }
    }

    private static bool IsImageFile(IFormFile file)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return imageExtensions.Contains(extension);
    }

    private async Task<ServiceResult<ProcessedImageResult>> ProcessImageAsync(IFormFile file)
    {
        try
        {
            using var inputStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(inputStream);

            // Get configuration values
            var maxDimension = _configuration.GetValue<int>("Limits:PhotoUpload:MaxDimension", 2048);
            var quality = _configuration.GetValue<int>("Limits:PhotoUpload:Quality", 85);

            // Resize if necessary
            if (image.Width > maxDimension || image.Height > maxDimension)
            {
                var ratio = Math.Min((double)maxDimension / image.Width, (double)maxDimension / image.Height);
                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);

                image.Mutate(x => x.Resize(newWidth, newHeight));
            }

            // Convert to JPEG and compress
            var outputStream = new MemoryStream();
            var encoder = new JpegEncoder { Quality = quality };
            await image.SaveAsync(outputStream, encoder);
            outputStream.Position = 0;

            var result = new ProcessedImageResult
            {
                ImageStream = outputStream,
                OriginalImage = image.Clone()
            };

            return ServiceResult<ProcessedImageResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image");
            return ServiceResult<ProcessedImageResult>.Failure("Failed to process image");
        }
    }

    private async Task<ServiceResult<string>> GenerateAndUploadThumbnailAsync(Image originalImage, string containerName, string userId)
    {
        try
        {
            // Create thumbnail (200x200)
            using var thumbnail = originalImage.Clone();
            thumbnail.Mutate(x => x.Resize(200, 200));

            // Save thumbnail to stream
            using var thumbnailStream = new MemoryStream();
            var encoder = new JpegEncoder { Quality = 80 };
            await thumbnail.SaveAsync(thumbnailStream, encoder);
            thumbnailStream.Position = 0;

            // Upload thumbnail
            var thumbnailFileName = $"{userId}/thumbnails/{Guid.NewGuid()}.jpg";
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var thumbnailBlobClient = containerClient.GetBlobClient(thumbnailFileName);

            var headers = new BlobHttpHeaders { ContentType = "image/jpeg" };
            await thumbnailBlobClient.UploadAsync(thumbnailStream, headers);

            return ServiceResult<string>.Success(thumbnailBlobClient.Uri.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail");
            return ServiceResult<string>.Failure("Failed to generate thumbnail");
        }
    }

    private class ProcessedImageResult
    {
        public MemoryStream ImageStream { get; set; } = null!;
        public Image OriginalImage { get; set; } = null!;
    }
}