using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using LibraryData.Models;

namespace LibraryData.Data;

public partial class LibraryContext : DbContext
{
    private readonly string _connectionString = null!;

    public LibraryContext()
    {
    }

    public LibraryContext(string conn)
    {
        _connectionString = conn;
    }

    public LibraryContext(DbContextOptions<LibraryContext> options)
        : base(options)
    {
    }

    public LibraryContext(DbContextOptions<LibraryContext> options, string conn)
        : base(options)
    {
        _connectionString = conn;
    }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Copy> Copies { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Loan> Loans { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer(_connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.AuthorId).HasName("PK__Authors__70DAFC142F19C5A1");

            entity.HasIndex(e => e.AuthorName, "UniqueAuthorName").IsUnique();

            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.AuthorName).HasMaxLength(50);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("PK__Books__3DE0C2275D41DCCD");

            entity.Property(e => e.BookId).HasColumnName("BookID");
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.BookTitle).HasMaxLength(50);

            entity.HasOne(d => d.Author).WithMany(p => p.Books)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK__Books__AuthorID__5EBF139D");
        });

        modelBuilder.Entity<Copy>(entity =>
        {
            entity.HasKey(e => e.CopyId).HasName("PK__Copies__C26CCCE52360DE3B");

            entity.Property(e => e.CopyId).HasColumnName("CopyID");
            entity.Property(e => e.BookId).HasColumnName("BookID");
            entity.Property(e => e.CopyCondition).HasMaxLength(50);

            entity.HasOne(d => d.Book).WithMany(p => p.Copies)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Copies__BookID__6477ECF3");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B8CEA6BC55");

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.CustomerName).HasMaxLength(50);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.LoanId).HasName("PK__Loans__4F5AD4372A70B523");

            entity.Property(e => e.LoanId).HasColumnName("LoanID");
            entity.Property(e => e.CopyId).HasColumnName("CopyID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

            entity.HasOne(d => d.Copy).WithMany(p => p.Loans)
                .HasForeignKey(d => d.CopyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Loans__CopyID__6754599E");

            entity.HasOne(d => d.Customer).WithMany(p => p.Loans)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Loans__CustomerI__68487DD7");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);

            entity.Property(e => e.TokenId).ValueGeneratedNever();
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
