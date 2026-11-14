# Library Management

Project owner: Azamat Merzemkhan, IT-2513. Individual project.

## Build and run on macOS

Required: .NET SDK 10.0.x and Apple Command Line Tools. Use arm64 .NET on a MacBook M2. If cc is missing, install the Apple tools with xcode-select --install.

Run from the project root:

```bash
mkdir -p runtimes
cc -dynamiclib -Wall -Wextra -Werror Native/fee.c -o runtimes/liblibraryfee.dylib
dotnet build App/App.csproj
dotnet run --project Tests/Tests.csproj
dotnet run --project App/App.csproj -- Data/library.csv
```

On Linux, replace the cc command with:

```bash
cc -shared -fPIC -Wall -Wextra -Werror Native/fee.c -o runtimes/liblibraryfee.so
```

The project files copy the matching native library into the output folder for DllImport. Rebuild .NET after changing the C library. No external packages are required. Windows is not supported. Exclude bin, obj and compiled native files from the submission; the commands above reproduce them.

## Scope and rules

Three workflows: borrow, return and renew. Two LINQ reports: overdue loans and stock by category. The native C function calculates an estimated fine of 100 KZT per late day. No money is collected.

A loan lasts 14 days and can be renewed once for 7 more days. Dates are integer days from 0 through 365000. The due day is not overdue. Reader names are case-sensitive. Standard policy allows 2 active loans; extended allows 4. With 2 active loans, only extended permits a third when the other checks pass. Backdated reader operations are rejected.

Types.cs contains immutable validated models, Result, Maybe, Map and Bind. Logic.cs contains the pure pipeline and reports. Overdue uses yield return and Program materialises a reusable snapshot with ToArray. Storage.cs handles CSV validation, stream disposal and temporary-file saving. The UI accepts new state only after saving succeeds. Native.cs handles native loading and entry-point failures.

## Data and demonstration

The default path is Data/library.csv relative to the working directory. Pass another relative or absolute path after --.

CSV header: Library,1

Book row: B,id,title,category,copies

Loan row: L,id,bookId,reader,start,due,returned,renewals,changed

Returned=-1 means active. Text must be trimmed, nonempty, at most 80 characters, without commas, quotes or newlines. Factories and Library.Create reject invalid values and relationships. Returned loans are retained as history. Single-process use is required; historical queries and crash/power-loss durability are outside scope.

For a demonstration, copy Data/library.csv to Data/demo.csv and run with that path. Borrow book 2 as Azamat on day 1, policy 1: loan 2 is due on day 15. Another reader cannot borrow its only copy. Renew loan 2 on day 2: due day 22. Return on day 3 succeeds; a repeated return fails. Report day 20: the original Demo Reader loan is 6 days overdue, total fine 600 KZT.

## Validation and submission

Last verified on Linux x64 with .NET SDK 10.0.100: build with 0 warnings and 0 errors; 25 tests passed, 0 failed. Tests cover pure/domain rules, complete workflows, missing/malformed files, policies, ordering, propagation, reports, lazy snapshots, resources and real native success/failure. Run the test command above on the target Mac.

Repository: https://github.com/merzemkhanazamat/fp-final-library
Branch: main
Submitted source commit: 379b14274253faf3c286bc0c3ad9bf1063ae15cb

Both mandatory reports are in the LMS archive rather than the public repository. The submitted code matches the recorded source commit; later documentation updates do not change application logic.

References: provided Final Project Requirements PDF, L4-L10 materials and the inspected LMS Final Exam requirements.
