using ImageApi.Application.Background.Interfaces;
using ImageApi.Application.Services.Interfaces;
using ImageApi.Domain.Enums;
using ImageApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImageApi.Application.Background
{
	public class ThumbnailGenerationBackgroundService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly IThumbnailJobQueue _jobQueue;
		private readonly ILogger<ThumbnailGenerationBackgroundService> _logger;

		public ThumbnailGenerationBackgroundService(
			IServiceProvider serviceProvider,
			IThumbnailJobQueue jobQueue,
			ILogger<ThumbnailGenerationBackgroundService> logger)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Thumbnail Background Service started");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					_logger.LogInformation("Checking queue for jobs");
					if (_jobQueue.TryDequeue(out var job) && job != null)
					{
						_logger.LogInformation("Processing thumbnail job {JobId} for image {ImageId}", job.Id, job.ImageId);
						job.Status = ThumbnailJobStatus.InProgress;

						using var scope = _serviceProvider.CreateScope();
						var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
						var trackedJob = dbContext.ThumbnailJobs.Attach(job);
						trackedJob.State = EntityState.Modified;
						await dbContext.SaveChangesAsync(stoppingToken);
						_logger.LogInformation("Saved InProgress status for job {JobId}", job.Id);

						try
						{
							var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
							await imageService.CreateThumbnailAsync(job.ImageId, 160);

							job.Status = ThumbnailJobStatus.Completed;
							job.ErrorMessage = null;
							trackedJob.State = EntityState.Modified;
							await dbContext.SaveChangesAsync(stoppingToken);
							_logger.LogInformation("Saved Completed status for job {JobId}", job.Id);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Failed processing thumbnail job {JobId}", job.Id);
							job.Status = ThumbnailJobStatus.Failed;
							job.ErrorMessage = ex.Message;
							trackedJob.State = EntityState.Modified;
							await dbContext.SaveChangesAsync(stoppingToken);
							_logger.LogInformation("Saved Failed status for job {JobId}", job.Id);
						}
					}
					else
					{
						await Task.Delay(1000, stoppingToken);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Unhandled error in background service");
					await Task.Delay(2000, stoppingToken);
				}
			}

			_logger.LogInformation("Thumbnail Background Service stopped");
		}
	}
}
