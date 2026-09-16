using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryEbookOcr.Pages;

/// <summary>
/// Upload 頁的「後端邏輯」：驗證 PDF、存到 Uploads、產生暫存 Job 資訊。
/// Lua 之後會把 CreateJob 接到 SQLite；現在先用本機檔案 + 暫存 Job Id。
/// </summary>
[RequestSizeLimit(50 * 1024 * 1024)] // 整份請求最大約 50MB
public class UploadModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadModel> _logger;
    private const long MaxFileBytes = 50 * 1024 * 1024; // 50MB

    public UploadModel(IWebHostEnvironment environment, ILogger<UploadModel> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>表單送來的 PDF 檔案</summary>
    [BindProperty]
    public IFormFile? PdfFile { get; set; }

    /// <summary>成功或失敗時顯示給使用者看的訊息</summary>
    public string? Message { get; set; }

    public bool IsSuccess { get; set; }

    public string? JobId { get; set; }
    public string? OriginalFileName { get; set; }
    public string? StoredFileName { get; set; }
    public string? Status { get; set; }
    public string? CreatedAt { get; set; }

    /// <summary>使用者用瀏覽器「打開」Upload 頁時會跑這裡（GET）</summary>
    public void OnGet()
    {
    }

    /// <summary>使用者按「Upload」送出表單時會跑這裡（POST）</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        // 1) 有沒有選檔？
        if (PdfFile is null || PdfFile.Length == 0)
        {
            IsSuccess = false;
            Message = "Please choose a PDF file before uploading.";
            return Page();
        }

        // 2) 副檔名必須是 .pdf
        var extension = Path.GetExtension(PdfFile.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            IsSuccess = false;
            Message = "Only PDF files are allowed.";
            return Page();
        }

        // 3) 大小限制
        if (PdfFile.Length > MaxFileBytes)
        {
            IsSuccess = false;
            Message = "File is too large. Maximum size is 50 MB.";
            return Page();
        }

        // 4) 準備 Uploads 資料夾（在專案根目錄旁，執行時通常在 ContentRoot）
        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
        Directory.CreateDirectory(uploadsFolder);

        // 5) 用 GUID 當磁碟檔名，避免中文檔名 / 重名問題；原始檔名另外記住
        var jobId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var storedFileName = $"{jobId}.pdf";
        var savedPath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = System.IO.File.Create(savedPath))
        {
            await PdfFile.CopyToAsync(stream);
        }

        _logger.LogInformation("Saved upload {Original} as {Stored}", PdfFile.FileName, storedFileName);

        // 6) 暫存 Job 資訊（之後改呼叫 Lua 的 CreateJob + SQLite）
        IsSuccess = true;
        Message = "Upload successful. A job was created with status Pending.";
        JobId = jobId;
        OriginalFileName = PdfFile.FileName;
        StoredFileName = storedFileName;
        Status = "Pending";
        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 清空表單上的檔案選擇狀態（避免重新整理時困惑）
        PdfFile = null;
        return Page();
    }
}
