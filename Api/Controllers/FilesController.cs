using Api.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutSystems.FileServer.Api.Responses;

namespace Api.Controllers;
[ApiController]
[Route("[controller]")]
public class FilesController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly Serilog.ILogger _logger;
    private readonly IConfiguration _configuration;
    private const int ONE_MEGABYTE_IN_BYTES = 1048576;

    public FilesController(IWebHostEnvironment environment, Serilog.ILogger logger, IConfiguration configuration)
    {
        _environment = environment;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Uploads a file to a specified folder path.
    /// </summary>
    /// <param name="request">The file upload request.</param>
    /// <param name="overwrite">Indicates whether to overwrite the file if it already exists. Defaults to false.</param>
    /// <returns>Returns the result of the upload operation.</returns>
    /// <response code="200">Returns the message "File uploaded successfully to [folderPath]".</response>
    /// <response code="400">Returns a BadRequest indicating that the file was not provided, the file size exceeded the maximum allowed size, or the file name or folder path were not provided.</response>
    /// <response code="401">Returns a Unauthorized indicating that the JWT token was not valid or missing.</response>
    /// <response code="409">Returns a Conflict indicating that the file already exists and overwrite is false.</response>
    /// <response code="500">Returns an InternalServerError indicating that an error occurred while uploading the file.</response>
    [Authorize]
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadFile([FromForm] FileUploadRequest request, [FromQuery] bool overwrite = false)
    {
        var maxFileSizeInMb = _configuration.GetValue<int>("AppSettings:MaxFileSizeInMb");
        var maxFileSizeInBytes = maxFileSizeInMb * ONE_MEGABYTE_IN_BYTES;

        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("Please provide a file to upload.");
        }

        if (request.File.Length > maxFileSizeInBytes)
        {
            return BadRequest($"File size exceeded. Maximum allowed size is {maxFileSizeInMb} megabytes.");
        }

        if (string.IsNullOrEmpty(request?.FileName))
        {
            return BadRequest("Please provide a file name.");
        }

        if (string.IsNullOrEmpty(request?.FolderPath))
        {
            return BadRequest("Please provide a folder path.");
        }

        var fileName = request.FileName;
        var folderPath = request.FolderPath;

        var filePath = Path.Combine(_environment.ContentRootPath, folderPath, fileName);

        if (System.IO.File.Exists(filePath) && !overwrite)
        {
            return Conflict($"File '{fileName}' already exists in '{folderPath}'.");
        }

        try
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            return Ok($"File '{fileName}' uploaded successfully to '{folderPath}'.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the file.");
        }
    }

    /// <summary>
    /// Lists files from a specified folder path.
    /// </summary>
    /// <param name="folderPath">The folder path.</param>
    /// <param name="includeSubfolders">Indicates whether to include subfolders or not. Defaults to false.</param>
    /// <returns>Returns the result of the list operation.</returns>
    /// <response code="200">Returns the list of the files of the specified path.</response>
    /// <response code="400">Returns a BadRequest indicating that the folder path was not provided.</response>
    /// <response code="401">Returns a Unauthorized indicating that the JWT token was not valid or missing.</response>
    /// <response code="404">Returns a NotFound indicating that the folder path does not exist.</response>
    /// <response code="500">Returns an InternalServerError indicating that an error occurred while uploading the file.</response>
    [Authorize]
    [HttpGet("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ListFiles(string folderPath, bool includeSubfolders)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            return BadRequest("Please provide a folder path.");
        }

        var filePath = Path.Combine(_environment.ContentRootPath, folderPath);

        if (!Directory.Exists(filePath))
        {
            return NotFound("The provided folder path does not exist.");
        }

        try
        {
            var rootFolder = new FolderResponse { Name = Path.GetFileName(folderPath) };

            PopulateFolder(rootFolder, folderPath, includeSubfolders);

            return Ok(rootFolder);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while listing files from the provided folder path.");

            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while listing files from the provided folder path.");
        }
    }

    private void PopulateFolder(FolderResponse folderResponse, string folderPath, bool includeSubfolders)
    {
        folderResponse.Files = Directory.GetFiles(folderPath).Select(Path.GetFileName).ToList();

        if (includeSubfolders)
        {
            folderResponse.Folders = Directory.GetDirectories(folderPath).Select(d =>
            {
                var folder = new FolderResponse { Name = Path.GetFileName(d) };
                PopulateFolder(folder, d, true);
                return folder;
            }).ToList();
        }
    }

    /// <summary>
    /// Lists files from a specified folder path.
    /// </summary>
    /// <param name="folderPath">The folder path.</param>
    /// <param name="fileName">The name of the file do be download, including his extenbsion.</param>
    /// <returns>Returns the file to be downloaded.</returns>
    /// <response code="200">Returns the file.</response>
    /// <response code="400">Returns a BadRequest indicating that the folder path or the file name was not provided.</response>
    /// <response code="401">Returns a Unauthorized indicating that the JWT token was not valid or missing.</response>
    /// <response code="404">Returns a NotFound indicating that the file does not exist.</response>
    /// <response code="500">Returns an InternalServerError indicating that an error occurred while downloading the file.</response>
    [Authorize]
    [HttpGet("download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult DownloadFile([FromQuery] string folderPath, [FromQuery] string fileName)
    {
        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(fileName))
        {
            return BadRequest("Please provide a valid file path and file name.");
        }

        var filePath = Path.Combine(_environment.ContentRootPath, folderPath, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"File '{fileName}' not found in '{folderPath}'.");
        }

        try
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            return File(fileStream, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while downloading the file.");

            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while downloading the file.");
        }
    }
}
