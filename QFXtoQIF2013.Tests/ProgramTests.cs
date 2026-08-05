using System;
using System.Reflection;
using Xunit;
using QFXtoQIF2013;

namespace QFXtoQIF2013.Tests
{
    public class ProgramTests
    {
        // ═══════════════════════════════════════════
        //  Class Structure Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Program_ClassExists()
        {
            var programType = typeof(Program);
            Assert.NotNull(programType);
        }

        [Fact]
        public void Program_IsInternalStatic()
        {
            var programType = typeof(Program);
            Assert.True(programType.IsAbstract);
            Assert.True(programType.IsSealed);
            Assert.Equal(TypeAttributes.NotPublic, programType.Attributes & TypeAttributes.VisibilityMask);
        }

        [Fact]
        public void Program_HasMainMethod()
        {
            var programType = typeof(Program);
            var mainMethod = programType.GetMethod("Main",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(mainMethod);
        }

        [Fact]
        public void Program_MainMethodIsSTAThread()
        {
            var programType = typeof(Program);
            var mainMethod = programType.GetMethod("Main",
                BindingFlags.Static | BindingFlags.NonPublic);
            var stathreadAttr = mainMethod!.GetCustomAttribute<STAThreadAttribute>();
            Assert.NotNull(stathreadAttr);
        }

        [Fact]
        public void Program_MainMethodReturnTypeIsVoid()
        {
            var programType = typeof(Program);
            var mainMethod = programType.GetMethod("Main",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.Equal(typeof(void), mainMethod!.ReturnType);
        }

        [Fact]
        public void Program_MainMethodHasNoParameters()
        {
            var programType = typeof(Program);
            var mainMethod = programType.GetMethod("Main",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.Empty(mainMethod!.GetParameters());
        }

        // ═══════════════════════════════════════════
        //  Namespace Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Program_IsInCorrectNamespace()
        {
            var programType = typeof(Program);
            Assert.Equal("QFXtoQIF2013", programType.Namespace);
        }

        // ═══════════════════════════════════════════
        //  Assembly Reference Tests
        // ═══════════════════════════════════════════

        [Fact]
        public void Program_CanAccessForm1Type()
        {
            // Verify that Program can reference Form1 (as it does in Application.Run)
            var form1Type = typeof(Form1);
            Assert.NotNull(form1Type);
            Assert.Equal("QFXtoQIF2013", form1Type.Namespace);
        }

        [Fact]
        public void Program_Form1IsPublicClass()
        {
            var form1Type = typeof(Form1);
            Assert.True(form1Type.IsPublic);
        }

        [Fact]
        public void Program_Form1InheritsFromForm()
        {
            var form1Type = typeof(Form1);
            Assert.True(typeof(System.Windows.Forms.Form).IsAssignableFrom(form1Type));
        }
    }
}
