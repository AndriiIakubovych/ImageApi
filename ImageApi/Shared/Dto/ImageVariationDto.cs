using System.ComponentModel.DataAnnotations;

namespace ImageApi.Shared.Dto
{
	public class ImageVariationDto
	{
		[Required]
		[Range(1, int.MaxValue)]
		public int Height { get; set; }

		[Required]
		[MaxLength(2048)]
		public string Url { get; set; } = string.Empty;
	}
}
