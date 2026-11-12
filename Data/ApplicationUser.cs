using Microsoft.AspNetCore.Identity;
namespace _bk12.Data;
public class ApplicationUser : IdentityUser<Guid>
{
    public string? ProfilePictureUrl { get; set; }
}
