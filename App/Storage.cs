using System;
using System.Collections.Generic;
using System.IO;

namespace LibraryApp;

public static class Storage
{
    public static Result<Library> Load(string path)
    {
        try
        {
            var books = new List<Book>();
            var loans = new List<Loan>();
            using (var reader = new StreamReader(path))
            {
                if (reader.ReadLine() != "Library,1")
                    return new Failure<Library>("Invalid CSV header");
                string? line;
                int number = 1;
                while ((line = reader.ReadLine()) != null)
                {
                    number++;
                    var p = line.Split(',');
                    if (p.Length == 5 && p[0] == "B")
                    {
                        var result = Book.Create(int.Parse(p[1]), p[2], p[3], int.Parse(p[4]));
                        if (result is Failure<Book> e)
                            return new Failure<Library>("Line " + number + ": " + e.Error);
                        books.Add(((Success<Book>)result).Value);
                    }
                    else if (p.Length == 9 && p[0] == "L")
                    {
                        var result = Loan.Create(int.Parse(p[1]), int.Parse(p[2]), p[3],
                            int.Parse(p[4]), int.Parse(p[5]), int.Parse(p[6]), int.Parse(p[7]),
                            int.Parse(p[8]));
                        if (result is Failure<Loan> e)
                            return new Failure<Library>("Line " + number + ": " + e.Error);
                        loans.Add(((Success<Loan>)result).Value);
                    }
                    else return new Failure<Library>("Invalid CSV row at line " + number);
                }
            }
            return Library.Create(books, loans);
        }
        catch (FileNotFoundException) { return new Failure<Library>("Data file was not found"); }
        catch (DirectoryNotFoundException) { return new Failure<Library>("Data directory was not found"); }
        catch (UnauthorizedAccessException) { return new Failure<Library>("Cannot read data file"); }
        catch (IOException) { return new Failure<Library>("Data read failed"); }
        catch (FormatException) { return new Failure<Library>("Malformed integer in CSV"); }
        catch (OverflowException) { return new Failure<Library>("CSV integer is too large"); }
        catch (ArgumentException) { return new Failure<Library>("Invalid data path"); }
    }

    public static Result<bool> Save(string path, Library state)
    {
        string temp = path + ".tmp";
        try
        {
            using (var writer = new StreamWriter(temp))
            {
                writer.WriteLine("Library,1");
                foreach (var b in state.Books)
                    writer.WriteLine("B," + b.Id + "," + b.Title + "," + b.Category + "," + b.Copies);
                foreach (var l in state.Loans)
                    writer.WriteLine("L," + l.Id + "," + l.BookId + "," + l.Reader + ","
                        + l.Start + "," + l.Due + "," + l.Returned + "," + l.Renewals
                        + "," + l.Changed);
            }
            File.Move(temp, path, true);
            return new Success<bool>(true);
        }
        catch (UnauthorizedAccessException) { return new Failure<bool>("Cannot write data file"); }
        catch (IOException) { return new Failure<bool>("Data save failed"); }
        catch (ArgumentException) { return new Failure<bool>("Invalid data path"); }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
