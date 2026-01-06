using Shared.FixedLength.Attributes;

namespace Shared.FixedLength.Models;

/// <summary>
/// Model demo v?i custom converter
/// </summary>
public class TransactionRecord
{
    [FixedLengthColumn(Order = 1, Length = 15, Padding = PaddingDirection.Left, PadChar = '0')]
    public long TransactionId { get; set; }

    [FixedLengthColumn(Order = 2, Length = 8, Format = "yyyyMMdd")]
    public DateOnly TransactionDate { get; set; }

    [FixedLengthColumn(Order = 3, Length = 6, Format = "HHmmss")]
    public TimeOnly TransactionTime { get; set; }

    [FixedLengthColumn(Order = 4, Length = 12, ConverterType = typeof(VndCurrencyConverter))]
    public decimal Amount { get; set; }

    [FixedLengthColumn(Order = 5, Length = 20)]
    public string CustomerCode { get; set; } = string.Empty;

    [FixedLengthColumn(Order = 6, Length = 50)]
    public string Description { get; set; } = string.Empty;

    [FixedLengthColumn(Order = 7, Length = 1, Format = "S")]
    public bool IsSuccessful { get; set; }
}
