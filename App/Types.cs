using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryApp;

public abstract class Result<T> { }

public sealed class Success<T> : Result<T>
{
    public T Value { get; }
    public Success(T value) { Value = value; }
}

public sealed class Failure<T> : Result<T>
{
    public string Error { get; }
    public Failure(string error) { Error = error; }
}

public abstract class Maybe<T> { }

public sealed class Something<T> : Maybe<T>
{
    public T Value { get; }
    public Something(T value) { Value = value; }
}

public sealed class Nothing<T> : Maybe<T> { }

public static class Extensions
{
    public static Result<U> Map<T, U>(this Result<T> result, Func<T, U> f)
        => result switch
        {
            Success<T> s => new Success<U>(f(s.Value)),
            Failure<T> e => new Failure<U>(e.Error),
            _ => throw new ArgumentException("Unknown result")
        };

    public static Result<U> Bind<T, U>(this Result<T> result, Func<T, Result<U>> f)
        => result switch
        {
            Success<T> s => f(s.Value),
            Failure<T> e => new Failure<U>(e.Error),
            _ => throw new ArgumentException("Unknown result")
        };
}

public static class Validation
{
    public static bool Text(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value == value.Trim()
            && value.Length <= 80 && value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0;
    }

    public static bool Day(int day) { return day >= 0 && day <= 365000; }
}

public sealed class Book
{
    public int Id { get; }
    public string Title { get; }
    public string Category { get; }
    public int Copies { get; }

    private Book(int id, string title, string category, int copies)
    {
        Id = id;
        Title = title;
        Category = category;
        Copies = copies;
    }

    public static Result<Book> Create(int id, string title, string category, int copies)
    {
        if (id <= 0 || !Validation.Text(title) || !Validation.Text(category)
            || copies <= 0 || copies > 100000)
            return new Failure<Book>("Invalid book");
        return new Success<Book>(new Book(id, title, category, copies));
    }
}

public sealed class Loan
{
    public int Id { get; }
    public int BookId { get; }
    public string Reader { get; }
    public int Start { get; }
    public int Due { get; }
    public int Returned { get; }
    public int Renewals { get; }
    public int Changed { get; }
    public bool Active => Returned == -1;

    private Loan(int id, int bookId, string reader, int start, int due, int returned, int renewals, int changed)
    {
        Id = id;
        BookId = bookId;
        Reader = reader;
        Start = start;
        Due = due;
        Returned = returned;
        Renewals = renewals;
        Changed = changed;
    }

    public static Result<Loan> Create(int id, int bookId, string reader, int start,
        int due, int returned, int renewals, int changed)
    {
        if (id <= 0 || bookId <= 0 || !Validation.Text(reader) || !Validation.Day(start)
            || !Validation.Day(changed) || changed < start
            || (returned != -1 && changed != returned)
            || (returned == -1 && renewals == 0 && changed != start)
            || (returned == -1 && renewals == 1 && changed > start + 14)
            || renewals < 0 || renewals > 1 || due != start + 14 + renewals * 7
            || (returned != -1 && (!Validation.Day(returned) || returned < start)))
            return new Failure<Loan>("Invalid loan");
        return new Success<Loan>(new Loan(id, bookId, reader, start, due, returned, renewals, changed));
    }
}

public sealed class Library
{
    private readonly Book[] books;
    private readonly Loan[] loans;
    public IEnumerable<Book> Books { get { foreach (var b in books) yield return b; } }
    public IEnumerable<Loan> Loans { get { foreach (var l in loans) yield return l; } }

    private Library(Book[] books, Loan[] loans)
    {
        this.books = books;
        this.loans = loans;
    }

    public static Result<Library> Create(IEnumerable<Book> books, IEnumerable<Loan> loans)
    {
        var b = books.ToArray();
        var l = loans.ToArray();
        if (b.Any(x => x is null) || l.Any(x => x is null))
            return new Failure<Library>("Null record");
        if (b.GroupBy(x => x.Id).Any(g => g.Count() > 1)
            || l.GroupBy(x => x.Id).Any(g => g.Count() > 1))
            return new Failure<Library>("Duplicate ID");
        if (l.Any(x => !b.Any(y => y.Id == x.BookId)))
            return new Failure<Library>("Loan refers to a missing book");
        foreach (var book in b)
        {
            if (l.Count(x => x.Active && x.BookId == book.Id) > book.Copies)
                return new Failure<Library>("Active loans exceed copies");
        }
        if (l.Where(x => x.Active).GroupBy(x => new { x.BookId, x.Reader })
            .Any(g => g.Count() > 1))
            return new Failure<Library>("Reader already has this book");
        return new Success<Library>(new Library(b, l));
    }
}
