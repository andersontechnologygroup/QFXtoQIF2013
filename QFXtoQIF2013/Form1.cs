using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QFXtoQIF2013
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "QFX files (*.qfx)|*.qfx";
            openFileDialog.Title = "Select a QFX file";
            openFileDialog.FileName = txtQFXFile.Text;
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtQFXFile.Text = openFileDialog.FileName;
            }
        }

        private async void btnConvert_Click(object sender, EventArgs e)
        {
            string qfxFile = txtQFXFile.Text;
            string accountName = txtAccountName.Text;

            if (string.IsNullOrWhiteSpace(qfxFile))
            {
                MessageBox.Show("Please select a QFX file to convert.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(accountName))
            {
                MessageBox.Show("Please enter an account name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(qfxFile))
            {
                MessageBox.Show("QFX input file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Disable controls to prevent duplicate operations during async run
            btnConvert.Enabled = false;
            btnBrowse.Enabled = false;
            lblDebugging.Text = "Converting QFX to QIF...";
            txtDebugging.Clear();

            // Progress reporting handler
            var debugging = new Progress<string>(status =>
            {
                txtDebugging.AppendText(status + Environment.NewLine);
            });

            var progress = new Progress<string>(status =>
            {
                lblDebugging.Text = status;
            });

            try
            {
                // Run CPU/IO bound task on thread pool
                string qifResult = await Task.Run(() => QfxToQifConverter.Convert(qfxFile, accountName, debugging, progress));

                if (!string.IsNullOrEmpty(qifResult))
                {
                    saveFileDialog.Filter = "QIF files (*.qif)|*.qif";
                    saveFileDialog.Title = "Save QIF file";
                    saveFileDialog.FileName = Path.GetFileNameWithoutExtension(qfxFile) + ".qif";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        await Task.Run(() => File.WriteAllText(saveFileDialog.FileName, qifResult, Encoding.UTF8));
                        MessageBox.Show("Conversion successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                lblDebugging.Text = "Conversion complete.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during conversion: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblDebugging.Text = "Conversion failed.";
            }
            finally
            {
                btnConvert.Enabled = true;
                btnBrowse.Enabled = true;
            }
        }
    }

    /// <summary>
    /// Decoupled, reusable conversion engine
    /// </summary>
    public static class QfxToQifConverter
    {
        public static string Convert(string qfxFilePath, string accountName, IProgress<string> debugging, IProgress<string> progress)
        {
            progress?.Report("Reading file...");
            debugging?.Report("Reading file...");
            string fileContent = File.ReadAllText(qfxFilePath, Encoding.UTF8);

            // Extract all <STMTTRN>...</STMTTRN> blocks safely using Regex
            var transactionMatches = Regex.Matches(fileContent, @"<STMTTRN>([\s\S]*?)</STMTTRN>", RegexOptions.IgnoreCase);
            progress?.Report($"Found {transactionMatches.Count + 1} records in the QFX file.");
            debugging?.Report($"Found {transactionMatches.Count + 1} records in the QFX file.");

            StringBuilder qif = new StringBuilder();

            // Build QIF Header
            qif.AppendLine("!Option:AutoSwitch");
            qif.AppendLine("!ACCOUNT");
            qif.AppendLine("N" + accountName);
            qif.AppendLine("TBank");
            qif.AppendLine("^");
            qif.AppendLine("!Clear:AutoSwitch");
            qif.AppendLine("!ACCOUNT");
            qif.AppendLine("N" + accountName);
            qif.AppendLine("TBank");
            qif.AppendLine("^");
            qif.AppendLine("!Type:Bank");

            debugging?.Report("Processing transactions...");

            for(int index = 0; index < transactionMatches.Count; index++)
            {
                var match = transactionMatches[index];

                Thread.Sleep(1);
                progress?.Report($"Processing transaction {index + 1} of {transactionMatches.Count + 1}...");

                string transBody = match.Groups[1].Value;

                // Extract fields within transaction safely using Regex
                string dateVal = ExtractTagValue(transBody, "DTPOSTED");
                string amountVal = ExtractTagValue(transBody, "TRNAMT");
                string nameVal = ExtractTagValue(transBody, "NAME");

                // Format & append date
                if (!string.IsNullOrEmpty(dateVal) && dateVal.Length >= 8)
                {
                    string dateStr = dateVal.Substring(0, 8);
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    {
                        qif.AppendLine("D" + parsedDate.ToString("MM/dd/yyyy"));
                    }
                }

                // Format & append amount
                if (!string.IsNullOrEmpty(amountVal))
                {
                    qif.AppendLine("T" + amountVal);
                    qif.AppendLine("C*");
                }

                // Format & append payee
                if (!string.IsNullOrEmpty(nameVal))
                {
                    qif.AppendLine("P" + nameVal);
                }

                // End of transaction entry in QIF
                qif.AppendLine("^");
            }

            debugging?.Report("Conversion completed successfully.");
            return qif.ToString();
        }

        private static string ExtractTagValue(string xml, string tagName)
        {
            // Matches the tag and captures all characters up to the next '<' (the next tag)
            var match = Regex.Match(xml, $@"<{tagName}>([^<]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }
    }
}