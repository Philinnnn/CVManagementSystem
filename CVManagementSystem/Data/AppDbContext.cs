namespace CVManagementSystem.Data;

using Models.Attributes;
using Models.Candidates;
using Models.Cvs;
using Models.Discussions;
using Models.Identity;
using Models.Positions;
using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AttributeDefinition> Attributes => Set<AttributeDefinition>();
    public DbSet<AttributeSelectOption> AttributeSelectOptions => Set<AttributeSelectOption>();
    
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAttribute> PositionAttributes => Set<PositionAttribute>();
    public DbSet<PositionAccessRule> PositionAccessRules => Set<PositionAccessRule>();
    
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateAttributeValue> CandidateAttributeValues => Set<CandidateAttributeValue>();
    public DbSet<CandidateProject> Projects => Set<CandidateProject>();
    
    public DbSet<Cv> Cvs => Set<Cv>();
    public DbSet<CvAttributeValue> CvAttributeValues => Set<CvAttributeValue>();
    public DbSet<CvLike> CvLikes => Set<CvLike>();
    
    public DbSet<Discussion> Discussions => Set<Discussion>();
    public DbSet<DiscussionMessage> DiscussionMessages => Set<DiscussionMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.Entity<PositionAttribute>()
            .HasKey(pa => new { pa.PositionId, pa.AttributeId });
        builder.Entity<CandidateAttributeValue>()
            .HasKey(cav => new { cav.CandidateId, cav.AttributeId });
        builder.Entity<CvLike>()
            .HasKey(cl => new { cl.CvId, cl.UserId });
        
        builder.Entity<User>()
            .HasIndex(u => u.Login)
            .IsUnique();
        builder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        builder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();
        builder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();
        builder.Entity<Candidate>()
            .HasIndex(c => c.UserId)
            .IsUnique();
        builder.Entity<Discussion>()
            .HasIndex(d => d.PositionId)
            .IsUnique();
        
        builder.Entity<AttributeSelectOption>()
            .HasIndex(aso => aso.AttributeId);
        builder.Entity<Position>()
            .HasIndex(p => p.CreatedBy);
        builder.Entity<Position>()
            .HasIndex(p => p.Open);
        builder.Entity<CandidateProject>()
            .HasIndex(cp => cp.CandidateId);
        builder.Entity<CvAttributeValue>()
            .HasIndex(cav => cav.CvId);
        builder.Entity<CvAttributeValue>()
            .HasIndex(cav => cav.AttributeId);
        builder.Entity<DiscussionMessage>()
            .HasIndex(dm => dm.DiscussionId);
        builder.Entity<DiscussionMessage>()
            .HasIndex(dm => dm.UserId);
        
        builder.Entity<Cv>()
            .Property(c => c.Version)
            .IsConcurrencyToken();
    }
}