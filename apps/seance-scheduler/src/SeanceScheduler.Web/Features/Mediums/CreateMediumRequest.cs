namespace SeanceScheduler.Web.Features.Mediums;

/// <summary>
/// Input contract for POST /api/mediums
/// </summary>
public sealed record CreateMediumRequest(string? Name, string? Specialty)
{
    public const int NameMaxLength = 100;
    public const int SpecialtyMaxLength = 200;

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors[nameof(Name)] = ["Name is required."];
        }
        else if (Name.Trim().Length > NameMaxLength)
        {
            errors[nameof(Name)] = [$"Name must be at most {NameMaxLength} characters."];
        }

        if (Specialty is not null && Specialty.Trim().Length > SpecialtyMaxLength)
        {
            errors[nameof(Specialty)] = [$"Specialty must be at most {SpecialtyMaxLength} characters."];
        }

        return errors;
    }
}