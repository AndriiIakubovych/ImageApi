namespace ImageApi.Shared.Exceptions
{
	public class InvalidImageResizeException : Exception
	{
		public InvalidImageResizeException(string message) : base(message) { }
		public InvalidImageResizeException(string message, Exception inner) : base(message, inner) { }
	}
}
