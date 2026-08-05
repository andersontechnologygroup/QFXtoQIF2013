using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace QFXtoQIF2013
{
    /// <summary>
    /// Decoupled, reusable conversion engine for transforming QFX files to QIF format.
    /// </summary>
    public static class QfxToQifConverter
    {
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

        /// <summary>
        /// Converts a QFX file to QIF format with account header information.
        /// </summary>
        /// <param name="qfxFilePath">Path to the input QFX file. Must exist and be readable.</param>
        /// <param name="accountName">Account name for the QIF header. Used to match Quicken 2013 import.</param>
        /// <param name="debugging">Progress callback for debug messages. Can be null.</param>
        /// <param name="progress">Progress callback for status updates. Can be null.</param>
        /// <returns>QIF formatted string ready for import, or empty string if conversion fails.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="qfxFilePath"/> or <paramref name="accountName"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file specified by <paramref name="qfxFilePath"/> does not exist.</exception>
        public static string Convert(string qfxFilePath, string accountName, IProgress<string> debugging, IProgress<string> progress)
        {
            // Assertion: Validate input parameters
            if (string.IsNullOrEmpty(qfxFilePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(qfxFilePath));
            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentException("Account name cannot be null or empty.", nameof(accountName));
            if (!File.Exists(qfxFilePath))
                throw new FileNotFoundException("QFX file not found.", qfxFilePath);

            progress?.Report("Reading file...");
            debugging?.Report("Reading file...");
            string fileContent = File.ReadAllText(qfxFilePath, Encoding.UTF8);

            // Assertion: File content was read successfully
            if (string.IsNullOrEmpty(fileContent))
            {
                debugging?.Report("Warning: QFX file is empty.");
                return BuildQifHeader(accountName);
            }

            // Extract all <STMTTRN>...</STMTTRN> blocks safely using Regex
            var transactionMatches = Regex.Matches(fileContent, TransactionPattern, RegexOptions.IgnoreCase);
            progress?.Report($"Found {transactionMatches.Count} records in the QFX file.");
            debugging?.Report($"Found {transactionMatches.Count} records in the QFX file.");

            // Build QIF output
            var qif = new StringBuilder(capacity: 1024);
            AppendQifHeader(qif, accountName);

            debugging?.Report("Processing transactions...");

            // Assertion: Loop is bounded by transactionMatches.Count (statically provable)
            for (int index = 0; index < transactionMatches.Count; index++)
            {
                var match = transactionMatches[index];

                progress?.Report($"Processing transaction {index + 1} of {transactionMatches.Count}...");

                string transBody = match.Groups[1].Value;

                // Extract fields within transaction safely using Regex
                string dateVal = ExtractTagValue(transBody, TagDatePosted);
                string amountVal = ExtractTagValue(transBody, TagTransactionAmount);
                string nameVal = ExtractTagValue(transBody, TagName);

                // Format & append date
                if (!string.IsNullOrEmpty(dateVal) && dateVal.Length >= MinDateLength)
                {
                    string dateStr = dateVal.Substring(0, MinDateLength);
                    if (DateTime.TryParseExact(dateStr, InputDateFormat, null,
                        System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    {
                        qif.AppendLine($"{DatePrefix}{parsedDate.ToString(OutputDateFormat)}");
                    }
                }

                // Format & append amount
                if (!string.IsNullOrEmpty(amountVal))
                {
                    qif.AppendLine($"{AmountPrefix}{amountVal}");
                    qif.AppendLine($"{ClearedPrefix}*");
                }

                // Format & append payee
                if (!string.IsNullOrEmpty(nameVal))
                {
                    qif.AppendLine($"{PayeePrefix}{nameVal}");
                }

                // End of transaction entry in QIF
                qif.AppendLine(RecordTerminator);
            }

            debugging?.Report("Conversion completed successfully.");
            return qif.ToString();
        }

        /// <summary>
        /// Builds the QIF header section with account information.
        /// </summary>
        /// <param name="accountName">The account name to include in the header.</param>
        /// <returns>QIF header string.</returns>
        private static string BuildQifHeader(string accountName)
        {
            var header = new StringBuilder();
            AppendQifHeader(header, accountName);
            return header.ToString();
        }

        /// <summary>
        /// Appends the QIF header section to the provided StringBuilder.
        /// </summary>
        /// <param name="qif">StringBuilder to append to.</param>
        /// <param name="accountName">The account name to include in the header.</param>
        private static void AppendQifHeader(StringBuilder qif, string accountName)
        {
            // First account block
            qif.AppendLine(OptionAutoSwitch);
            qif.AppendLine(AccountTag);
            qif.AppendLine($"N{accountName}");
            qif.AppendLine(BankType);
            qif.AppendLine(RecordTerminator);

            // Second account block (required for Quicken 2013 import)
            qif.AppendLine(ClearAutoSwitch);
            qif.AppendLine(AccountTag);
            qif.AppendLine($"N{accountName}");
            qif.AppendLine(BankType);
            qif.AppendLine(RecordTerminator);

            // Transaction type declaration
            qif.AppendLine(TypeBank);
        }

        /// <summary>
        /// Extracts the value of an XML-like tag from a transaction body.
        /// Matches the tag and captures all characters up to the next '&lt;' (the next tag).
        /// </summary>
        /// <param name="xml">The XML-like string to search.</param>
        /// <param name="tagName">The tag name to extract (without angle brackets).</param>
        /// <returns>The trimmed tag value, or <see cref="string.Empty"/> if the tag is not found or empty.</returns>
        internal static string ExtractTagValue(string xml, string tagName)
        {
            // Assertion: Validate input parameters
            if (string.IsNullOrEmpty(xml))
                return string.Empty;
            if (string.IsNullOrEmpty(tagName))
                return string.Empty;

            var pattern = string.Format(TagValuePattern, tagName);
            var match = Regex.Match(xml, pattern, RegexOptions.IgnoreCase);

            // Assertion: Check if match was successful
            if (!match.Success)
                return string.Empty;

            return match.Groups[1].Value.Trim();
        }
    }
}
