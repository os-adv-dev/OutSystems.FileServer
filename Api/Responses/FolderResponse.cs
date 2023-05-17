namespace OutSystems.FileServer.Api.Responses;

internal class FolderResponse
{
    public string Name { get; set; }
    public List<string?> Files { get; internal set; }
    public List<FolderResponse> Folders { get; internal set; }
}