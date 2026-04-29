namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class FcoLaborLine
{
    public int FcoLaborLineId { get; set; }
    public int FcoDocumentId { get; set; }
    public FcoDocument FcoDocument { get; set; } = null!;

    public string Position { get; set; } = string.Empty;

    // Direct | Indirect
    public string LaborType { get; set; } = "Direct";

    public string? CraftCode { get; set; }
    public string? NavCode { get; set; }

    public decimal StHours { get; set; }
    public decimal OtHours { get; set; }
    public decimal DtHours { get; set; }

    public decimal BillStRate { get; set; }
    public decimal BillOtRate { get; set; }
    public decimal BillDtRate { get; set; }

    public decimal Subtotal { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
}
