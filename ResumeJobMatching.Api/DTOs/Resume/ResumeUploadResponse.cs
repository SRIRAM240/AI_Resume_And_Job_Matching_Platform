namespace ResumeJobMatching.Api.DTOs.Resume;

public class ResumeUploadResponse
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    public string Message { get; set; } = string.Empty;
}