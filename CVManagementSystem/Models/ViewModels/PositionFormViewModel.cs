namespace CVManagementSystem.Models.ViewModels;

public class PositionFormViewModel
{
    public int Id { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public int MaxProjects { get; set; }
    public bool Open { get; set; } = true;
    public string? ProjectTagsRaw { get; set; }

    public int[] SelectedAttributeIds { get; set; } = [];
    public int[] RequiredAttributeIds { get; set; } = [];

    public int[] RuleAttributeIds { get; set; } = [];
    public string[] RuleOperators { get; set; } = [];
    public string[] RuleExpectedValues { get; set; } = [];
}