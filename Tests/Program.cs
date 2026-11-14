using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibraryApp;

public static class Tests
{
    private static int passed;
    private static int failed;

    public static int Main()
    {
        Run("Book validation", () =>
        {
            Equal(true, Book.Create(1, "Book", "IT", 1) is Success<Book>);
            Equal(true, Book.Create(0, "Book", "IT", 1) is Failure<Book>);
            Equal(true, Book.Create(1, "Book", "IT", 0) is Failure<Book>);
            Equal(true, Book.Create(1, "A,B", "IT", 1) is Failure<Book>);
        });
        Run("Loan validation", () =>
        {
            Equal(true, Loan.Create(1, 1, "A", 0, 14, -1, 0, 0) is Success<Loan>);
            Equal(true, Loan.Create(1, 1, "A", 0, 10, -1, 0, 0) is Failure<Loan>);
            Equal(true, Loan.Create(1, 1, "A", 5, 19, 4, 0, 4) is Failure<Loan>);
        });
        Run("Request boundaries", () =>
        {
            Equal(true, Request.Create(1, "A", 0) is Success<Request>);
            Equal(true, Request.Create(1, "A", 365000) is Success<Request>);
            Equal(true, Request.Create(1, "A", -1) is Failure<Request>);
            Equal(true, Request.Create(1, "", 0) is Failure<Request>);
        });
        Run("Duplicate and missing foreign keys", () =>
        {
            var b = Get(Book.Create(1, "A", "IT", 1));
            Equal(true, Library.Create(new[] { b, b }, Array.Empty<Loan>()) is Failure<Library>);
            var loan = Get(Loan.Create(1, 9, "A", 0, 14, -1, 0, 0));
            Equal(true, Library.Create(new[] { b }, new[] { loan }) is Failure<Library>);
        });
        Run("Maybe lookup", () =>
        {
            Equal(true, Logic.FindBook(Seed(), 1) is Something<Book>);
            Equal(true, Logic.FindBook(Seed(), 99) is Nothing<Book>);
            Equal(true, Logic.FindLoan(Seed(), 99) is Nothing<Loan>);
        });
        Run("Availability validation", () =>
        {
            var request = Get(Request.Create(2, "B", 0));
            Equal(true, Logic.PrepareLoan(Seed(), request) is Success<Loan>);
            var s = Get(Logic.Borrow(Seed(), 2, "A", 0, Logic.Limit(2)));
            Equal(true, Logic.PrepareLoan(s, request) is Failure<Loan>);
            Equal(true, Logic.PrepareLoan(s, Get(Request.Create(99, "B", 0))) is Failure<Loan>);
        });
        Run("Map and Bind failure propagation", () =>
        {
            int calls = 0;
            Result<int> start = new Failure<int>("stop");
            var result = start.Map(n => { calls++; return n + 1; })
                .Bind(n => { calls++; return new Success<int>(n + 1); });
            Equal(0, calls);
            var invalid = Logic.Borrow(Seed(), 1, "", 0, count => { calls++; return true; });
            Equal(true, invalid is Failure<Library>);
            Equal(0, calls);
            Equal("stop", ((Failure<int>)result).Error);
            Equal(6, Get(new Success<int>(2).Map(n => n * 3)));
        });
        Run("Configured limits", () =>
        {
            Equal(false, Logic.Limit(2)(2));
            Equal(true, Logic.Limit(4)(2));
            var s = Get(Logic.Borrow(Seed(), 1, "A", 0, Logic.Limit(4)));
            s = Get(Logic.Borrow(s, 2, "A", 0, Logic.Limit(4)));
            Equal(true, Logic.Borrow(s, 3, "A", 0, Logic.Limit(2)) is Failure<Library>);
            Equal(true, Logic.Borrow(s, 3, "A", 0, Logic.Limit(4)) is Success<Library>);
        });
        Run("Borrow and return complete workflow", () =>
        {
            var original = Seed();
            var borrowed = Get(Logic.Borrow(original, 1, "A", 0, Logic.Limit(2)));
            Equal(0, original.Loans.Count());
            Equal(1, borrowed.Loans.Count());
            var returned = Get(Logic.Return(borrowed, 1, 3));
            Equal(false, returned.Loans.Single().Active);
            Equal(true, borrowed.Loans.Single().Active);
            Equal(true, Logic.Return(returned, 1, 4) is Failure<Library>);
        });
        Run("Borrow renew return complete workflow", () =>
        {
            var s = Get(Logic.Borrow(Seed(), 1, "A", 0, Logic.Limit(2)));
            s = Get(Logic.Renew(s, 1, 5));
            Equal(21, s.Loans.Single().Due);
            Equal(true, Logic.Return(s, 1, 4) is Failure<Library>);
            s = Get(Logic.Return(s, 1, 6));
            Equal(6, s.Loans.Single().Returned);
            Equal(true, Logic.Renew(s, 1, 7) is Failure<Library>);
        });
        Run("Missing file", () => WithFolder(folder =>
        {
            Equal(true, Storage.Load(Path.Combine(folder, "missing.csv")) is Failure<Library>);
        }));
        Run("Malformed file releases resource", () => WithFolder(folder =>
        {
            string path = Path.Combine(folder, "bad.csv");
            File.WriteAllText(path, "Library,1\nB,x,A,IT,1\n");
            Equal(true, Storage.Load(path) is Failure<Library>);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Equal(true, stream.CanWrite);
            File.Delete(path);
            Equal(false, File.Exists(path));
        }));
        Run("Missing and invalid operation ordering", () =>
        {
            Equal(true, Logic.Return(Seed(), 1, 0) is Failure<Library>);
            Equal(true, Logic.Renew(Seed(), 1, 0) is Failure<Library>);
            var s = Get(Logic.Borrow(Seed(), 1, "A", 5, Logic.Limit(2)));
            Equal(true, Logic.Return(s, 1, 4) is Failure<Library>);
            Equal(true, Logic.Renew(s, 1, 4) is Failure<Library>);
            Equal(true, Logic.Borrow(s, 2, "A", 4, Logic.Limit(2)) is Failure<Library>);
        });
        Run("Unavailable and duplicate borrowing", () =>
        {
            var s = Get(Logic.Borrow(Seed(), 2, "A", 0, Logic.Limit(2)));
            Equal(true, Logic.Borrow(s, 2, "B", 0, Logic.Limit(2)) is Failure<Library>);
            Equal(true, Logic.Borrow(s, 2, "A", 0, Logic.Limit(2)) is Failure<Library>);
            Equal(true, Logic.Borrow(s, 99, "B", 0, Logic.Limit(2)) is Failure<Library>);
            Equal(1, s.Loans.Count());
        });
        Run("Renewal and due-day boundaries", () =>
        {
            var s = Get(Logic.Borrow(Seed(), 1, "A", 0, Logic.Limit(2)));
            Equal(0, Logic.Overdue(s.Loans, 14).Count());
            Equal(1, Logic.Overdue(s.Loans, 15).Count());
            Equal(true, Logic.Renew(s, 1, 15) is Failure<Library>);
            s = Get(Logic.Renew(s, 1, 14));
            Equal(21, s.Loans.Single().Due);
            Equal(true, Logic.Renew(s, 1, 15) is Failure<Library>);
        });
        Run("Overdue reader cannot borrow", () =>
        {
            var s = Get(Logic.Borrow(Seed(), 1, "A", 0, Logic.Limit(2)));
            Equal(true, Logic.Borrow(s, 2, "A", 15, Logic.Limit(4)) is Failure<Library>);
            s = Get(Logic.Return(s, 1, 15));
            Equal(true, Logic.Borrow(s, 2, "A", 15, Logic.Limit(4)) is Success<Library>);
        });
        Run("Reports empty and ties", () =>
        {
            var empty = Get(Library.Create(Array.Empty<Book>(), Array.Empty<Loan>()));
            Equal(0, Logic.OverdueReport(empty, 0).Length);
            Equal(0, Logic.CategoryReport(empty).Length);
            var s = Get(Logic.Borrow(Seed(), 1, "A", 0, Logic.Limit(4)));
            s = Get(Logic.Borrow(s, 3, "B", 0, Logic.Limit(4)));
            var overdue = Logic.OverdueReport(s, 15);
            Equal("1 | A | CSharp | late days: 1", overdue[0]);
            Equal("2 | B | Maths | late days: 1", overdue[1]);
            var stock = Logic.CategoryReport(s);
            Equal("IT | copies: 3 | borrowed: 1 | available: 2", stock[0]);
            Equal("Math | copies: 1 | borrowed: 1 | available: 0", stock[1]);
        });
        Run("Lazy query and snapshot", () =>
        {
            var s = Get(Logic.Borrow(Seed(), 1, "A", 0, Logic.Limit(2)));
            var query = Logic.Overdue(s.Loans, 15);
            var first = query.ToArray();
            var second = query.ToArray();
            Equal(1, first.Length);
            Equal(first[0].Id, second[0].Id);
            var returned = Get(Logic.Return(s, 1, 15));
            Equal(1, first.Length);
            Equal(0, Logic.Overdue(returned.Loans, 15).Count());
            Equal(true, s.Loans.Single().Active);
        });
        Run("Deferred source is not enumerated at construction", () =>
        {
            int visits = 0;
            var state = Get(Logic.Borrow(Seed(), 1, "A", 0, Logic.Limit(2)));
            IEnumerable<Loan> Source()
            {
                visits++;
                foreach (var loan in state.Loans) yield return loan;
            }
            var query = Logic.Overdue(Source(), 15);
            Equal(0, visits);
            var snapshot = query.ToArray();
            Equal(1, visits);
            Equal(1, snapshot.Count());
            Equal(1, snapshot.Count());
            Equal(1, visits);
        });
        Run("Caller collection isolation", () =>
        {
            var books = Seed().Books.ToArray();
            var s = Get(Library.Create(books, Array.Empty<Loan>()));
            books[0] = Get(Book.Create(8, "Changed", "Other", 1));
            Equal(1, s.Books.First().Id);
        });
        Run("Save load roundtrip", () => WithFolder(folder =>
        {
            string path = Path.Combine(folder, "data.csv");
            var s = Get(Logic.Borrow(Seed(), 1, "A", 0, Logic.Limit(2)));
            s = Get(Logic.Renew(s, 1, 5));
            Equal(true, Get(Storage.Save(path, s)));
            var loaded = Get(Storage.Load(path));
            Equal(3, loaded.Books.Count());
            Equal(21, loaded.Loans.Single().Due);
            Equal(5, loaded.Loans.Single().Changed);
            Equal(true, Get(Storage.Save(path, loaded)));
            Equal(false, File.Exists(path + ".tmp"));
        }));
        Run("Save failure keeps domain state", () => WithFolder(folder =>
        {
            var s = Seed();
            Equal(true, Storage.Save(Path.Combine(folder, "absent", "data.csv"), s) is Failure<bool>);
            Equal(0, s.Loans.Count());
        }));
        Run("Loaded invalid business data rejected", () => WithFolder(folder =>
        {
            string path = Path.Combine(folder, "invalid.csv");
            File.WriteAllText(path, "Library,1\nB,1,A,IT,1\nB,1,B,IT,1\n");
            Equal(true, Storage.Load(path) is Failure<Library>);
            File.WriteAllText(path, "Library,1\nL,1,99,A,0,14,-1,0,0\n");
            Equal(true, Storage.Load(path) is Failure<Library>);
        }));
        Run("Real native fee and report total", () =>
        {
            Equal(600, Get(Native.Fee(14, 20, 100)));
            Equal(0, Get(Native.Fee(14, 14, 100)));
            Equal(0, Get(Native.Fee(14, 10, 100)));
            Equal(true, Native.Fee(-1, 20, 100) is Failure<int>);
            var s = Get(Logic.Borrow(Seed(), 1, "A", 0, Logic.Limit(2)));
            Equal(600L, Get(Native.TotalFee(Logic.Overdue(s.Loans, 20).ToArray(), 20)));
        });
        Run("Controlled native entry failure", () =>
        {
            var result = Native.CheckMissingEntry();
            Equal(true, result is Failure<int>);
            Equal("Native function was not found", ((Failure<int>)result).Error);
        });
        Console.WriteLine("Passed: " + passed + ", failed: " + failed);
        return failed == 0 ? 0 : 1;
    }

    private static Library Seed()
    {
        return Get(Library.Create(new[]
        {
            Get(Book.Create(1, "CSharp", "IT", 2)),
            Get(Book.Create(2, "FP", "IT", 1)),
            Get(Book.Create(3, "Maths", "Math", 1))
        }, Array.Empty<Loan>()));
    }

    private static T Get<T>(Result<T> result)
    {
        if (result is Success<T> s) return s.Value;
        throw new Exception(((Failure<T>)result).Error);
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception("Expected: " + expected + "; actual: " + actual);
    }

    private static void Run(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }

    private static void WithFolder(Action<string> test)
    {
        string folder = Path.Combine(Path.GetTempPath(), "LibraryTests-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try { test(folder); }
        finally { Directory.Delete(folder, true); }
    }
}
