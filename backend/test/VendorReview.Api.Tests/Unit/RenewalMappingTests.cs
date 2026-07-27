using VendorReview.Api.Domain;
using VendorReview.Api.Services;
using Xunit;

namespace VendorReview.Api.Tests.Unit;

/// <summary>Renewal state + next-review-due computation on the vendor projection.</summary>
public class RenewalMappingTests
{
    private static readonly DateOnly Today = new(2026, 7, 27);

    private static Vendor Vendor(VendorStatus status, DateOnly? lastReview) =>
        new() { Name = "V", Status = status, LastReview = lastReview };

    [Fact]
    public void Rejected_vendor_is_not_applicable()
    {
        var dto = Vendor(VendorStatus.Rejected, new DateOnly(2026, 1, 1)).ToDto(12, Today);
        Assert.Equal("NotApplicable", dto.RenewalState);
        Assert.Null(dto.NextReviewDue);
    }

    [Fact]
    public void Approved_without_last_review_is_unknown()
    {
        var dto = Vendor(VendorStatus.Approved, null).ToDto(12, Today);
        Assert.Equal("Unknown", dto.RenewalState);
        Assert.Null(dto.NextReviewDue);
    }

    [Fact]
    public void Recent_review_is_current()
    {
        // Reviewed 2 months ago, 12-month interval → due next year → Current.
        var dto = Vendor(VendorStatus.Approved, Today.AddMonths(-2)).ToDto(12, Today);
        Assert.Equal("Current", dto.RenewalState);
        Assert.Equal(Today.AddMonths(-2).AddMonths(12), dto.NextReviewDue);
    }

    [Fact]
    public void Within_60_days_of_due_is_due_soon()
    {
        // Due in 30 days (reviewed 11 months ago, 12-month interval).
        var dto = Vendor(VendorStatus.Approved, Today.AddMonths(-12).AddDays(30)).ToDto(12, Today);
        Assert.Equal("DueSoon", dto.RenewalState);
    }

    [Fact]
    public void Past_due_is_overdue()
    {
        var dto = Vendor(VendorStatus.Approved, Today.AddMonths(-18)).ToDto(12, Today);
        Assert.Equal("Overdue", dto.RenewalState);
    }
}
