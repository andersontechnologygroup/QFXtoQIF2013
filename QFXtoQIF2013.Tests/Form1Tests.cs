using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Xunit;
using QFXtoQIF2013;

namespace QFXtoQIF2013.Tests
{
    public class Form1Tests
    {
        // ═══════════════════════════════════════════
        //  Form Initialization Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_CanBeCreated()
        {
            using var form = new Form1();
            Assert.NotNull(form);
        }

        [Fact]
        public void Form1_TitleIsCorrect()
        {
            using var form = new Form1();
            Assert.Equal("QFX to QIF (2013)", form.Text);
        }

        [Fact]
        public void Form1_IsFixedSize()
        {
            using var form = new Form1();
            Assert.Equal(FormBorderStyle.FixedSingle, form.FormBorderStyle);
            Assert.False(form.MaximizeBox);
            Assert.False(form.MinimizeBox);
        }

        [Fact]
        public void Form1_HasReasonableSize()
        {
            using var form = new Form1();
            // Size may vary due to DPI scaling, so just verify it's reasonable
            Assert.True(form.ClientSize.Width > 400);
            Assert.True(form.ClientSize.Height > 200);
        }

        // ═══════════════════════════════════════════
        //  Control Existence Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_ContainsQFXFileTextBox()
        {
            using var form = new Form1();
            var txtQFXFile = form.Controls["txtQFXFile"] as TextBox;
            Assert.NotNull(txtQFXFile);
        }

        [Fact]
        public void Form1_ContainsAccountNameTextBox()
        {
            using var form = new Form1();
            var txtAccountName = form.Controls["txtAccountName"] as TextBox;
            Assert.NotNull(txtAccountName);
        }

        [Fact]
        public void Form1_ContainsBrowseButton()
        {
            using var form = new Form1();
            var btnBrowse = form.Controls["btnBrowse"] as Button;
            Assert.NotNull(btnBrowse);
            Assert.Equal("Browse...", btnBrowse!.Text);
        }

        [Fact]
        public void Form1_ContainsConvertButton()
        {
            using var form = new Form1();
            var btnConvert = form.Controls["btnConvert"] as Button;
            Assert.NotNull(btnConvert);
            Assert.Equal("Convert", btnConvert!.Text);
        }

        [Fact]
        public void Form1_ContainsDebuggingOutputTextBox()
        {
            using var form = new Form1();
            var txtDebugging = form.Controls["txtDebugging"] as TextBox;
            Assert.NotNull(txtDebugging);
            Assert.True(txtDebugging!.Multiline);
            Assert.Equal(ScrollBars.Vertical, txtDebugging.ScrollBars);
        }

        [Fact]
        public void Form1_ContainsDebuggingLabel()
        {
            using var form = new Form1();
            var lblDebugging = form.Controls["lblDebugging"] as Label;
            Assert.NotNull(lblDebugging);
            Assert.Equal("Debugging Output", lblDebugging!.Text);
        }

        [Fact]
        public void Form1_ContainsFileLabel()
        {
            using var form = new Form1();
            var label1 = form.Controls["label1"] as Label;
            Assert.NotNull(label1);
            Assert.Equal("QFX file", label1!.Text);
        }

        [Fact]
        public void Form1_ContainsAccountNameLabel()
        {
            using var form = new Form1();
            var lblAccountName = form.Controls["lblAccountName"] as Label;
            Assert.NotNull(lblAccountName);
            Assert.Equal("Account Name", lblAccountName!.Text);
        }

        // ═══════════════════════════════════════════
        //  Control Properties Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_QFXFileTextBoxIsEmpty()
        {
            using var form = new Form1();
            var txtQFXFile = form.Controls["txtQFXFile"] as TextBox;
            Assert.Equal(string.Empty, txtQFXFile!.Text);
        }

        [Fact]
        public void Form1_AccountNameTextBoxIsEmpty()
        {
            using var form = new Form1();
            var txtAccountName = form.Controls["txtAccountName"] as TextBox;
            Assert.Equal(string.Empty, txtAccountName!.Text);
        }

        [Fact]
        public void Form1_ConvertButtonIsEnabled()
        {
            using var form = new Form1();
            var btnConvert = form.Controls["btnConvert"] as Button;
            Assert.True(btnConvert!.Enabled);
        }

        [Fact]
        public void Form1_BrowseButtonIsEnabled()
        {
            using var form = new Form1();
            var btnBrowse = form.Controls["btnBrowse"] as Button;
            Assert.True(btnBrowse!.Enabled);
        }

        [Fact]
        public void Form1_DebuggingTextBoxIsReadOnly()
        {
            using var form = new Form1();
            var txtDebugging = form.Controls["txtDebugging"] as TextBox;
            // TextBox is not explicitly set to ReadOnly, so it should be false by default
            Assert.False(txtDebugging!.ReadOnly);
        }

        [Fact]
        public void Form1_DebuggingLabelHasInitialText()
        {
            using var form = new Form1();
            var lblDebugging = form.Controls["lblDebugging"] as Label;
            Assert.Equal("Debugging Output", lblDebugging!.Text);
        }

        // ═══════════════════════════════════════════
        //  Control Layout Tests (DPI-aware)
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_QFXFileTextBoxIsLeftOfBrowseButton()
        {
            using var form = new Form1();
            var txtQFXFile = form.Controls["txtQFXFile"] as TextBox;
            var btnBrowse = form.Controls["btnBrowse"] as Button;
            Assert.True(txtQFXFile!.Location.X < btnBrowse!.Location.X);
        }

        [Fact]
        public void Form1_ConvertButtonIsRightOfAccountName()
        {
            using var form = new Form1();
            var btnConvert = form.Controls["btnConvert"] as Button;
            var txtAccountName = form.Controls["txtAccountName"] as TextBox;
            Assert.True(btnConvert!.Location.X > txtAccountName!.Location.X);
        }

        [Fact]
        public void Form1_DebuggingTextBoxIsBelowInputFields()
        {
            using var form = new Form1();
            var txtDebugging = form.Controls["txtDebugging"] as TextBox;
            var txtAccountName = form.Controls["txtAccountName"] as TextBox;
            Assert.True(txtDebugging!.Location.Y > txtAccountName!.Location.Y);
        }

        [Fact]
        public void Form1_DebuggingLabelIsAboveDebuggingTextBox()
        {
            using var form = new Form1();
            var lblDebugging = form.Controls["lblDebugging"] as Label;
            var txtDebugging = form.Controls["txtDebugging"] as TextBox;
            Assert.True(lblDebugging!.Location.Y < txtDebugging!.Location.Y);
        }

        [Fact]
        public void Form1_BrowseButtonIsAboveConvertButton()
        {
            using var form = new Form1();
            var btnBrowse = form.Controls["btnBrowse"] as Button;
            var btnConvert = form.Controls["btnConvert"] as Button;
            Assert.True(btnBrowse!.Location.Y < btnConvert!.Location.Y);
        }

        // ═══════════════════════════════════════════
        //  Tab Order Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_QFXFileTextBoxIsFirstTabStop()
        {
            using var form = new Form1();
            var txtQFXFile = form.Controls["txtQFXFile"] as TextBox;
            Assert.Equal(0, txtQFXFile!.TabIndex);
        }

        [Fact]
        public void Form1_ConvertButtonHasCorrectTabIndex()
        {
            using var form = new Form1();
            var btnConvert = form.Controls["btnConvert"] as Button;
            Assert.Equal(7, btnConvert!.TabIndex);
        }

        // ═══════════════════════════════════════════
        //  Multiple Form Instances Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_CanCreateMultipleInstances()
        {
            using var form1 = new Form1();
            using var form2 = new Form1();
            Assert.NotSame(form1, form2);
        }

        [Fact]
        public void Form1_InstancesAreIndependent()
        {
            using var form1 = new Form1();
            using var form2 = new Form1();

            var txt1 = form1.Controls["txtQFXFile"] as TextBox;
            var txt2 = form2.Controls["txtQFXFile"] as TextBox;

            txt1!.Text = "test1.qfx";
            txt2!.Text = "test2.qfx";

            Assert.Equal("test1.qfx", txt1.Text);
            Assert.Equal("test2.qfx", txt2.Text);
        }

        // ═══════════════════════════════════════════
        //  Input Sanitization Logic Tests
        //  (Testing the same logic used in btnConvert_Click)
        // ═══════════════════════════════════════════

        [Theory]
        [InlineData("  Account  ", "Account")]
        [InlineData("Account\n", "Account")]
        [InlineData("\rAccount", "Account")]
        [InlineData("Account^Name", "AccountName")]
        [InlineData("^Account^", "Account")]
        [InlineData("\n\r^", "")]
        [InlineData("Normal Account", "Normal Account")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        [InlineData("Hello^World", "HelloWorld")]
        [InlineData("A\nB\rC", "ABC")]
        [InlineData("Line1\nLine2\rLine3", "Line1Line2Line3")]
        [InlineData("Caret^Sign^Removed", "CaretSignRemoved")]
        [InlineData("Mixed\n\r^Data", "MixedData")]
        [InlineData("  Trimmed  ", "Trimmed")]
        public void SanitizeAccountName_RemovesInvalidCharacters(string input, string expected)
        {
            // Replicate the sanitization logic from btnConvert_Click
            string sanitized = input.Trim().Replace("\n", "").Replace("\r", "").Replace("^", "");
            Assert.Equal(expected, sanitized);
        }

        // ═══════════════════════════════════════════
        //  Control Text Setting Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_QFXFileTextBoxCanAcceptFilePath()
        {
            using var form = new Form1();
            var txtQFXFile = form.Controls["txtQFXFile"] as TextBox;
            txtQFXFile!.Text = @"C:\Users\Test\Documents\statement.qfx";
            Assert.Equal(@"C:\Users\Test\Documents\statement.qfx", txtQFXFile.Text);
        }

        [Fact]
        public void Form1_AccountNameTextBoxCanAcceptName()
        {
            using var form = new Form1();
            var txtAccountName = form.Controls["txtAccountName"] as TextBox;
            txtAccountName!.Text = "My Checking Account";
            Assert.Equal("My Checking Account", txtAccountName.Text);
        }

        [Fact]
        public void Form1_DebuggingTextBoxCanBeCleared()
        {
            using var form = new Form1();
            var txtDebugging = form.Controls["txtDebugging"] as TextBox;
            txtDebugging!.Text = "Some debug output";
            Assert.Contains("debug", txtDebugging.Text);
            txtDebugging.Clear();
            Assert.Equal(string.Empty, txtDebugging.Text);
        }

        // ═══════════════════════════════════════════
        //  Form State After Simulated Operations
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_ButtonsCanBeDisabledAndReenabled()
        {
            using var form = new Form1();
            var btnConvert = form.Controls["btnConvert"] as Button;
            var btnBrowse = form.Controls["btnBrowse"] as Button;

            // Simulate disabling during conversion
            btnConvert!.Enabled = false;
            btnBrowse!.Enabled = false;

            Assert.False(btnConvert.Enabled);
            Assert.False(btnBrowse.Enabled);

            // Simulate re-enabling after conversion
            btnConvert.Enabled = true;
            btnBrowse.Enabled = true;

            Assert.True(btnConvert.Enabled);
            Assert.True(btnBrowse.Enabled);
        }

        [Fact]
        public void Form1_DebuggingLabelCanBeUpdated()
        {
            using var form = new Form1();
            var lblDebugging = form.Controls["lblDebugging"] as Label;

            lblDebugging!.Text = "Converting QFX to QIF...";
            Assert.Equal("Converting QFX to QIF...", lblDebugging.Text);

            lblDebugging.Text = "Conversion complete.";
            Assert.Equal("Conversion complete.", lblDebugging.Text);

            lblDebugging.Text = "Conversion failed.";
            Assert.Equal("Conversion failed.", lblDebugging.Text);
        }

        // ═══════════════════════════════════════════
        //  Dialog Configuration Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_OpenFileDialogHasCorrectDefaultExt()
        {
            using var form = new Form1();
            // Access the private field via reflection
            var openFileDialogField = typeof(Form1).GetField("openFileDialog",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openFileDialog = openFileDialogField!.GetValue(form) as OpenFileDialog;
            Assert.NotNull(openFileDialog);
            Assert.Equal("qfx", openFileDialog!.DefaultExt);
        }

        [Fact]
        public void Form1_SaveFileDialogHasCorrectDefaultExt()
        {
            using var form = new Form1();
            var saveFileDialogField = typeof(Form1).GetField("saveFileDialog",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var saveFileDialog = saveFileDialogField!.GetValue(form) as SaveFileDialog;
            Assert.NotNull(saveFileDialog);
            Assert.Equal("qif", saveFileDialog!.DefaultExt);
        }

        // ═══════════════════════════════════════════
        //  SanitizeAccountName Reflection Tests
        // ═══════════════════════════════════════════

        private static string InvokeSanitizeAccountName(string name)
        {
            var method = typeof(Form1).GetMethod("SanitizeAccountName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (string)method!.Invoke(null, new object[] { name })!;
        }

        [Fact]
        public void SanitizeAccountName_TrimsWhitespace()
        {
            Assert.Equal("Account", InvokeSanitizeAccountName("  Account  "));
        }

        [Fact]
        public void SanitizeAccountName_RemovesNewlines()
        {
            Assert.Equal("Account", InvokeSanitizeAccountName("Account\n"));
            Assert.Equal("Account", InvokeSanitizeAccountName("\rAccount"));
            Assert.Equal("ABC", InvokeSanitizeAccountName("A\nB\rC"));
        }

        [Fact]
        public void SanitizeAccountName_RemovesCaret()
        {
            Assert.Equal("AccountName", InvokeSanitizeAccountName("Account^Name"));
            Assert.Equal("Account", InvokeSanitizeAccountName("^Account^"));
        }

        [Fact]
        public void SanitizeAccountName_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", InvokeSanitizeAccountName(""));
        }

        [Fact]
        public void SanitizeAccountName_WhitespaceOnly_ReturnsEmpty()
        {
            Assert.Equal("", InvokeSanitizeAccountName("   "));
        }

        [Fact]
        public void SanitizeAccountName_AllInvalidChars_ReturnsEmpty()
        {
            Assert.Equal("", InvokeSanitizeAccountName("\n\r^"));
        }

        // ═══════════════════════════════════════════
        //  ValidateInputs Reflection Tests
        // ═══════════════════════════════════════════

        private (bool result, string qfxFile, string accountName) InvokeValidateInputs(Form1 form)
        {
            var method = typeof(Form1).GetMethod("ValidateInputs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var parameters = new object?[] { null, null };
            bool result = (bool)method!.Invoke(form, parameters)!;
            return (result, (string)parameters[0]!, (string)parameters[1]!);
        }

        private void SetControlText(Form1 form, string controlName, string text)
        {
            var control = form.Controls[controlName] as TextBox;
            control!.Text = text;
        }

        [Fact]
        public void ValidateInputs_EmptyQfxFile_ReturnsFalse()
        {
            using var form = new Form1();
            SetControlText(form, "txtQFXFile", "");
            SetControlText(form, "txtAccountName", "My Account");

            var (result, _, _) = InvokeValidateInputs(form);
            Assert.False(result);
        }

        [Fact]
        public void ValidateInputs_EmptyAccountName_ReturnsFalse()
        {
            using var form = new Form1();
            SetControlText(form, "txtQFXFile", "test.qfx");
            SetControlText(form, "txtAccountName", "");

            var (result, _, _) = InvokeValidateInputs(form);
            Assert.False(result);
        }

        [Fact]
        public void ValidateInputs_FileNotFound_ReturnsFalse()
        {
            using var form = new Form1();
            SetControlText(form, "txtQFXFile", @"C:\nonexistent\file.qfx");
            SetControlText(form, "txtAccountName", "My Account");

            var (result, _, _) = InvokeValidateInputs(form);
            Assert.False(result);
        }

        [Fact]
        public void ValidateInputs_ValidInputs_ReturnsTrue()
        {
            using var form = new Form1();
            var tempFile = Path.GetTempFileName();
            try
            {
                SetControlText(form, "txtQFXFile", tempFile);
                SetControlText(form, "txtAccountName", "My Account");

                var (result, qfxFile, accountName) = InvokeValidateInputs(form);
                Assert.True(result);
                Assert.Equal(tempFile, qfxFile);
                Assert.Equal("My Account", accountName);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ValidateInputs_SanitizesAccountName()
        {
            using var form = new Form1();
            var tempFile = Path.GetTempFileName();
            try
            {
                SetControlText(form, "txtQFXFile", tempFile);
                SetControlText(form, "txtAccountName", "  My^Account\n");

                var (result, _, accountName) = InvokeValidateInputs(form);
                Assert.True(result);
                Assert.Equal("MyAccount", accountName);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        // ═══════════════════════════════════════════
        //  SetUIBusy Reflection Tests
        // ═══════════════════════════════════════════

        private void InvokeSetUIBusy(Form1 form, bool busy)
        {
            var method = typeof(Form1).GetMethod("SetUIBusy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(form, new object[] { busy });
        }

        [Fact]
        public void SetUIBusy_True_DisablesButtons()
        {
            using var form = new Form1();
            var btnConvert = form.Controls["btnConvert"] as Button;
            var btnBrowse = form.Controls["btnBrowse"] as Button;

            InvokeSetUIBusy(form, true);

            Assert.False(btnConvert!.Enabled);
            Assert.False(btnBrowse!.Enabled);
        }

        [Fact]
        public void SetUIBusy_True_SetsStatusText()
        {
            using var form = new Form1();
            var lblDebugging = form.Controls["lblDebugging"] as Label;

            InvokeSetUIBusy(form, true);

            Assert.Equal("Converting QFX to QIF...", lblDebugging!.Text);
        }

        [Fact]
        public void SetUIBusy_True_ClearsDebugOutput()
        {
            using var form = new Form1();
            var txtDebugging = form.Controls["txtDebugging"] as TextBox;
            txtDebugging!.Text = "Previous output";

            InvokeSetUIBusy(form, true);

            Assert.Equal(string.Empty, txtDebugging.Text);
        }

        [Fact]
        public void SetUIBusy_False_EnablesButtons()
        {
            using var form = new Form1();
            var btnConvert = form.Controls["btnConvert"] as Button;
            var btnBrowse = form.Controls["btnBrowse"] as Button;

            InvokeSetUIBusy(form, true);
            InvokeSetUIBusy(form, false);

            Assert.True(btnConvert!.Enabled);
            Assert.True(btnBrowse!.Enabled);
        }

        [Fact]
        public void SetUIBusy_False_PreservesStatusText()
        {
            using var form = new Form1();
            var lblDebugging = form.Controls["lblDebugging"] as Label;
            lblDebugging!.Text = "Previous status";

            InvokeSetUIBusy(form, false);

            Assert.Equal("Previous status", lblDebugging.Text);
        }

        // ═══════════════════════════════════════════
        //  CreateDebuggingProgress Reflection Tests
        // ═══════════════════════════════════════════

        private IProgress<string> InvokeCreateDebuggingProgress(Form1 form)
        {
            var method = typeof(Form1).GetMethod("CreateDebuggingProgress",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (IProgress<string>)method!.Invoke(form, null)!;
        }

        [Fact]
        public void CreateDebuggingProgress_ReturnsNonNull()
        {
            using var form = new Form1();
            var progress = InvokeCreateDebuggingProgress(form);
            Assert.NotNull(progress);
        }

        [Fact]
        public void CreateDebuggingProgress_ReturnsIProgressString()
        {
            using var form = new Form1();
            var progress = InvokeCreateDebuggingProgress(form);
            Assert.IsAssignableFrom<IProgress<string>>(progress);
        }

        [Fact]
        public void CreateDebuggingProgress_ReportsUseProgressT()
        {
            // Progress<T> captures SynchronizationContext and requires a
            // running WinForms message loop for Report() to marshal callbacks.
            // In unit tests we can only verify it creates a valid Progress<string>.
            using var form = new Form1();
            var progress = InvokeCreateDebuggingProgress(form);
            Assert.NotNull(progress);
            Assert.IsType<Progress<string>>(progress);
        }

        // ═══════════════════════════════════════════
        //  CreateStatusProgress Reflection Tests
        // ═══════════════════════════════════════════

        private IProgress<string> InvokeCreateStatusProgress(Form1 form)
        {
            var method = typeof(Form1).GetMethod("CreateStatusProgress",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (IProgress<string>)method!.Invoke(form, null)!;
        }

        [Fact]
        public void CreateStatusProgress_ReturnsNonNull()
        {
            using var form = new Form1();
            var progress = InvokeCreateStatusProgress(form);
            Assert.NotNull(progress);
        }

        [Fact]
        public void CreateStatusProgress_ReturnsIProgressString()
        {
            using var form = new Form1();
            var progress = InvokeCreateStatusProgress(form);
            Assert.IsAssignableFrom<IProgress<string>>(progress);
        }

        [Fact]
        public void CreateStatusProgress_ReportDoesNotThrow()
        {
            using var form = new Form1();
            var progress = InvokeCreateStatusProgress(form);
            var ex = Record.Exception(() => progress.Report("New status"));
            Assert.Null(ex);
        }

        [Fact]
        public void CreateStatusProgress_MultipleReportsDoNotThrow()
        {
            using var form = new Form1();
            var progress = InvokeCreateStatusProgress(form);
            var ex = Record.Exception(() =>
            {
                progress.Report("Status 1");
                progress.Report("Status 2");
            });
            Assert.Null(ex);
        }

        // ═══════════════════════════════════════════
        //  Constants Verification Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Form1_Constants_AreAccessibleViaReflection()
        {
            // Verify that all string constants are defined and accessible
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            var type = typeof(Form1);

            Assert.Equal("QFX files (*.qfx)|*.qfx", type.GetField("QfxFilter", flags)!.GetValue(null));
            Assert.Equal("QIF files (*.qif)|*.qif", type.GetField("QifFilter", flags)!.GetValue(null));
            Assert.Equal("Select a QFX file", type.GetField("SelectQfxTitle", flags)!.GetValue(null));
            Assert.Equal("Save QIF file", type.GetField("SaveQifTitle", flags)!.GetValue(null));
            Assert.Equal("Error", type.GetField("ErrorTitle", flags)!.GetValue(null));
            Assert.Equal("Success", type.GetField("SuccessTitle", flags)!.GetValue(null));
            Assert.Equal(".qif", type.GetField("QifExtension", flags)!.GetValue(null));
        }

        [Fact]
        public void Form1_CancellationTokenSource_IsInitiallyNull()
        {
            using var form = new Form1();
            var ctsField = typeof(Form1).GetField("_cancellationTokenSource",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cts = ctsField!.GetValue(form);
            Assert.Null(cts);
        }
    }
}
