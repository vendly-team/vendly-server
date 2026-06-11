namespace VendlyServer.Application.Services.Pricing;

public class PricingOptions
{
    public const string SectionName = "Pricing";

    // Category price topilmasa ishlatiladigan default markup (foiz)
    public decimal DefaultMarkupPercent { get; set; }

    // 0 bo'lsa yaxlitlash o'chiriladi
    public decimal DefaultRoundingStep { get; set; }
}
