using Shared.FixedLength.Attributes;

namespace Shared.FixedLength.Models;

/// <summary>
/// Ví d? model cho employee record
/// </summary>
public class EmployeeRecord
{
    [FixedLengthColumn(Order = 1, Length = 10, Padding = PaddingDirection.Left, PadChar = '0')]
    public int EmployeeId { get; set; }

    [FixedLengthColumn(Order = 2, Length = 30, Padding = PaddingDirection.Right)]
    public string FullName { get; set; } = string.Empty;

    [FixedLengthColumn(Order = 3, Length = 8, Format = "yyyyMMdd")]
    public DateTime BirthDate { get; set; }

    [FixedLengthColumn(Order = 4, Length = 10, Format = "0000000.00")]
    public decimal Salary { get; set; }

    [FixedLengthColumn(Order = 5, Length = 1, Format = "Y")]
    public bool IsActive { get; set; }

    [FixedLengthColumn(Order = 6, Length = 15, Padding = PaddingDirection.Right)]
    public string? Department { get; set; }
}
