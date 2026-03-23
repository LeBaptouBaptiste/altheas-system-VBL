using API_Althea_systems.Common.Enums;

namespace API_Althea_systems.Services.IServices;

public interface IVatCalculationService
{
    decimal GetVatRate(VatRate rate);
    string GetVatRateLabel(VatRate rate, string locale = "fr");
    decimal CalculateTTC(decimal priceHT, VatRate rate);
    decimal CalculateVAT(decimal priceHT, VatRate rate);
    decimal CalculateHT(decimal priceTTC, VatRate rate);
    VatCalculationResult CalculateOrderTotals(IEnumerable<(decimal PriceHT, int Quantity, VatRate VatRate)> items, decimal shippingCost);
}

public record VatCalculationResult(
    decimal TotalHT,
    decimal TotalVAT,
    decimal TotalTTC,
    decimal ShippingCost,
    IReadOnlyDictionary<VatRate, decimal> VatBreakdown
);
