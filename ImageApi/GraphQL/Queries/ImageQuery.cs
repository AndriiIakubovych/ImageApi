using ImageApi.Application.Services.Interfaces;
using ImageApi.Shared.Dto;

namespace ImageApi.GraphQL.Queries
{
	public class ImageQuery
	{
		public async Task<ImageDto> GetImage(Guid id, [Service] IImageService imageService)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("Invalid ID", nameof(id));
			}
			return await imageService.GetImageAsync(id);
		}

		public async Task<string> GetImageVariation(Guid id, int height, [Service] IImageService imageService)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("Invalid ID", nameof(id));
			}
			return await imageService.GetImageVariationUrlAsync(id, height);
		}

		public async Task<ImageDto> GetImageWithVariations(Guid id, [Service] IImageService imageService)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("Invalid ID", nameof(id));
			}
			return await imageService.GetImageWithVariationsAsync(id);
		}
	}
}
