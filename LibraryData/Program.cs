// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Dapper;
using Microsoft.Data.SqlClient;

using LibraryData.Models;
using LibraryData.Data;

namespace LibraryData;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");


        //###############################################################################

        await using var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=aaa1;Trusted_Connection=True;Encrypt=False;");


        //CREATE
        {


            ////create author
            //var sql_create_authors =
            //    @"CREATE TABLE Authors (
            //    AuthorID INT IDENTITY(1,1) PRIMARY KEY,
            //    AuthorName nvarchar(50) NOT NULL
            //    )";
            //await connection.ExecuteAsync(sql_create_authors);

            //add name uniqueness
            //var sql_add_unique_author_name =
            //    @"ALTER TABLE dbo.Authors
            //    ADD CONSTRAINT UniqueAuthorName
            //    UNIQUE (AuthorName)";
            //await connection.ExecuteAsync(sql_add_unique_author_name);



            ////create book
            //var sql_create_books =
            //    @"CREATE TABLE Books (
            //    BookID INT IDENTITY(1,1) PRIMARY KEY,
            //    BookTitle nvarchar(50) NOT NULL,
            //    AuthorID INT FOREIGN KEY (authorID) REFERENCES Authors
            //    )";
            //await connection.ExecuteAsync(sql_create_books);

            ////create book copy
            //var sql_create_copies =
            //    @"CREATE TABLE Copies (
            //    CopyID INT IDENTITY(1,1) PRIMARY KEY,
            //    BookID INT NOT NULL FOREIGN KEY REFERENCES Books(BookID),
            //    CopyCondition nvarchar(50) NULL
            //    )";
            //await connection.ExecuteAsync(sql_create_copies);


            //create customer
            //var sql_create_customers =
            //    @"CREATE TABLE Customers (
            //    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
            //    CustomerName nvarchar(50) NOT NULL
            //    )";
            //await connection.ExecuteAsync(sql_create_customers);


            
            ////create loan
            //var sql_create_loans =
            //    @"CREATE TABLE Loans (
            //    LoanID INT IDENTITY(1,1) PRIMARY KEY,
            //    CopyID INT NOT NULL FOREIGN KEY REFERENCES Copies(CopyID),
            //    CustomerID INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerID),
            //    LoanDate DATETIME2 NOT NULL,
            //    ReturnDate DATETIME2 NULL
            //    )";
            //await connection.ExecuteAsync(sql_create_loans);
            





            //old creates
            {
                //var sql = @"CREATE TABLE Customers (
                //        login NVARCHAR(50) PRIMARY KEY, 
                //        fullName NVARCHAR(100)
                //        )";
                //await connection.ExecuteAsync(sql);

                //sql = @"CREATE TABLE Items (
                //    type NVARCHAR(50) PRIMARY KEY,
                //    price DECIMAL(10, 2) NOT NULL,
                //    inStock INT NOT NULL DEFAULT 0
                //    )";
                //await connection.ExecuteAsync(sql);

                //sql = @"CREATE TABLE Orders (
                //    time DATETIME NOT NULL,
                //    login NVARCHAR(50) NOT NULL,
                //    PRIMARY KEY (time, login),
                //    FOREIGN KEY (login) REFERENCES Customers(login)
                //    )";
                //await connection.ExecuteAsync(sql);

                //sql = @"CREATE TABLE OrderItems (
                //    time DATETIME NOT NULL,
                //    login NVARCHAR(50) NOT NULL,
                //    itemType NVARCHAR(50) NOT NULL,
                //    quantity INT NOT NULL,

                //    PRIMARY KEY (time, login, itemType),
                //    FOREIGN KEY (time, login) REFERENCES Orders(time, login),
                //    FOREIGN KEY (itemType) REFERENCES Items(type)
                //    )";
                //await connection.ExecuteAsync(sql);
            }
        }


        //INSERT/DELETE/SET
        {
            //Console.WriteLine("\nInser Loan\n");
            //var sql_insert_loan = @"
            //INSERT INTO Loans (CopyID, CustomerID, LoanDate)
            //VALUES (@CopyID, @CustomerID, @LoanDate)";
            //var loanstart = new DateTime(2025, 3, 30, 0, 0, 0, DateTimeKind.Utc);
            //await connection.ExecuteAsync(sql_insert_loan, new {CopyID = 2, CustomerID = 2, LoanDate = loanstart});

            //Console.WriteLine("\nSet Loan ReturnDate\n");
            //var finish_loan = @"
            //    UPDATE dbo.Loans
            //    SET ReturnDate = @ReturnDate
            //    WHERE LoanID = @LoanID";
            //var return_date = new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc);
            //await connection.ExecuteAsync(finish_loan, new {LoanID = 1, ReturnDate = return_date});


            //for (int i = 0; i < 2; i++)
            //{
            //    Console.WriteLine("\nInsert Copy\n");
            //    var insert_copies = @"INSERT INTO Copies(BookID, CopyCondition) VALUES (@ID, @CopyCondition)";
            //    await connection.ExecuteAsync(insert_copies, new { ID = 3, CopyCondition = "lightly used" });
            //}

            //Console.WriteLine("\nInsert Customer\n");
            //var insert_authors = @"INSERT INTO Customers (CustomerName) VALUES (@Name)";
            //await connection.ExecuteAsync(insert_authors, new { Name = "Jonas" });


            //Console.WriteLine("\nInsert Book\n");
            //var insert_babicka = @"INSERT INTO Books(BookTitle, AuthorID) VALUES (@Title, @ID)";
            //await connection.ExecuteAsync(insert_babicka, new { Title = "Valka s Mloky", ID = 5 });


            //Console.WriteLine("\nInsert Nemcova\n");
            //var insert_authors = @"INSERT INTO Authors (AuthorName) VALUES (@Name)";
            //await connection.ExecuteAsync(insert_authors, new { Name = "Vrchlicky" });


            //var ins = "INSERT INTO people (name, age) VALUES (@Name, @Age)";
            //await connection.ExecuteAsync(ins, new { Name = "Jirka", Age = 16 });


            //Console.WriteLine("\nDelete Nemcova\n");
            //var delete_nemcova = @"DELETE FROM Authors WHERE AuthorName = (@Name)";
            //await connection.ExecuteAsync(delete_nemcova, new { Name = "Nemcova" });

            //Console.WriteLine("\nDELETE\n");
            //var del = "DELETE FROM people where name = @Name";
            //await connection.ExecuteAsync(del, new { Name = "Jirka" });
        }



        //SELECT
        {
            Console.Out.WriteLine("select loans");
            var sql_loans = @"SELECT * FROM dbo.Loans";

            var results_loans= await connection.QueryAsync<LoanRecord>(sql_loans);

            foreach (var row in results_loans)
            {
                Console.WriteLine($"loanID: {row.LoanID}, copyID: {row.CopyID}, customerID: {row.CustomerID}, loanDate: {row.LoanDate}, returnDate: {row.ReturnDate}");
            }


            Console.WriteLine("\nselect all copies of Babicka\n");
            var sql_babicka_copies = @"SELECT b.BookTitle, c.CopyID, c.CopyCondition
                                FROM dbo.Copies c JOIN dbo.Books b
                                ON c.BookID = b.BookID
                                WHERE b.BookTitle = 'Babicka'";
            var babicka_copies = await connection.QueryAsync(sql_babicka_copies);
            foreach (var row in babicka_copies)
            {
                Console.WriteLine($"bookTitle: {row.BookTitle}, CopyID: {row.CopyID}, Condition: {row.CopyCondition}");
            }



            //Console.WriteLine("\nselect ppl LEFT JOIN cars\n");

            //var sql_peopleXcars = @"SELECT p.name, p.age, c.carManufacturer FROM
            //people p LEFT JOIN cars c
            //ON c.ownerPerson = p.name";
            ////var sql = "SELECT * FROM people";

            //var results2 = await connection.QueryAsync(sql_peopleXcars);


            //foreach (var row in results2)
            //{
            //    Console.WriteLine($"name: {row.name}, age: {row.age ?? "none"}, car {row.carManufacturer ?? "none"}");
            //}




            //Console.WriteLine("\nselect franta cars\n");

            ////await using var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=aaa1;Trusted_Connection=True;Encrypt=False;");
            //var sql_franta_auta = @"SELECT p.name, p.age, c.carManufacturer FROM
            //people p LEFT JOIN cars c
            //ON c.ownerPerson = p.name
            //where p.name = 'Franta'";
            ////var sql = "SELECT * FROM people";

            //results2 = await connection.QueryAsync(sql_franta_auta);


            //foreach (var row in results2)
            //{
            //    Console.WriteLine($"name: {row.name}, age: {row.age ?? "none"}, car {row.carManufacturer ?? "none"}");
            //}
        }
        string connectionString = "Server=localhost\\SQLEXPRESS;Database=aaa1;Trusted_Connection=True;Encrypt=False;";
        Console.WriteLine("\n\n\nUSING MY API\n");
        var api = new DBApi(connectionString);


        var loans = await api.getAllLoans();
        foreach (var row in loans)
        {
            Console.WriteLine($"loanID: {row.LoanID}, copyID: {row.CopyID}, customerID: {row.CustomerID}, loanDate: {row.LoanDate}, returnDate: {row.ReturnDate}");
        }

        Console.WriteLine();
        Console.WriteLine();

        var books = await api.getAllBooks();
        foreach (var row in books)
        {
            Console.WriteLine($"BookID: {row.BookID}, BookTitle: {row.BookTitle}, AuthorID: {row.AuthorID}");
        }

        //Console.WriteLine("\ninsert book\n");
        //var book = new BookRecord(BookID: 0, BookTitle: "V zamku a podzamci", AuthorID: 3);
        //await api.addBook(book);
        //Console.WriteLine("\nBooks updated\n");
        //var newBooks = await api.getAllBooks();
        //foreach (var row in newBooks)
        //{
        //    Console.WriteLine($"BookID: {row.BookID}, BookTitle: {row.BookTitle}, AuthorID: {row.AuthorID}");
        //}



        Console.WriteLine("\n\n----------EF STUFF----------\n");


        var options = new DbContextOptionsBuilder<LibraryContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=aaa1;Trusted_Connection=True;Encrypt=False;")
            .Options;

        await using var libDb = new LibraryContext(options);


        var authors = await libDb.Authors.OrderBy(x => x.AuthorName).ToListAsync();
        Console.WriteLine($"Found {authors.Count} author(s)");
        foreach (Author a in authors)
        {
            Console.WriteLine($"{a.AuthorId}\t{a.AuthorName}");
        }


        //var options = new DbContextOptionsBuilder<Aaa1Con>


        Console.WriteLine();
        var nemcovky = libDb.Books
            .Where(b => b.AuthorId == 3);
        foreach (Book b in nemcovky)
        {
            Console.WriteLine($"nemcovka: {b.BookTitle}");
        }

        Console.WriteLine();







    }
}






