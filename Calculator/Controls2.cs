using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Calculator
{
    public abstract class EquationBoxBase
    {
        // Include a 'Constants' textbox locally for each EBox and one globally (cannot have repeat values for local)
        // CheckBox to enable/disable an EB for graphing

        protected RichTextBox rtbEquation;

        public virtual string Variable { get { return "X"; } }

        public virtual string Text { get { return rtbEquation.Text; } set { rtbEquation.Text = value; } }

        public virtual bool HasEquation { get { return !string.IsNullOrEmpty(rtbEquation.Text); } }
    }

    // Holds one or more EquationBox instances, with a + button to allow adding more equations
    public class EquationPanel : Panel
    {

    }

    public class CalculatorForm : Form
    {
        private CalculatorPanel calculatorPanel;

        public CalculatorForm()
        {
            calculatorPanel = new CalculatorPanel(this, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            calculatorPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
        }

        protected override void OnLoad(EventArgs e)
        {

            base.OnLoad(e);
        }
    }

    public class CalculatorPanel : Panel
    {

        public CalculatorPanel(Control parent, int left, int top, int width, int height) 
        {
            this.Parent = parent;
            this.SetBounds(left, top, width, height);
        }
    }
}
