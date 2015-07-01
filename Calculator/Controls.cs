
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed.")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1634:FileHeaderMustShowCopyright", Justification = "Reviewed.")]

namespace Calculator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using JonUtility;
    using U = JonUtility.Utility;
    using CS = CalculatorSettings;
    
    public abstract class CloseBoxBase : Panel
    {
        public abstract event EventHandler Closing;

        public static CloseBoxBase Create()
        {
            return new CloseBox();
        }

        public CloseBoxBase()
        {

        }
    }

    public class CloseBox : CloseBoxBase
    {
        public override event EventHandler Closing;

        private Button closeButton;
        private string buttonText = "X";

        public CloseBox()
        {
            this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            this.BorderStyle = System.Windows.Forms.BorderStyle.None;

            this.BackColor = Color.Transparent;
            this.closeButton = new Button();
            this.closeButton.Text = this.buttonText;
            this.closeButton.Location = new Point(0, 0);
            this.closeButton.FlatStyle = FlatStyle.Flat;
            this.closeButton.Parent = this;
            this.closeButton.Click += closeButton_Click;
        }

        void closeButton_Click(object sender, EventArgs e)
        {
            this.Closing.SafeRaise(this);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            this.closeButton.Size = this.ClientSize;
            base.OnSizeChanged(e);
        }
    }

    [Flags]
    public enum CloseBoxMode : int
    {
        Disabled = 0,
        Close = 1,
        SingleBoxClear = 2
    }

    public class CalculatorForm : Form
    {
        private CalculatorPanel calculatorPanel;

        public CalculatorForm()
        {
            this.Text = "Calculator";
            this.calculatorPanel = new CalculatorPanel(this, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            this.calculatorPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            this.calculatorPanel.BackColor = Color.WhiteSmoke;
        }

        protected override void OnLoad(EventArgs e)
        {
            this.CenterForm(500, 500);
            this.calculatorPanel.TEMP_SETANCHOR();
            base.OnLoad(e);
        }
    }

    public class CalculatorPanel : Panel
    {
        private GraphBox graphBox;
        private EquationBoxContainer container;

        public CalculatorPanel()
            : this(null, 0, 0, 300, 200)
        {
        }

        public void TEMP_SETANCHOR()
        {
            this.container.Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Right;
        }

        public CalculatorPanel(Control parent, int left, int top, int width, int height)
        {
            this.Parent = parent;
            this.SetBounds(left, top, width, height);
            this.container = EquationBoxContainer.Create();
            this.container.SetBounds(25, 25, 300, 150);
            this.container.Parent = this; 
            this.container.BorderStyle = BorderStyle.FixedSingle;

            this.graphBox = U.NewControl<GraphBox>(this, string.Empty, 25, this.container.Bottom + 5, 301, 301);
            var btn = U.NewControl<Button>(this, "Graph", 0, 0, 80, 22);

            btn.Click += (sender, e) =>
            {
                var equations = this.container.GetEquations();
                int count = equations.Count();
                if (count > 0)
                {
                    try
                    {
                        var delegates = equations.Select(eq => EquationParser.ParseEquation(eq));
                        this.graphBox.Graph(delegates);
                    }
                    catch (EquationValidationException ex)
                    {                        
                        throw;
                    }
         
                }
            };
            var btn2 = U.NewControl<Button>(this, "Clear", btn.Right + 10, 0, 80, 22);
            btn2.Click += (sender, e) =>
            {
                this.graphBox.Clear(true);
            };
        }
    }


    /// <summary>
    /// This class functions similar to a label, having no caret shown, but allows the kind of specially-formatted text that you normally would use a RichTextBox to display.
    /// </summary>
    internal class UnselectableRichTextBox : RichTextBox
    {
        [DllImport("user32.dll", EntryPoint = "HideCaret")]

        public static extern long HideCaret(IntPtr hwnd);

        public UnselectableRichTextBox()
        {
            this.ReadOnly = true;
            this.ScrollBars = RichTextBoxScrollBars.None;
            this.BorderStyle = System.Windows.Forms.BorderStyle.None;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            HideCaret(this.Handle);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            this.Parent.Focus();
        }
    }

    internal class ExpressionRichTextBox : RichTextBox
    {

    }
}
