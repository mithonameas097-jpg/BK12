using System.ComponentModel.DataAnnotations;

namespace _bk12.Data
{
    public class Club
    {
        [Key]
        public int Club_ID { get; set; }
        public Guid? User_ID { get; set; }
        [Required]
        [MaxLength(50)]
        public string Club_Name { get; set; } = string.Empty;
        [MaxLength(255)]
        public string? Logo_Url { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }
        public bool Inactive { get; set; } = false;
        public DateTime Created_Date { get; set; } = DateTime.Now;
        public Guid? Updated_UID { get; set; } = Guid.NewGuid();
        public DateTime Updated_Date { get; set; } = DateTime.Now;
    }
}