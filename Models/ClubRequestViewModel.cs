using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace _bk12.Models
{
    public class ClubRequestViewModel
    {
        public int Club_ID { get; set; }
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Club name is required.")]
        public string ClubName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        public string? Logo_Url { get; set; }
        public IFormFile? LogoFile { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewedBy { get; set; }
        public string? RejectedReason { get; set; }
        public string? Request_Type { get; set; }
    }
}