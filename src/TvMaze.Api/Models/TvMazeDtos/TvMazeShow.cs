using System.Text.Json.Serialization;

namespace TvMaze.Api.Models.TvMazeDtos;

public class TvMazeShow
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
