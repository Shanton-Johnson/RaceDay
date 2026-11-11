using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("AppUsers", t => t.HasCheckConstraint("CK_AppUsers_Role", "[Role] IN ('Organiser','Participant')"));
            e.HasKey(x => x.UserId);
            e.Property(x => x.FirstName).HasMaxLength(60).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(60).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.HasAlternateKey(x => x.Email).HasName("UQ_AppUsers_Email");
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(25);
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.ProfilePictureUrl).HasMaxLength(2048);
            e.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<EventType>(e =>
        {
            e.ToTable("EventTypes");
            e.HasKey(x => x.EventTypeId);
            e.Property(x => x.TypeName).HasMaxLength(20).IsRequired();
            e.HasAlternateKey(x => x.TypeName).HasName("UQ_EventTypes_TypeName");
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_Distance", "[DistanceKm] > 0");
                t.HasCheckConstraint("CK_Events_Status", "[EventStatus] IN ('Upcoming','Completed','Cancelled')");
            });
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("datetime2(0)");
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(8,2)");
            e.Property(x => x.BannerImageUrl).HasMaxLength(2048);
            e.Property(x => x.EventStatus).HasMaxLength(20).HasDefaultValue("Upcoming");
            e.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Organiser).WithMany(x => x.OrganisedEvents).HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EventType).WithMany(x => x.Events).HasForeignKey(x => x.EventTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories", t =>
            {
                t.HasCheckConstraint("CK_Categories_Type", "[CategoryType] IN ('Age','Distance')");
                t.HasCheckConstraint("CK_Categories_AgeRange", "([MinAge] IS NULL OR [MinAge] >= 0) AND ([MaxAge] IS NULL OR [MaxAge] >= 0) AND ([MinAge] IS NULL OR [MaxAge] IS NULL OR [MinAge] <= [MaxAge])");
                t.HasCheckConstraint("CK_Categories_DistanceRange", "([MinDistanceKm] IS NULL OR [MinDistanceKm] > 0) AND ([MaxDistanceKm] IS NULL OR [MaxDistanceKm] > 0) AND ([MinDistanceKm] IS NULL OR [MaxDistanceKm] IS NULL OR [MinDistanceKm] <= [MaxDistanceKm])");
                t.HasCheckConstraint("CK_Categories_RangeByType", "([CategoryType]='Age' AND ([MinAge] IS NOT NULL OR [MaxAge] IS NOT NULL) AND [MinDistanceKm] IS NULL AND [MaxDistanceKm] IS NULL) OR ([CategoryType]='Distance' AND ([MinDistanceKm] IS NOT NULL OR [MaxDistanceKm] IS NOT NULL) AND [MinAge] IS NULL AND [MaxAge] IS NULL)");
            });
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryName).HasMaxLength(80).IsRequired();
            e.Property(x => x.CategoryType).HasMaxLength(20).IsRequired();
            e.Property(x => x.MinDistanceKm).HasColumnType("decimal(8,2)");
            e.Property(x => x.MaxDistanceKm).HasColumnType("decimal(8,2)");
            e.HasAlternateKey(x => new { x.EventId, x.CategoryName }).HasName("UQ_Categories_Event_CategoryName");
            e.HasAlternateKey(x => new { x.CategoryId, x.EventId }).HasName("UQ_Categories_CategoryId_EventId");
            e.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Pending','Confirmed','Cancelled')"));
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.EnrolmentStatus).HasMaxLength(20).HasDefaultValue("Confirmed");
            e.HasAlternateKey(x => new { x.ParticipantId, x.EventId }).HasName("UQ_Enrolments_Participant_Event");
            e.HasOne(x => x.Participant).WithMany(x => x.ParticipantEnrolments).HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Event).WithMany(x => x.Enrolments).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(x => x.Enrolments).HasForeignKey(x => new { x.CategoryId, x.EventId }).HasPrincipalKey(x => new { x.CategoryId, x.EventId }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results", t =>
            {
                t.HasCheckConstraint("CK_Results_Position", "[FinishingPosition] > 0");
                t.HasCheckConstraint("CK_Results_TotalFinishers", "[TotalFinishers] > 0");
                t.HasCheckConstraint("CK_Results_PositionWithinField", "[FinishingPosition] <= [TotalFinishers]");
            });
            e.HasKey(x => x.ResultId);
            e.Property(x => x.FinishTime).HasColumnType("time(0)");
            e.Property(x => x.RecordedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasAlternateKey(x => x.EnrolmentId).HasName("UQ_Results_Enrolment");
            e.HasOne(x => x.Enrolment).WithOne(x => x.Result).HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventType>().HasData(
            new EventType { EventTypeId = 1, TypeName = "Run" },
            new EventType { EventTypeId = 2, TypeName = "Walk" },
            new EventType { EventTypeId = 3, TypeName = "Cycle" });
    }
}
