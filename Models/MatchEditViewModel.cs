using Microsoft.Identity.Client;

namespace _bk12.Models
{
    public class MatchEditViewModel
    {
        public int Request_ID { get; set; }
        public int Home_Club { get; set; }
        public int Away_Club { get; set; }
        public int Stadium_ID { get; set; }
        public DateTime MatchDate { get; set; }
        public TimeSpan MatchTime { get; set; }
        public string? Note { get; set; }
        public int? Temp_Req_ID { get; set; }
    }
}