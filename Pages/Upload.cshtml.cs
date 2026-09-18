using LibraryEbookOcr.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryEbookOcr.Pages;

/// <summary>
/// Upload page: validate PDF, save to Uploads, insert a Pending Job in SQLite.
/// </summary>
[RequestSizeLimit(50 * 1024 * 1024)]
public class UploadModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadModel> _logger;
    private readonly AppDbContext _db;
    private const long MaxFileBytes = 50 * 1024 * 1024;

    public UploadModel(
        IWebHostEnvironment environment,
        ILogger<UploadModel> logger,
        AppDbContext db)
    {
        _environment = environment;
        _logger = logger;
        _db = db;
    }

    [BindProperty]
    public IFormFile? PdfFile { get; set; }

    public string? Message { get; set; }

    public bool IsSuccess { get; set; }

    public string? JobId { get; set; }
    public string? OriginalFileName { get; set; }
    public string? StoredFileName { get; set; }
    public string? Status { get; set; }
    public string? CreatedAt { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (PdfFile is null || PdfFile.Length == 0)
        {
            IsSuccess = false;
            Message = "Please choose a PDF file before uploading.";
            return Page();
        }

        var extension = Path.GetExtension(PdfFile.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            IsSuccess = false;
            Message = "Only PDF files are allowed.";
            return Page();
        }

        if (PdfFile.Length > MaxFileBytes)
        {
            IsSuccess = false;
            Message = "File is too large. Maximum size is 50 MB.";
            return Page();
        }

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
        Directory.CreateDirectory(uploadsFolder);

        var jobId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var storedFileName = $"{jobId}.pdf";
        var savedPath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = System.IO.File.Create(savedPath))
        {
            await PdfFile.CopyToAsync(stream);
        }

        var createdAt = DateTime.Now;
        var job = new Job
        {
            PublicId = jobId,
            OriginalFileName = PdfFile.FileName,
            StoredFileName = storedFileName,
            Status = "Pending",
            CreatedAt = createdAt
        };
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Saved upload {Original} as {Stored}; SQLite job Id={Id} PublicId={PublicId}",
            PdfFile.FileName,
            storedFileName,
            job.Id,
            job.PublicId);

        IsSuccess = true;
        Message = "Upload successful. A job was created with status Pending.";
        JobId = jobId;
        OriginalFileName = PdfFile.FileName;
        StoredFileName = storedFileName;
        Status = "Pending";
        CreatedAt = createdAt.ToString("yyyy-MM-dd HH:mm:ss");

        PdfFile = null;
        return Page();
    }
}
