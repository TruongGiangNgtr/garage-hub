using GarageHub.Common.Dtos.Ocr;

namespace GarageHub.Service.Ocr;

public interface IOcrService
{
    /// <summary>Reads the license plate in the image and looks up a vehicle owned by the specified user.</summary>
    Task<PlateScanResultDto> ScanPlateAsync(int ownerId, Stream image, string fileName, CancellationToken cancellationToken = default);
}
