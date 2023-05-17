namespace Api.Requests;

public class FileUploadRequest
{
    public string FileName { get; set; }

    public string FolderPath { get; set; }

    public IFormFile File { get; set; }
}