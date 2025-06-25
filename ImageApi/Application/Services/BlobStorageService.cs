using Azure.Storage.Blobs;
using ImageApi.Application.Services.Interfaces;

namespace ImageApi.Application.Services
{
	public class BlobStorageService : IBlobStorageService
	{
		private readonly BlobServiceClient _blobServiceClient;
		private readonly string _containerName = "images";

		public BlobStorageService(string connectionString)
		{
			_blobServiceClient = new BlobServiceClient(connectionString);
			InitializeContainer().GetAwaiter().GetResult();
		}

		public async Task UploadFileAsync(string fileName, Stream content)
		{
			var container = _blobServiceClient.GetBlobContainerClient(_containerName);
			var blob = container.GetBlobClient(fileName);
			await blob.UploadAsync(content, overwrite: true);
		}

		public string GetFileUrl(string fileName)
		{
			var container = _blobServiceClient.GetBlobContainerClient(_containerName);
			var blob = container.GetBlobClient(fileName);
			return blob.Uri.ToString();
		}

		public async Task<Stream> DownloadFileAsync(string fileName)
		{
			var container = _blobServiceClient.GetBlobContainerClient(_containerName);
			var blob = container.GetBlobClient(fileName);
			var response = await blob.DownloadAsync();
			return response.Value.Content;
		}

		public async Task DeleteFileAsync(string fileName)
		{
			var container = _blobServiceClient.GetBlobContainerClient(_containerName);
			var blob = container.GetBlobClient(fileName);
			await blob.DeleteIfExistsAsync();
		}

		private async Task InitializeContainer()
		{
			var container = _blobServiceClient.GetBlobContainerClient(_containerName);
			await container.CreateIfNotExistsAsync();
		}
	}
}
