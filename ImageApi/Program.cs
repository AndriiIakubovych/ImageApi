using ImageApi.Application.Background;
using ImageApi.Application.Background.Interfaces;
using ImageApi.Application.Services;
using ImageApi.Application.Services.Interfaces;
using ImageApi.GraphQL.ErrorFilters;
using ImageApi.GraphQL.Mutations;
using ImageApi.GraphQL.Queries;
using ImageApi.Infrastructure.Persistence;
using ImageApi.Middlewares;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

namespace ImageApi
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			
			var blobConnectionString = builder.Configuration.GetSection("AzureBlobStorage")["StorageKey"]
				?? throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

			var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
				?? throw new InvalidOperationException("Database connection string is not configured.");

			builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dbConnectionString));
			builder.Services.AddSingleton<IBlobStorageService>(new BlobStorageService(blobConnectionString));

			builder.Services.AddScoped<IImageService, ImageService>();
			builder.Services.AddSingleton<IThumbnailJobQueue, ThumbnailJobQueue>();
			builder.Services.AddHostedService<ThumbnailGenerationBackgroundService>();
			builder.Services.Configure<KestrelServerOptions>(options =>
				options.Limits.MaxRequestBodySize = builder.Configuration.GetValue("FileUpload:MaxRequestSizeInMb",
				100) * 1024 * 1024);

			builder.Services
				.AddGraphQLServer()
				.AddQueryType<RootQuery>()
				.AddMutationType<ImageMutation>()
				.AddType<UploadType>()
				.AddErrorFilter<GraphQLErrorFilter>();

			var app = builder.Build();

			app.UseMiddleware<ExceptionHandlingMiddleware>();
			app.UseHttpsRedirection();
			app.MapGraphQL();
			app.MapGet("/", () => "Image API started!");

			app.Run();
		}
	}
}
