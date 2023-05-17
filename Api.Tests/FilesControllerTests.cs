using Api.Controllers;
using Api.Requests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Serilog;

namespace Api.Tests;

public class FilesControllerTests
{
    private readonly Mock<ILogger> _logger;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly FilesController _controller;

    public FilesControllerTests()
    {
        _logger = new Mock<ILogger>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _configurationMock = new Mock<IConfiguration>();
        _controller = new FilesController(_environmentMock.Object, _logger.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenFileIsNull()
    {
        // Arrange
        var request = new FileUploadRequest
        {
            FileName = "test.txt",
            FolderPath = "uploads"
        };

        // Act
        var result = await _controller.UploadFile(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please provide a file to upload.", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenFileNameIsNullOrEmpty()
    {
        // Arrange
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.Length).Returns(10);

        var request = new FileUploadRequest
        {
            FileName = null,
            FolderPath = "uploads",
            File = formFileMock.Object
        };

        // Act
        var result = await _controller.UploadFile(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please provide a file name.", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenFolderPathIsNullOrEmpty()
    {
        // Arrange
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.Length).Returns(10);

        var request = new FileUploadRequest
        {
            FileName = "test.txt",
            FolderPath = null,
            File = formFileMock.Object
        };

        // Act
        var result = await _controller.UploadFile(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please provide a folder path.", badRequestResult.Value);
    }

    [Fact(Skip = "Tight IO coupling, need to abstract filesystem provider later")]
    public async Task UploadFile_ReturnsOkResult_WhenFileIsUploadedSuccessfully()
    {
        // Arrange
        var request = new FileUploadRequest
        {
            FileName = "test.txt",
            FolderPath = "uploads"
        };

        var filePath = Path.Combine(_environmentMock.Object.ContentRootPath, request.FolderPath, request.FileName);

        var fileStreamMock = new Mock<FileStream>(filePath, FileMode.Create);
        fileStreamMock.Setup(m => m.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.Length).Returns(10);

        formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), CancellationToken.None))
            .Callback<Stream, CancellationToken>((stream, token) =>
            {
                fileStreamMock.Object.CopyToAsync(stream);
            })
            .Returns(Task.CompletedTask);

        request.File = formFileMock.Object;

        _environmentMock.Setup(x => x.ContentRootPath).Returns("C:\\myapp");

        var controller = new FilesController(_environmentMock.Object, _logger.Object, _configurationMock.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext,
        };

        // Act
        var result = await controller.UploadFile(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal($"File '{request.FileName}' uploaded successfully to '{request.FolderPath}'.", okResult.Value);
        Assert.False(string.IsNullOrEmpty(filePath));
    }


    [Fact]
    public async Task UploadFile_ReturnsStatusCode500_WhenAnExceptionOccurs()
    {
        // Arrange
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.Length).Returns(10);
        formFileMock.Setup(f => f.FileName).Returns("file.txt");

        var fileUploadRequest = new FileUploadRequest
        {
            FileName = "file.txt",
            FolderPath = "uploads",
            File = formFileMock.Object
        };

        var exceptionMessage = "An error occurred while uploading the file.";
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.Setup(m => m.ContentRootPath).Returns("C:/test");

        var mockFileStream = new Mock<FileStream>();
        mockFileStream.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception(exceptionMessage));

        var controller = new FilesController(mockEnvironment.Object, _logger.Object, _configurationMock.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext,
        };

        // Act
        var result = await controller.UploadFile(fileUploadRequest) as ObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal(exceptionMessage, result.Value);
        _logger.Verify(x => x.Error(It.IsAny<string>()), Times.Once);
    }
}