using _bk12.Data;

namespace _bk12.Models
{
    public class ManageClubViewModel
    {
        public bool HasClub { get; set; }
        public Club? OwnedClub { get; set; }
        public List<Club> AvailableClubs { get; set; } = new();
        public string? OwnerUsername { get; set; }
    }
}
