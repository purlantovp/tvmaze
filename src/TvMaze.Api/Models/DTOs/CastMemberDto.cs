namespace TvMaze.Api.Models.DTOs;

public class CastMemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
}
