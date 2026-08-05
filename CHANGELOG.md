# Changelog

All notable changes to the QFXtoQIF2013 project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.0] - 2026-08-05

### Added

#### Core Features
- **Input validation** — `Convert` method now validates `qfxFilePath` and `accountName` parameters, throwing `ArgumentException` for null/empty values
- **File existence check** — Throws `FileNotFoundException` with clear message when QFX file not found
- **Empty file handling** — Gracefully handles empty QFX files by returning valid QIF header
- **Debug warning for empty files** — Reports warning via `debugging` callback when QFX file is empty

#### Code Quality
- **XML documentation** — Full XML docs on all public methods (`Convert`, `ExtractTagValue`)
- **Constants extraction** — 14 constants for QIF header strings, tag names, date formats, and regex patterns
- **Form1 refactoring** — Extracted helper methods to comply with NASA Power of 10 Rule 4 (functions < 60 lines):
  - `ValidateInputs` — Input validation logic
  - `SanitizeAccountName` — Account name sanitization
  - `SetUIBusy` — UI state management
  - `CreateDebuggingProgress` — Progress callback factory
  - `CreateStatusProgress` — Status progress factory
  - `SaveQifFileAsync` — Async file save operation
- **Form1 constants** — 17 constants for dialog filters, titles, messages, and status text
- **Form1 XML documentation** — Full XML docs on class and all 8 methods
- **CancellationToken support** — Added `_cancellationTokenSource` for long-running operation cancellation
- **StringBuilder pre-allocation** — `StringBuilder` initialized with capacity 1024 for better performance
- **Assertion density** — Added 5 assertions in `Convert` and 3 in `ExtractTagValue` (NASA Power of 10 Rule 5)
- **TreatWarningsAsErrors** — Enabled in both `.csproj` files to enforce zero warnings (NASA Power of 10 Rule 10)
- **Fixed nullable warnings** — Resolved 72+ CS8625 warnings by using `null!` for intentional null values

#### Testing
- **204 unit tests** — Comprehensive test suite covering:
  - QFX to QIF converter (82 tests — 29 new edge case tests)
  - Form1 UI (74 tests — 25 new reflection-based tests for private methods)
  - Program entry point (10 tests)
  - Targeted mutant killer tests (38 tests)
- **Stryker mutation testing** — Configured and achieving 80.74% mutation score with 3 survived mutants (all in UI layer)
- **Input validation tests** — Tests for null/empty parameters with message validation
- **Empty file tests** — Tests for empty file handling and debug reporting
- **Edge case tests** — 29 new tests covering:
  - Unicode emoji & CJK characters (4 tests)
  - Very large files — 10,000 transactions (2 tests)
  - Special amount formats — no decimal, leading zeros, many decimals, billions (5 tests)
  - Boundary dates — leap year, Y2K, Dec 31, Jan 1 (4 tests)
  - HTML entities — `&lt;`, `&gt;`, `&quot;` captured as literal text (3 tests)
  - Special payee characters — newlines, carriage returns, tabs, null bytes (4 tests)
  - Single-field transactions — only date, only amount, only name (3 tests)
  - QIF structure — ends with caret, all required headers (2 tests)
  - Progress verification — transaction count matches reports (1 test)
  - Mixed issues — multiple transactions with all missing fields (1 test)
- **Form1 private method tests** — 25 new reflection-based tests covering:
  - `SanitizeAccountName` — 6 tests for trimming, newline removal, caret removal, edge cases
  - `ValidateInputs` — 5 tests for empty file, empty account, file not found, valid inputs, sanitization
  - `SetUIBusy` — 5 tests for button disable/enable, status text, debug output clear
  - `CreateDebuggingProgress` — 3 tests for type verification and `IProgress<string>` interface
  - `CreateStatusProgress` — 3 tests for type verification and `IProgress<string>` interface
  - Constants — 1 test verifying all 7 string constants via reflection
  - `_cancellationTokenSource` — 1 test confirming initial null state

#### Documentation
- **AGENTS.md** — Code review standards including NASA Power of 10 rules and test naming convention
- **stryker-config.json** — Mutation testing configuration

### Changed

#### Bug Fixes
- **Removed unnecessary `Thread.Sleep(1)`** — Removed from transaction processing loop

#### UI Improvements
- **Fixed form resize** — Added `FormBorderStyle.FixedSingle` to prevent drag-resize
- **Fixed scrollbars** — Changed debug output textbox from `Horizontal` to `Vertical` scrollbars

#### Code Quality
- **Test naming consistency** — Fixed inconsistent test names across all test files:
  - Renamed `AccountName_Sanitization_RemovesInvalidCharacters` → `SanitizeAccountName_RemovesInvalidCharacters`
  - Renamed `QIF_TransactionPayeeFollowsCaret` → `QIF_Transaction_PayeeFollowsCaret`
  - Updated AGENTS.md with test naming standard: `ClassName_Method_Scenario` or `Method_Scenario`
  - All 204 tests now follow consistent naming convention

### Technical Details

#### QfxToQifConverter.cs
```csharp
// Input validation added
if (string.IsNullOrEmpty(qfxFilePath))
    throw new ArgumentException("File path cannot be null or empty.", nameof(qfxFilePath));
if (string.IsNullOrEmpty(accountName))
    throw new ArgumentException("Account name cannot be null or empty.", nameof(accountName));
if (!File.Exists(qfxFilePath))
    throw new FileNotFoundException("QFX file not found.", qfxFilePath);

// Empty file handling
if (string.IsNullOrEmpty(fileContent))
{
    debugging?.Report("Warning: QFX file is empty.");
    return BuildQifHeader(accountName);
}
```

#### Constants Extracted
```csharp
// QIF Header Constants
private const string OptionAutoSwitch = "!Option:AutoSwitch";
private const string AccountTag = "!ACCOUNT";
private const string ClearAutoSwitch = "!Clear:AutoSwitch";
private const string TypeBank = "!Type:Bank";
private const string BankType = "TBank";
private const string RecordTerminator = "^";

// QIF Transaction Line Prefixes
private const char DatePrefix = 'D';
private const char AmountPrefix = 'T';
private const char ClearedPrefix = 'C';
private const char PayeePrefix = 'P';

// Transaction Tag Names
private const string TagDatePosted = "DTPOSTED";
private const string TagTransactionAmount = "TRNAMT";
private const string TagName = "NAME";

// Date Format Constants
private const string InputDateFormat = "yyyyMMdd";
private const string OutputDateFormat = "MM/dd/yyyy";
private const int MinDateLength = 8;

// Regex Patterns
private const string TransactionPattern = @"<STMTTRN>([\s\S]*?)</STMTTRN>";
private const string TagValuePattern = @"<{0}>([^<]+)";
```

---

## [1.0.5] - 2026-07-16

### Added

#### Core Features
- **Input validation** — `Convert` method now validates `qfxFilePath` and `accountName` parameters, throwing `ArgumentException` for null/empty values
- **File existence check** — Throws `FileNotFoundException` with clear message when QFX file not found
- **Debug warning for empty files** — Reports warning via `debugging` callback when QFX file is empty

#### Testing
- **30 unit tests** — Test suite covering:
  - QFX to QIF converter (30 tests)
- **Input validation tests** — Tests for null/empty parameters with message validation
- **Empty file tests** — Tests for empty file handling and debug reporting

#### Documentation
- **CHANGELOG.md** — This file

### Changed

#### Bug Fixes
- **Fixed off-by-one record count** — Removed erroneous `+ 1` from progress reports showing transaction count

#### UI Improvements
- **Input sanitization** — Account name now trimmed and stripped of `\n`, `\r`, `^` characters

#### Refactoring
- **Extracted `QfxToQifConverter`** — Moved converter class to its own file (`QfxToQifConverter.cs`)
- **Made `ExtractTagValue` internal** — Changed from `private` to `internal` for testability
- **Added `InternalsVisibleTo`** — Test project can access internal types

---

## [1.0.0] - 2026-06-09

### Added

#### Initial Release
- **QFX to QIF conversion** — Converts QFX file format to QIF format
- **Account name header** — Adds required header for Quicken 2013 import
- **Progress reporting** — Displays conversion progress in UI
- **Debug output** — Shows detailed conversion logs
- **File dialogs** — Open/Save file dialogs with proper filters

#### Technical Implementation
- **Windows Forms UI** — Simple, fixed-size form with input fields
- **Async conversion** — Uses `Task.Run` for non-blocking conversion
- **Regex parsing** — Case-insensitive XML tag extraction
- **Date formatting** — Converts `yyyyMMdd` to `MM/dd/yyyy`
- **Error handling** — Try/catch with user-friendly error messages

#### Project Structure
- **QFXtoQIF2013/** — Main application project
- **QFXtoQIF2013.sln** — Solution file
- **.NET 9.0** — Target framework

---

## Version History Summary

| Version | Date | Tests | Mutation Score | Survived |
|---|---|---|---|---|
| 1.1.0 | 2026-08-05 | 204 | 80.74% | 3 |
| 1.0.5 | 2026-07-16 | 30 | N/A | N/A |
| 1.0.0 | 2026-06-09 | 0 | N/A | N/A |

---

## Migration Guide

### From 1.0.0 to 1.1.0

**Breaking Changes:** None

**New Dependencies:** None

**Configuration Changes:** None

**API Changes:**
- `QfxToQifConverter.Convert()` now throws `ArgumentException` for null/empty parameters
- `QfxToQifConverter.Convert()` now throws `FileNotFoundException` for missing files

**Recommended Actions:**
- Update any code that calls `Convert` to handle the new exceptions
- Run `dotnet test` to verify compatibility
- Run `dotnet stryker` to verify mutation score

---

## Contributors

- **Jason Anderson** — Project Lead
- **Buffy (AI)** — Code review, testing, and documentation

---

## License

MIT License - See [LICENSE](LICENSE) for details.
