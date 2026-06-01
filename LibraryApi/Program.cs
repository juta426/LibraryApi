

using Microsoft.Data.SqlClient;
using LibraryData;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.Routing;






// --- SETUP -------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("AppDb")
    ?? throw new InvalidOperationException("missing connection string");

builder.Services.AddScoped<IDBApi>(_ => new DBApi(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT string"
    });
    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});



var jwtSection = builder.Configuration.GetSection("JwtSettings");
string issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Missing Issuer");
string audience= jwtSection["Audience"] ?? throw new InvalidOperationException("Missing Audience");
var keyBytes = Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!) ?? throw new InvalidOperationException("Missing SecretKey");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false;

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization( opts =>
{
    opts.AddPolicy("CanReadBooks", p => p.RequireRole("BooksAccess", "Admin"));
    opts.AddPolicy("CanReadAuthors", p => p.RequireRole("AuthorsAccess", "Admin"));
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});






var app = builder.Build();




if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



//ËXCEPTIONS

//app.MapException<InvalidOperationException>(ex =>
//    Results.Problem(title: ex.Message, statusCode: 400));

app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (ex is InvalidOperationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                title = ex.Message,
                status = 400
            });
            return;
        }


        // fallback: generic 500
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "An unexpected error occurred.",
            status = 500
        });
    });
});


app.UseAuthentication();
app.UseAuthorization();
//app.UseHttpsRedirection();






// --- BOOKS -------------------------------------------------------------

app.MapPost("/books", async (BookCreateDto b, IDBApi db) =>
{
    int id = await db.addBook(new BookRecord(0, b.Title, b.AuthorId));
    return Results.Created($"/books/{id}", new { id });
})
.WithName("AddBook").WithTags("Books")
.WithOpenApi(o => { o.Summary = "Add a new book"; return o; });

app.MapDelete("/books/{id:int}", (int id, IDBApi db) => db.removeBookByID(id))
   .WithName("RemoveBookById").WithTags("Books")
   .WithOpenApi(o => { o.Summary = "Delete book by ID"; return o; });

app.MapDelete("/books/by-title/{title}", (string title, IDBApi db) => db.removeBookByTitle(title))
   .WithName("RemoveBookByTitle").WithTags("Books")
   .WithOpenApi(o => { o.Summary = "Delete book by title"; return o; });

app.MapGet("/books/by-author/{id:int}", (int id, IDBApi db) => db.getBooksByAuthorID(id))
   .WithName("GetBooksByAuthorId").WithTags("Books")
   .WithOpenApi(o => { o.Summary = "Books for an author ID"; return o; });

app.MapGet("/books/by-author", (string name, IDBApi db) => db.getBooksByAuthorName(name))
   .WithName("GetBooksByAuthorName").WithTags("Books")
   .WithOpenApi(o => { o.Summary = "Books for an author name"; return o; });

// --- AUTHORS -----------------------------------------------------------

app.MapPost("/authors", (AuthorDto a, IDBApi db) => db.createAuthor(a.Name))
   .WithName("CreateAuthor").WithTags("Authors")
   .WithOpenApi(o => { o.Summary = "Create a new author. Returns the new authors ID. on name collision returns the existing authors ID"; return o; });

app.MapDelete("/authors/{id:int}", (int id, IDBApi db) => db.removeAuthorByID(id))
   .WithName("RemoveAuthorById").WithTags("Authors")
   .WithOpenApi(o => { o.Summary = "Delete author by ID"; return o; });

app.MapDelete("/authors", (string name, IDBApi db) => db.removeAuthorByName(name))
   .WithName("RemoveAuthorByName").WithTags("Authors")
   .WithOpenApi(o => { o.Summary = "Delete author by name"; return o; });

app.MapGet("/authors/{id:int}", async (int id, IDBApi db) =>
{
    var author = await db.getAuthorByID(id);
    return author is null ? Results.NotFound() : Results.Ok(author);
})
.WithName("GetAuthorById")
.WithTags("Authors")
.WithOpenApi(o => { o.Summary = "Author by ID"; return o; });

app.MapGet("/authors/by-name", async (string name, IDBApi db) =>
{
    var author = await db.getAuthorByName(name);
    return author is null ? Results.NotFound() : Results.Ok(author);
})
.WithName("GetAuthorByName")
.WithTags("Authors")
.WithOpenApi(o => { o.Summary = "Author by name"; return o; });

// --- COPIES ------------------------------------------------------------

app.MapPost("/copies", (CopyDto c, IDBApi db) => db.addCopy(new CopyRecord(0, c.BookId, c.Condition)))
   .WithName("AddCopy").WithTags("Copies")
   .WithOpenApi(o => { o.Summary = "Add a single copy"; return o; });

app.MapPost("/copies/bulk", (BulkCopyDto req, IDBApi db) => db.addCopyNTimes(new CopyRecord(0, req.BookId, req.Condition), req.Count))
   .WithName("AddCopiesNTimes").WithTags("Copies")
   .WithOpenApi(o => { o.Summary = "Add N identical copies"; return o; });

app.MapDelete("/copies/{id:int}", (int id, IDBApi db) => db.removeCopy(id))
   .WithName("RemoveCopy").WithTags("Copies")
   .WithOpenApi(o => { o.Summary = "Delete copy by ID"; return o; });

app.MapPut("/copies/{id:int}/condition", (int id, string condition, IDBApi db) => db.changeCopyCondition(id, condition))
   .WithName("ChangeCopyCondition").WithTags("Copies")
   .WithOpenApi(o => { o.Summary = "Update copy condition"; return o; });

// --- LOANS -------------------------------------------------------------

app.MapPost("/loans/by-title", (LoanByTitleDto l, IDBApi db) => db.createLoanByTitle(l.BookTitle, l.CustomerId))
   .WithName("CreateLoanByTitle").WithTags("Loans")
   .WithOpenApi(o => { o.Summary = "Loan a book by title"; return o; });

app.MapPost("/loans/by-id", (LoanByIdDto l, IDBApi db) => db.createLoanByID(l.BookId, l.CustomerId))
   .WithName("CreateLoanById").WithTags("Loans")
   .WithOpenApi(o => { o.Summary = "Loan a book by ID"; return o; });

app.MapPut("/loans/{id:int}/close", (int id, IDBApi db) => db.closeLoan(id))
   .WithName("CloseLoanNow").WithTags("Loans")
   .WithOpenApi(o => { o.Summary = "Close loan (now)"; return o; });

app.MapPut("/loans/{id:int}/close-date", (int id, DateTime date, IDBApi db) => db.closeLoanWithDate(id, date))
   .WithName("CloseLoanWithDate").WithTags("Loans")
   .WithOpenApi(o => { o.Summary = "Close loan with custom date"; return o; });



// --- GET-ALL -----------------------------------------------------------


app.MapGet("/authors", (IDBApi db) => db.getAllAuthors())
    .RequireAuthorization("CanReadAuthors")
    .WithTags("Get All")
    .WithOpenApi(o => { o.Summary = "All authors, protected"; return o; });

app.MapGet("/books", (IDBApi db) => db.getAllBooks())
   .WithName("GetAllBooks")
   .WithTags("Get All")
   .RequireAuthorization("CanReadBooks")
   .WithOpenApi(o => { o.Summary = "All books, protected"; return o; });

app.MapGet("/loans", (IDBApi db) => db.getAllLoans())
    .WithTags("Get All")
    .RequireAuthorization("AdminOnly")
    .WithOpenApi(o => { o.Summary = "All loans, protected"; return o; });


app.MapGet("/customers", (IDBApi db) => db.getAllCustomers()).WithTags("Get All").WithOpenApi(o => { o.Summary = "All customers"; return o; });
app.MapGet("/copies", (IDBApi db) => db.getAllCopies()).WithTags("Get All").WithOpenApi(o => { o.Summary = "All copies"; return o; });







// --- EF ----------------------------------------------------------------


app.MapGet("/authors-ef", (IDBApi db) => db.getAllAuthorsEF())
    .WithTags("EF")
    .WithOpenApi(o => { o.Summary = "All authors, done with EF"; return o; });

app.MapGet("/books/by-author/{name}", async (string name, IDBApi db) => await db.getAllBooksByAuthorByNameEF(name))
    .WithTags("EF")
    .WithOpenApi(o => { o.Summary = "All books written by an author, takes the author name, done with EF"; return o; });


app.MapPatch("copies/{id:int}/{newCondition}", async (int id, string newCondition, IDBApi db) => { await db.changeCopyConditionEF(newCondition, id); })
    .WithTags("EF")
    .WithOpenApi(o => { o.Summary = "Change the condition of a copy, done with EF"; return o; });

app.MapGet("/books/copy-count", (IDBApi db) => db.getNumberOfCopiesPerBook())
    .WithTags("EF")
    .WithOpenApi(o => { o.Summary = "Every book and the number of copies owned, done with EF"; return o; });

app.MapGet("/books/with-author-names", (IDBApi db) => db.getBookTitleWithAuthorNames())
    .WithTags("EF")
    .WithOpenApi(o => { o.Summary = "Every book and its author, done with EF"; return o; });

app.MapPost("/books-ef/{bookTitle}/{authorId:int}", (string bookTitle, int authorId, IDBApi db) => db.addBookEF(new BookCreateDto(bookTitle, authorId)))
    .WithTags("EF")
    .WithOpenApi(o => { o.Summary = "Post a new book for an existing author, done with EF"; return o; });




// --- JWT ---------------------------------------------------------------



app.MapPost("/login", async (LoginDto loginDto, IConfiguration config, IDBApi db) =>
{
    string[] roles;
    // Dummy Accounts
    if (loginDto.Username == "admin" && loginDto.Password == "adm")
        roles = new string[] { "Admin", "AuthorsAccess", "BooksAccess" };
    else if (loginDto.Username == "authors" && loginDto.Password == "aut")
        roles = new string[] { "AuthorsAccess" };
    else if (loginDto.Username == "books" && loginDto.Password == "boo")
        roles = new string[] { "BooksAccess" };
    else roles = new string[] { "Default" };


    // JWT
    var jwtSection = config.GetSection("JwtSettings");
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, loginDto.Username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

    };
    //var roles = new[] { "Admin", "AuthorsAccess", "BooksAccess" };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var token = new JwtSecurityToken(
        issuer: jwtSection["Issuer"],
        audience: jwtSection["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSection["ExpiryMinutes"]!)),
        signingCredentials: creds
        );

    string jwt = new JwtSecurityTokenHandler().WriteToken(token);

    // refresh token
    var refreshId = Guid.NewGuid();
    await db.insertRefreshTokenAsync(refreshId, loginDto.Username, DateTime.UtcNow.AddMinutes(int.Parse(jwtSection["RefreshExpiryMinutes"]!)));

    return Results.Ok(new { 
        token = jwt, 
        refresh = refreshId
    });
})
    .WithTags("Auth")
    .WithOpenApi(o => { o.Summary = "Login -> JWT"; return o; });



app.MapPost("/refresh", async (RefreshDto refreshDto, IConfiguration config, IDBApi db) =>
{
    var rec = await db.getRefreshTokenAsync(refreshDto.RefreshId);
    if (rec is null || rec.ExpiresUtc < DateTime.UtcNow)
        return Results.Unauthorized();


    string[] roles;
    // Dummy Accounts
    if (rec.UserName == "admin")
        roles = new string[] { "Admin", "AuthorsAccess", "BooksAccess" };
    else if (rec.UserName == "authors")
        roles = new string[] { "AuthorsAccess" };
    else if (rec.UserName == "books")
        roles = new string[] { "BooksAccess" };
    else roles = new string[] { "Default" };


    var jwtSection = config.GetSection("JwtSettings");
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, rec.UserName),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var access = new JwtSecurityToken(
        issuer: jwtSection["Issuer"],
        audience: jwtSection["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSection["ExpiryMinutes"]!)),
        signingCredentials: creds);

    string newJwt = new JwtSecurityTokenHandler().WriteToken(access);



    var newRefresh = Guid.NewGuid();
    await db.insertRefreshTokenAsync(newRefresh, rec.UserName, DateTime.UtcNow.AddMinutes(int.Parse(jwtSection["RefreshExpiryMinutes"]!)));

    return Results.Ok(new { 
        token = newJwt, 
        refresh = newRefresh 
    });
})
.WithTags("Auth")
.AllowAnonymous()
.WithOpenApi(o => { o.Summary = "refresh -> new JWT"; return o; });















app.Run();










