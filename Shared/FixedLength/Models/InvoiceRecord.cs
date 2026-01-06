using Shared.FixedLength.Attributes;

namespace Shared.FixedLength.Models;

/// <summary>
/// Ví d? file invoice (hóa ðõn) - format ph?c t?p hõn
/// </summary>
public class InvoiceRecord
{
    // Header info
    [FixedLengthColumn(Order = 1, Length = 12, Padding = PaddingDirection.Left, PadChar = '0')]
    public long InvoiceNumber { get; set; }

    [FixedLengthColumn(Order = 2, Length = 8, Format = "yyyyMMdd")]
    public DateOnly InvoiceDate { get; set; }

    [FixedLengthColumn(Order = 3, Length = 8, Format = "yyyyMMdd")]
    public DateOnly? DueDate { get; set; }

    // Customer info
    [FixedLengthColumn(Order = 4, Length = 20)]
    public string CustomerCode { get; set; } = string.Empty;

    [FixedLengthColumn(Order = 5, Length = 50)]
    public string CustomerName { get; set; } = string.Empty;

    // Amount info
    [FixedLengthColumn(Order = 6, Length = 15, Padding = PaddingDirection.Left, PadChar = '0', Format = "000000000000.00")]
    public decimal SubTotal { get; set; }

    [FixedLengthColumn(Order = 7, Length = 15, Padding = PaddingDirection.Left, PadChar = '0', Format = "000000000000.00")]
    public decimal TaxAmount { get; set; }

    [FixedLengthColumn(Order = 8, Length = 15, Padding = PaddingDirection.Left, PadChar = '0', Format = "000000000000.00")]
    public decimal TotalAmount { get; set; }

    // Status
    [FixedLengthColumn(Order = 9, Length = 10)]
    public string Status { get; set; } = "PENDING";

    [FixedLengthColumn(Order = 10, Length = 1, Format = "Y")]
    public bool IsPaid { get; set; }

    // Reference
    [FixedLengthColumn(Order = 11, Length = 30)]
    public string? Reference { get; set; }

    // Notes (optional)
    [FixedLengthColumn(Order = 12, Length = 100)]
    public string? Notes { get; set; }
}
