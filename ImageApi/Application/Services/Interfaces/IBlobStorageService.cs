namespace ImageApi.Application.Services.Interfaces
{
	public interface IBlobStorageService
	{
		Task UploadFileAsync(string fileName, Stream stream);
		string GetFileUrl(string fileName);
		Task<Stream> DownloadFileAsync(string fileName);
		Task DeleteFileAsync(string fileName);
	}
}
