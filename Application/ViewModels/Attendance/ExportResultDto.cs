namespace Application.ViewModels.Attendance;

public class ExportResultDto
{
    public byte[] Content { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = "application/octet-stream";
}
