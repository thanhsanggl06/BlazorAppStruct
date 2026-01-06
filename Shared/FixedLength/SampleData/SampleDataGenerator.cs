using Shared.FixedLength.Models;

namespace Shared.FixedLength.SampleData;

/// <summary>
/// Generator cho sample data ð? demo
/// </summary>
public static class SampleDataGenerator
{
    private static readonly Random Random = new();

    #region Employee Data

    private static readonly string[] FirstNames = 
    { 
        "Nguy?n", "Tr?n", "Lê", "Ph?m", "Hoàng", "V?", "Ð?ng", "Bùi", "Ð?", "H?" 
    };

    private static readonly string[] LastNames = 
    { 
        "Vãn A", "Th? B", "Vãn C", "Th? D", "Vãn E", "Th? F", 
        "Vãn G", "Th? H", "Vãn I", "Th? K" 
    };

    private static readonly string[] Departments = 
    { 
        "IT", "HR", "Sales", "Marketing", "Finance", "Operations", "R&D", "Legal" 
    };

    public static List<EmployeeRecord> GenerateEmployees(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new EmployeeRecord
            {
                EmployeeId = 1000 + i,
                FullName = $"{FirstNames[Random.Next(FirstNames.Length)]} {LastNames[Random.Next(LastNames.Length)]}",
                BirthDate = DateTime.Now.AddYears(-Random.Next(25, 55)).AddDays(-Random.Next(0, 365)),
                Salary = Random.Next(5000, 50000) * 1000m,
                IsActive = Random.Next(0, 10) > 1,
                Department = Departments[Random.Next(Departments.Length)]
            })
            .ToList();
    }

    #endregion

    #region Transaction Data

    private static readonly string[] CustomerCodes = 
    { 
        "CUST001", "CUST002", "CUST003", "CUST004", "CUST005",
        "CUST006", "CUST007", "CUST008", "CUST009", "CUST010"
    };

    private static readonly string[] TransactionDescriptions = 
    { 
        "Thanh toán hóa ðõn", "N?p ti?n", "Chuy?n kho?n", "Rút ti?n", 
        "Mua hàng", "Ð?t c?c", "Hoàn ti?n", "Thanh toán phí"
    };

    public static List<TransactionRecord> GenerateTransactions(int count)
    {
        var baseId = 100000000000000L;

        return Enumerable.Range(1, count)
            .Select(i => new TransactionRecord
            {
                TransactionId = baseId + i,
                TransactionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-Random.Next(0, 90))),
                TransactionTime = TimeOnly.FromDateTime(DateTime.Now.AddMinutes(-Random.Next(0, 1440))),
                Amount = Random.Next(100, 50000) * 1000m,
                CustomerCode = CustomerCodes[Random.Next(CustomerCodes.Length)],
                Description = TransactionDescriptions[Random.Next(TransactionDescriptions.Length)],
                IsSuccessful = Random.Next(0, 10) > 0 // 90% success rate
            })
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.TransactionTime)
            .ToList();
    }

    #endregion

    #region Invoice Data

    private static readonly string[] InvoiceStatuses = 
    { 
        "PENDING", "PAID", "OVERDUE", "CANCELLED", "DRAFT"
    };

    private static readonly string[] CompanyNames = 
    { 
        "Công ty TNHH ABC", "Công ty CP XYZ", "Công ty TNHH Tech Solutions",
        "Công ty CP Digital Marketing", "Công ty TNHH Innovation Co",
        "Công ty CP Global Trade", "Công ty TNHH Fast Delivery"
    };

    public static List<InvoiceRecord> GenerateInvoices(int count)
    {
        var baseInvoiceNumber = 202400001L;

        return Enumerable.Range(1, count)
            .Select(i =>
            {
                var invoiceDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-Random.Next(0, 60)));
                var dueDate = invoiceDate.AddDays(Random.Next(7, 45));
                var subTotal = Random.Next(1000, 100000) * 1000m;
                var taxAmount = subTotal * 0.1m; // 10% VAT
                var totalAmount = subTotal + taxAmount;
                var isPaid = Random.Next(0, 10) > 3; // 60% paid
                var status = isPaid ? "PAID" : (dueDate < DateOnly.FromDateTime(DateTime.Now) ? "OVERDUE" : "PENDING");

                return new InvoiceRecord
                {
                    InvoiceNumber = baseInvoiceNumber + i,
                    InvoiceDate = invoiceDate,
                    DueDate = dueDate,
                    CustomerCode = CustomerCodes[Random.Next(CustomerCodes.Length)],
                    CustomerName = CompanyNames[Random.Next(CompanyNames.Length)],
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    Status = status,
                    IsPaid = isPaid,
                    Reference = $"REF{Random.Next(10000, 99999)}",
                    Notes = isPaid ? "Ð? thanh toán ð?y ð?" : null
                };
            })
            .OrderBy(inv => inv.InvoiceDate)
            .ToList();
    }

    #endregion
}
