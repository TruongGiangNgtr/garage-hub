using GarageHub.Common.Dtos.Ocr;
using GarageHub.Common.Exceptions;
using GarageHub.Service.Ocr;
using Microsoft.AspNetCore.Mvc;

namespace GarageHub.Web.Controllers;

[ApiController]
[Route("api/ocr")]
public class OcrApiController : ControllerBase
{
    private readonly IOcrService _ocrService;

    public OcrApiController(IOcrService ocrService)
    {
        _ocrService = ocrService;
    }

    // TODO: add [Authorize] once Google login + cookie auth is configured.
    [HttpPost("scan-plate")]
    public async Task<ActionResult<PlateScanResultDto>> ScanPlate(IFormFile? image, CancellationToken cancellationToken)
    {
        if (image is null || image.Length == 0)
        {
            throw new ValidationException("Vui lòng chọn ảnh biển số");
        }
        if (!image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("File tải lên phải là ảnh");
        }

        await using var stream = image.OpenReadStream();
        var result = await _ocrService.ScanPlateAsync(stream, image.FileName, cancellationToken);
        return Ok(result);
    }
}
