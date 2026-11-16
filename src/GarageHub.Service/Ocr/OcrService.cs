using GarageHub.Common.Dtos.Ocr;
using GarageHub.Repository.Entities;
using GarageHub.Repository.Repositories;

namespace GarageHub.Service.Ocr;

public class OcrService : IOcrService
{
    private readonly IPlateRecognizerClient _plateRecognizerClient;
    private readonly IRepository<Vehicle> _vehicleRepository;

    public OcrService(IPlateRecognizerClient plateRecognizerClient, IRepository<Vehicle> vehicleRepository)
    {
        _plateRecognizerClient = plateRecognizerClient;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<PlateScanResultDto> ScanPlateAsync(
        int ownerId, Stream image, string fileName, CancellationToken cancellationToken = default)
    {
        var read = await _plateRecognizerClient.ReadPlateAsync(image, fileName, cancellationToken);
        if (read is null)
        {
            return new PlateScanResultDto(null, null, false, null, null);
        }

        var plate = Normalize(read.Plate);

        // Stored plates may contain separators ("51G-123.45"), so compare without them.
        var matches = await _vehicleRepository.FindAsync(
            v => v.OwnerId == ownerId
                && v.PlateNumber.Replace("-", "").Replace(".", "").Replace(" ", "").ToUpper() == plate,
            cancellationToken);
        var vehicle = matches.FirstOrDefault();

        return new PlateScanResultDto(plate, read.Score, vehicle is not null, vehicle?.Brand, vehicle?.Model);
    }

    private static string Normalize(string plate) =>
        new string(plate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}
