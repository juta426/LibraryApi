using System;
using System.Collections.Generic;

namespace LibraryData.Models;

public partial class Loan
{
    public int LoanId { get; set; }

    public int CopyId { get; set; }

    public int CustomerId { get; set; }

    public DateTime LoanDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public virtual Copy Copy { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
