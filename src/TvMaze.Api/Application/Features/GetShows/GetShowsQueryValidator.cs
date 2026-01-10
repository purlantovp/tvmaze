using FluentValidation;

namespace TvMaze.Api.Application.Features.GetShows;

public class GetShowsQueryValidator : AbstractValidator<GetShowsQuery>
{
    public GetShowsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must not exceed 100");

        RuleFor(x => x.OrderBy)
            .Must(BeValidOrderBy)
            .When(x => !string.IsNullOrWhiteSpace(x.OrderBy))
            .WithMessage("Order by must be 'name' or 'name_desc'");
    }

    private bool BeValidOrderBy(string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
            return true;

        var validOrderByValues = new[] { "name", "name_desc" };
        return validOrderByValues.Contains(orderBy.ToLower());
    }
}
