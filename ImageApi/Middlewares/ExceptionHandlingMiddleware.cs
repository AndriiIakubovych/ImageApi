using ImageApi.Shared.Exceptions;

namespace ImageApi.Middlewares
{
	public class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<ExceptionHandlingMiddleware> _logger;

		public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
		{
			_next = next ?? throw new ArgumentNullException(nameof(next));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (InvalidImageResizeException ex)
			{
				_logger.LogWarning(ex, "Invalid image resize request");
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				await context.Response.WriteAsJsonAsync(new { error = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Internal server error occurred: {Message}", ex.Message);
				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
				await context.Response.WriteAsJsonAsync(new { error = "Internal Server Error", details = ex.ToString() });
			}
		}
	}
}
