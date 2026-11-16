namespace GarageHub.Service.Ocr;

public interface IPlateRecognizerClient
{
    /// <summary>
    /// Sends the image to Plate Recognizer and returns the best plate, or null when none was read.
    /// Throws ValidationException when Plate Recognizer rejects the image.
    /// </summary>
    Task<PlateReadResult?> ReadPlateAsync(Stream image, string fileName, CancellationToken cancellationToken = default);
}

/// <param name="Plate">Plate text as returned by Plate Recognizer (lowercase, no separators).</param>
/// <param name="Score">Confidence from 0 to 1.</param>
public record PlateReadResult(string Plate, double Score);
