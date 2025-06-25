using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImageApi.Domain.Entities
{
	public class ImageVariation
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		public Guid ImageId { get; set; }

		[Required]
		[Range(1, int.MaxValue)]
		public int Height { get; set; }

		[Required]
		[MaxLength(255)]
		public string FilePath { get; set; } = string.Empty;

		[ForeignKey(nameof(ImageId))]
		public Image Image { get; set; } = null!;
	}
}
