using FluentAssertions;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Services;

namespace API_Althea_systems.Tests.Services;

public class VatCalculationServiceTests
{
    private readonly VatCalculationService _sut = new();

    [Theory]
    [InlineData(VatRate.Standard, 0.20)]
    [InlineData(VatRate.Intermediate, 0.10)]
    [InlineData(VatRate.Reduced, 0.055)]
    [InlineData(VatRate.Zero, 0)]
    public void GetVatRate_ReturnsCorrectRate(VatRate rate, decimal expected)
    {
        _sut.GetVatRate(rate).Should().Be(expected);
    }

    [Fact]
    public void GetVatRateLabel_French_ReturnsCorrectLabel()
    {
        _sut.GetVatRateLabel(VatRate.Standard, "fr").Should().Be("TVA 20%");
        _sut.GetVatRateLabel(VatRate.Reduced, "fr").Should().Be("TVA 5.5%");
        _sut.GetVatRateLabel(VatRate.Zero, "fr").Should().Be("Exonéré");
    }

    [Fact]
    public void GetVatRateLabel_English_ReturnsCorrectLabel()
    {
        _sut.GetVatRateLabel(VatRate.Standard, "en").Should().Be("VAT 20%");
        _sut.GetVatRateLabel(VatRate.Zero, "en").Should().Be("Exempt");
    }

    [Fact]
    public void CalculateTTC_StandardRate_ReturnsCorrectAmount()
    {
        _sut.CalculateTTC(100m, VatRate.Standard).Should().Be(120m);
    }

    [Fact]
    public void CalculateTTC_ReducedRate_ReturnsCorrectAmount()
    {
        _sut.CalculateTTC(100m, VatRate.Reduced).Should().Be(105.50m);
    }

    [Fact]
    public void CalculateVAT_StandardRate_ReturnsCorrectAmount()
    {
        _sut.CalculateVAT(100m, VatRate.Standard).Should().Be(20m);
    }

    [Fact]
    public void CalculateHT_StandardRate_ReturnsCorrectAmount()
    {
        _sut.CalculateHT(120m, VatRate.Standard).Should().Be(100m);
    }

    [Fact]
    public void CalculateOrderTotals_MultipleItems_ReturnsCorrectTotals()
    {
        var items = new List<(decimal, int, VatRate)>
        {
            (100m, 2, VatRate.Standard),   // 200 HT, 40 VAT
            (50m, 3, VatRate.Reduced)      // 150 HT, 8.25 VAT
        };

        var result = _sut.CalculateOrderTotals(items, 15m);

        result.TotalHT.Should().Be(350m);
        result.TotalVAT.Should().Be(48.25m);
        result.TotalTTC.Should().Be(413.25m);
        result.ShippingCost.Should().Be(15m);
        result.VatBreakdown.Should().ContainKey(VatRate.Standard).WhoseValue.Should().Be(40m);
        result.VatBreakdown.Should().ContainKey(VatRate.Reduced).WhoseValue.Should().Be(8.25m);
    }

    [Fact]
    public void CalculateOrderTotals_EmptyItems_ReturnsZeros()
    {
        var result = _sut.CalculateOrderTotals([], 0m);

        result.TotalHT.Should().Be(0m);
        result.TotalVAT.Should().Be(0m);
        result.TotalTTC.Should().Be(0m);
    }
}
