using System.Text.Json.Serialization;

namespace TvMaze.Api.Models.TvMazeDtos;

public class TvMazeCastMember
{
    [JsonPropertyName("person")]
    public TvMazePerson Person { get; set; } = new();
}
