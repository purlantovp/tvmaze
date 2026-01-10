namespace TvMaze.Api.Models.Results;

public class ScrapeResult
{
    public int TotalScraped { get; set; }
    public int NewShows { get; set; }
    public int UpdatedShows { get; set; }
    public int StartPage { get; set; }
    public int PageCount { get; set; }
}