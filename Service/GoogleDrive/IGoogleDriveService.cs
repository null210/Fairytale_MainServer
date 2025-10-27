namespace MainServer.Service.GoogleDrive
{
    public interface IGoogleDriveService
    {
        Task<string> UploadFile(byte[] fileData, string fileName);
        Task<byte[]> DownloadFile(string fileId);
        Task DeleteFile(string fileId);
        Task<List<Google.Apis.Drive.v3.Data.File>> ListFiles(string folderId = null);
    }
}
