using System.Globalization;

namespace VentasMVC.Services;

public sealed class Monedas(IConfiguration configuracion)
{
    private static readonly CultureInfo Formato = CultureInfo.GetCultureInfo("en-US");

    public decimal CordobasPorDolar { get; } =
        configuracion.GetValue<decimal>("Monedas:CordobasPorDolar") is > 0 and var tasa ? tasa : 36.62m;

    public string Cordobas(decimal importe) => "C$ " + importe.ToString("N2", Formato);
    public decimal ConvertirADolares(decimal importe, decimal? tasa = null) =>
        decimal.Round(importe / (tasa ?? CordobasPorDolar), 2, MidpointRounding.AwayFromZero);

    public string FormatearDolares(decimal importe) => "US$ " + importe.ToString("N2", Formato);
    public string Dolares(decimal importe, decimal? tasa = null) => FormatearDolares(ConvertirADolares(importe, tasa));
}
