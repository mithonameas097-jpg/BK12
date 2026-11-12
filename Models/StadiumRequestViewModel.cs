namespace _bk12.Models
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using System;
    using System.ComponentModel.DataAnnotations;

    public class StadiumRequestViewModel
    {
        public int RequestId { get; set; }
        public int? StadiumId { get; set; }
        public string UserId { get; set; } = string.Empty;
        [BindNever]
        public string? RequestType { get; set; }

        [Required(ErrorMessage = "Stadium Name is required.")]
        [StringLength(50, ErrorMessage = "Stadium Name cannot exceed 50 characters.")] 
        public string StadiumName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Capacity is required.")]
        [Range(5000, int.MaxValue, ErrorMessage = "Minimum capacity must be at least 5,000.")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Zone A capacity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Zone A capacity must be valid.")]
        public int ZoneA { get; set; }

        [Required(ErrorMessage = "Zone B capacity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Zone B capacity must be valid.")]
        public int ZoneB { get; set; }

        public string? Description { get; set; }

        public string? Stadium_IMG { get; set; }

        [Required(ErrorMessage = "Map URL is required.")]
        public string? Map_Url { get; set; }

        public IFormFile? StadiumImageFile { get; set; }
        public string? Status { get; set; }
        public string? RejectedReason { get; set; }
        public DateTime RequestDate { get; set; }
        public string? Stadium_Location { get; set; }
    }
}