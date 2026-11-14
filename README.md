# Library Management

Project owner: Azamat Merzemkhan, IT-2513. Individual project.

## Run

Required: .NET SDK 10.0.x and a C compiler. On MacBook M2 use arm64 .NET and Apple Command Line Tools. If cc is missing, run xcode-select --install.

From the extracted LibraryFinal folder:

```bash
sh test.sh
sh run.sh
```

The scripts build the C library first. To use another data file:

```bash
sh run.sh Data/demo.csv
```

Separate commands:

```bash
sh Native/build.sh
dotnet build App/App.csproj
dotnet build Tests/Tests.csproj
dotnet run --project Tests/Tests.csproj
dotnet run --project App/App.csproj -- Data/library.csv
```

Native/build.sh uses cc -dynamiclib on macOS and cc -shared -fPIC on Linux. The project copies the matching library into its output directory for DllImport. Rebuild .NET after changing the native library. Native binaries, bin and obj folders are excluded from the archive. No external packages are required. Windows is not supported.

## Application

Menu: 1 books and active loans, 2 borrow, 3 return, 4 renew, 5 reports, 0 exit.

Book and Loan are related by BookId. A loan lasts 14 days and can be renewed once for 7 more days. Dates are integer simulation days from 0 through 365000. Due day is not overdue. Reader names are case-sensitive. Standard policy permits 2 active loans; extended permits 4. With 2 active loans, standard rejects a third and extended permits it if other checks pass.

Logic.cs contains the pure workflows and two LINQ reports. Types.cs contains validated immutable models, Result, Maybe, Map and Bind. Overdue uses yield return; Program materialises a snapshot before reusing it. Storage.cs owns CSV I/O and disposes streams with using. It writes a temporary file before replacing the old data. Program accepts new state only after saving succeeds. Native.cs calls Native/fee.c to calculate a report estimate of 100 KZT per late day. No money is collected.

## Data and limits

Default path: Data/library.csv, relative to the project folder when using run.sh.

Header: Library,1

Book row: B,id,title,category,copies

Loan row: L,id,bookId,reader,start,due,returned,renewals,changed

Returned=-1 means active. Text must be trimmed, nonempty, at most 80 characters, without commas, quotes or newlines. Loaded values and relationships are validated. Missing files, malformed content and native loading errors produce explicit failures. Returned loans remain as history. Use one process per data file. Historical queries, concurrent writes and power-loss durability are outside scope.

## Verification and demonstration

Verified on Linux x64, .NET SDK 10.0.100: build with 0 warnings and 0 errors; 25 tests passed, 0 failed. Real P/Invoke success and a deliberately missing native symbol are tested. Tests include 8 pure/domain scenarios, 2 full workflows and 2 missing/malformed file scenarios, plus reports, ordering, policies, lazy snapshots, resources and persistence. See Evidence/tests.txt. Run sh test.sh on the target Mac before submission.

Copy the sample before a demonstration: cp Data/library.csv Data/demo.csv. Borrow book 2 as Azamat on day 1, policy 1: loan 2 is due on day 15. Borrow the same book as Other: unavailable. Renew loan 2 on day 2: due day 22. Return on day 3: success; repeat return: failure. Report day 20: original Demo Reader loan is 6 days overdue, native fee total 600 KZT. Actual output is in Evidence/demo.txt.

## Submission

Repository URL: https://github.com/merzemkhanazamat/fp-final-library
Branch: main
Submitted source commit: 379b14274253faf3c286bc0c3ad9bf1063ae15cb

The source files in this archive match the submitted source commit above. A subsequent documentation-only commit on main records the source identifier; it does not change application code. Both reports are included in the LMS archive, rather than the public repository; review the individual contribution account in your own words.

References: supplied Final Project Requirements PDF, L4-L10 materials and the live LMS Final Exam requirements. DEFENSE.md contains seven defense questions and answers.
