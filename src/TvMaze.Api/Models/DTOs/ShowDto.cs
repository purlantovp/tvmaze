namespace TvMaze.Api.Models.DTOs;

public class ShowDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<CastMemberDto> Cast { get; set; } = new();
}
