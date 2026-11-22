using System.Text.RegularExpressions;

namespace HauntedInventory.Web.Features.Items;

/// <summary>
/// Input contract for POST /api/items. Only client-settable fields are exposed (no Id, no timestamps).
/// </summary>
public sealed record CreateItemRequest(string? Name, string? Description, string? Sku, int? ReorderLevel)
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;
    public const int SkuMinLength = 3;
    public const int SkuMaxLength = 32;
    public const int ReorderLevelMinValue = 0;

    // SKU format: uppercase letters, digits, dashes only, 3-32 characters
    private static readonly Regex SkuRegex = new(@"^[A-Z0-9\-]{3,32}$", RegexOptions.Compiled);

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

        // Validate SKU: required, normalized, format check
        if (string.IsNullOrWhiteSpace(Sku))
        {
            errors[nameof(Sku)] = ["SKU is required."];
        }
        else
        {
            var normalizedSku = Sku.Trim().ToUpperInvariant();
            if (normalizedSku.Length < SkuMinLength || normalizedSku.Length > SkuMaxLength)
            {
                errors[nameof(Sku)] = [$"SKU must be between {SkuMinLength} and {SkuMaxLength} characters."];
            }
            else if (!SkuRegex.IsMatch(normalizedSku))
            {
                errors[nameof(Sku)] = ["SKU must contain only uppercase letters, digits, and dashes."];
            }
        }

        // Validate ReorderLevel
        if (ReorderLevel.HasValue && ReorderLevel < ReorderLevelMinValue)
        {
            errors[nameof(ReorderLevel)] = [$"Reorder level must be at least {ReorderLevelMinValue}."];
        }

        return errors;
    }

    /// <summary>
    /// Returns the normalized SKU (trimmed and uppercased) or null if not provided.
    /// </summary>
    public string? GetNormalizedSku() => string.IsNullOrWhiteSpace(Sku) ? null : Sku.Trim().ToUpperInvariant();
}
