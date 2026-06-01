using Microsoft.Data.SqlClient;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Windows.Markup;

using LibraryData.Data;
using LibraryData.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryData
{
    public interface IDBApi
    {
        Task<int> createAuthor(string authorName);
        Task removeAuthorByName(string authorName);
        Task removeAuthorByID(int id);
        Task<AuthorRecord?> getAuthorByID(int id);
        Task<AuthorRecord?> getAuthorByName(string name);
        Task<int> createLoanByTitle(string bookTitle, int customerId);
        Task<int> createLoanByID(int bookID, int customerId);
        Task closeLoan(int loanID);
        Task closeLoanWithDate(int loanID, DateTime loanEnd);
        Task<int> addBook(BookRecord book);
        Task removeBookByID(int BookID);
        Task removeBookByTitle(string title);
        Task<int> addCopy(CopyRecord copy);
        Task<List<int>> addCopyNTimes(CopyRecord copy, int n);
        Task removeCopy(int id);
        Task changeCopyCondition(int copyID, string newCondition);


        Task<IEnumerable<BookRecord>> getBooksByAuthorName(string authorName);
        Task<IEnumerable<BookRecord>> getBooksByAuthorID(int id);

        Task<IEnumerable<LoanRecord>> getAllLoans();
        Task<IEnumerable<BookRecord>> getAllBooks();
        Task<IEnumerable<CustomerRecord>> getAllCustomers();
        Task<IEnumerable<CopyRecord>> getAllCopies();
        Task<IEnumerable<AuthorRecord>> getAllAuthors();

        Task insertRefreshTokenAsync(Guid tokenId, string userName, DateTime expiresUtc);
        Task<RefreshToken?> getRefreshTokenAsync(Guid tokenId);


        Task<IEnumerable<Author>> getAllAuthorsEF();
        Task<IEnumerable<Book>> getAllBooksByAuthorByNameEF(string authorName);
        Task<Copy> changeCopyConditionEF(string newCondition, int copyID);
        Task<IEnumerable<BookTitleAndCopyCountDto>> getNumberOfCopiesPerBook();
        Task<IEnumerable<BookTitleAuthorNameDto>> getBookTitleWithAuthorNames();
        Task<Book> addBookEF(BookCreateDto bookDto);

    }


    public class DBApi : IDBApi
    {
        private readonly string connectionString;
        private LibraryContext libDb;


        public DBApi(string connString)
        {
            connectionString = connString;

            var opts = new DbContextOptionsBuilder<LibraryContext>()
                .UseSqlServer(connectionString)
                .Options;
            libDb = new LibraryContext(opts, connectionString);
        }

        

        //tokens
        
        public async Task insertRefreshTokenAsync(Guid tokenId, string userName, DateTime expiresUtc)
        {
            await using var conn = new SqlConnection(connectionString);
            var sql = """
                INSERT INTO dbo.RefreshTokens (TokenId, UserName, ExpiresUtc)
                VALUES (@Id, @User, @Exp);
                """;
            await conn.ExecuteAsync(sql, new { Id = tokenId, User = userName, Exp = expiresUtc });

        }

        public async Task<RefreshToken?> getRefreshTokenAsync(Guid tokenId)
        {
            await using var conn = new SqlConnection(connectionString);
            return await conn.QuerySingleOrDefaultAsync<RefreshToken>(
                "SELECT * FROM dbo.RefreshTokens WHERE TokenId = @Id",
                new { Id = tokenId });
        }

        






        //authors


        //creates the author and returns its id. if author with this name already exissts, only returns its id
        public async Task<int> createAuthor(string authorName)
        {
            await using var conn = new SqlConnection(connectionString);

            const string insertSql = """
                INSERT INTO dbo.Authors (AuthorName)
                VALUES (@AuthorName);
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """;

            try
            {
                return await conn.ExecuteScalarAsync<int>(insertSql, new { AuthorName = authorName });
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                //author already exists -> return its id
                const string idSql = "SELECT AuthorID FROM dbo.Authors WHERE AuthorName = @AuthorName";
                return await conn.ExecuteScalarAsync<int>(idSql, new { AuthorName = authorName });
            }
        }


        public async Task removeAuthorByID(int id)
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = """
                DELETE FROM dbo.Authors 
                WHERE AuthorID = @AuthorID
                """;
            await connection.ExecuteAsync(sql, new { AuthorID = id});
        }

        public async Task removeAuthorByName(string authorName)
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = """
                DELETE FROM dbo.Authors
                WHERE AuthorName = @Name
                """;
            await connection.ExecuteAsync(sql, new { Name = authorName});
        }

        
        public async Task<AuthorRecord?> getAuthorByID(int id)
        {
            await using var conn = new SqlConnection(connectionString);
            const string sql = "SELECT * FROM dbo.Authors WHERE AuthorID = @Id";
            return await conn.QuerySingleOrDefaultAsync<AuthorRecord>(sql, new { Id = id });
        }

        
        public async Task<AuthorRecord?> getAuthorByName(string name)
        {
            await using var conn = new SqlConnection(connectionString);
            const string sql = "SELECT * FROM dbo.Authors WHERE AuthorName = @Name";
            return await conn.QuerySingleOrDefaultAsync<AuthorRecord>(sql, new { Name = name });
        }




        //loans

        public async Task<int> createLoanByTitle(string bookTitle, int customerId)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = """
                SELECT TOP 1 BookID
                FROM dbo.Books
                WHERE BookTitle = @BookTitle
                """;
            var bookID = await connection.ExecuteScalarAsync<int?>(sql, new {BookTitle = bookTitle});
            if (bookID is null) throw new InvalidOperationException("no book for the title found"); 
            return await createLoanByID(bookID.Value, customerId);
        }

        public async Task<int> createLoanByID(int bookID, int customerId)
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();

            //validate customer
            const string customerSql = "SELECT 1 FROM dbo.Customers WHERE CustomerID = @Id";
            var customerExists = await conn.ExecuteScalarAsync<int?>(customerSql, new { Id = customerId }, transaction);
            if (customerExists is null)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("customer does not exist");
            }

            //pick one free copy of that book
            const string copySql = """
                SELECT TOP 1 CopyID
                FROM dbo.Copies WITH (UPDLOCK, HOLDLOCK)
                WHERE BookID = @BookID
                AND NOT EXISTS ( SELECT 1
                    FROM dbo.Loans
                    WHERE CopyID = Copies.CopyID
                    AND ReturnDate IS NULL )
                ORDER BY CopyID
            """;

            int? copyId = await conn.QueryFirstOrDefaultAsync<int?>(
                              copySql, new { BookID = bookID }, transaction);

            if (copyId is null)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("No available copy for this book.");
            }

            //insert the loan and return its id
            const string loanSql = """
                INSERT INTO dbo.Loans (CopyID, CustomerID, LoanDate)
                OUTPUT INSERTED.LoanID
                VALUES (@CopyID, @CustomerID, SYSUTCDATETIME());
                """;

            int loanId = await conn.QuerySingleAsync<int>(
                             loanSql,
                             new { CopyID = copyId, CustomerID = customerId },
                             transaction);

            await transaction.CommitAsync();
            return loanId;
        }




        public async Task closeLoan(int loanID)
        {
            await using var connection = new SqlConnection(connectionString);
            var sql = @"
                UPDATE dbo.Loans
                SET ReturnDate = @ReturnDate
                WHERE LoanID = @LoanID";
            await connection.ExecuteAsync(sql, new {LoanID = loanID, ReturnDate = DateTime.UtcNow });
        }

        public async Task closeLoanWithDate(int loanID, DateTime loanEnd)
        {
            await using var connection = new SqlConnection(connectionString);
            var sql = @"
                UPDATE dbo.Loans
                SET ReturnDate = @ReturnDate
                WHERE LoanID = @LoanID";
            await connection.ExecuteAsync(sql, new { LoanID = loanID, ReturnDate = loanEnd });
        }




        //books

        public async Task<int> addBook(BookRecord book)
        {
            await using var conn = new SqlConnection(connectionString);

            const string insertSql = """
                INSERT INTO dbo.Books (BookTitle, AuthorID)
                VALUES (@BookTitle, @AuthorID);
                SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

            try
            {
                return await conn.ExecuteScalarAsync<int>(
                             insertSql, new { BookTitle = book.BookTitle, AuthorID = book.AuthorID });
            }
            catch (SqlException ex) when (ex.Number == 2627)   // duplicate key
            {
                //author + title already exists -> return the existing ID
                const string idSql = """
                    SELECT BookID
                    FROM dbo.Books
                    WHERE AuthorID = @AuthorID
                    AND BookTitle = @Title
                """;

                return await conn.ExecuteScalarAsync<int>(
                             idSql, new { Title = book.BookTitle, AuthorID = book.AuthorID });
            }
        }

        public async Task removeBookByID(int bookID)
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = """
                DELETE FROM dbo.Books 
                WHERE BookID = @BookID
                """;
            await connection.ExecuteAsync(sql, new { BookID = bookID });
        }

        public async Task removeBookByTitle(string title)
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = """
                DELETE FROM dbo.Books 
                WHERE BookTitle = @Title
                """;
            await connection.ExecuteAsync(sql, new { Title = title});
        }

        //copies

        public async Task<int> addCopy(CopyRecord copy)
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = @"
            INSERT INTO Copies (BookID, CopyCondition)
            VALUES (@BookID, @CopyCondition);
            SELECT CAST(SCOPE_IDENTITY() AS int);";
            return await connection.ExecuteScalarAsync<int>(sql, new { copy.BookID, copy.CopyCondition});
        }

        public async Task<List<int>> addCopyNTimes(CopyRecord copy, int n)
        {
            var ids = new List<int>();
            if (n <= 0) return ids;
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var sql = @"
            INSERT INTO Copies (BookID, CopyCondition)
            VALUES (@BookID, @CopyCondition);
            SELECT CAST(SCOPE_IDENTITY() AS int);";
            try
            {
                for (int i = 0; i < n; i++)
                {
                    ids.Add(
                        await connection.QuerySingleAsync<int>(
                            sql, new { copy.BookID, copy.CopyCondition }, transaction));
                }
                await transaction.CommitAsync();
                return ids;
            } catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task removeCopy(int id)
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = """
                DELETE FROM dbo.Copies 
                WHERE CopyID = @ID
                """;
            await connection.ExecuteAsync(sql, new { ID = id});
        }

        public async Task changeCopyCondition(int copyID, string newCondition)
        {
            if (string.IsNullOrWhiteSpace(newCondition)) return;
            await using var connection = new SqlConnection(connectionString);
            var sql = @"
                UPDATE dbo.Copies
                SET CopyCondition = @CopyCondition
                WHERE CopyID = @CopyID";
            await connection.ExecuteAsync(sql, new { CopyID = copyID, CopyCondition = newCondition});
        }







        public async Task<IEnumerable<BookRecord>> getBooksByAuthorID(int id)
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = """
                SELECT *
                FROM dbo.Books
                WHERE AuthorID = @AuthorID
                """;
            return await connection.QueryAsync<BookRecord>(sql, new { AuthorID = id });

        }

        public async Task<IEnumerable<BookRecord>> getBooksByAuthorName(string authorName)
        {
            if (string.IsNullOrWhiteSpace(authorName)) 
                throw new ArgumentException("no author name given", nameof(authorName));
            await using var connection = new SqlConnection(connectionString);

            var sql = """
                SELECT AuthorID
                FROM dbo.Authors
                WHERE AuthorName = @authorName
                """;

            int? authorID = await connection.ExecuteScalarAsync<int?>(sql, new {AuthorName = authorName});

            if (authorID is null)
                throw new InvalidOperationException("no author found");

            return await getBooksByAuthorID(authorID.Value);



        }







        // get all

        public async Task<IEnumerable<LoanRecord>> getAllLoans()
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = @"SELECT * FROM dbo.Loans";
            return await connection.QueryAsync<LoanRecord>(sql);
        }


        public async Task<IEnumerable<BookRecord>> getAllBooks()
        {
            await using var connection = new SqlConnection(connectionString);  

            var sql = @"SELECT * FROM dbo.Books";
            return await connection.QueryAsync<BookRecord>(sql);
        }

        public async Task<IEnumerable<CustomerRecord>> getAllCustomers()
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = @"SELECT * FROM dbo.Customers";
            return await connection.QueryAsync<CustomerRecord>(sql);
        }

        public async Task<IEnumerable<CopyRecord>> getAllCopies()
        {
            await using var connection = new SqlConnection(connectionString);

            var sql = @"SELECT * FROM dbo.Copies";
            return await connection.QueryAsync<CopyRecord>(sql);
        }

        public async Task<IEnumerable<AuthorRecord>> getAllAuthors()
        {
            await using var connection = new SqlConnection( connectionString);

            var sql = "SELECT * FROM dbo.Authors";
            return await connection.QueryAsync<AuthorRecord>(sql);
        }



        // EF

        public async Task<IEnumerable<Author>> getAllAuthorsEF()
        {
            var authors = await libDb.Authors
                .OrderBy(a => a.AuthorName)
                .ToListAsync();
            return authors;
        }


        public async Task<IEnumerable<Book>> getAllBooksByAuthorByNameEF(string authorName)
        {
            var books = await libDb.Books
                .Where(book => book.Author.AuthorName == authorName)
                .ToListAsync();
            return books;

        }

        public async Task<Copy> changeCopyConditionEF(string newCondition, int copyID)
        {
            var copy = await libDb.Copies.FindAsync(copyID);
            if (copy == null)
                throw new ArgumentException("Copy does not exist");
            copy.CopyCondition = newCondition;
            await libDb.SaveChangesAsync();
            return copy;
        }

        public async Task<IEnumerable<BookTitleAndCopyCountDto>> getNumberOfCopiesPerBook()
        {
            var ret = await libDb.Books
                .GroupJoin(
                    libDb.Copies,
                    b => b.BookId,
                    c => c.BookId,
                    (b, copies) => new BookTitleAndCopyCountDto
                    (
                        b.BookTitle,
                        copies.Count()
                    ))
                .ToListAsync();
            return ret;
        }

        public async Task<IEnumerable<BookTitleAuthorNameDto>> getBookTitleWithAuthorNames()
        {
            return await libDb.Books
                .Select(book => new BookTitleAuthorNameDto(
                    book.BookTitle,
                    book.Author.AuthorName))
                .ToListAsync();
        }

        public async Task<Book> addBookEF (BookCreateDto bookDto)
        {
            if (string.IsNullOrWhiteSpace(bookDto.Title))
                throw new ArgumentException("Book title is required");

            if (!await libDb.Authors.AnyAsync(a => a.AuthorId == bookDto.AuthorId))
                throw new ArgumentException("Author not found");

            if (await libDb.Books.AnyAsync(b => b.BookTitle == bookDto.Title))
                throw new ArgumentException("Book with this title already exists");

            var book = new Book
            {
                BookTitle = bookDto.Title,
                AuthorId = bookDto.AuthorId
            };

            libDb.Books.Add(book);
            await libDb.SaveChangesAsync();

            return book;
        }



        // TODO: safety
    }
}
