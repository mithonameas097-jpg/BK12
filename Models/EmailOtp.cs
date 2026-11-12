using System.ComponentModel.DataAnnotations;

namespace _bk12.Models
{
    public class EmailOtp
    {
        [Key]
        public int OTP_ID { get; set; }
        public Guid User_ID { get; set; }
        public string OTP_Code { get; set; } = "";
        public DateTime Expiry_Date { get; set; }
        public bool Is_Used { get; set; }
        public DateTime Created_Date { get; set; }
        public string Otp_purpose { get; set; } = "";
    }
}