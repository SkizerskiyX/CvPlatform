namespace CvPlatform.Infrastructure.Integrations.Odoo;

public sealed class PositionExportToken
{
    public Guid PositionId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}
