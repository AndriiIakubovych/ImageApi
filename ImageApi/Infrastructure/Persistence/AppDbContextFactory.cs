using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ImageApi.Infrastructure.Persistence
{
	public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
	{
		public AppDbContext CreateDbContext(string[] args)
		{
			var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
			if (string.IsNullOrEmpty(connectionString))
			{
				throw new InvalidOperationException("DB_CONNECTION_STRING is not configured.");
			}

			var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
			optionsBuilder.UseNpgsql(connectionString);

			return new AppDbContext(optionsBuilder.Options);
		}
	}
}
