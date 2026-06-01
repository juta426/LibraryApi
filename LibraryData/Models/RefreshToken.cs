using System;
using System.Collections.Generic;

namespace LibraryData.Models;

public partial class RefreshToken
{
    public Guid TokenId { get; set; }

    public string UserName { get; set; } = null!;

    public DateTime ExpiresUtc { get; set; }
}
