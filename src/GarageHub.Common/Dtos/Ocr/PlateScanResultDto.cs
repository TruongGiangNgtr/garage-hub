namespace GarageHub.Common.Dtos.Ocr;

/// <param name="PlateNumber">Recognized plate, uppercase without separators (e.g. "51G12345"). Null when no plate was read.</param>
/// <param name="Confidence">OCR confidence from 0 to 1. Null when no plate was read.</param>
/// <param name="VehicleFound">True when a registered vehicle has this plate; Brand and Model are then filled.</param>
public record PlateScanResultDto(string? PlateNumber, double? Confidence, bool VehicleFound, string? Brand, string? Model);
