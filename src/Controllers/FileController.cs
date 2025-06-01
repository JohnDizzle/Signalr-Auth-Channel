using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthChannel.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public FileController(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _webHostEnvironment = env;
    }
   
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Connect to Azure Blob Storage using Azure.Storage.Blobs  
            var blobServiceClient = new BlobServiceClient(_configuration["AzureStorage"]);
            var containerClient = blobServiceClient.GetBlobContainerClient("uploads");

            // Ensure the container exists  
            await containerClient.CreateIfNotExistsAsync();

            // Generate a unique file name  
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            // Upload the file to Azure Blob Storage  
            var blobClient = containerClient.GetBlobClient(fileName);
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            // Return success response  
            return Ok($"File uploaded successfully. Blob URL: {blobClient.Uri}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error uploading file: {ex.Message}");
        }
    }

}


