namespace TvMaze.Api.Models;

public class Show
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<CastMember> Cast { get; set; } = new List<CastMember>();
}
