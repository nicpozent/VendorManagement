using VendorReview.Api.Domain;
using VendorReview.Api.Services;
using Xunit;

namespace VendorReview.Api.Tests.Unit;

/// <summary>Server-authoritative verdict engine — the core scoring rules.</summary>
public class VerdictEngineTests
{
    private readonly VerdictEngine _engine = new();

    private static Review Review(ReviewStatus status, params (ItemStatus status, string? mitigation)[] items)
    {
        var section = new ReviewSectionScore
        {
            SectionName = "S",
            Items = items.Select((it, i) => new ReviewItemScore
            { Label = $"item{i}", Status = it.status, Mitigation = it.mitigation }).ToList(),
        };
        return new Review { VendorName = "V", Status = status, Sections = new() { section } };
    }

    [Fact]
    public void Empty_draft_is_in_progress()
    {
        var v = _engine.Evaluate(Review(ReviewStatus.Draft), blockerCapsVerdict: true);
        Assert.Equal("InProgress", v.Verdict);
        Assert.Equal(0, v.ReadinessPct);
    }

    [Fact]
    public void All_pass_is_proceed_at_100()
    {
        var v = _engine.Evaluate(
            Review(ReviewStatus.InProgress, (ItemStatus.Pass, null), (ItemStatus.Pass, null)),
            blockerCapsVerdict: true);
        Assert.Equal("Proceed", v.Verdict);
        Assert.Equal(100, v.ReadinessPct);
        Assert.Equal(2, v.PassCount);
    }

    [Fact]
    public void One_pass_one_concern_is_conditions_at_50()
    {
        var v = _engine.Evaluate(
            Review(ReviewStatus.InProgress, (ItemStatus.Pass, null), (ItemStatus.Concern, null)),
            blockerCapsVerdict: true);
        Assert.Equal("ProceedWithConditions", v.Verdict);
        Assert.Equal(50, v.ReadinessPct);
        Assert.Equal(1, v.ConcernCount);
    }

    [Fact]
    public void Unmitigated_blocker_caps_verdict_when_enabled()
    {
        var v = _engine.Evaluate(
            Review(ReviewStatus.InProgress, (ItemStatus.Pass, null), (ItemStatus.Blocker, null)),
            blockerCapsVerdict: true);
        Assert.Equal("DoNotProceed", v.Verdict);
        Assert.Equal(1, v.BlockerCount);
    }

    [Fact]
    public void Mitigated_blocker_does_not_cap()
    {
        var v = _engine.Evaluate(
            Review(ReviewStatus.InProgress, (ItemStatus.Pass, null), (ItemStatus.Blocker, "mitigation plan")),
            blockerCapsVerdict: true);
        Assert.Equal("ProceedWithConditions", v.Verdict); // openBlockers == 0
    }

    [Fact]
    public void Blocker_does_not_cap_when_toggle_off()
    {
        var v = _engine.Evaluate(
            Review(ReviewStatus.InProgress, (ItemStatus.Blocker, null)),
            blockerCapsVerdict: false);
        Assert.Equal("ProceedWithConditions", v.Verdict);
    }
}
