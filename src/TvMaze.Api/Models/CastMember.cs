namespace TvMaze.Api.Models;

public class CastMember
{
    public int CastMemberId { get; set; }  // Actor ID from TVMaze
    public int ShowId { get; set; }       // Show ID from TVMaze
    public string Name { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }

    // Navigation property
    public Show? Show { get; set; }
}
