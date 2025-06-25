using HotChocolate.Types;
using ImageApi.Application.Background.Interfaces;
using ImageApi.Application.Services;
using ImageApi.Application.Services.Interfaces;
using ImageApi.Domain.Entities;
using ImageApi.Domain.Enums;
using ImageApi.Infrastructure.Persistence;
using ImageApi.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageApi.Tests
{
	public class ImageServiceTests
	{
		private const string testFile = "test.jpg";
		private const string testUrl = "https://example.com/";

		private readonly AppDbContext _dbContext;
		private readonly Mock<IBlobStorageService> _blobStorageMock;
		private readonly Mock<IThumbnailJobQueue> _jobQueueMock;
		private readonly ILogger<ImageService> _logger;
		private readonly ImageService _imageService;

		public ImageServiceTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			_dbContext = new AppDbContext(options);

			_blobStorageMock = new Mock<IBlobStorageService>();
			_jobQueueMock = new Mock<IThumbnailJobQueue>();
			_logger = new LoggerFactory().CreateLogger<ImageService>();

			_imageService = new ImageService(_dbContext, _blobStorageMock.Object, _jobQueueMock.Object, _logger);
		}

		[Fact]
		public async Task UploadImageAsync_Should_SaveImage_And_EnqueueJob()
		{
			var file = CreateMockFile();

			_blobStorageMock
				.Setup(b => b.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>()))
				.Returns(Task.CompletedTask);

			var imageId = await _imageService.UploadImageAsync(file);

			var imageInDb = await _dbContext.Images.FindAsync(imageId);
			Assert.NotNull(imageInDb);

			var jobInDb = await _dbContext.ThumbnailJobs.FirstOrDefaultAsync(j => j.ImageId == imageId);
			Assert.NotNull(jobInDb);
			Assert.Equal(ThumbnailJobStatus.Pending, jobInDb.Status);

			_jobQueueMock.Verify(q => q.Enqueue(It.Is<ThumbnailJob>(j => j.ImageId == imageId)), Times.Once);
		}

		[Fact]
		public async Task UploadImageAsync_Should_ThrowException_OnDuplicate()
		{
			var file = CreateMockFile();

			_blobStorageMock
				.Setup(b => b.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>()))
				.Returns(Task.CompletedTask);

			await _imageService.UploadImageAsync(file);

			await Assert.ThrowsAsync<ArgumentException>(() => _imageService.UploadImageAsync(file));
		}

		[Fact]
		public async Task GetImageAsync_Should_Return_ImageDto()
		{
			var file = CreateMockFile();
			_blobStorageMock.Setup(b => b.GetFileUrl(It.IsAny<string>())).Returns($"{testUrl}{testFile}");
			var imageId = await _imageService.UploadImageAsync(file);

			var imageDto = await _imageService.GetImageAsync(imageId);

			Assert.NotNull(imageDto);
			Assert.Equal(imageId, imageDto.Id);
			Assert.Contains($"{testUrl}{testFile}", imageDto.Url);
		}

		[Fact]
		public async Task GetImageAsync_Should_Throw_ImageNotFoundException()
		{
			var invalidId = Guid.NewGuid();

			await Assert.ThrowsAsync<ImageNotFoundException>(() => _imageService.GetImageAsync(invalidId));
		}

		[Fact]
		public async Task GetImageVariationUrlAsync_Should_Return_VariationUrl()
		{
			var height = 100;
			var file = CreateMockFile();
			var imageId = await _imageService.UploadImageAsync(file);

			var originalContent = new MemoryStream(LoadTestImage());
			_blobStorageMock.Setup(b => b.DownloadFileAsync(It.Is<string>(n => n.Contains(imageId.ToString()))))
				.ReturnsAsync(() =>
				{
					originalContent.Position = 0;
					return originalContent;
				});

			_blobStorageMock.Setup(b => b.UploadFileAsync(It.Is<string>(n => n.Contains($"{imageId}_{height}.jpg")), It.IsAny<Stream>()))
				.Returns(Task.CompletedTask);
			_blobStorageMock.Setup(b => b.GetFileUrl(It.Is<string>(n => n.Contains($"{imageId}_{height}.jpg"))))
				.Returns($"{testUrl}{imageId}_{height}.jpg");

			var url = await _imageService.GetImageVariationUrlAsync(imageId, height);

			Assert.Equal($"{testUrl}{imageId}_{height}.jpg", url);
			_blobStorageMock.Verify(b => b.UploadFileAsync(It.Is<string>(n => n.Contains($"{imageId}_{height}.jpg")),
				It.IsAny<Stream>()), Times.Once);
		}

		[Fact]
		public async Task GetImageVariationUrlAsync_Should_Throw_InvalidImageResizeException()
		{
			var file = CreateMockFile();
			var imageId = await _imageService.UploadImageAsync(file);

			var image = await _dbContext.Images.FindAsync(imageId);
			if (image == null) throw new InvalidOperationException("Image not found after upload");

			_blobStorageMock.Setup(b => b.DownloadFileAsync(It.Is<string>(n => n == image.FilePath))).ReturnsAsync(() =>
			{
				var stream = new MemoryStream();
				SixLabors.ImageSharp.Image.LoadPixelData([new Rgba32(255, 0, 0, 255)], 1, 1).SaveAsJpeg(stream);
				stream.Position = 0;
				return stream;
			});

			await Assert.ThrowsAsync<InvalidImageResizeException>(() => _imageService.GetImageVariationUrlAsync(imageId, 2));
		}

		[Fact]
		public async Task GetImageWithVariationsAsync_Should_Return_ImageWithVariations()
		{
			var height = 100;
			var file = CreateMockFile();
			var imageId = await _imageService.UploadImageAsync(file);
			var variation = new ImageVariation
			{
				Id = Guid.NewGuid(),
				ImageId = imageId,
				Height = height,
				FilePath = $"{imageId}_{height}.jpg"
			};
			_dbContext.ImageVariations.Add(variation);
			await _dbContext.SaveChangesAsync();
			_blobStorageMock.Setup(b => b.GetFileUrl(It.IsAny<string>())).Returns((string fileName) => $"{testUrl}{fileName}");

			var imageDto = await _imageService.GetImageWithVariationsAsync(imageId);

			Assert.NotNull(imageDto);
			Assert.Equal(imageId, imageDto.Id);
			Assert.Single(imageDto.Variations);
			Assert.Equal(height, imageDto.Variations[0].Height);
			Assert.Contains($"{imageId}_{height}.jpg", imageDto.Variations[0].Url);
		}

		[Fact]
		public async Task CreateThumbnailAsync_Should_Create_Thumbnail()
		{
			var height = 160;
			var file = CreateMockFile();
			var imageId = await _imageService.UploadImageAsync(file);

			var image = await _dbContext.Images.FindAsync(imageId);
			if (image == null) throw new InvalidOperationException("Image not found after upload");

			var originalContent = new MemoryStream(LoadTestImage());
			_blobStorageMock.Setup(b => b.DownloadFileAsync(It.Is<string>(n => n == image.FilePath)))
				.ReturnsAsync(() =>
				{
					originalContent.Position = 0;
					return originalContent;
				});

			_blobStorageMock.Setup(b => b.UploadFileAsync(It.Is<string>(n => n.Contains($"{imageId}_{height}.jpg")),
				It.IsAny<Stream>())).Returns(Task.CompletedTask);
			_blobStorageMock.Setup(b => b.GetFileUrl(It.Is<string>(n => n.Contains($"{imageId}_{height}.jpg"))))
				.Returns($"{testUrl}{imageId}_{height}.jpg");

			await _imageService.CreateThumbnailAsync(imageId, height);

			var variation = await _dbContext.ImageVariations
				.FirstOrDefaultAsync(v => v.ImageId == imageId && v.Height == height);
			Assert.NotNull(variation);
			Assert.Equal(height, variation.Height);
			_blobStorageMock.Verify(b => b.UploadFileAsync(It.Is<string>(n => n.Contains($"{imageId}_{height}.jpg")),
				It.IsAny<Stream>()), Times.Once);
		}

		[Fact]
		public async Task CreateThumbnailAsync_Should_Throw_InvalidImageResizeException()
		{
			var file = CreateMockFile();
			var imageId = await _imageService.UploadImageAsync(file);
			_blobStorageMock.Setup(b => b.DownloadFileAsync(It.IsAny<string>())).ReturnsAsync(() =>
			{
				var stream = new MemoryStream();
				SixLabors.ImageSharp.Image.LoadPixelData([new Rgba32(255, 0, 0, 255)], 1, 1).SaveAsJpeg(stream);
				stream.Position = 0;
				return stream;
			});

			await Assert.ThrowsAsync<InvalidImageResizeException>(() => _imageService.CreateThumbnailAsync(imageId, 2));
		}

		[Fact]
		public async Task DeleteImageAsync_Should_Delete_From_Blob_And_Db()
		{
			var height = 100;
			var file = CreateMockFile();

			_blobStorageMock
				.Setup(b => b.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>()))
				.Returns(Task.CompletedTask);
			_blobStorageMock
				.Setup(b => b.DeleteFileAsync(It.IsAny<string>()))
				.Returns(Task.CompletedTask);

			var imageId = await _imageService.UploadImageAsync(file);
			var variation = new ImageVariation
			{
				Id = Guid.NewGuid(),
				ImageId = imageId,
				Height = height,
				FilePath = $"{imageId}_{height}.jpg"
			};

			_dbContext.ImageVariations.Add(variation);
			await _dbContext.SaveChangesAsync();

			await _imageService.DeleteImageAsync(imageId);

			var imageInDb = await _dbContext.Images.FindAsync(imageId);
			Assert.Null(imageInDb);
		}

		private byte[] LoadTestImage()
		{
			var assembly = typeof(ImageServiceTests).Assembly;
			using var stream = assembly.GetManifestResourceStream($"ImageApi.Tests.TestData.{testFile}")
				?? throw new FileNotFoundException("Embedded resource not found");
			using var ms = new MemoryStream();
			stream.CopyTo(ms);
			return ms.ToArray();
		}

		private IFile CreateMockFile()
		{
			var content = LoadTestImage();

			var fileMock = new Mock<IFile>();
			fileMock.Setup(f => f.Name).Returns(testFile);
			fileMock.Setup(f => f.Length).Returns(content.Length);
			fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));

			return fileMock.Object;
		}
	}
}
