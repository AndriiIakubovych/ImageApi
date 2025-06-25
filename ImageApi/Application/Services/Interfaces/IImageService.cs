using ImageApi.Shared.Dto;

namespace ImageApi.Application.Services.Interfaces
{
	public interface IImageService
	{
		Task<Guid> UploadImageAsync(IFile file);
		Task<ImageDto> GetImageAsync(Guid id);
		Task<string> GetImageVariationUrlAsync(Guid imageId, int height);
		Task<ImageDto> GetImageWithVariationsAsync(Guid id);
		Task DeleteImageAsync(Guid id);
		Task CreateThumbnailAsync(Guid imageId, int thumbnailHeight);
	}
}
