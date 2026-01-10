using System.Text.Json.Serialization;

namespace TvMaze.Api.Models.TvMazeDtos;

public class TvMazePerson
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("birthday")]
    public DateTime? Birthday { get; set; }
}
