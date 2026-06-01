using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.Globalization;


namespace LibraryData;

public record LoginDto(string Username, string Password);


public record AuthorRecord(int AuthorID, string AuthorName);
public record BookRecord(int BookID, string BookTitle, int AuthorID);
public record CopyRecord(int CopyID, int BookID, string? CopyCondition);
public record CustomerRecord(int CustomerID, string CustomerName);
public record LoanRecord(int LoanID, int CopyID, int CustomerID, DateTime LoanDate, DateTime? ReturnDate);





public record BookCreateDto(string Title, int AuthorId);
public record AuthorDto(string Name);
public record CopyDto(int BookId, string? Condition);
public record BulkCopyDto(int BookId, string? Condition, int Count);
public record LoanByTitleDto(string BookTitle, int CustomerId);
public record LoanByIdDto(int BookId, int CustomerId);

public record RefreshToken(Guid TokenId, string UserName, DateTime ExpiresUtc);
public record RefreshDto(Guid RefreshId);

public record BookTitleAndCopyCountDto(string BookTitle, int CopyCount);
public record BookTitleAuthorNameDto(string BookTitle, string AuthorName);
