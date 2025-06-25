using ImageApi.Application.Services.Interfaces;

namespace ImageApi.GraphQL.Mutations
{
	public class ImageMutation
	{
		public async Task<Guid> UploadImage(IFile file, [Service] IImageService imageService)
		{
			ArgumentNullException.ThrowIfNull(file);
			return await imageService.UploadImageAsync(file);
		}

		public async Task<bool> DeleteImage(Guid id, [Service] IImageService imageService)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("Invalid ID", nameof(id));
			}
			await imageService.DeleteImageAsync(id);
			return true;
		}
	}
}
