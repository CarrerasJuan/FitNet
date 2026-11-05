namespace FitNet.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public int StatusCode { get; set; } = 500;
    public string? Title { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Path { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
