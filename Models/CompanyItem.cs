namespace ErpHub.Models;

public class CompanyItem
{
    public int Id { get; set; }
    public required string Naziv { get; set; }
    public string Pib { get; set; } = string.Empty;
    public string MaticniBroj { get; set; } = string.Empty;
    public string DbPath { get; set; } = string.Empty;

    public string DisplayText => string.IsNullOrWhiteSpace(Pib)
        ? Naziv
        : $"{Naziv} (PIB: {Pib})";
}
