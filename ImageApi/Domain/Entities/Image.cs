using System.ComponentModel.DataAnnotations;

namespace ImageApi.Domain.Entities
{
	public class Image
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		[MaxLength(255)]
		public string FilePath { get; set; } = string.Empty;

		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(32)]
		public string Hash { get; set; } = string.Empty;

		public List<ImageVariation> Variations { get; set; } = new();
	}
}
