using System.ComponentModel.DataAnnotations;

namespace ImageApi.Shared.Dto
{
	public class ImageDto
	{
		[Required]
		public Guid Id { get; set; }

		[Required]
		[MaxLength(2048)]
		public string Url { get; set; } = string.Empty;

		[Required]
		public DateTime CreatedAt { get; set; }

		public List<ImageVariationDto> Variations { get; set; } = new();
	}
}
