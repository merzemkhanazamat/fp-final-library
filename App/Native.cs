using System;
using System.Runtime.InteropServices;

namespace LibraryApp;

public static class Native
{
    [DllImport("libraryfee", EntryPoint = "overdue_fee", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Calculate(int due, int day, int rate);

    [DllImport("libraryfee", EntryPoint = "missing_fee", CallingConvention = CallingConvention.Cdecl)]
    private static extern int MissingEntry(int due, int day, int rate);

    public static Result<int> Fee(int due, int day, int rate)
    {
        return Call(() => Calculate(due, day, rate));
    }

    public static Result<int> CheckMissingEntry()
    {
        return Call(() => MissingEntry(0, 1, 100));
    }

    private static Result<int> Call(Func<int> calculate)
    {
        try
        {
            int value = calculate();
            if (value < 0) return new Failure<int>("Invalid fee input");
            return new Success<int>(value);
        }
        catch (DllNotFoundException) { return new Failure<int>("Native library was not found"); }
        catch (EntryPointNotFoundException) { return new Failure<int>("Native function was not found"); }
        catch (BadImageFormatException) { return new Failure<int>("Wrong native architecture"); }
    }

    public static Result<long> TotalFee(Loan[] loans, int day)
    {
        long total = 0;
        foreach (var loan in loans)
        {
            var result = Fee(loan.Due, day, 100);
            if (result is Failure<int> e) return new Failure<long>(e.Error);
            total += ((Success<int>)result).Value;
        }
        return new Success<long>(total);
    }
}
