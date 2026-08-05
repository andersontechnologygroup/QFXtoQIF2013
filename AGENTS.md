# Code Review Standards

This document defines the code review standards and quality requirements for the QFXtoQIF2013 project. All code contributions must comply with these standards before merging.

---

## NASA Power of 10 Rules

The following rules are adapted from **Gerard J. Holzmann's "Power of 10" rules for safety-critical code** (NASA Jet Propulsion Laboratory, 2006). While originally designed for C in safety-critical embedded systems, these principles provide a rigorous foundation for software quality in any context.

### Rule 1: Restrict to Simple Control Flow

**Restrict all code to very simple control flow constructs.**

- Do not use `goto` statements
- Do not use direct or indirect recursion
- Prefer simple, linear control flows

**Rationale:** Complex control flow makes code difficult to review, test, and verify. Simple flows allow static analysis tools to verify execution paths.

```csharp
// ❌ Non-Compliant: Complex nested ternary
return x > 0 ? (y > 0 ? 1 : -1) : (y > 0 ? -1 : 1);

// ✅ Compliant: Simple control flow
if (x > 0 && y > 0) return 1;
if (x < 0 && y < 0) return 1;
return -1;
```

### Rule 2: Give All Loops a Fixed Upper Bound

**Every loop must have a statically provable upper bound on its number of iterations.**

- All `for` and `while` loops must have a maximum iteration count
- If a static checker cannot prove termination, the rule is violated

**Rationale:** Unbounded loops risk infinite execution or resource exhaustion.

```csharp
// ❌ Non-Compliant: Unbounded while loop
while (reader.ReadLine() != null) { /* process */ }

// ✅ Compliant: Fixed upper bound with stored value
const int MaxLines = 100000;
int linesRead = 0;
string? line;
while (linesRead < MaxLines && (line = reader.ReadLine()) != null)
{
    Process(line);
    linesRead++;
}
```

### Rule 3: No Dynamic Memory Allocation After Initialization

**Do not use dynamic memory allocation once the system has entered its operational phase.**

- Avoid `new` allocations in hot paths or processing loops
- Prefer pre-allocated buffers, pools, or stack allocation
- Use `ArrayPool<T>` for temporary buffers

**Rationale:** Runtime allocation can cause fragmentation, leaks, or non-deterministic failures.

```csharp
// ❌ Non-Compliant: Allocation in processing loop
for (int i = 0; i < transactions.Count; i++)
{
    var buffer = new byte[1024]; // Allocation per iteration
    Process(transactions[i], buffer);
}

// ✅ Compliant: Using ArrayPool
var pool = ArrayPool<byte>.Shared;
var buffer = pool.Rent(1024);
try
{
    for (int i = 0; i < transactions.Count; i++)
        Process(transactions[i], buffer);
}
finally
{
    pool.Return(buffer);
}
```

### Rule 4: Keep Functions Small

**No function should be longer than what can be printed on a single standard sheet of paper (approximately 60 lines).**

- Maximum 60 lines per function
- One statement per line
- Functions should do one thing well

**Rationale:** Long functions are difficult to comprehend, test, and verify as single units.

```csharp
// ❌ Non-Compliant: Monolithic function
public void ProcessFile(string path)
{
    // 200 lines of mixed concerns
}

// ✅ Compliant: Single responsibility
public void ProcessFile(string path)
{
    var content = ReadFile(path);
    var transactions = ParseTransactions(content);
    var qif = ConvertToQif(transactions);
    WriteOutput(qif);
}
```

### Rule 5: Maintain High Assertion Density

**Average at least two assertions per function.** Assertions must be side-effect-free boolean tests checking conditions that should never happen.

- Every function should validate its preconditions
- Every function should validate critical invariants
- Assertions must have corresponding recovery actions

**Rationale:** Assertions document programmer assumptions and immediately catch logic errors.

```csharp
// ❌ Non-Compliant: No validation
public string ExtractTagValue(string xml, string tagName)
{
    return Regex.Match(xml, $@"<{tagName}>([^<]+)").Groups[1].Value;
}

// ✅ Compliant: Multiple assertions
public string ExtractTagValue(string xml, string tagName)
{
    if (string.IsNullOrEmpty(xml))
        return string.Empty;                    // Assertion: non-empty input
    if (string.IsNullOrEmpty(tagName))
        return string.Empty;                    // Assertion: valid tag name

    var match = Regex.Match(xml, $@"<{tagName}>([^<]+)", RegexOptions.IgnoreCase);
    if (!match.Success)
        return string.Empty;                    // Assertion: tag found
    return match.Groups[1].Value.Trim();
}
```

### Rule 6: Declare Data at the Smallest Possible Scope

**All data objects must be declared at the innermost scope possible.**

- Avoid global variables
- Declare variables as close to their first use as possible
- Use `readonly` or `const` where applicable

**Rationale:** Minimizing scope reduces coupling and prevents unintended modifications.

```csharp
// ❌ Non-Compliant: Wide scope
string result;
if (condition)
{
    result = ComputeA();
}
else
{
    result = ComputeB();
}
UseResult(result);

// ✅ Compliant: Minimal scope
var result = condition ? ComputeA() : ComputeB();
UseResult(result);
```

### Rule 7: Check Return Values and Parameters

**Every caller must check return values. Every callee must validate input parameters.**

- Never ignore return values from methods that can fail
- Validate all public method parameters
- Use guard clauses at method entry

**Rationale:** Unchecked returns and unverified inputs cause silent failures and crashes.

```csharp
// ❌ Non-Compliant: Ignored return value
int risk = CalculateRisk(score);

// ✅ Compliant: Checking return value
int result = CalculateRisk(score);
if (result < 0)
{
    Logger.LogError("Risk calculation returned negative value");
    return false;
}
```

### Rule 8: Limit Complex Language Features

**Restrict use of complex language features that obscure logic.**

- Avoid excessive use of `#if` / conditional compilation
- Avoid complex LINQ chains that obscure intent
- Prefer explicit code over clever one-liners

**Rationale:** Complex features can hide logic and confuse static analysis tools.

```csharp
// ❌ Non-Compliant: Obscure LINQ chain
var result = data.Where(x => x.IsValid).GroupBy(x => x.Type)
    .Select(g => new { Type = g.Key, Count = g.Count() })
    .OrderByDescending(x => x.Count).First().Type;

// ✅ Compliant: Readable logic
var validItems = data.Where(x => x.IsValid);
var grouped = validItems.GroupBy(x => x.Type);
var largestGroup = grouped.OrderByDescending(g => g.Count()).First();
var result = largestGroup.Key;
```

### Rule 9: Restrict Indirection

**Limit indirection to a single level. Avoid complex pointer/reference chains.**

- Prefer direct references over deep object graphs
- Avoid chains of more than 2 property accesses
- Use meaningful variable names for intermediate values

**Rationale:** Multiple levels of indirection obscure data flow and make verification difficult.

```csharp
// ❌ Non-Compliant: Deep indirection
var value = config.Settings.Database.Connections.Primary.ConnectionString;

// ✅ Compliant: Named intermediate values
var databaseSettings = config.Settings.Database;
var connectionConfig = databaseSettings.Connections.Primary;
var connectionString = connectionConfig.ConnectionString;
```

### Rule 10: Compile with All Warnings Enabled

**All code must compile with zero warnings under strict warning settings.**

- Enable `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in production
- Address all nullable reference type warnings
- Use the strictest analyzers available

**Rationale:** Compiler warnings frequently indicate latent bugs. Zero warnings ensures they are addressed.

```xml
<!-- .csproj configuration -->
<PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Nullable>enable</Nullable>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

---

## General Code Review Standards

### 1. Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase | `QfxToQifConverter` |
| Methods | PascalCase | `ExtractTagValue` |
| Properties | PascalCase | `TransactionCount` |
| Private fields | _camelCase | `_tempDir` |
| Parameters | camelCase | `qfxFilePath` |
| Local variables | camelCase | `transactionMatches` |
| Constants | PascalCase | `MaxRetries` |
| Interfaces | I-prefix | `IProgress<string>` |

### 2. Error Handling

- Use specific exception types, not generic `Exception`
- Always include meaningful error messages
- Log errors before re-throwing
- Use `try/catch` for recoverable errors, let others propagate
- Validate inputs at public API boundaries

### 3. Documentation

- All public APIs must have XML documentation comments
- Complex algorithms must have explanatory comments
- Use `/// <summary>` for all public classes and methods
- Document non-obvious design decisions

### 4. Testing Requirements

- All public methods must have unit tests
- Edge cases and error paths must be tested
- Test names must describe the scenario: `ClassName_Method_Scenario` or `Method_Scenario`
- Maintain minimum 80% code coverage
- Run Stryker mutation testing before major releases

**Test naming examples:**

| Pattern | Example |
|---|---|
| Public method test | `Convert_SingleTransaction_DateFormatted` |
| Form-level test | `Form1_CanBeCreated` |
| Private method (reflection) | `SanitizeAccountName_TrimsWhitespace` |
| Edge case test | `Convert_EmptyFile_ReturnsValidQifHeader` |
| Error path test | `Convert_NullFilePath_ThrowsArgumentException` |

### 5. SOLID Principles

- **S**ingle Responsibility: Each class/method does one thing
- **O**pen/Closed: Open for extension, closed for modification
- **L**iskov Substitution: Subtypes must be substitutable
- **I**nterface Segregation: Prefer small, focused interfaces
- **D**ependency Inversion: Depend on abstractions, not concretions

### 6. Code Organization

- One class per file (with matching name)
- Organize using regions for related methods
- Group `using` directives: System first, then third-party, then project
- Keep related files in the same namespace/folder

### 7. Security

- Never hardcode credentials or secrets
- Validate and sanitize all user input
- Use parameterized queries for database access
- Apply principle of least privilege
- Log security-relevant events

### 8. Performance

- Avoid premature optimization
- Profile before optimizing
- Prefer `Span<T>` and `Memory<T>` for buffer operations
- Use `StringBuilder` for string concatenation in loops
- Cache expensive computations when appropriate

---

## Project-Specific Standards

### QFX/QIF Processing

- All XML parsing must use case-insensitive regex (`RegexOptions.IgnoreCase`)
- Date formats must be validated before parsing with `DateTime.TryParseExact`
- File I/O must use `File.ReadAllText`/`WriteAllText` with explicit encoding (`Encoding.UTF8`)
- Progress reporting must support null callbacks using `?.Report()` pattern
- Transaction extraction must handle malformed XML gracefully (missing tags, empty values)

### WinForms UI

- Controls must be accessible via `Controls["name"]` for testability
- Form must use `FormBorderStyle.FixedSingle` for fixed layouts
- Use `SyncProgress<T>` in tests, not `Progress<T>` (which posts asynchronously)
- Disable interactive controls during async operations
- Re-enable controls in `finally` blocks

### Converter Architecture

- `QfxToQifConverter` must be `static` and stateless
- `ExtractTagValue` must be `internal` for testability
- QIF header must contain exactly 2 `!ACCOUNT` blocks
- Each transaction must end with exactly one `^` terminator
- Empty tag values must return `string.Empty`, not throw

---

## Mutation Testing Requirements

- Maintain minimum 50% mutation score
- No survived mutants on critical code paths
- Equivalent mutants must be documented and added to `stryker-config.json` ignore list
- Run `dotnet stryker` before major releases
- Track mutation score progression across releases

```json
// stryker-config.json - ignore equivalent mutants
{
  "stryker-config": {
    "ignore-mutations": ["StringLiteral"]
  }
}
```

---

## Concurrency Standards

- Use `async/await` for all I/O operations
- Never call `.Result` or `.Wait()` on tasks (causes deadlocks)
- Use `CancellationToken` for cancellable operations
- UI updates must dispatch to UI thread via `Invoke`/`BeginInvoke`
- Use `Task.Run()` only for CPU-bound work, not I/O

```csharp
// ❌ Non-Compliant: Blocking on async
var result = Task.Run(() => ProcessData()).Result;

// ✅ Compliant: Proper async/await
var result = await Task.Run(() => ProcessData());
```

---

## Versioning Policy

- Follow Semantic Versioning (SemVer): `MAJOR.MINOR.PATCH`
- **Major** (X.0.0): Breaking changes to public API
- **Minor** (0.X.0): New features, backward-compatible
- **Patch** (0.0.X): Bug fixes, backward-compatible
- Update `AssemblyVersion` for each release
- Document all changes in `CHANGELOG.md`

---

## Review Checklist

Before approving a pull request, verify:

- [ ] Code compiles with zero warnings
- [ ] All existing tests pass (`dotnet test`)
- [ ] New code has corresponding tests
- [ ] No `goto`, recursion, or unbounded loops
- [ ] Functions are under 60 lines
- [ ] All return values are checked
- [ ] Input parameters are validated
- [ ] No global mutable state introduced
- [ ] Naming follows conventions
- [ ] XML documentation present on public APIs
- [ ] No hardcoded credentials or secrets
- [ ] Error handling is appropriate
- [ ] Code is readable without excessive comments
- [ ] No dead code or commented-out code
- [ ] Mutations tested with Stryker (`dotnet stryker`)
- [ ] No `Progress<T>` in test code (use `SyncProgress<T>`)
- [ ] File I/O uses explicit encoding
- [ ] Regex patterns are case-insensitive where appropriate

---

## References

- [NASA Power of 10 Rules](https://en.wikipedia.org/wiki/Power_of_10_(programming_rules))
- Holzmann, G. J. (2006). "The Power of 10: Rules for Developing Safety-Critical Code." NASA/JPL Laboratory for Reliable Software.
- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [SOLID Principles](https://learn.microsoft.com/en-us/dotnet/standard/modern-web-apps-azure-architecture/principles)
- [Async/Await Best Practices](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)
- [Semantic Versioning](https://semver.org/)
