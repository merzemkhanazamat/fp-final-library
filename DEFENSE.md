# Defense

1. What does the project do?
It manages borrowing, returning and renewing library books. It reports overdue loans, category stock and estimated late fees.

2. What is pure in your code?
Logic uses explicit inputs and returns new values. It does not read Console input, access files or change the old Library. Storage, Console and native calls are outside that core.

3. How do Map, Bind and Maybe work?
Map changes a successful value. Bind connects a function returning another Result. Failure skips the next transformation. Maybe represents found and missing lookups.

4. How do you prevent an invalid sequence?
Factories validate construction. Return and Renew require an existing active loan. Changed rejects backdating. A returned loan cannot be returned or renewed again. Failed operations leave the old state unchanged.

5. Where are currying and higher-order functions?
Limit receives a maximum and returns a function checking the active-loan count. Limit(2) and Limit(4) configure two policies. CheckReader accepts and invokes that Func. With two active loans, only extended allows a third if the other checks pass.

6. Explain reports and lazy processing.
OverdueReport filters, orders by due day and ID, and projects rows. CategoryReport groups and aggregates stock. Overdue uses yield return and starts enumeration only when consumed. ToArray creates a reusable snapshot. Reusing it does not repeat the source iteration.

7. Explain files, native integration and tests.
Storage validates CSV and disposes streams with using. Save uses a temporary file; the UI adopts the state only after saving. DllImport calls overdue_fee in C for the late-fee total. Missing functions and libraries return failures. The suite has 25 passing tests on Linux, including the real native call and a deliberately missing symbol. The Mac run still needs local verification.

Live change practice: change the standard limit from 2 to 3, update the affected policy test and run sh test.sh. Changing the renewal duration also requires changing Loan.Create's invariant, Renew and related tests.
