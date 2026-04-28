using FruitShop.Constants;

namespace FruitShop.Services;

/// <summary>
/// Service xử lý upload và xóa file - tách khỏi FruitController
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveImageAsync(IFormFile? file, string folder, IWebHostEnvironment env);
    void DeleteImage(string? fileName, string folder, IWebHostEnvironment env);
    (bool IsValid, string Error) ValidateImage(IFormFile? file);
}

public class FileStorageService : IFileStorageService
{
    public async Task<string> SaveImageAsync(IFormFile? file, string folder, IWebHostEnvironment env)
    {
        if (file == null || file.Length == 0)
            return AppConstants.ImageUpload.DefaultImage;

        var uploadDir = Path.Combine(env.WebRootPath, folder);
        Directory.CreateDirectory(uploadDir);

        var ext      = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return fileName;
    }

    public void DeleteImage(string? fileName, string folder, IWebHostEnvironment env)
    {
        if (string.IsNullOrEmpty(fileName) || fileName == AppConstants.ImageUpload.DefaultImage) return;

        var filePath = Path.Combine(env.WebRootPath, folder, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public (bool IsValid, string Error) ValidateImage(IFormFile? file)
    {
        if (file == null) return (true, string.Empty);

        if (file.Length > AppConstants.ImageUpload.MaxFileSize)
            return (false, $"Ảnh không được vượt quá {AppConstants.ImageUpload.MaxFileSize / 1024 / 1024}MB");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AppConstants.ImageUpload.AllowedExtensions.Contains(ext))
            return (false, "Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .gif, .webp");

        return (true, string.Empty);
    }
}
