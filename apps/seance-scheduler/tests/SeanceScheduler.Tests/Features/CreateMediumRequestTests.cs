using SeanceScheduler.Web.Features.Mediums;
using Xunit;

namespace SeanceScheduler.Tests.Features;

/// <summary>Input validation lives in the request contract; the service assumes validated input.</summary>
public class CreateMediumRequestTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingName_ReturnsNameError(string? name)
    {
        var errors = new CreateMediumRequest(name, null).Validate();

        Assert.Equal(["Name"], errors.Keys);
    }

    [Fact]
    public void Validate_NameAtLimit_IsValid_AndOneOver_IsRejected()
    {
        Assert.Empty(new CreateMediumRequest(new string('a', 100), null).Validate());
        Assert.Equal(["Name"], new CreateMediumRequest(new string('a', 101), null).Validate().Keys);
    }

    [Fact]
    public void Validate_NameLengthIsMeasuredAfterTrim()
    {
        Assert.Empty(new CreateMediumRequest($"  {new string('a', 100)}  ", null).Validate());
    }

    [Fact]
    public void Validate_SpecialtyOverLimit_ReturnsSpecialtyError()
    {
        Assert.Empty(new CreateMediumRequest("Leota", new string('s', 200)).Validate());
        Assert.Empty(new CreateMediumRequest("Leota", $"  {new string('s', 200)}  ").Validate());
        Assert.Equal(["Specialty"], new CreateMediumRequest("Leota", new string('s', 201)).Validate().Keys);
    }
}
