using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;
using QFXtoQIF2013;

namespace QFXtoQIF2013.Tests
{
    /// <summary>
    /// Synchronous IProgress<T> implementation for deterministic testing.
    /// Unlike Progress<T>, which posts to SynchronizationContext,
    /// this captures values immediately on the calling thread.
    /// </summary>
    internal class SyncProgress<T> : IProgress<T>
    {
        private readonly List<T> _values = new();
        public IReadOnlyList<T> Values => _values;
        public void Report(T value) => _values.Add(value);
    }

    
    public class QfxToQifConverterTests : IDisposable
    {
        private readonly string _tempDir;

        public QfxToQifConverterTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "qfxtoqif_tests_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private string WriteTempQfx(string content)
        {
            var path = Path.Combine(_tempDir, "test.qfx");
            File.WriteAllText(path, content);
            return path;
        }

        // ── QIF Header Tests ──

        [Fact]
        public void Convert_IncludesAccountNameInHeader()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>100.00</TRNAMT><NAME>Store</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "My Checking", null!, null!);

            Assert.Contains("NMy Checking", result);
        }

        [Fact]
        public void Convert_IncludesQIFHeaderStructure()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>50.00</TRNAMT><NAME>Shop</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("!Option:AutoSwitch", result);
            Assert.Contains("!ACCOUNT", result);
            Assert.Contains("TBank", result);
            Assert.Contains("!Clear:AutoSwitch", result);
            Assert.Contains("!Type:Bank", result);
        }

        [Fact]
        public void Convert_HeaderContainsAccountNameTwice()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "TestAcct", null!, null!);

            // Account header appears twice (once per !ACCOUNT block)
            int firstIndex = result.IndexOf("NTestAcct");
            int secondIndex = result.IndexOf("NTestAcct", firstIndex + 1);
            Assert.True(firstIndex >= 0, "First account name not found");
            Assert.True(secondIndex >= 0, "Second account name not found");
        }

        // ── Single Transaction Tests ──

        [Fact]
        public void Convert_SingleTransaction_DateFormatted()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230315</DTPOSTED><TRNAMT>25.50</TRNAMT><NAME>Coffee Shop</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D03/15/2023", result);
        }

        [Fact]
        public void Convert_SingleTransaction_AmountIncluded()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>-123.45</TRNAMT><NAME>Bills</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T-123.45", result);
            Assert.Contains("C*", result);
        }

        [Fact]
        public void Convert_SingleTransaction_PayeeName()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>My Payee</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PMy Payee", result);
        }

        [Fact]
        public void Convert_TransactionEndsWithCaret()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Store</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // After the header and transaction, there should be a '^' terminator
            // Find the last '^' in the output
            int lastCaret = result.LastIndexOf('^');
            Assert.True(lastCaret > 0, "Transaction terminator '^' not found");
        }

        // ── Multiple Transaction Tests ──

        [Fact]
        public void Convert_MultipleTransactions_AllParsed()
        {
            var qfx = @"
<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Store1</NAME></STMTTRN>
<STMTTRN><DTPOSTED>20230102</DTPOSTED><TRNAMT>20.00</TRNAMT><NAME>Store2</NAME></STMTTRN>
<STMTTRN><DTPOSTED>20230103</DTPOSTED><TRNAMT>30.00</TRNAMT><NAME>Store3</NAME></STMTTRN>";
            var path = WriteTempQfx(qfx);

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PStore1", result);
            Assert.Contains("PStore2", result);
            Assert.Contains("PStore3", result);
        }

        [Fact]
        public void Convert_MultipleTransactions_DatesFormatted()
        {
            var qfx = @"
<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>A</NAME></STMTTRN>
<STMTTRN><DTPOSTED>20231231</DTPOSTED><TRNAMT>20.00</TRNAMT><NAME>B</NAME></STMTTRN>";
            var path = WriteTempQfx(qfx);

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D01/01/2023", result);
            Assert.Contains("D12/31/2023", result);
        }

        // ── Missing/Empty Field Tests ──

        [Fact]
        public void Convert_MissingDate_OmitsDateLine()
        {
            var path = WriteTempQfx("<STMTTRN><TRNAMT>10.00</TRNAMT><NAME>Store</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Should not contain any 'D' date line in the transaction section
            // (header doesn't have D lines)
            var lines = result.Split('\n');
            bool hasDateLine = false;
            bool pastHeader = false;
            foreach (var line in lines)
            {
                if (line.Contains("!Type:Bank"))
                    pastHeader = true;
                if (pastHeader && line.StartsWith("D"))
                    hasDateLine = true;
            }
            Assert.False(hasDateLine, "Date line should not be present when DTPOSTED is missing");
        }

        [Fact]
        public void Convert_MissingAmount_OmitsAmountAndCheckmark()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><NAME>Store</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // No T line should appear in transaction section
            var lines = result.Split('\n');
            bool pastHeader = false;
            bool hasAmountLine = false;
            foreach (var line in lines)
            {
                if (line.Contains("!Type:Bank"))
                    pastHeader = true;
                if (pastHeader && line.StartsWith("T"))
                    hasAmountLine = true;
            }
            Assert.False(hasAmountLine, "Amount line should not be present when TRNAMT is missing");
        }

        [Fact]
        public void Convert_MissingPayee_OmitsPayeeLine()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.DoesNotContain("\nP", result);
        }

        [Fact]
        public void Convert_NoTransactions_StillReturnsValidHeader()
        {
            var path = WriteTempQfx("<OFX>Some header content</OFX>");

            var result = QfxToQifConverter.Convert(path, "EmptyAcct", null!, null!);

            Assert.Contains("!Option:AutoSwitch", result);
            Assert.Contains("NEmptyAcct", result);
            Assert.Contains("!Type:Bank", result);
        }

        // ── Date Edge Cases ──

        [Fact]
        public void Convert_DateWithTimeSuffix_ExtractsDatePortion()
        {
            // QFX dates can have time suffixes like 20230101120000
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101120000</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Store</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D01/01/2023", result);
        }

        [Fact]
        public void Convert_DateTooShort_OmitsDate()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>2023</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Store</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            var lines = result.Split('\n');
            bool pastHeader = false;
            foreach (var line in lines)
            {
                if (line.Contains("!Type:Bank"))
                    pastHeader = true;
                if (pastHeader && line.StartsWith("D"))
                    Assert.Fail("Should not parse a date shorter than 8 characters");
            }
        }

        // ── Case Insensitivity Tests ──

        [Fact]
        public void Convert_LowercaseTags_StillParsed()
        {
            var path = WriteTempQfx("<stmttrn><dtposted>20230101</dtposted><trnamt>10.00</trnamt><name>Store</name></stmttrn>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D01/01/2023", result);
            Assert.Contains("T10.00", result);
            Assert.Contains("PStore", result);
        }

        // ── Progress Reporting Tests ──

        [Fact]
        public void Convert_ReportsProgress()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Store</NAME></STMTTRN>");
            var messages = new List<string>();
            var progress = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", null!, progress);

            Assert.Contains(progress.Values, m => m.Contains("Reading file"));
            Assert.Contains(progress.Values, m => m.Contains("Found 1 records"));
            Assert.Contains(progress.Values, m => m.Contains("Processing transaction 1 of 1"));
        }

        [Fact]
        public void Convert_NullProgress_DoesNotThrow()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Store</NAME></STMTTRN>");

            var exception = Record.Exception(() => QfxToQifConverter.Convert(path, "Account", null!, null!));

            Assert.Null(exception);
        }

        // ── Whitespace / Special Characters ──

        [Fact]
        public void Convert_PayeeWithLeadingTrailingSpaces_IsTrimmed()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>  Padded Name  </NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PPadded Name", result);
            Assert.DoesNotContain("P  Padded Name", result);
        }

        [Fact]
        public void Convert_NegativeAmount_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>-500.00</TRNAMT><NAME>Withdrawal</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T-500.00", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Malformed / Unusual XML
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_UnclosedTag_TreatedAsEmpty()
        {
            // STMTTRN without closing tag — regex won't match, so 0 transactions
            var path = WriteTempQfx("<OFX><STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT></OFX>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // No transactions parsed, but header is still valid
            Assert.Contains("!Type:Bank", result);
            Assert.DoesNotContain("D01/01/2023", result);
        }

        [Fact]
        public void Convert_NestedSTMTTRN_MatchesInnermostPairs()
        {
            // Nested STMTTRN tags — regex uses lazy match, should grab content between first open/close pair
            var path = WriteTempQfx("<STMTTRN><STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT></STMTTRN></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Should parse at least one transaction
            Assert.Contains("D01/01/2023", result);
        }

        [Fact]
        public void Convert_MissingClosingTag_LazyRegexSpansBoth()
        {
            // First STMTTRN has no </STMTTRN> closing tag.
            // The lazy regex <STMTTRN>([\s\S]*?)</STMTTRN> matches from the first <STMTTRN>
            // to the first </STMTTRN> — which is the second STMTTRN's closing tag.
            // ExtractTagValue uses Regex.Match (first match only), so only the first
            // DTPOSTED/TRNAMT are extracted from the combined block.
            var qfx = "<OFX>" +
                       "<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT>" +  // no </STMTTRN>
                       "<STMTTRN><DTPOSTED>20230102</DTPOSTED><TRNAMT>20.00</TRNAMT><NAME>Good</NAME></STMTTRN>" +
                       "</OFX>";
            var path = WriteTempQfx(qfx);

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Only the first transaction's data is extracted (first-match semantics)
            Assert.Contains("D01/01/2023", result);
            Assert.Contains("T10.00", result);
        }

        [Fact]
        public void Convert_ExtraWhitespaceBetweenTags_ParsedCorrectly()
        {
            var qfx = "<STMTTRN>\r\n  <DTPOSTED>  20230101  </DTPOSTED>\r\n  <TRNAMT>  42.50  </TRNAMT>\r\n  <NAME>  Whitespace Store  </NAME>\r\n</STMTTRN>";
            var path = WriteTempQfx(qfx);

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D01/01/2023", result);
            Assert.Contains("T42.50", result);
            Assert.Contains("PWhitespace Store", result);
        }

        [Fact]
        public void Convert_CompletelyEmptyFile_ReturnsValidQIF()
        {
            var path = WriteTempQfx("");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("!Option:AutoSwitch", result);
            Assert.Contains("!Type:Bank", result);
        }

        [Fact]
        public void Convert_WhitespaceOnlyFile_ReturnsValidQIF()
        {
            var path = WriteTempQfx("   \n\r\n  \t  ");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("!Option:AutoSwitch", result);
            Assert.Contains("!Type:Bank", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Large Files
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_1000Transactions_AllParsed()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                sb.Append($"<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>{i}.00</TRNAMT><NAME>Merchant{i}</NAME></STMTTRN>");
            }
            var path = WriteTempQfx(sb.ToString());

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Verify first, last, and middle merchants are present
            Assert.Contains("PMerchant0", result);
            Assert.Contains("PMerchant499", result);
            Assert.Contains("PMerchant999", result);
        }

        [Fact]
        public void Convert_LargeTransactionCount_ProgressReportsFinalCount()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 500; i++)
            {
                sb.Append($"<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>1.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            }
            var path = WriteTempQfx(sb.ToString());
            var progress = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", null!, progress);

            Assert.Contains(progress.Values, m => m.Contains("Found 500 records"));
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Unicode / Special Characters
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_UnicodePayee_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Caf\u00e9 M\u00fcnchen</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PCafé München", result);        }

        [Fact]
        public void Convert_AmpersandInPayee_PreservedInOutput()        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>AT&amp;T</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // The regex captures [^<]+ which includes the literal '&amp;' text from the file
            Assert.Contains("PAT&", result);
        }

        [Fact]
        public void Convert_WhitespaceInPayeeName_Trimmed()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>\tStore Name\n</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // ExtractTagValue trims the value
            Assert.Contains("PStore Name", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: File I/O
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_FileNotFound_ThrowsIOException()
        {
            // Use a path where the directory exists but the file does not
            var badPath = Path.Combine(_tempDir, "nonexistent.qfx");
            Assert.ThrowsAny<IOException>(() =>
                QfxToQifConverter.Convert(badPath, "Account", null!, null!));
        }

        [Fact]
        public void Convert_BinaryContent_NoTransactionsParsed()
        {
            // Write binary garbage that won't match XML tags
            var path = Path.Combine(_tempDir, "binary.qfx");
            File.WriteAllBytes(path, new byte[] { 0x00, 0x01, 0xFF, 0xFE, 0x89, 0xAB, 0xCD, 0xEF });

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // No transactions, but header is still valid
            Assert.Contains("!Option:AutoSwitch", result);
            Assert.Contains("!Type:Bank", result);
        }

        [Fact]
        public void Convert_VeryLongFieldName_PreservedInOutput()
        {
            var longName = new string('A', 500);
            var path = WriteTempQfx($"<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>{longName}</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains($"P{longName}", result);
        }

        [Fact]
        public void Convert_LeetDate_Unparsed()
        {
            // Non-numeric date like "abcdefg" should not parse as a valid date
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>abcdefg</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Date should not appear in output (length < 8, so skipped)
            var lines = result.Split('\n');
            bool pastHeader = false;
            foreach (var line in lines)
            {
                if (line.Contains("!Type:Bank"))
                    pastHeader = true;
                if (pastHeader && line.StartsWith("D"))
                    Assert.Fail("Non-numeric date should not be parsed");
            }
        }

        [Fact]
        public void Convert_ZeroAmount_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>0.00</TRNAMT><NAME>Zero</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T0.00", result);
            Assert.Contains("PZero", result);
        }

        [Fact]
        public void Convert_VeryLongAmount_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>9999999999.99</TRNAMT><NAME>Big</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T9999999999.99", result);
        }

        // ═══════════════════════════════════════════
        //  Coverage: Both progress AND debugging callbacks
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_BothCallbacks_ReadingFileReported()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var debugging = new SyncProgress<string>();
            var progress = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", debugging, progress);

            Assert.Contains(debugging.Values, m => m.Contains("Reading file"));
            Assert.Contains(progress.Values, m => m.Contains("Reading file"));
        }

        [Fact]
        public void Convert_BothCallbacks_FoundRecordsReported()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var debugging = new SyncProgress<string>();
            var progress = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", debugging, progress);

            Assert.Contains(debugging.Values, m => m.Contains("Found 1 records"));
            Assert.Contains(progress.Values, m => m.Contains("Found 1 records"));
        }

        [Fact]
        public void Convert_BothCallbacks_ProcessingTransactionsReported()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var debugging = new SyncProgress<string>();
            var progress = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", debugging, progress);

            Assert.Contains(debugging.Values, m => m.Contains("Processing transactions..."));
            Assert.Contains(progress.Values, m => m.Contains("Processing transaction 1 of 1"));
        }

        [Fact]
        public void Convert_BothCallbacks_CompletionReported()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var debugging = new SyncProgress<string>();
            var progress = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", debugging, progress);

            Assert.Contains(debugging.Values, m => m.Contains("Conversion completed successfully."));
        }

        [Fact]
        public void Convert_BothCallbacks_NullCallbacks_DoesNotThrow()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");

            var exception = Record.Exception(() => QfxToQifConverter.Convert(path, "Account", null!, null!));

            Assert.Null(exception);
        }

        [Fact]
        public void Convert_DebuggingOnly_NoProgressCallback()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var debugging = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", debugging, null!);

            Assert.Contains(debugging.Values, m => m.Contains("Reading file"));
            Assert.Contains(debugging.Values, m => m.Contains("Conversion completed successfully."));
        }

        [Fact]
        public void Convert_ProgressOnly_NoDebuggingCallback()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var progress = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", null!, progress);

            Assert.Contains(progress.Values, m => m.Contains("Reading file"));
            Assert.Contains(progress.Values, m => m.Contains("Processing transaction 1 of 1"));
        }

        [Fact]
        public void Convert_NoTransactions_DebuggingReportsProcessingHeader()
        {
            var path = WriteTempQfx("<OFX>No transactions here</OFX>");
            var debugging = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", debugging, null!);

            Assert.Contains(debugging.Values, m => m.Contains("Processing transactions..."));
            Assert.Contains(debugging.Values, m => m.Contains("Found 0 records"));
        }

        [Fact]
        public void Convert_MultipleTransactions_DebuggingReportsAllSteps()
        {
            var qfx = "<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>A</NAME></STMTTRN>" +
                       "<STMTTRN><DTPOSTED>20230102</DTPOSTED><TRNAMT>20.00</TRNAMT><NAME>B</NAME></STMTTRN>";
            var path = WriteTempQfx(qfx);
            var debugging = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", debugging, null!);

            Assert.Contains(debugging.Values, m => m.Contains("Found 2 records"));
            Assert.Contains(debugging.Values, m => m.Contains("Processing transactions..."));
            Assert.Contains(debugging.Values, m => m.Contains("Conversion completed successfully."));
        }

        // ═══════════════════════════════════════════
        //  Coverage: Date parsing paths
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_InvalidDateFormat_DateOmitted()
        {
            // Date has 8 chars but is not valid (month 13)
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20231301</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // TryParseExact should fail for month 13, so no D line
            var lines = result.Split('\n');
            bool pastHeader = false;
            foreach (var line in lines)
            {
                if (line.Contains("!Type:Bank"))
                    pastHeader = true;
                if (pastHeader && line.StartsWith("D"))
                    Assert.Fail("Invalid date should not produce D line");
            }
        }

        [Fact]
        public void Convert_InvalidDateFormatDay32_DateOmitted()
        {
            // Date has 8 chars but day is 32
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230132</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            var lines = result.Split('\n');
            bool pastHeader = false;
            foreach (var line in lines)
            {
                if (line.Contains("!Type:Bank"))
                    pastHeader = true;
                if (pastHeader && line.StartsWith("D"))
                    Assert.Fail("Invalid day 32 should not produce D line");
            }
        }

        [Fact]
        public void Convert_DateExactly8Chars_Parsed()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230615</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D06/15/2023", result);
        }

        [Fact]
        public void Convert_DateExactly7Chars_DateOmitted()
        {
            // One char short of 8
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>2023061</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            var lines = result.Split('\n');
            bool pastHeader = false;
            foreach (var line in lines)
            {
                if (line.Contains("!Type:Bank"))
                    pastHeader = true;
                if (pastHeader && line.StartsWith("D"))
                    Assert.Fail("7-char date should not produce D line");
            }
        }

        // ═══════════════════════════════════════════
        //  Coverage: ExtractTagValue paths
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_EmptyTagValue_TagOmitted()
        {
            // Tag exists but has no content between tags
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT></TRNAMT><NAME>X</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Empty TRNAMT should not produce T line
            Assert.DoesNotContain("T\n", result);
            Assert.Contains("D01/01/2023", result);
            Assert.Contains("PX", result);        }

        [Fact]
        public void Convert_AllFieldsPresent_AllLinesGenerated()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>99.99</TRNAMT><NAME>Full Store</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D01/01/2023", result);
            Assert.Contains("T99.99", result);
            Assert.Contains("C*", result);
            Assert.Contains("PFull Store", result);
            Assert.Contains("^", result);
        }

        [Fact]
        public void Convert_MixedFieldPresence_SomeLinesOmitted()
        {
            // Transaction 1: all fields, Transaction 2: only date, Transaction 3: only amount+name
            var qfx = "<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>A</NAME></STMTTRN>" +
                       "<STMTTRN><DTPOSTED>20230102</DTPOSTED></STMTTRN>" +
                       "<STMTTRN><TRNAMT>30.00</TRNAMT><NAME>C</NAME></STMTTRN>";
            var path = WriteTempQfx(qfx);

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Transaction 1: all lines present
            Assert.Contains("D01/01/2023", result);
            Assert.Contains("T10.00", result);
            Assert.Contains("PA", result);
            // Transaction 2: only date (no T, no P)
            Assert.Contains("D01/02/2023", result);
            // Transaction 3: no date, has T and P
            Assert.Contains("T30.00", result);
            Assert.Contains("PC", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Unicode Emoji & CJK Characters
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_EmojiInPayee_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Coffee ☕ Shop</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PCoffee ☕ Shop", result);
        }

        [Fact]
        public void Convert_CJKCharacters_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>東京タワー</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("P東京タワー", result);
        }

        [Fact]
        public void Convert_KoreanCharacters_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>서울역</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("P서울역", result);
        }

        [Fact]
        public void Convert_ArabicCharacters_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>متجر</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("Pمتجر", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Very Large Files (MB range)
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_10000Transactions_AllParsed()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 10000; i++)
            {
                sb.Append($"<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>{i}.00</TRNAMT><NAME>M{i}</NAME></STMTTRN>");
            }
            var path = WriteTempQfx(sb.ToString());

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Verify first, last, and middle merchants
            Assert.Contains("PM0", result);
            Assert.Contains("PM4999", result);
            Assert.Contains("PM9999", result);
        }

        [Fact]
        public void Convert_VeryLongTransactionBlock_ParsedCorrectly()
        {
            // Single transaction with extremely long content between tags
            var longContent = new string('X', 10000);
            var qfx = $"<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>{longContent}</NAME></STMTTRN>";
            var path = WriteTempQfx(qfx);

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains($"P{longContent}", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Special Characters in Amounts
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_AmountWithNoDecimalPoint_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>100</TRNAMT><NAME>Cash</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T100", result);
        }

        [Fact]
        public void Convert_AmountWithLeadingZeros_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>007.50</TRNAMT><NAME>Lucky</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T007.50", result);
        }

        [Fact]
        public void Convert_AmountWithManyDecimalPlaces_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>1.123456789</TRNAMT><NAME>Precision</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T1.123456789", result);
        }

        [Fact]
        public void Convert_VeryLargeAmount_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>999999999999.99</TRNAMT><NAME>Billion</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T999999999999.99", result);
        }

        [Fact]
        public void Convert_NegativeAmountWithDecimals_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>-0.01</TRNAMT><NAME>OneCent</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T-0.01", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Boundary Dates
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_LeapYearDate_ParsedCorrectly()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20240229</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>LeapDay</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D02/29/2024", result);
        }

        [Fact]
        public void Convert_Year2000Date_ParsedCorrectly()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20000101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Y2K</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D01/01/2000", result);
        }

        [Fact]
        public void Convert_DateDec31_ParsedCorrectly()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20231231</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>NewYearsEve</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D12/31/2023", result);
        }

        [Fact]
        public void Convert_DateJan1_ParsedCorrectly()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>NewYear</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D01/01/2023", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: HTML Entities in Content
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_HTMLEntityLessThan_PreservedInOutput()
        {
            // &lt; is captured as literal text by regex [^<]+ (not decoded)
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>A&lt;B</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PA&lt;B", result);
        }

        [Fact]
        public void Convert_HTMLEntityGreaterThan_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>A&gt;B</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PA&gt;B", result);
        }

        [Fact]
        public void Convert_HTMLEntityQuotes_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>A&quot;B</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PA&quot;B", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Special Characters in Payee
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_PayeeWithNewline_TrimsContent()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Line1\nLine2</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Regex [^<]+ captures the newline, but Trim() should clean it
            Assert.Contains("PLine1", result);
        }

        [Fact]
        public void Convert_PayeeWithCarriageReturn_TrimsContent()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Line1\rLine2</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PLine1", result);
        }

        [Fact]
        public void Convert_PayeeWithTab_TrimsContent()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>\tTabbed Name\t</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PTabbed Name", result);
        }

        [Fact]
        public void Convert_PayeeWithNullByte_PreservedInOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Null\0Byte</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // Null byte is part of the captured text
            Assert.Contains("PNull", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Single-Field Transactions
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_TransactionWithOnlyDate_ValidOutput()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D01/01/2023", result);
            Assert.DoesNotContain("T\n", result);
        }

        [Fact]
        public void Convert_TransactionWithOnlyAmount_ValidOutput()
        {
            var path = WriteTempQfx("<STMTTRN><TRNAMT>50.00</TRNAMT></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("T50.00", result);
            Assert.Contains("C*", result);
        }

        [Fact]
        public void Convert_TransactionWithOnlyName_ValidOutput()
        {
            var path = WriteTempQfx("<STMTTRN><NAME>PayeeOnly</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("PPayeeOnly", result);
        }

        // ═══════════════════════════════════════════
        //  Edge Cases: Multiple Transactions with Mixed Issues
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_MultipleTransactions_AllMissingFields_StillParses()
        {
            var qfx = "<STMTTRN><DTPOSTED>20230101</DTPOSTED></STMTTRN>" +
                       "<STMTTRN><TRNAMT>20.00</TRNAMT></STMTTRN>" +
                       "<STMTTRN><NAME>OnlyName</NAME></STMTTRN>";
            var path = WriteTempQfx(qfx);

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            Assert.Contains("D01/01/2023", result);
            Assert.Contains("T20.00", result);
            Assert.Contains("POnlyName", result);
        }

        [Fact]
        public void Convert_TransactionCount_MatchesProgressReport()
        {
            var qfx = "<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>1.00</TRNAMT><NAME>A</NAME></STMTTRN>" +
                       "<STMTTRN><DTPOSTED>20230102</DTPOSTED><TRNAMT>2.00</TRNAMT><NAME>B</NAME></STMTTRN>" +
                       "<STMTTRN><DTPOSTED>20230103</DTPOSTED><TRNAMT>3.00</TRNAMT><NAME>C</NAME></STMTTRN>";
            var path = WriteTempQfx(qfx);
            var progress = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Account", null!, progress);

            Assert.Contains(progress.Values, m => m.Contains("Found 3 records"));
            Assert.Contains(progress.Values, m => m.Contains("Processing transaction 1 of 3"));
            Assert.Contains(progress.Values, m => m.Contains("Processing transaction 2 of 3"));
            Assert.Contains(progress.Values, m => m.Contains("Processing transaction 3 of 3"));
        }

        [Fact]
        public void Convert_QIFOutput_EndsWithCaret()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "Account", null!, null!);

            // QIF format requires ^ as record terminator (with \r\n line endings)
            Assert.EndsWith("^\r\n", result);
        }

        [Fact]
        public void Convert_QIFOutput_ContainsAllRequiredHeaders()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");

            var result = QfxToQifConverter.Convert(path, "TestAccount", null!, null!);

            // Verify all required QIF headers are present
            Assert.Contains("!Option:AutoSwitch", result);
            Assert.Contains("!ACCOUNT", result);
            Assert.Contains("NTestAccount", result);
            Assert.Contains("!Clear:AutoSwitch", result);
            Assert.Contains("!Type:Bank", result);
            Assert.Contains("TBank", result);
        }
    }
}
