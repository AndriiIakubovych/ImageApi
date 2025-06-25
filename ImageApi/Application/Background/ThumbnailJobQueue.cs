using ImageApi.Application.Background.Interfaces;
using ImageApi.Domain.Entities;
using System.Collections.Concurrent;

namespace ImageApi.Application.Background
{
	public class ThumbnailJobQueue : IThumbnailJobQueue
	{
		private readonly ConcurrentQueue<ThumbnailJob> _queue = new();

		public void Enqueue(ThumbnailJob job)
		{
			ArgumentNullException.ThrowIfNull(job);
			_queue.Enqueue(job);
		}

		public bool TryDequeue(out ThumbnailJob? job)
		{
			return _queue.TryDequeue(out job);
		}
	}
}
