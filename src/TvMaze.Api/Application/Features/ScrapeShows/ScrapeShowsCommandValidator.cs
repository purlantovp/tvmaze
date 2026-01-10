using FluentValidation;

namespace TvMaze.Api.Application.Features.ScrapeShows;

public class ScrapeShowsCommandValidator : AbstractValidator<ScrapeShowsCommand>
{
    public ScrapeShowsCommandValidator()
    {
        RuleFor(x => x.StartPage)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Start page must be 0 or greater");

        RuleFor(x => x.PageCount)
            .GreaterThan(0)
            .WithMessage("Page count must be greater than 0")
            .LessThanOrEqualTo(50)
            .WithMessage("Page count must not exceed 50");
    }
}
