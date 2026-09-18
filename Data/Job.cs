using System.ComponentModel.DataAnnotations;

namespace LibraryEbookOcr.Data;

/// <summary>
/// One conversion job. Maps to the Jobs table in SQLite.
/// </summary>
public class Job
{
    public int Id { get; set; }

    /// <summary>Short id shown on the Upload page and used in the saved PDF name.</summary>
    [MaxLength(16)]
    public string PublicId { get; set; } = "";

    [MaxLength(260)]
    public string OriginalFileName { get; set; } = "";

    [MaxLength(260)]
    public string StoredFileName { get; set; } = "";

    [MaxLength(32)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }
}
