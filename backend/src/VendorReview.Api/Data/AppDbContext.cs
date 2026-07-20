using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VendorReview.Api.Domain;
using VendorReview.Api.Services;

namespace VendorReview.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<SectionItem> SectionItems => Set<SectionItem>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewSectionScore> ReviewSectionScores => Set<ReviewSectionScore>();
    public DbSet<ReviewItemScore> ReviewItemScores => Set<ReviewItemScore>();
    public DbSet<OpenQuestion> OpenQuestions => Set<OpenQuestion>();
    public DbSet<ArchivedReview> ArchivedReviews => Set<ArchivedReview>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Store all enums as strings for readable, stable columns.
        foreach (var entityType in b.Model.GetEntityTypes())
            foreach (var prop in entityType.GetProperties())
            {
                var clr = Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType;
                if (!clr.IsEnum) continue;
                var converterType = typeof(EnumToStringConverter<>).MakeGenericType(clr);
                prop.SetValueConverter((ValueConverter)Activator.CreateInstance(converterType)!);
                prop.SetColumnType("text");
            }

        b.Entity<Category>().Property(c => c.IncludedSectionIds).HasColumnType("uuid[]");
        b.Entity<Review>().Property(r => r.ApproverObjectIds).HasColumnType("text[]");

        b.Entity<Section>()
            .HasMany(s => s.Items).WithOne(i => i.Section!)
            .HasForeignKey(i => i.SectionId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Review>()
            .HasMany(r => r.Sections).WithOne()
            .HasForeignKey(s => s.ReviewId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Review>()
            .HasMany(r => r.OpenQuestions).WithOne()
            .HasForeignKey(q => q.ReviewId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<ReviewSectionScore>()
            .HasMany(s => s.Items).WithOne()
            .HasForeignKey(i => i.ReviewSectionScoreId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<AppUser>().HasIndex(u => u.EntraObjectId).IsUnique();
        b.Entity<AuditEvent>().HasIndex(a => a.Utc);

        // Personal-data columns encrypted at rest (AES-256-GCM) via a value converter.
        // Stored as `text` because ciphertext is longer than the plaintext limits.
        var enc = new EncryptedConverter();
        void Encrypt<T>(System.Linq.Expressions.Expression<Func<T, string?>> prop) where T : class =>
            b.Entity<T>().Property(prop).HasConversion(enc!).HasColumnType("text");

        Encrypt<Vendor>(v => v.ContactName!);
        Encrypt<Vendor>(v => v.ContactEmail!);
        Encrypt<AppUser>(u => u.DisplayName!);
        Encrypt<AppUser>(u => u.Email);
        Encrypt<AppUser>(u => u.JobTitle);
        Encrypt<Review>(r => r.NdaContactName);
        Encrypt<Review>(r => r.NdaContactEmail);
        Encrypt<Review>(r => r.OwnerEmail);
    }
}
