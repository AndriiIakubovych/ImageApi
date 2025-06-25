using ImageApi.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ImageApi.Domain.Entities
{
	public class ThumbnailJob
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		public Guid ImageId { get; set; }

		[Required]
		public ThumbnailJobStatus Status { get; set; } = ThumbnailJobStatus.Pending;

		public string? ErrorMessage { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public Image Image { get; set; } = null!;
	}
}
