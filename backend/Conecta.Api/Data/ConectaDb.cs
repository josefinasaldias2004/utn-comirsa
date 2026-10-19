using Conecta.Api.Domain;
using Microsoft.EntityFrameworkCore;
namespace Conecta.Api.Data;
public sealed class ConectaDb(DbContextOptions<ConectaDb> options) : DbContext(options) {
 public DbSet<Member> Members => Set<Member>();
 public DbSet<Listing> Listings => Set<Listing>();
 protected override void OnModelCreating(ModelBuilder model) {
  var m = model.Entity<Member>(); m.ToTable("Members"); m.HasKey(x => x.Id);
  m.Property(x => x.Name).HasMaxLength(120);
  m.Property(x => x.Email).HasMaxLength(254);
  m.HasIndex(x => x.Email).IsUnique();
  m.Property(x => x.PasswordHash).HasMaxLength(512);
  m.Property(x => x.Organization).HasMaxLength(20);
  var l = model.Entity<Listing>(); l.ToTable("Listings"); l.HasKey(x => x.Id);
  l.Property(x => x.Title).HasMaxLength(100); l.Property(x => x.Description).HasMaxLength(3000);
  l.Property(x => x.Type).HasMaxLength(16); l.Property(x => x.Category).HasMaxLength(30);
  l.Property(x => x.Price).HasPrecision(18,2); l.Property(x => x.Contact).HasMaxLength(254);
  l.Property(x => x.ImageMime).HasMaxLength(32);
  l.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
  l.HasIndex(x => x.CreatedAt);
 }
}
