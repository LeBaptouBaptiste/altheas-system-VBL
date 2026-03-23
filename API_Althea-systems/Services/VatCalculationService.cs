using API_Althea_systems.Common.Enums;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class VatCalculationService : IVatCalculationService
{
    private static readonly Dictionary<VatRate, decimal> VatRates = new()
    {
        { VatRate.Standard, 0.20m },
        { VatRate.Intermediate, 0.10m },
        { VatRate.Reduced, 0.055m },
        { VatRate.Zero, 0m }
    };

    private static readonly Dictionary<(VatRate, string), string> VatLabels = new()
    {
        { (VatRate.Standard, "fr"), "TVA 20%" },
        { (VatRate.Standard, "en"), "VAT 20%" },
        { (VatRate.Intermediate, "fr"), "TVA 10%" },
        { (VatRate.Intermediate, "en"), "VAT 10%" },
        { (VatRate.Reduced, "fr"), "TVA 5.5%" },
        { (VatRate.Reduced, "en"), "VAT 5.5%" },
        { (VatRate.Zero, "fr"), "Exonéré" },
        { (VatRate.Zero, "en"), "Exempt" }
    };

    public decimal GetVatRate(VatRate rate)
        => VatRates.GetValueOrDefault(rate, 0.20m);

    public string GetVatRateLabel(VatRate rate, string locale = "fr")
        => VatLabels.GetValueOrDefault((rate, locale), $"TVA {GetVatRate(rate) * 100}%");

    public decimal CalculateTTC(decimal priceHT, VatRate rate)
        => Math.Round(priceHT * (1 + GetVatRate(rate)), 2);

    public decimal CalculateVAT(decimal priceHT, VatRate rate)
        => Math.Round(priceHT * GetVatRate(rate), 2);

    public decimal CalculateHT(decimal priceTTC, VatRate rate)
        => Math.Round(priceTTC / (1 + GetVatRate(rate)), 2);

    public VatCalculationResult CalculateOrderTotals(
        IEnumerable<(decimal PriceHT, int Quantity, VatRate VatRate)> items,
        decimal shippingCost)
    {
        var totalHT = 0m;
        var totalVAT = 0m;
        var vatBreakdown = new Dictionary<VatRate, decimal>();

        foreach (var (priceHT, quantity, vatRate) in items)
        {
            var lineHT = priceHT * quantity;
            var lineVAT = CalculateVAT(lineHT, vatRate);

            totalHT += lineHT;
            totalVAT += lineVAT;

            if (vatBreakdown.ContainsKey(vatRate))
                vatBreakdown[vatRate] += lineVAT;
            else
                vatBreakdown[vatRate] = lineVAT;
        }

        return new VatCalculationResult(
            Math.Round(totalHT, 2),
            Math.Round(totalVAT, 2),
            Math.Round(totalHT + totalVAT + shippingCost, 2),
            shippingCost,
            vatBreakdown
        );
    }
}
