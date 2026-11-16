using GarageHub.Common.Dtos.Ocr;

namespace GarageHub.Service.Ocr;

public interface IOcrService
{
    /// <summary>Reads the license plate in the image and looks up a registered vehicle with that plate.</summary>
    Task<PlateScanResultDto> ScanPlateAsync(Stream image, string fileName, CancellationToken cancellationToken = default);
}
