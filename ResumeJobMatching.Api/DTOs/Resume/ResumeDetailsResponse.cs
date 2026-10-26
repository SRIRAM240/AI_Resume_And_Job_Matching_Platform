namespace ResumeJobMatching.Api.DTOs.Resume;

public class ResumeDetailsResponse
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    public bool HasExtractedText { get; set; }

    public int ExtractedTextLength { get; set; }
}