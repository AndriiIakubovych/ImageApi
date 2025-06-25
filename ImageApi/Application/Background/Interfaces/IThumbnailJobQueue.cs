using ImageApi.Domain.Entities;

namespace ImageApi.Application.Background.Interfaces
{
	public interface IThumbnailJobQueue
	{
		void Enqueue(ThumbnailJob job);
		bool TryDequeue(out ThumbnailJob? job);
	}
}
