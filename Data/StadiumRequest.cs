namespace _bk12.Models
{
    using System;

    public class StadiumRequest
    {
        public int Id { get; set; }
        public Guid? UserId { get; set; }
        public string? Request_Type { get; set; }
        public int? Stadium_ID { get; set; }
        public string? Stadium_Name { get; set; }
        public string? Stadium_IMG { get; set; }

        public string? Description { get; set; }
        public string? Map_Url { get; set; }
        public int? Capacity { get; set; }
        public int? ZoneA { get; set; }
        public int? ZoneB { get; set; }
        public string? Status { get; set; }
        public string? RejectedReason { get; set; }
        public DateTime RequestDate { get; set; }
        public string? Stadium_Location { get; set; }
    }
}