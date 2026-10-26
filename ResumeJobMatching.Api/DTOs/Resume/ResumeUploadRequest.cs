using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ResumeJobMatching.Api.DTOs.Resume;

public class ResumeUploadRequest
{
    [Required]
    public IFormFile? File { get; set; }
}