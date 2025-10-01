namespace BankingPatterns.Core.Entities;

public class LoanApplication
{
    public Guid Id { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int CreditScore { get; set; }
    public decimal AnnualIncome { get; set; }
    public LoanStatus Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime ApplicationDate { get; set; }

    public LoanApplication(string applicantName, decimal amount, int creditScore, decimal annualIncome)
    {
        Id = Guid.NewGuid();
        ApplicantName = applicantName;
        Amount = amount;
        CreditScore = creditScore;
        AnnualIncome = annualIncome;
        Status = LoanStatus.Pending;
        ApplicationDate = DateTime.UtcNow;
    }
}

public enum LoanStatus
{
    Pending,
    Approved,
    Rejected
}
