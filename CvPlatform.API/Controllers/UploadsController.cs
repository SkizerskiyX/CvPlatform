using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/uploads"), Authorize]
public sealed class UploadsController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("configuration")]
    public IActionResult Configuration()
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var uploadPreset = configuration["Cloudinary:UploadPreset"];
        return Ok(new { enabled = !string.IsNullOrWhiteSpace(cloudName) && !string.IsNullOrWhiteSpace(uploadPreset), cloudName, uploadPreset });
    }
}
