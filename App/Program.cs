using System;
using System.Linq;

namespace LibraryApp;

public static class Program
{
    public static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : "Data/library.csv";
        var loaded = Storage.Load(path);
        if (loaded is Failure<Library> error)
        {
            Console.WriteLine(error.Error);
            return;
        }
        var state = ((Success<Library>)loaded).Value;
        Func<int, bool> standard = Logic.Limit(2);
        Func<int, bool> extended = Logic.Limit(4);
        while (true)
        {
            Console.WriteLine("\n1 Books  2 Borrow  3 Return  4 Renew  5 Reports  0 Exit");
            string? choice = Console.ReadLine();
            if (choice == null || choice == "0") return;
            if (choice == "1")
            {
                foreach (var b in state.Books)
                    Console.WriteLine(b.Id + " | " + b.Title + " | " + b.Category + " | copies: " + b.Copies);
                foreach (var l in state.Loans.Where(l => l.Active))
                    Console.WriteLine("Loan " + l.Id + " | book " + l.BookId + " | " + l.Reader + " | due " + l.Due);
                continue;
            }
            if (choice == "5")
            {
                if (!ReadInt("Current day (0..365000): ", out int day) || !Validation.Day(day))
                {
                    Console.WriteLine("Invalid day");
                    continue;
                }
                var pending = Logic.Overdue(state.Loans, day);
                var snapshot = pending.ToArray();
                Console.WriteLine("Overdue count: " + snapshot.Length);
                Print("Overdue loans", Logic.OverdueReport(state, day));
                Print("Category stock", Logic.CategoryReport(state));
                var fee = Native.TotalFee(snapshot, day);
                if (fee is Success<long> total) Console.WriteLine("Overdue fee: " + total.Value + " KZT");
                else Console.WriteLine(((Failure<long>)fee).Error);
                continue;
            }
            Result<Library> result;
            if (choice == "2")
            {
                if (!ReadInt("Book ID: ", out int id)) continue;
                Console.Write("Reader name (case-sensitive): ");
                string reader = Console.ReadLine() ?? "";
                if (!ReadInt("Borrow day: ", out int day)) continue;
                Console.Write("Policy (1 standard: 2 loans, 2 extended: 4 loans): ");
                string? policy = Console.ReadLine();
                if (policy != "1" && policy != "2")
                {
                    Console.WriteLine("Invalid policy");
                    continue;
                }
                result = Logic.Borrow(state, id, reader, day, policy == "1" ? standard : extended);
            }
            else if (choice == "3" || choice == "4")
            {
                if (!ReadInt("Loan ID: ", out int id) || !ReadInt("Operation day: ", out int day)) continue;
                result = choice == "3" ? Logic.Return(state, id, day) : Logic.Renew(state, id, day);
            }
            else
            {
                Console.WriteLine("Unknown command");
                continue;
            }
            if (result is Failure<Library> failed) Console.WriteLine(failed.Error);
            else
            {
                var next = ((Success<Library>)result).Value;
                var saved = Storage.Save(path, next);
                if (saved is Failure<bool> problem) Console.WriteLine(problem.Error);
                else
                {
                    state = next;
                    Console.WriteLine("Success. Data saved.");
                }
            }
        }
    }

    private static bool ReadInt(string prompt, out int value)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out value)) return true;
        Console.WriteLine("Enter a valid integer");
        return false;
    }

    private static void Print(string title, string[] rows)
    {
        Console.WriteLine(title);
        if (rows.Length == 0) Console.WriteLine("No matching records");
        foreach (string row in rows) Console.WriteLine(row);
    }
}
