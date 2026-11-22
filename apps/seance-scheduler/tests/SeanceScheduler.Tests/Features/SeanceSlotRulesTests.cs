using SeanceScheduler.Web.Features.Seances;
using Xunit;

namespace SeanceScheduler.Tests.Features;

public class SeanceSlotRulesTests
{
    private static readonly DateTime Now = new(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Start = new(2030, 6, 2, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void LocalAndUtcKinds_AreComparedAsUtc()
    {
        // Same instants expressed in different kinds must give the same result on any host time zone.
        Assert.Empty(SeanceSlotRules.Check(Start.ToLocalTime(), Start.AddHours(1), Now));
        Assert.Empty(SeanceSlotRules.Check(Start, Start.AddHours(1).ToLocalTime(), Now));
    }

    [Fact]
    public void UnspecifiedKind_IsTreatedAsUtc()
    {
        var start = DateTime.SpecifyKind(Start, DateTimeKind.Unspecified);
        Assert.Empty(SeanceSlotRules.Check(start, start.AddMinutes(30), Now));
    }

    [Fact]
    public void EndNotAfterStart_ReportsEndsAt()
    {
        Assert.Equal([("EndsAt", "End time must be after start time.")], SeanceSlotRules.Check(Start, Start, Now));
    }

    [Fact]
    public void PastAndOffGrid_ReportsAllFailures()
    {
        var start = Now.AddDays(-1).AddMinutes(10);
        var errors = SeanceSlotRules.ToErrors(SeanceSlotRules.Check(start, start.AddMinutes(20), Now));

        Assert.Equal(2, errors["StartsAt"].Length);
        Assert.Equal(2, errors["EndsAt"].Length);
    }
}
