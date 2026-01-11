using Microsoft.EntityFrameworkCore;
using TvMaze.Api.Models;

namespace TvMaze.Api.Data;

public class TvMazeContext : DbContext, ITvMazeContext
{
    public TvMazeContext(DbContextOptions<TvMazeContext> options) : base(options)
    {
    }

    public DbSet<Show> Shows { get; set; }
    public DbSet<CastMember> CastMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Show>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasMany(e => e.Cast)
                  .WithOne(e => e.Show)
                  .HasForeignKey(e => e.ShowId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CastMember>(entity =>
        {
            entity.HasKey(e => new { e.CastMemberId, e.ShowId });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Birthday).IsRequired(false);
        });
    }
}
