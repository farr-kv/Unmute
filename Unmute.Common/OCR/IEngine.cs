namespace Flute.Common.OCR
{
    public interface IEngine
    {
        Task<IEnumerable<Result>> ReadTextAsync(byte[] imageBytes);
    }
}