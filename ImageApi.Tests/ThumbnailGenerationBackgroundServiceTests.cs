using ImageApi.Application.Background;
using ImageApi.Application.Background.Interfaces;
using ImageApi.Application.Services.Interfaces;
using ImageApi.Domain.Entities;
using ImageApi.Domain.Enums;
using ImageApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace ImageApi.Tests
{
	public class ThumbnailGenerationBackgroundServiceTests
	{
		[Fact]
		public async Task BackgroundService_Should_Process_Job_From_Queue()
		{
			var height = 160;
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;
			var dbContext = new AppDbContext(options);

			var job = new ThumbnailJob
			{
				Id = Guid.NewGuid(),
				ImageId = Guid.NewGuid(),
				Status = ThumbnailJobStatus.Pending
			};
			dbContext.ThumbnailJobs.Add(job);
			await dbContext.SaveChangesAsync();

			var imageServiceMock = new Mock<IImageService>();
			var jobQueueMock = new Mock<IThumbnailJobQueue>();
			var loggerMock = new Mock<ILogger<ThumbnailGenerationBackgroundService>>();

			bool firstCall = true;
			jobQueueMock
				.Setup(q => q.TryDequeue(out It.Ref<ThumbnailJob?>.IsAny))
				.Returns((out ThumbnailJob outJob) =>
				{
					if (firstCall)
					{
						outJob = job;
						firstCall = false;
						return true;
					}
					outJob = null!;
					return false;
				});

			imageServiceMock
				.Setup(s => s.CreateThumbnailAsync(job.ImageId, height))
				.Returns(Task.CompletedTask);

			var serviceProviderMock = new Mock<IServiceProvider>();
			serviceProviderMock
				.Setup(sp => sp.GetService(typeof(AppDbContext)))
				.Returns(dbContext);
			serviceProviderMock
				.Setup(sp => sp.GetService(typeof(IImageService)))
				.Returns(imageServiceMock.Object);

			var scopeMock = new Mock<IServiceScope>();
			scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

			var scopeFactoryMock = new Mock<IServiceScopeFactory>();
			scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(scopeMock.Object);

			serviceProviderMock
				.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
				.Returns(scopeFactoryMock.Object);

			var service = new ThumbnailGenerationBackgroundService(
				serviceProviderMock.Object, jobQueueMock.Object, loggerMock.Object);

			var cancellationTokenSource = new CancellationTokenSource();

			var runTask = service.StartAsync(cancellationTokenSource.Token);
			await Task.Delay(500);

			imageServiceMock.Verify(s => s.CreateThumbnailAsync(job.ImageId, height), Times.Once);
			var updatedJob = await dbContext.ThumbnailJobs.FindAsync(job.Id);
			Assert.Equal(ThumbnailJobStatus.Completed, updatedJob?.Status);

			cancellationTokenSource.Cancel();
			await runTask;
		}
	}
}
