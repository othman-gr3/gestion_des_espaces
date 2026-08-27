using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Controllers;

/// <summary>
/// Stores an uploaded image on disk under wwwroot/uploads and returns its absolute URL —
/// the returned URL is stored directly on an entity's Image field, the same field that
/// already accepts external picture URLs, so both remain interchangeable.
/// </summary>
[ApiController]
[Route("api/uploads")]
[Authorize(Policy = "ReferentielAdmin")]
public sealed class UploadsController : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    private readonly IWebHostEnvironment _environment;

    public UploadsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost("image")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> UploadImageAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "Aucun fichier reçu." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { detail = "Le fichier dépasse la taille maximale autorisée (5 Mo)." });

        // Never trust the client-supplied filename beyond its extension: the extension itself
        // is checked against an allowlist alongside the declared content type, and the file is
        // always saved under a freshly generated name — no path traversal surface either way.
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { detail = "Format de fichier non autorisé. Utilisez JPG, PNG, GIF ou WEBP." });

        var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsPath = Path.Combine(webRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
        return Ok(new { url });
    }
}
