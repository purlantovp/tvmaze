namespace TvMaze.Api.Configuration;

public class CacheSettings
{
    public int ShowByIdCacheDurationMinutes { get; set; } = 30;
    public int ShowsListCacheDurationMinutes { get; set; } = 10;
    public int ShowCountCacheDurationMinutes { get; set; } = 15;
    public bool EnableCaching { get; set; } = true;
}
