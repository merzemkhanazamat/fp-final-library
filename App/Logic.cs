using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryApp;

public sealed class Request
{
    public int BookId { get; }
    public string Reader { get; }
    public int Day { get; }

    private Request(int bookId, string reader, int day)
    {
        BookId = bookId;
        Reader = reader;
        Day = day;
    }

    public static Result<Request> Create(int bookId, string reader, int day)
    {
        if (bookId <= 0 || !Validation.Text(reader) || !Validation.Day(day))
            return new Failure<Request>("Invalid request");
        return new Success<Request>(new Request(bookId, reader, day));
    }
}

public static class Logic
{
    public static Func<int, bool> Limit(int maximum) => count => count < maximum;

    public static Maybe<Book> FindBook(Library state, int id)
    {
        foreach (var book in state.Books)
            if (book.Id == id) return new Something<Book>(book);
        return new Nothing<Book>();
    }

    public static Maybe<Loan> FindLoan(Library state, int id)
    {
        foreach (var loan in state.Loans)
            if (loan.Id == id) return new Something<Loan>(loan);
        return new Nothing<Loan>();
    }

    public static Result<Request> CheckReader(Library state, Request request, Func<int, bool> policy)
    {
        var own = state.Loans.Where(l => l.Reader == request.Reader).ToArray();
        if (own.Any(l => l.Changed > request.Day))
            return new Failure<Request>("Day is before the reader's latest operation");
        if (own.Any(l => l.Active && l.BookId == request.BookId))
            return new Failure<Request>("Reader already has this book");
        if (own.Any(l => l.Active && l.Due < request.Day))
            return new Failure<Request>("Return overdue books first");
        if (!policy(own.Count(l => l.Active)))
            return new Failure<Request>("Reader loan limit reached");
        return new Success<Request>(request);
    }

    public static Result<Loan> PrepareLoan(Library state, Request request)
    {
        return FindBook(state, request.BookId) switch
        {
            Nothing<Book> => new Failure<Loan>("Book was not found"),
            Something<Book> b when state.Loans.Count(l => l.Active && l.BookId == b.Value.Id)
                >= b.Value.Copies => new Failure<Loan>("No copies of " + b.Value.Title),
            Something<Book> => NewLoan(state, request),
            _ => new Failure<Loan>("Unknown lookup result")
        };
    }

    private static Result<Loan> NewLoan(Library state, Request request)
    {
        int last = 0;
        foreach (var loan in state.Loans) if (loan.Id > last) last = loan.Id;
        if (last == int.MaxValue) return new Failure<Loan>("Loan ID limit reached");
        return Loan.Create(last + 1, request.BookId, request.Reader, request.Day,
            request.Day + 14, -1, 0, request.Day);
    }

    public static Result<Library> Borrow(Library state, int bookId, string reader, int day,
        Func<int, bool> policy)
    {
        return Request.Create(bookId, reader, day)
            .Bind(r => CheckReader(state, r, policy))
            .Bind(r => PrepareLoan(state, r))
            .Map(l => state.Loans.Concat(new[] { l }))
            .Bind(loans => Library.Create(state.Books, loans));
    }

    public static Result<Library> Return(Library state, int loanId, int day)
    {
        if (!Validation.Day(day)) return new Failure<Library>("Invalid day");
        if (FindLoan(state, loanId) is not Something<Loan> found)
            return new Failure<Library>("Loan was not found");
        var loan = found.Value;
        if (!loan.Active) return new Failure<Library>("Loan was already returned");
        if (state.Loans.Any(l => l.Reader == loan.Reader && l.Changed > day))
            return new Failure<Library>("Return day is before borrowing or renewal");
        return Loan.Create(loan.Id, loan.BookId, loan.Reader, loan.Start, loan.Due, day, loan.Renewals, day)
            .Bind(updated => Replace(state, updated));
    }

    public static Result<Library> Renew(Library state, int loanId, int day)
    {
        if (!Validation.Day(day)) return new Failure<Library>("Invalid day");
        if (FindLoan(state, loanId) is not Something<Loan> found)
            return new Failure<Library>("Loan was not found");
        var loan = found.Value;
        if (!loan.Active) return new Failure<Library>("Returned loans cannot be renewed");
        if (day < loan.Changed || day > loan.Due
            || state.Loans.Any(l => l.Reader == loan.Reader && l.Changed > day))
            return new Failure<Library>("Renew between borrowing and due day");
        if (loan.Renewals != 0) return new Failure<Library>("Only one renewal is allowed");
        return Loan.Create(loan.Id, loan.BookId, loan.Reader, loan.Start, loan.Due + 7, -1, 1, day)
            .Bind(updated => Replace(state, updated));
    }

    private static Result<Library> Replace(Library state, Loan updated)
    {
        return Library.Create(state.Books, state.Loans.Select(l => l.Id == updated.Id ? updated : l));
    }

    public static IEnumerable<Loan> Overdue(IEnumerable<Loan> loans, int day)
    {
        foreach (var loan in loans)
            if (loan.Active && loan.Due < day) yield return loan;
    }

    public static string[] OverdueReport(Library state, int day)
    {
        return Overdue(state.Loans, day)
            .OrderBy(l => l.Due).ThenBy(l => l.Id)
            .Select(l => l.Id + " | " + l.Reader + " | "
                + ((Something<Book>)FindBook(state, l.BookId)).Value.Title
                + " | late days: " + (day - l.Due))
            .ToArray();
    }

    public static string[] CategoryReport(Library state)
    {
        return state.Books.GroupBy(b => b.Category)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Sum(b => b.Copies),
                Active = state.Loans.Count(l => l.Active && g.Any(b => b.Id == l.BookId))
            })
            .OrderByDescending(x => x.Active).ThenBy(x => x.Category, StringComparer.Ordinal)
            .Select(x => x.Category + " | copies: " + x.Total + " | borrowed: "
                + x.Active + " | available: " + (x.Total - x.Active))
            .ToArray();
    }
}
