namespace _bk12.Models
{
    public class HomeMatchViewModel
    {
        public int Match_ID { get; set; }
        public DateTime Schedule { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Home_Club_Name { get; set; } = string.Empty;
        public string Home_Club_Logo { get; set; } = string.Empty;
        public string Away_Club_Name { get; set; } = string.Empty;
        public string Away_Club_Logo { get; set; } = string.Empty;
        public string Stadium_Name { get; set; } = string.Empty;
        public string Map_Url { get; set; } = string.Empty;
    }
}