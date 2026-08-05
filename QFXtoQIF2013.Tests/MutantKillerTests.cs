using System;
using System.IO;
using System.Linq;
using Xunit;
using QFXtoQIF2013;

namespace QFXtoQIF2013.Tests
{
    /// <summary>
    /// Targeted tests designed to kill specific survived mutants from Stryker.
    /// These verify exact QIF output structure that Contains-based tests miss.
    /// </summary>
    public class MutantKillerTests : IDisposable
    {
        private readonly string _tempDir;

        public MutantKillerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "mutant_killer_" + Guid.NewGuid().ToString("N")[..8]);
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

        // ═══════════════════════════════════════════
        //  Kill Mutants: QIF Header Structure
        //  (Lines 28-36: !ACCOUNT, TBank, ^ in header)
        // ═══════════════════════════════════════════

        [Fact]
        public void QIF_Header_ContainsExactlyTwoAccountLines()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n');
            int accountCount = lines.Count(l => l.Trim() == "!ACCOUNT");
            Assert.Equal(2, accountCount);
        }

        [Fact]
        public void QIF_Header_ContainsExactlyTwoTBankLines()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n');
            int tbankCount = lines.Count(l => l.Trim() == "TBank");
            Assert.Equal(2, tbankCount);
        }

        [Fact]
        public void QIF_Header_ContainsExactlyTwoHeaderCarets()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n');
            // Header has 2 carets, plus 1 for the transaction = 3 total
            int caretCount = lines.Count(l => l.Trim() == "^");
            Assert.Equal(3, caretCount); // 2 header + 1 transaction
        }

        [Fact]
        public void QIF_Header_FirstAccountBlockHasCorrectOrder()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "MyAcct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();

            // Find first !ACCOUNT and verify the block structure
            int firstAccount = Array.IndexOf(lines, "!ACCOUNT");
            Assert.True(firstAccount >= 0);
            Assert.Equal("NMyAcct", lines[firstAccount + 1]);
            Assert.Equal("TBank", lines[firstAccount + 2]);
            Assert.Equal("^", lines[firstAccount + 3]);
        }

        [Fact]
        public void QIF_Header_SecondAccountBlockHasCorrectOrder()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "MyAcct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();

            // Find second !ACCOUNT and verify the block structure
            int firstAccount = Array.IndexOf(lines, "!ACCOUNT");
            int secondAccount = Array.IndexOf(lines, "!ACCOUNT", firstAccount + 1);
            Assert.True(secondAccount >= 0);
            Assert.Equal("NMyAcct", lines[secondAccount + 1]);
            Assert.Equal("TBank", lines[secondAccount + 2]);
            Assert.Equal("^", lines[secondAccount + 3]);
        }

        [Fact]
        public void QIF_Header_StartsWithOptionAutoSwitch()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
            Assert.Equal("!Option:AutoSwitch", lines[0]);
        }

        [Fact]
        public void QIF_Header_HasClearAutoSwitchBetweenBlocks()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
            // After first ^, should have !Clear:AutoSwitch
            int firstCaret = Array.IndexOf(lines, "^");
            Assert.Equal("!Clear:AutoSwitch", lines[firstCaret + 1]);
        }

        [Fact]
        public void QIF_Header_EndsWithTypeBank()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
            // Find !Type:Bank - should be after second ^
            int secondCaret = Array.IndexOf(lines, "^");
            secondCaret = Array.IndexOf(lines, "^", secondCaret + 1);
            Assert.Equal("!Type:Bank", lines[secondCaret + 1]);
        }

        [Fact]
        public void QIF_Header_AccountNameFollowsAccountTag()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "TestAccount", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
            // Every !ACCOUNT should be followed by N<accountName>
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "!ACCOUNT")
                {
                    Assert.Equal("NTestAccount", lines[i + 1]);
                }
            }
        }

        // ═══════════════════════════════════════════
        //  Kill Mutant: Transaction Terminator (Line 78)
        // ═══════════════════════════════════════════

        [Fact]
        public void QIF_Transactions_EachEndsWithCaret()
        {
            var qfx = "<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>A</NAME></STMTTRN>" +
                      "<STMTTRN><DTPOSTED>20230102</DTPOSTED><TRNAMT>20.00</TRNAMT><NAME>B</NAME></STMTTRN>" +
                      "<STMTTRN><DTPOSTED>20230103</DTPOSTED><TRNAMT>30.00</TRNAMT><NAME>C</NAME></STMTTRN>";
            var path = WriteTempQfx(qfx);
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();

            // 2 header carets + 3 transaction carets = 5 total
            int caretCount = lines.Count(l => l == "^");
            Assert.Equal(5, caretCount);
        }

        [Fact]
        public void QIF_NoTransactions_NoTransactionCarets()
        {
            var path = WriteTempQfx("<OFX>No transactions</OFX>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();

            // Only 2 header carets
            int caretCount = lines.Count(l => l == "^");
            Assert.Equal(2, caretCount);
        }

        [Fact]
        public void QIF_Transaction_PayeeFollowsCaret()
        {
            var qfx = "<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Store1</NAME></STMTTRN>" +
                      "<STMTTRN><DTPOSTED>20230102</DTPOSTED><TRNAMT>20.00</TRNAMT><NAME>Store2</NAME></STMTTRN>";
            var path = WriteTempQfx(qfx);
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();

            // After !Type:Bank, we should see transaction data
            int typeBankIndex = Array.IndexOf(lines, "!Type:Bank");
            // First transaction: D..., T..., C*, P..., ^
            Assert.Equal("D01/01/2023", lines[typeBankIndex + 1]);
            Assert.Equal("T10.00", lines[typeBankIndex + 2]);
            Assert.Equal("C*", lines[typeBankIndex + 3]);
            Assert.Equal("PStore1", lines[typeBankIndex + 4]);
            Assert.Equal("^", lines[typeBankIndex + 5]);
        }

        // ═══════════════════════════════════════════
        //  Kill Mutant: ExtractTagValue (Line 89)
        // ═══════════════════════════════════════════

        [Fact]
        public void ExtractTagValue_TagNotFound_ReturnsEmpty()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            // NAME tag is missing, so no P line should appear in transaction section
            var lines = result.Split('\n');
            bool pastHeader = false;
            foreach (var line in lines)
            {
                if (line.Trim() == "!Type:Bank") pastHeader = true;
                if (pastHeader && line.Trim().StartsWith("P"))
                    Assert.Fail("Should not have payee line when NAME tag is missing");
            }
        }

        [Fact]
        public void ExtractTagValue_EmptyTagContent_ReturnsEmpty()
        {
            // Tag exists but has no content between > and <
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT></TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            // Empty TRNAMT should not produce T line
            var lines = result.Split('\n');
            bool pastHeader = false;
            foreach (var line in lines)
            {
                if (line.Trim() == "!Type:Bank") pastHeader = true;
                if (pastHeader && line.Trim() == "T")
                    Assert.Fail("Should not have T line when TRNAMT is empty");
            }
        }

        [Fact]
        public void ExtractTagValue_MultipleTags_ExtractsFirst()
        {
            // Two NAME tags - should extract the first one
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>First</NAME><NAME>Second</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            Assert.Contains("PFirst", result);
            Assert.DoesNotContain("PSecond", result);
        }

        [Fact]
        public void ExtractTagValue_TagWithSpaces_TrimsContent()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>  Padded  </NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            Assert.Contains("PPadded", result);
            Assert.DoesNotContain("P  Padded", result);
        }

        [Fact]
        public void ExtractTagValue_CaseInsensitive_ExtractsValue()
        {
            var path = WriteTempQfx("<STMTTRN><dtposted>20230101</dtposted><trnamt>10.00</trnamt><name>Lower</name></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            Assert.Contains("D01/01/2023", result);
            Assert.Contains("T10.00", result);
            Assert.Contains("PLower", result);
        }

        // ═══════════════════════════════════════════
        //  Kill Mutants: Exact QIF Line Content
        // ═══════════════════════════════════════════

        [Fact]
        public void QIF_Header_AllLinesPresent_InOrder()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "MyAcct", null!, null!);
            var expected = new[]
            {
                "!Option:AutoSwitch",
                "!ACCOUNT",
                "NMyAcct",
                "TBank",
                "^",
                "!Clear:AutoSwitch",
                "!ACCOUNT",
                "NMyAcct",
                "TBank",
                "^",
                "!Type:Bank",
                "D01/01/2023",
                "T10.00",
                "C*",
                "PX",
                "^"
            };
            var actual = result.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void QIF_Header_EmptyAccountName_ThrowsArgumentException()
        {
            // Empty account name should be rejected by input validation
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            Assert.Throws<ArgumentException>(() => QfxToQifConverter.Convert(path, "", null!, null!));
        }

        // ═══════════════════════════════════════════
        //  Input Validation Tests (Rule 5 & Rule 7)
        // ═══════════════════════════════════════════

        [Fact]
        public void Convert_NullFilePath_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => QfxToQifConverter.Convert(null!, "Acct", null!, null!));
            Assert.Contains("File path", ex.Message);
            Assert.Equal("qfxFilePath", ex.ParamName);
        }

        [Fact]
        public void Convert_EmptyFilePath_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => QfxToQifConverter.Convert("", "Acct", null!, null!));
            Assert.Contains("File path", ex.Message);
            Assert.Equal("qfxFilePath", ex.ParamName);
        }

        [Fact]
        public void Convert_NullAccountName_ThrowsArgumentException()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var ex = Assert.Throws<ArgumentException>(() => QfxToQifConverter.Convert(path, null!, null!, null!));
            Assert.Contains("Account name", ex.Message);
            Assert.Equal("accountName", ex.ParamName);
        }

        [Fact]
        public void Convert_NonexistentFile_ThrowsFileNotFoundException()
        {
            var badPath = Path.Combine(_tempDir, "nonexistent.qfx");
            var ex = Assert.Throws<FileNotFoundException>(() => QfxToQifConverter.Convert(badPath, "Acct", null!, null!));
            Assert.Contains("QFX file not found", ex.Message);
            Assert.Equal(badPath, ex.FileName);
        }

        [Fact]
        public void Convert_EmptyFile_ReturnsValidQifHeader()
        {
            // Empty file should still return valid QIF header
            var path = WriteTempQfx("");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            Assert.Contains("!Option:AutoSwitch", result);
            Assert.Contains("!Type:Bank", result);
        }

        [Fact]
        public void Convert_EmptyFile_DebuggingReportsWarning()
        {
            // Verify empty file warning is reported
            var path = WriteTempQfx("");
            var debugMessages = new List<string>();
            var debugging = new SyncProgress<string>();

            QfxToQifConverter.Convert(path, "Acct", debugging, null!);

            Assert.Contains(debugging.Values, m => m.Contains("Warning: QFX file is empty"));
        }

        [Fact]
        public void QIF_Header_SpecialAccountName_PreservedExactly()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>X</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct & Savings #2", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();
            // Every N line should have exact account name
            foreach (var line in lines)
            {
                if (line.StartsWith("N"))
                {
                    Assert.Equal("NAcct & Savings #2", line);
                }
            }
        }

        // ═══════════════════════════════════════════
        //  Kill Mutants: Transaction Field Ordering
        // ═══════════════════════════════════════════

        [Fact]
        public void QIF_Transaction_FieldOrder_IsDateThenAmountThenCheckThenPayee()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230615</DTPOSTED><TRNAMT>42.50</TRNAMT><NAME>Shop</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();

            int typeBank = Array.IndexOf(lines, "!Type:Bank");
            Assert.Equal("D06/15/2023", lines[typeBank + 1]);
            Assert.Equal("T42.50", lines[typeBank + 2]);
            Assert.Equal("C*", lines[typeBank + 3]);
            Assert.Equal("PShop", lines[typeBank + 4]);
            Assert.Equal("^", lines[typeBank + 5]);
        }

        [Fact]
        public void QIF_Transaction_MissingAmount_SkipsAmountAndCheck()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><NAME>NoAmount</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();

            int typeBank = Array.IndexOf(lines, "!Type:Bank");
            Assert.Equal("D01/01/2023", lines[typeBank + 1]);
            // Next should be payee, not amount
            Assert.Equal("PNoAmount", lines[typeBank + 2]);
            Assert.Equal("^", lines[typeBank + 3]);
        }

        [Fact]
        public void QIF_Transaction_MissingDate_SkipsDateLine()
        {
            var path = WriteTempQfx("<STMTTRN><TRNAMT>10.00</TRNAMT><NAME>NoDate</NAME></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();

            int typeBank = Array.IndexOf(lines, "!Type:Bank");
            // First should be amount, not date
            Assert.Equal("T10.00", lines[typeBank + 1]);
            Assert.Equal("C*", lines[typeBank + 2]);
            Assert.Equal("PNoDate", lines[typeBank + 3]);
            Assert.Equal("^", lines[typeBank + 4]);
        }

        [Fact]
        public void QIF_Transaction_MissingPayee_SkipsPayeeLine()
        {
            var path = WriteTempQfx("<STMTTRN><DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT></STMTTRN>");
            var result = QfxToQifConverter.Convert(path, "Acct", null!, null!);
            var lines = result.Split('\n').Select(l => l.Trim()).ToArray();

            int typeBank = Array.IndexOf(lines, "!Type:Bank");
            Assert.Equal("D01/01/2023", lines[typeBank + 1]);
            Assert.Equal("T10.00", lines[typeBank + 2]);
            Assert.Equal("C*", lines[typeBank + 3]);
            Assert.Equal("^", lines[typeBank + 4]);
        }

        // ═══════════════════════════════════════════
        //  Kill Mutant: ExtractTagValue return value
        //  (Line 89: match.Success ? ... : string.Empty)
        // ═══════════════════════════════════════════

        [Fact]
        public void ExtractTagValue_Directly_TagMissing_ReturnsEmptyString()
        {
            // Directly test ExtractTagValue - when tag is missing, must return string.Empty
            string xml = "<DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT>";
            string result = QfxToQifConverter.ExtractTagValue(xml, "NAME");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ExtractTagValue_Directly_TagPresent_ReturnsValue()
        {
            string xml = "<DTPOSTED>20230101</DTPOSTED><TRNAMT>10.00</TRNAMT><NAME>Store</NAME>";
            string result = QfxToQifConverter.ExtractTagValue(xml, "NAME");
            Assert.Equal("Store", result);
        }

        [Fact]
        public void ExtractTagValue_Directly_TagEmpty_ReturnsEmptyString()
        {
            string xml = "<DTPOSTED>20230101</DTPOSTED><TRNAMT></TRNAMT>";
            string result = QfxToQifConverter.ExtractTagValue(xml, "TRNAMT");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ExtractTagValue_Directly_TagWithSpaces_ReturnsTrimmed()
        {
            string xml = "<NAME>  Hello World  </NAME>";
            string result = QfxToQifConverter.ExtractTagValue(xml, "NAME");
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void ExtractTagValue_Directly_CaseInsensitive()
        {
            string xml = "<name>lowercase</name>";
            string result = QfxToQifConverter.ExtractTagValue(xml, "NAME");
            Assert.Equal("lowercase", result);
        }

        [Fact]
        public void ExtractTagValue_Directly_EmptyXml_ReturnsEmptyString()
        {
            string result = QfxToQifConverter.ExtractTagValue("", "NAME");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ExtractTagValue_Directly_CompletelyUnrelatedTag_ReturnsEmpty()
        {
            // No matching tag at all - should return empty
            string xml = "<DTPOSTED>20230101</DTPOSTED>";
            string result = QfxToQifConverter.ExtractTagValue(xml, "NAME");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ExtractTagValue_Directly_TagFollowedByAnotherTag_StopsAtNextTag()
        {
            // Regex stops at the next '<'
            string xml = "<NAME>Store</NAME><TRNAMT>10.00</TRNAMT>";
            string result = QfxToQifConverter.ExtractTagValue(xml, "NAME");
            Assert.Equal("Store", result);
            // Should NOT capture beyond </NAME>
            Assert.DoesNotContain("TRNAMT", result);
        }
    }
}
