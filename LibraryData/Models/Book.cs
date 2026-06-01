using System;
using System.Collections.Generic;

namespace LibraryData.Models;

public partial class Book
{
    public int BookId { get; set; }

    public string BookTitle { get; set; } = null!;

    public int? AuthorId { get; set; }

    public virtual Author Author { get; set; } = null!;

    public virtual ICollection<Copy> Copies { get; set; } = new List<Copy>();
}
