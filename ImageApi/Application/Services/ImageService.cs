using ImageApi.Application.Background.Interfaces;
using ImageApi.Application.Services.Interfaces;
using ImageApi.Domain.Entities;
using ImageApi.Domain.Enums;
using ImageApi.Infrastructure.Persistence;
using ImageApi.Shared.Dto;
using ImageApi.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;
using ImageSharp = SixLabors.ImageSharp;
using InputOutput = System.IO;

namespace ImageApi.Application.Services
{
	public class ImageService : IImageService
	{
		private readonly AppDbContext _dbContext;
		private readonly IBlobStorageService _blobStorageService;
		private readonly IThumbnailJobQueue _jobQueue;
		private readonly ILogger<ImageService> _logger;

		public ImageService(AppDbContext dbContext,
			IBlobStorageService blobStorageService,
			IThumbnailJobQueue jobQueue,
			ILogger<ImageService> logger)
		{
			_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			_blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
			_jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<Guid> UploadImageAsync(IFile file)
		{
			if (file == null || file.Length == 0)
			{
				throw new ArgumentException("File is empty or not provided.", nameof(file));
			}

			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
			var fileExtension = InputOutput.Path.GetExtension(file.Name).ToLowerInvariant();
			if (!allowedExtensions.Contains(fileExtension))
			{
				throw new ArgumentException("Invalid file type. Only *.jpg, *.jpeg, and *.png are allowed.", nameof(file));
			}

			using var stream = file.OpenReadStream();
			var hash = BitConverter.ToString(MD5.Create().ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
			stream.Position = 0;

			if (await _dbContext.Images.AnyAsync(i => i.Hash == hash))
			{
				throw new ArgumentException("Duplicate image detected.", nameof(file));
			}

			var fileId = Guid.NewGuid();
			var fileName = $"{fileId}{fileExtension}";

			await _blobStorageService.UploadFileAsync(fileName, stream);

			_logger.LogInformation("Uploaded file {FileName} to Azure Blob Storage", fileName);

			var image = new Image
			{
				Id = fileId,
				FilePath = fileName,
				Hash = hash,
				CreatedAt = DateTime.UtcNow
			};

			_dbContext.Images.Add(image);

			var thumbnailJob = new ThumbnailJob
			{
				Id = Guid.NewGuid(),
				ImageId = fileId,
				Status = ThumbnailJobStatus.Pending
			};

			_dbContext.ThumbnailJobs.Add(thumbnailJob);
			await _dbContext.SaveChangesAsync();

			_logger.LogInformation("Enqueuing thumbnail job {JobId} for image {ImageId}", thumbnailJob.Id, fileId);
			_jobQueue.Enqueue(thumbnailJob);

			return fileId;
		}

		public async Task<ImageDto> GetImageAsync(Guid id)
		{
			var image = await _dbContext.Images.FindAsync(id)
				?? throw new ImageNotFoundException(id);

			return new ImageDto
			{
				Id = image.Id,
				Url = _blobStorageService.GetFileUrl(image.FilePath),
				CreatedAt = image.CreatedAt
			};
		}

		public async Task<string> GetImageVariationUrlAsync(Guid imageId, int height)
		{
			var image = await _dbContext.Images.FindAsync(imageId)
				?? throw new ImageNotFoundException(imageId);

			if (image.Variations.Any(v => v.Height == height))
			{
				var existing = image.Variations.First(v => v.Height == height);
				return _blobStorageService.GetFileUrl(existing.FilePath);
			}

			using var originalStream = await _blobStorageService.DownloadFileAsync(image.FilePath);
			using var memoryStream = new MemoryStream();
			await originalStream.CopyToAsync(memoryStream);
			memoryStream.Position = 0;

			using var originalImage = await ImageSharp.Image.LoadAsync(memoryStream);
			if (height > originalImage.Height)
			{
				throw new InvalidImageResizeException("Requested height exceeds original image height");
			}

			var ratio = (double)height / originalImage.Height;
			var width = (int)(originalImage.Width * ratio);

			using var thumbnail = originalImage.Clone(x => x.Resize(width, height));
			using var outputStream = new MemoryStream();
			await thumbnail.SaveAsync(outputStream, new JpegEncoder());
			outputStream.Position = 0;

			var variationFileName = $"{imageId}_{height}.jpg";
			await _blobStorageService.UploadFileAsync(variationFileName, outputStream);

			var variation = new ImageVariation
			{
				Id = Guid.NewGuid(),
				ImageId = imageId,
				Height = height,
				FilePath = variationFileName
			};

			_dbContext.ImageVariations.Add(variation);
			await _dbContext.SaveChangesAsync();

			return _blobStorageService.GetFileUrl(variationFileName);
		}

		public async Task<ImageDto> GetImageWithVariationsAsync(Guid id)
		{
			var image = await _dbContext.Images
				.Include(i => i.Variations)
				.FirstOrDefaultAsync(i => i.Id == id)
				?? throw new ImageNotFoundException(id);

			return new ImageDto
			{
				Id = image.Id,
				Url = _blobStorageService.GetFileUrl(image.FilePath),
				CreatedAt = image.CreatedAt,
				Variations = [.. image.Variations.Select(v => new ImageVariationDto
				{
					Height = v.Height,
					Url = _blobStorageService.GetFileUrl(v.FilePath)
				})]
			};
		}

		public async Task DeleteImageAsync(Guid id)
		{
			var image = await _dbContext.Images
				.Include(i => i.Variations)
				.FirstOrDefaultAsync(i => i.Id == id)
				?? throw new ImageNotFoundException(id);

			_logger.LogInformation("Deleting image {ImageId} with all variations", id);

			await _blobStorageService.DeleteFileAsync(image.FilePath);
			foreach (var variation in image.Variations)
			{
				await _blobStorageService.DeleteFileAsync(variation.FilePath);
			}

			_dbContext.ImageVariations.RemoveRange(image.Variations);
			_dbContext.Images.Remove(image);
			await _dbContext.SaveChangesAsync();

			_logger.LogInformation("Deleted image {ImageId} and all files successfully", id);
		}

		public async Task CreateThumbnailAsync(Guid imageId, int thumbnailHeight)
		{
			var image = await _dbContext.Images.Include(i => i.Variations).FirstOrDefaultAsync(i => i.Id == imageId)
				?? throw new ImageNotFoundException(imageId);

			if (image.Variations.Any(v => v.Height == thumbnailHeight))
			{
				_logger.LogInformation("Thumbnail already exists for image {ImageId} height {Height}", imageId, thumbnailHeight);
				return;
			}

			using var originalStream = await _blobStorageService.DownloadFileAsync(image.FilePath);
			using var memoryStream = new MemoryStream();
			await originalStream.CopyToAsync(memoryStream);
			memoryStream.Position = 0;

			using var originalImage = await ImageSharp.Image.LoadAsync(memoryStream);
			if (thumbnailHeight > originalImage.Height)
			{
				throw new InvalidImageResizeException("Thumbnail height exceeds original image height");
			}

			var ratio = (double)thumbnailHeight / originalImage.Height;
			var width = (int)(originalImage.Width * ratio);

			using var thumbnail = originalImage.Clone(x => x.Resize(width, thumbnailHeight));
			using var outputStream = new MemoryStream();
			await thumbnail.SaveAsync(outputStream, new JpegEncoder());
			outputStream.Position = 0;

			var variationFileName = $"{imageId}_{thumbnailHeight}.jpg";
			await _blobStorageService.UploadFileAsync(variationFileName, outputStream);

			var variation = new ImageVariation
			{
				Id = Guid.NewGuid(),
				ImageId = imageId,
				Height = thumbnailHeight,
				FilePath = variationFileName
			};

			_dbContext.ImageVariations.Add(variation);
			await _dbContext.SaveChangesAsync();
		}
	}
}
