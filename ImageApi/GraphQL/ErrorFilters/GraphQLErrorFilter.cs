using ImageApi.Shared.Exceptions;

namespace ImageApi.GraphQL.ErrorFilters
{
	public class GraphQLErrorFilter : IErrorFilter
	{
		public IError OnError(IError error)
		{
			if (error.Exception is InvalidImageResizeException)
			{
				return error
					.WithMessage(error.Exception.Message)
					.WithCode("INVALID_RESIZE")
					.RemoveExtension("stackTrace");
			}

			return error;
		}
	}
}
