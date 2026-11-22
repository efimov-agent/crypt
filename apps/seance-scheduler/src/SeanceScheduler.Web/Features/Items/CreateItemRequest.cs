namespace SeanceScheduler.Web.Features.Items;

/// <summary>
/// Input contract for POST /api/items. Only client-settable fields are exposed (no Id, no timestamps).
/// </summary>
public sealed record CreateItemRequest(string? Name, string? Description)
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

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

        if (Description is { Length: > DescriptionMaxLength })
        {
            errors[nameof(Description)] = [$"Description must be at most {DescriptionMaxLength} characters."];
        }

        return errors;
    }
}
