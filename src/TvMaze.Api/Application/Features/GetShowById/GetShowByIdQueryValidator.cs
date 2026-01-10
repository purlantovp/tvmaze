using FluentValidation;

namespace TvMaze.Api.Application.Features.GetShowById;

public class GetShowByIdQueryValidator : AbstractValidator<GetShowByIdQuery>
{
    public GetShowByIdQueryValidator()
    {
        RuleFor(x => x.ShowId)
            .GreaterThan(0)
            .WithMessage("Show ID must be greater than 0");
    }
}
