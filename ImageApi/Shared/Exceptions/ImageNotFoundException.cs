namespace ImageApi.Shared.Exceptions
{
	public class ImageNotFoundException : Exception
	{
		public ImageNotFoundException(Guid id)
			: base($"Image with ID {id} not found.") { }

		public ImageNotFoundException(string message)
			: base(message) { }

		public ImageNotFoundException(string message, Exception inner)
			: base(message, inner) { }
	}
}
