namespace CVManagementSystem.Models.ViewModels;

using Services.Candidates.Dtos;
using Services.Cvs.Dtos;

public class CandidateProfileViewModel
{
    public ProfileDto Profile { get; set; } = null!;
    public List<ProjectDto> Projects { get; set; } = [];
    public List<CvListItemDto> Cvs { get; set; } = [];
}