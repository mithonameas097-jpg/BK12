namespace _bk12.Data
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Club_Requests")]
    public class ClubRequest
    {
        [Key]
        [Column("Club_Request_ID")]
        public int Id { get; set; }

        [Column("User_ID")]
        public Guid? UserId { get; set; }

        [Column("Request_Type")]
        public string Request_Type { get; set; } = string.Empty;

        [Column("Club_ID")]
        public int? ClubId { get; set; }

        [Column("Club_Name")]
        public string ClubName { get; set; } = string.Empty;

        [Column("Description")]
        public string Description { get; set; } = string.Empty;

        [Column("Status")]
        public string Status { get; set; } = string.Empty;

        [Column("Submitted_Date")]
        public DateTime SubmittedDate { get; set; }

        [Column("Logo_Url")]
        public string? Logo_Url { get; set; }

        [Column("Rejected_Reason")]
        public string? Rejected_Reason { get; set; }

        [Column("Updated_Date")]
        public DateTime? Updated_Date { get; set; }

        [Column("Reviewed_By")]
        public string? Reviewed_By { get; set; }
    }
}