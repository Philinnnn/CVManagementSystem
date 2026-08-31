namespace CVManagementSystem.Models.Cvs;

using Identity;

public class CvLike
{
    public int CvId { get; set; }
    public Cv Cv { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}