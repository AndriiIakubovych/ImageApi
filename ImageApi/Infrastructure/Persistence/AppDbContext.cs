using Microsoft.EntityFrameworkCore;
using ImageApi.Domain.Entities;

namespace ImageApi.Infrastructure.Persistence
{
	public class AppDbContext : DbContext
	{
		public DbSet<Image> Images => Set<Image>();
		public DbSet<ImageVariation> ImageVariations => Set<ImageVariation>();
		public DbSet<ThumbnailJob> ThumbnailJobs => Set<ThumbnailJob>();

		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Image>()
				.HasMany(i => i.Variations)
				.WithOne(iv => iv.Image)
				.HasForeignKey(iv => iv.ImageId);

			modelBuilder.Entity<ImageVariation>()
				.HasIndex(iv => new { iv.ImageId, iv.Height })
				.IsUnique();

			modelBuilder.Entity<ThumbnailJob>()
				.HasOne(j => j.Image)
				.WithMany()
				.HasForeignKey(j => j.ImageId);

			modelBuilder.Entity<ThumbnailJob>()
				.Property(e => e.Status)
				.HasConversion<string>();

			base.OnModelCreating(modelBuilder);
		}
	}
}
