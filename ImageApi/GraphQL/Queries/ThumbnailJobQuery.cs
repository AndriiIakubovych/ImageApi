using ImageApi.Domain.Entities;
using ImageApi.Infrastructure.Persistence;

namespace ImageApi.GraphQL.Queries
{
	public class ThumbnailJobQuery
	{
		public async Task<ThumbnailJob?> GetThumbnailJobStatus(Guid id, [Service] AppDbContext db)
		{
			return await db.ThumbnailJobs.FindAsync(id);
		}
	}
}
