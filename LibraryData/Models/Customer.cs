using System;
using System.Collections.Generic;

namespace LibraryData.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
