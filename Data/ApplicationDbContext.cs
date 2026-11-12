using _bk12.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace _bk12.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EmailOtp> Email_OTPs { get; set; }
        public DbSet<Club> Clubs { get; set; } = default!;
        public DbSet<ClubRequest> ClubRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Configure Club Foreign Key relationship to ApplicationUser
            builder.Entity<Club>(entity =>
            {
                entity.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey(c => c.User_ID)
                      .OnDelete(DeleteBehavior.Restrict);

                // EXPLICIT TYPE MAPPING: Ensure EF Core knows these are uniqueidentifiers in SQL
                entity.Property(c => c.User_ID).HasColumnType("uniqueidentifier");
                entity.Property(c => c.Updated_UID).HasColumnType("uniqueidentifier");
            });

            // 2. Configure ClubRequest mapping, table name, and explicit uniqueidentifier mapping
            builder.Entity<ClubRequest>(entity =>
            {
                entity.ToTable("Club_Requests");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasColumnName("Club_Request_ID");
                entity.Property(r => r.ClubName).HasColumnName("Club_Name");
                entity.Property(r => r.Logo_Url).HasColumnName("Logo_Url");
                entity.Property(r => r.SubmittedDate).HasColumnName("Submitted_Date");

                // EXPLICIT TYPE MAPPING: Fixes the Guid-to-string conversion crash on SaveChangesAsync
                entity.Property(r => r.UserId).HasColumnType("uniqueidentifier");
            });
        }
    }
}