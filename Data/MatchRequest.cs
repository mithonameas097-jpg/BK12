namespace _bk12.Data
{
    public class MatchRequest
    {
        public int Request_ID { get; set; }
        public int? Match_ID { get; set; }
        public string? Request_Type { get; set; }

        public int Home_Club { get; set; }
        public string? Home_Club_Name { get; set; }
        public string? Home_Logo { get; set; }

        public int Away_Club { get; set; }
        public string? Away_Club_Name { get; set; }
        public string? Away_Logo { get; set; }

        public int Stadium_ID { get; set; }
        public string? Stadium_Name { get; set; }
        public string? Stadium_IMG { get; set; }  
        public string? Stadium_Location { get; set; } 

        public string? User_ID { get; set; }
        public DateTime Schedule { get; set; }
        public string? Status { get; set; }
        public string? Rejected_Reason { get; set; }
        public DateTime Requested_Date { get; set; }
        public DateTime? Updated_Date { get; set; }
        public string? Updated_UID { get; set; }
        public string? Add_Note { get; set; }
    }
}