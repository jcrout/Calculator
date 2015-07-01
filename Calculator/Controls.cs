//-----------------------------------------------------------------------
// To Do:
//  Graph Box:
//      Drawing:
//          -Zoom Factor
//          -X ranges, Y ranges (default -10 to 10 each)
//          -Option for full gridlines, tick marks on lines
//          -Set colors for gridlines/ticks, including alternating colors
//      -Lines between points
//          -Set colors for lines, including alternating/set patterns
//          -Line thickness
//          -Temporally draw lines?
//      -Points
//          -Option to draw circles on points, or other shapes/images
//          -Cache the points for tracing
//      -Tracer
//          -Iterate over cached points
//          -OnTraceNext, passing the point to trace, allow users to override method?
//      -Modes
//          -Degrees/Radians
//
// Include a 'Constants' textbox locally for each EBox and one globally (cannot have repeat values for local)
// CheckBox to enable/disable an EB for graphing
//-----------------------------------------------------------------------

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

    public interface ICalculatorControl
    {
        Size DefaultSize { get; }
    }

    // Holds one or more EquationBox instances, with a + button to allow adding more equations
    public abstract class EquationBoxContainerBase : Panel
    {
        public abstract IEnumerable<EquationBoxBase> EquationBoxes { get; }

        public abstract IEnumerable<Equation> GetEquations();

        public abstract bool EnableAddingEquationBoxes { get; }

        public abstract CloseBoxMode CloseBoxMode { get; }

    }

    public class EquationBoxContainer : EquationBoxContainerBase
    {
        private const int bufferLeft = 5;
        private const int bufferRight = 5;
        private const int bufferTop = 5;
        private const int bufferBoxSpacing = 3;
        private const int bufferCloseBoxSpacing = 5;

        private static int minimumBoxes = 3;
        private static Size closeBoxSize;

        private List<EquationBoxBase> equationBoxes;
        private List<CloseBoxBase> closeBoxes;
        private bool addEquationBoxEnabled = true;
        private CloseBoxMode closeBoxMode = CloseBoxMode.Close | CloseBoxMode.SingleBoxClear;
        private string closeBoxText = "X";

        static EquationBoxContainer()
        {
            SetCloseBoxSize();
        }

        private static void SetCloseBoxSize()
        {
            var temp = CloseBoxBase.Create();
            var tempEquationBox = EquationBoxBase.Create();
            int width = temp.Width, height = temp.Height;

            if (temp.Width < 3 || temp.Width > 22)
            {
                width = 22;
            }

            if (temp.Height < 3 || temp.Height > 22)
            {
                height = 22;
            }

            closeBoxSize = new Size(width, height);
        }

        public EquationBoxContainer()
        {
            this.AutoScroll = true;
            this.AutoScrollMargin = new Size(bufferLeft, bufferTop);
            this.AutoSize = false;
            this.equationBoxes = new List<EquationBoxBase>();
            this.closeBoxes = new List<CloseBoxBase>();

            this.AddBoxes(minimumBoxes);
        }

        public override IEnumerable<EquationBoxBase> EquationBoxes
        {
            get { return this.equationBoxes; }
        }

        public override IEnumerable<Equation> GetEquations()
        {
            int count = this.equationBoxes.Count;
            var equations = new List<Equation>();
            for (int i = 0; i < count; i++)
            {
                var box = this.equationBoxes[i];
                var equation = box.GetEquation();
                if (equation == null || ReferenceEquals(equation, Equation.Empty) || equation.Text == string.Empty)
                {
                    continue;
                }

                equations.Add(equation);
            }

            return equations;
        }

        public override bool EnableAddingEquationBoxes
        {
            get { return this.addEquationBoxEnabled; }
        }

        private void AddBoxes(int count = 1)
        {
            this.SuspendLayout();

            for (int i = 0; i < count; i++)
            {
                var box = EquationBoxBase.Create();
                box.Parent = this;
                box.Left = bufferLeft;
                box.Top = GetBoxTop();
                box.Width = GetBoxWidth();
                box.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                box.Index = this.equationBoxes.Count;
                box.HasEquationChanged += box_HasEquationChanged;
                box.SizeChanged += box_SizeChanged;

                this.equationBoxes.Add(box);
            }

            this.UpdateCloseBoxes();
            this.FormatControlPositions();

            this.ResumeLayout(true);
        }

        private void UpdateCloseBoxes()
        {
            if (this.closeBoxMode.HasFlag(CloseBoxMode.Disabled))
            {
                return;
            }

            int count = this.equationBoxes.Count;
            if (count == 1)
            {
                ClearAllCloseBoxes();
                return;
            }


            int closeBoxCount = this.closeBoxes.Count;
            if (closeBoxCount < count)
            {
                for (int i = closeBoxCount; i < count; i++)
                {
                    var newBox = CloseBoxBase.Create();
                    newBox.Text = closeBoxText;
                    newBox.Size = closeBoxSize;
                    newBox.Parent = this;
                    if (i == 0)
                    {
                        newBox.Top = bufferTop;
                    }
                    newBox.Closing += closeBox_Closing;
                    closeBoxes.Add(newBox);
                }
            }
        }

        private void closeBox_Closing(object sender, EventArgs e)
        {
            var closeBox = (CloseBoxBase)sender;
            int index = this.closeBoxes.IndexOf(closeBox);

            this.SuspendLayout();

            this.RemoveBox(index);
            this.FormatControlPositions();

            this.ResumeLayout(true);
        }

        private void RemoveBox(int index)
        {
            RemoveBoxes(new List<int>(1) { index });
        }

        private void RemoveBoxes(List<int> indexes)
        {
            if (indexes == null)
            {
                return;
            }

            int count = indexes.Count;
            if (count == 0)
            {
                return;
            }
            else if (count > 1)
            {
                indexes.Sort();
                indexes.Reverse();
            }

            for (int i = 0; i < count; i++)
            {
                int index = indexes[i];
                _RemoveCloseBox(index);
                _RemoveEquationBox(index);
            }

            if (equationBoxes.Count == 1)
            {
                _RemoveCloseBox(0);
            }
        }

        private void _RemoveEquationBox(int index)
        {
            var equationBox = this.equationBoxes[index];
            this.equationBoxes.RemoveAt(index);
            this.Controls.Remove(equationBox);
        }

        private void _RemoveCloseBox(int index)
        {
            var closeBox = this.closeBoxes[index];
            closeBox.Click -= closeBox_Closing;
            this.closeBoxes.RemoveAt(index);
            this.Controls.Remove(closeBox);
        }

        private void ClearAllCloseBoxes()
        {
            foreach (var closeBox in this.closeBoxes)
            {
                closeBox.Parent = null;
                closeBox.Dispose();
            }
        }

        private void box_HasEquationChanged(object sender, EventArgs e)
        {
            var box = (EquationBoxBase)sender;
            int index = this.equationBoxes.IndexOf(box);
            if (box.HasEquation && index == this.equationBoxes.Count - 1)
            {
                this.AddBoxes(1);
            }
        }

        private void box_SizeChanged(object sender, EventArgs e)
        {
            this.SuspendLayout();
            this.FormatControlPositions();
            this.ResumeLayout(true);
        }

        private int GetBoxTop()
        {
            if (this.equationBoxes.Count == 0)
            {
                return bufferTop;
            }

            int height = this.equationBoxes[this.equationBoxes.Count - 1].Bottom + bufferBoxSpacing;
            return height;
        }

        private int GetBoxWidth()
        {
            int baseWidth = this.ClientSize.Width - bufferLeft - bufferRight;
            return baseWidth - (this.closeBoxMode.HasFlag(CloseBoxMode.Disabled)
                ? closeBoxSize.Width + bufferCloseBoxSpacing
                : 0);
        }

        private int GetCloseBoxLeft()
        {
            if (this.closeBoxMode.HasFlag(CloseBoxMode.Disabled))
            {
                return this.ClientSize.Width - bufferRight - closeBoxSize.Width;
            }
            else
            {
                return this.ClientSize.Width;
            }
        }

        private void UpdateCloseBox(int index)
        {

        }

        protected virtual void FormatControlPositions()
        {
            int scrollOffset = this.AutoScrollPosition.Y;
            int width = this.GetBoxWidth();
            int closeBoxLeft = this.GetCloseBoxLeft();

            for (int i = 0; i < this.equationBoxes.Count; i++)
            {
                var equationBox = this.equationBoxes[i];
                var closeBox = (i < this.closeBoxes.Count) ? this.closeBoxes[i] : null;

                if (equationBox.Index != i)
                {
                    equationBox.Index = i;
                }

                if (equationBox.Width != width)
                {
                    equationBox.Width = width;
                }

                if (closeBox != null && closeBox.Left != closeBoxLeft)
                {
                    closeBox.Left = closeBoxLeft;
                }

                int top = 0;
                if (i > 0)
                {
                    top = this.equationBoxes[i - 1].Bottom + bufferBoxSpacing;
                }
                else
                {
                    top = bufferTop + scrollOffset;
                }

                if (equationBox.Top != top)
                {
                    equationBox.Top = top;
                }

                if (closeBox != null && closeBox.Top != top)
                {
                    closeBox.Top = top;
                }
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            this.SuspendLayout();
            this.FormatControlPositions();
            this.ResumeLayout(true);

            base.OnSizeChanged(e);
        }

        public override CloseBoxMode CloseBoxMode
        {
            get { throw new NotImplementedException(); }
        }
    }

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
        private EquationBoxContainerBase container;

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
            this.container = U.NewControl<EquationBoxContainer>(this, string.Empty, 25, 25, 300, 150);
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
