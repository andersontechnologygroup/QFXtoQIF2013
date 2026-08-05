using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QFXtoQIF2013
{
    /// <summary>
    /// Main form for the QFX to QIF converter application.
    /// </summary>
    public partial class Form1 : Form
    {
        private const string QfxFilter = "QFX files (*.qfx)|*.qfx";
        private const string QifFilter = "QIF files (*.qif)|*.qif";
        private const string SelectQfxTitle = "Select a QFX file";
        private const string SaveQifTitle = "Save QIF file";
        private const string ErrorTitle = "Error";
        private const string SuccessTitle = "Success";
        private const string MsgSelectFile = "Please select a QFX file to convert.";
        private const string MsgEnterAccount = "Please enter an account name.";
        private const string MsgFileNotFound = "QFX input file not found.";
        private const string MsgConversionSuccessful = "Conversion successful!";
        private const string StatusConverting = "Converting QFX to QIF...";
        private const string StatusComplete = "Conversion complete.";
        private const string StatusFailed = "Conversion failed.";
        private const string StatusCancelled = "Conversion cancelled.";
        private const string QifExtension = ".qif";

        private CancellationTokenSource? _cancellationTokenSource;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = QfxFilter;
            openFileDialog.Title = SelectQfxTitle;
            openFileDialog.FileName = txtQFXFile.Text;
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtQFXFile.Text = openFileDialog.FileName;
            }
        }

        private async void btnConvert_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out string qfxFile, out string accountName))
                return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            SetUIBusy(true);
            var debugging = CreateDebuggingProgress();
            var progress = CreateStatusProgress();

            try
            {
                string qifResult = await Task.Run(() =>
                    QfxToQifConverter.Convert(qfxFile, accountName, debugging, progress),
                    cancellationToken);

                if (!string.IsNullOrEmpty(qifResult))
                {
                    await SaveQifFileAsync(qifResult, qfxFile, cancellationToken);
                }
                lblDebugging.Text = StatusComplete;
            }
            catch (OperationCanceledException)
            {
                lblDebugging.Text = StatusCancelled;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during conversion: {ex.Message}",
                    ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblDebugging.Text = StatusFailed;
            }
            finally
            {
                SetUIBusy(false);
            }
        }

        private bool ValidateInputs(out string qfxFile, out string accountName)
        {
            qfxFile = txtQFXFile.Text;
            accountName = SanitizeAccountName(txtAccountName.Text);
            if (string.IsNullOrWhiteSpace(qfxFile))
            {
                MessageBox.Show(MsgSelectFile, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(accountName))
            {
                MessageBox.Show(MsgEnterAccount, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!File.Exists(qfxFile))
            {
                MessageBox.Show(MsgFileNotFound, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private static string SanitizeAccountName(string name)
        {
            return name.Trim().Replace("\n", "").Replace("\r", "").Replace("^", "");
        }

        private void SetUIBusy(bool busy)
        {
            btnConvert.Enabled = !busy;
            btnBrowse.Enabled = !busy;
            lblDebugging.Text = busy ? StatusConverting : lblDebugging.Text;
            txtDebugging.Clear();
        }

        private IProgress<string> CreateDebuggingProgress()
        {
            return new Progress<string>(status => txtDebugging.AppendText(status + Environment.NewLine));
        }

        private IProgress<string> CreateStatusProgress()
        {
            return new Progress<string>(status => lblDebugging.Text = status);
        }

        private async Task SaveQifFileAsync(string qifContent, string qfxFilePath, CancellationToken cancellationToken)
        {
            saveFileDialog.Filter = QifFilter;
            saveFileDialog.Title = SaveQifTitle;
            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(qfxFilePath) + QifExtension;
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Run(() => File.WriteAllText(saveFileDialog.FileName, qifContent, Encoding.UTF8), cancellationToken);
                MessageBox.Show(MsgConversionSuccessful, SuccessTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
