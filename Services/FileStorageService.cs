namespace FitNet.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;

    // Constantes de validación defensiva server-side
    private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 Megabytes (soporte para GIFs animados)
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/jpg", "image/png", "image/x-png", "image/webp", "image/pjpeg", "image/gif"];
    private static readonly string[] AllowedSubfolders = ["avatars", "progress", "exercises"];

    public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subfolder)
    {
        if (!ValidateImage(file, out var error))
        {
            throw new InvalidOperationException(error ?? "Archivo no válido para almacenamiento.");
        }

        // 1. Limpiar y asegurar subcarpeta permitida (evitar path traversal)
        var safeSubfolder = subfolder.Trim().Trim('/', '\\').ToLowerInvariant();
        if (!AllowedSubfolders.Contains(safeSubfolder))
        {
            throw new ArgumentException($"Subcarpeta no permitida: {subfolder}. Opciones válidas: avatars, progress, exercises.");
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadRoot = Path.Combine(webRoot, "uploads", safeSubfolder);

        if (!Directory.Exists(uploadRoot))
        {
            Directory.CreateDirectory(uploadRoot);
        }

        // 2. Generar nombre de archivo único mediante GUID (Regla: nunca usar nombre del usuario)
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadRoot, uniqueFileName);

        // 3. Escribir stream en disco
        await using (var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(fileStream);
        }

        _logger.LogInformation("Archivo guardado físicamente en {Path} ({Size} KB).", physicalPath, file.Length / 1024);

        // 4. Devolver ruta web relativa estándar para persistir en BD
        return $"/uploads/{safeSubfolder}/{uniqueFileName}";
    }

    public Task<bool> DeleteFileAsync(string? relativeWebPath)
    {
        if (string.IsNullOrWhiteSpace(relativeWebPath))
        {
            return Task.FromResult(false);
        }

        try
        {
            // Solo procesar archivos que residan en /uploads/
            var normalizedPath = relativeWebPath.Trim().Replace('\\', '/');
            if (!normalizedPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) &&
                !normalizedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(false);
            }

            var cleanRelativePath = normalizedPath.TrimStart('/');
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var physicalPath = Path.Combine(webRoot, cleanRelativePath);

            // Validar que el archivo resida efectivamente dentro del directorio wwwroot/uploads
            var fullPhysicalPath = Path.GetFullPath(physicalPath);
            var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
            if (!fullPhysicalPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Intento de borrado fuera del directorio uploads: {Path}", relativeWebPath);
                return Task.FromResult(false);
            }

            if (File.Exists(fullPhysicalPath))
            {
                File.Delete(fullPhysicalPath);
                _logger.LogInformation("Archivo anterior eliminado del disco: {Path}", fullPhysicalPath);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intentar eliminar el archivo físico en {Path}", relativeWebPath);
        }

        return Task.FromResult(false);
    }

    public bool ValidateImage(IFormFile? file, out string? errorMessage)
    {
        errorMessage = null;

        if (file == null || file.Length == 0)
        {
            errorMessage = "No se ha seleccionado ningún archivo.";
            return false;
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            errorMessage = $"El archivo excede el tamaño máximo permitido de {MaxFileSizeInBytes / (1024 * 1024)} MB.";
            return false;
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            errorMessage = $"Formato de archivo no admitido ({extension}). Formatos permitidos: JPG, JPEG, PNG, WEBP, GIF.";
            return false;
        }

        var contentType = file.ContentType.ToLowerInvariant();
        if (!AllowedMimeTypes.Contains(contentType))
        {
            errorMessage = $"El tipo de contenido ({contentType}) no corresponde a una imagen válida.";
            return false;
        }

        return true;
    }
}
