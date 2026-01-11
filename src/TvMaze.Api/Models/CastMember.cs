namespace TvMaze.Api.Models;

public class CastMember
{
    public int CastMemberId { get; set; } 
    public int ShowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }

    public Show? Show { get; set; }
}
