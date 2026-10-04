namespace CVManagementSystem.Services.Common;

public class SupportTicketDto
{
    public string ReportedBy { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string Link { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public List<string> AdminEmails { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;
}