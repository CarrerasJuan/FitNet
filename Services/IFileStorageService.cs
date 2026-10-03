namespace FitNet.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Guarda un archivo subido en el subdirectorio especificado dentro de wwwroot/uploads/{subfolder}.
    /// Valida tamaño (máx 2MB), extensiones (.jpg, .jpeg, .png, .webp) y genera un nombre único GUID.
    /// </summary>
    /// <param name="file">Archivo IFormFile recibido en la petición.</param>
    /// <param name="subfolder">Subcarpeta de destino (ej: "avatars", "progress", "exercises").</param>
    /// <returns>Ruta relativa web (ej: "/uploads/avatars/guid.webp") para persistir en BD.</returns>
    Task<string> SaveFileAsync(IFormFile file, string subfolder);

    /// <summary>
    /// Elimina físicamente un archivo del disco a partir de su ruta web relativa.
    /// Evita archivos huérfanos al actualizar fotos o avatares.
    /// </summary>
    /// <param name="relativeWebPath">Ruta web relativa guardada en BD (ej: "/uploads/avatars/guid.webp").</param>
    Task<bool> DeleteFileAsync(string? relativeWebPath);

    /// <summary>
    /// Valida si el archivo cumple con las reglas de tamaño, extensión y tipo MIME.
    /// </summary>
    /// <param name="file">Archivo a validar.</param>
    /// <param name="errorMessage">Mensaje de error en caso de no ser válido.</param>
    bool ValidateImage(IFormFile? file, out string? errorMessage);
}
