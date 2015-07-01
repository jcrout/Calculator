
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


    // Holds one or more EquationBox instances, with a + button to allow adding more equations
    public abstract class EquationBoxContainer : Panel
    {
        public abstract IEnumerable<EquationBoxBase> EquationBoxes { get; }

        public abstract IEnumerable<Equation> GetEquations();

        public abstract bool EnableAddingEquationBoxes { get; }

        public abstract CloseBoxMode CloseBoxMode { get; }

        public static EquationBoxContainer Create()
        {
            return new DefaultEquationBoxContainer();
        }
    }

    public class DefaultEquationBoxContainer : EquationBoxContainer
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

        static DefaultEquationBoxContainer()
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

        public DefaultEquationBoxContainer()
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
}
