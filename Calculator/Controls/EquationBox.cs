// Textbox for the equation itself
// -Variable textbox next to it? probably a panel-wide thing, a label stating y(x), and allow user to format how it is shown or if it is even shown
// Textbox for each constant expression
// -Box to left which states the shorthand, defaults to A on first, B on second, etc. - allow dev to specify a function to return default value
// Some way to expand and collapse a group of constant textboxes
//  -Start with 1 empty one by default
//  -Option to always show constant box, always show if there is one or more constants, etc.

namespace Calculator
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using JonUtility;
    using U = JonUtility.Utility;


    public abstract class EquationBoxBase : Panel
    {
        public abstract event EventHandler HasEquationChanged;
        public abstract event EventHandler EquationTextChanged;

        public abstract IEnumerable<Variable> Variables { get; }

        public abstract IEnumerable<Constant> Constants { get; }

        public abstract void Clear();

        public abstract int Index { get; set; }

        public abstract bool HasEquation { get; }

        public abstract Equation GetEquation();

        public abstract IEnumerable<Constant> GetConstants();

        public static EquationBoxBase Create()
        {
            return new EquationBox();
        }
    }

    public class EquationBox : EquationBoxBase
    {
        public override event EventHandler EquationTextChanged;
        public override event EventHandler HasEquationChanged;

        private const int bufferConstantBoxTop = 5;
        private const int bufferConstantBoxLeft = 5;
        private const int bufferConstantBoxVerticalSpacing = 3;
        private const int bufferBetweenVariableAndEquationBoxes = 0;

        private UnselectableRichTextBox variableLabel;
        private ExpressionRichTextBox equationBox;
        private Button btnExpandCollapse;
        private Panel constantPanel;
        private List<ConstantBoxBase> constantBoxes;
        private int index = 1;
        private string expandText = "Const";
        private string collapseText = "Hide";
        private bool hasEquationText = false;
        private bool hasEquation = false;
        private bool clearingText = false;

        public EquationBox()
        {
            this.SuspendLayout();

            this.variableLabel = new UnselectableRichTextBox();
            this.variableLabel.Parent = this;
            this.variableLabel.Left = 0;
            this.variableLabel.Top = 0;

            this.equationBox = new ExpressionRichTextBox(); // U.NewControl<RichTextBox>(this, string.Empty, 0, 0, this.ClientSize.Width, 20);
            this.equationBox.Parent = this;
            this.equationBox.Height = 22;
            this.equationBox.TextChanged += equationBox_TextChanged;

            this.btnExpandCollapse = new Button();
            this.btnExpandCollapse.Parent = this;
            this.btnExpandCollapse.Top = 0;
            this.btnExpandCollapse.Height = this.equationBox.Height;
            this.btnExpandCollapse.Text = this.expandText;
            this.btnExpandCollapse.FlatStyle = FlatStyle.Flat;
            this.btnExpandCollapse.Click += btnExpandCollapse_Click;
            SetExpandButtonText();

            this.constantPanel = new Panel();
            this.constantPanel.Parent = this;
            this.constantPanel.Left = 0;
            this.constantPanel.Top = this.equationBox.Bottom;
            this.constantPanel.Height = 0;
            //this.constantPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            //this.constantPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.constantBoxes = new List<ConstantBoxBase>();

            this.Height = equationBox.Height;

            this.ResumeLayout(true);
        }

        protected virtual void SetExpandButtonText()
        {
            var expandTextSize = TextRenderer.MeasureText(this.expandText, this.btnExpandCollapse.Font);
            var collapseTextSize = TextRenderer.MeasureText(this.collapseText, this.btnExpandCollapse.Font);

            int largestWidth = Math.Max(expandTextSize.Width, collapseTextSize.Width);
            btnExpandCollapse.Width = largestWidth + 10;
        }

        private void btnExpandCollapse_Click(object sender, EventArgs e)
        {
            bool expand = this.btnExpandCollapse.Text == this.expandText;
            if (expand)
            {
                if (this.constantBoxes.Count == 0)
                {
                    AddConstantBox();
                }

                this.btnExpandCollapse.Text = this.collapseText;
                this.ResizePanels(true);
            }
            else
            {
                this.constantPanel.AutoSize = false;
                this.btnExpandCollapse.Text = this.expandText;
                this.ResizePanels(false);
            }
        }

        private int GetConstantPanelHeight()
        {
            int count = this.constantBoxes.Count;
            return bufferConstantBoxTop +
                   (count * this.equationBox.Height) +
                   ((count - 1) * bufferConstantBoxVerticalSpacing);
        }

        private void AddConstantBox()
        {
            int count = this.constantBoxes.Count;
            int top = GetConstantPanelHeight();
            var shorthand = GetConstantShorthand(count);
            var constantBox = ConstantBoxBase.Create(shorthand);
            constantBox.Parent = this.constantPanel;
            constantBox.SetBounds(
                bufferConstantBoxLeft,
                top,
                this.constantPanel.Width - bufferConstantBoxLeft,
                22);
            constantBox.ConstantTextChanged += constantBox_ConstantTextChanged;

            this.constantBoxes.Add(constantBox);

            this.ResizePanels(btnExpandCollapse.Text == this.collapseText);
        }

        private void ResizePanels(bool constantsExpanded)
        {
            if (constantsExpanded)
            {
                this.constantPanel.Height = GetConstantPanelHeight();
            }
            else
            {
                this.constantPanel.Height = 0;
            }

            this.Height = this.constantPanel.Bottom;
        }

        private void constantBox_ConstantTextChanged(object sender, EventArgs e)
        {
            var box = (ConstantBoxBase)sender;
            int index = this.constantBoxes.IndexOf(box);
            if (index == this.constantBoxes.Count - 1)
            {
                this.AddConstantBox();
            }
        }

        private void equationBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.equationBox.Text))
            {
                this.SetHasEquationValue(false);
            }
            else
            {
                this.SetHasEquationValue(true);
            }

            this.EquationTextChanged.SafeRaise(this);
        }

        private void SetHasEquationValue(bool hasEquation)
        {
            if (this.hasEquationText != hasEquation)
            {
                this.hasEquationText = hasEquation;
                this.HasEquationChanged.SafeRaise(this);
            }
        }

        protected string GetConstantShorthand(int constantBoxIndex)
        {
            if (constantBoxIndex < 26)
            {
                return ((char)(65 + constantBoxIndex)).ToString();
            }
            else
            {
                var counters = new List<int>() { 65, 64 };
                for (int i = 0; i < constantBoxIndex; i++)
                {
                    for (int i2 = 0; i2 < counters.Count; i2++)
                    {
                        counters[i2]++;
                        if (counters[i2] == 91)
                        {
                            counters[i2] = 65;
                            if (i2 == counters.Count - 1)
                            {
                                counters.Add(64);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                return new string(counters.Select(i => (char)i).Reverse().ToArray());
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            this.UpdateBackColors();
            base.OnBackColorChanged(e);
        }

        private void UpdateBackColors()
        {
            this.variableLabel.BackColor = this.BackColor;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            OnFormatControlPositions();
            base.OnSizeChanged(e);
        }

        protected virtual void OnFormatControlPositions()
        {
            this.SuspendLayout();

            this.UpdateIndex();
            this.variableLabel.Top = (this.equationBox.Height - this.variableLabel.Height) / 2;

            this.btnExpandCollapse.Left = this.ClientSize.Width - this.btnExpandCollapse.Width;
            this.equationBox.Left = this.variableLabel.Right + bufferBetweenVariableAndEquationBoxes;
            this.equationBox.Width = this.btnExpandCollapse.Left - this.equationBox.Left - 5;

            this.constantPanel.Width = this.Width;

            this.ResumeLayout(true);
        }

        protected virtual string GetVariableLabelText()
        {
            return "Y" + (this.index + 1).ToString() + " =";
        }

        protected virtual void SetVariableLabelText(string text)
        {
            const string pattern = @"\d+";

            this.variableLabel.Text = text;
            var textSize = TextRenderer.MeasureText(text, this.Font);
            var matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                this.variableLabel.SelectionStart = match.Index;
                this.variableLabel.SelectionLength = match.Length;
                this.variableLabel.SelectionFont = new Font(this.variableLabel.Font.FontFamily, this.variableLabel.Font.Size - 1.5f, FontStyle.Regular);
                this.variableLabel.SelectionCharOffset = -2;
            }

            this.variableLabel.SelectionStart = text.Length;
            this.variableLabel.Width = textSize.Width;
            this.variableLabel.Height = textSize.Height + 2;
        }

        public override IEnumerable<Variable> Variables
        {
            get { throw new NotImplementedException(); }
        }

        public override IEnumerable<Constant> Constants
        {
            get { throw new NotImplementedException(); }
        }

        public override bool HasEquation
        {
            get 
            {
                if (!this.hasEquationText)
                {
                    return false;
                }
                else
                {
                    return this.IsEquationValid(); 
                }         
            }
        }

        public bool IsEquationValid()
        {
            if (!this.hasEquationText)
            {
                return false;
            }

            Equation equation = this.GetEquation();
            try
            {
                EquationParser.ValidateEquation(equation);
                return true;
            }
            catch (EquationValidationException ex)
            {
                return false;                
            }
        }

        public override Equation GetEquation()
        {
            if (!this.hasEquationText)
            {
                return Equation.Empty;
            }

            var constants = this.GetConstants();
            var equation = Equation.Create(
                this.equationBox.Text.Trim(), 
                constants);

            try
            {
                EquationParser.ValidateEquation(equation);
                return equation;
            }
            catch (EquationValidationException ex)
            {
                return Equation.Empty;
            }
        }

        public override IEnumerable<Constant> GetConstants()
        {
            var constantList = from constantBox in this.constantBoxes    
                               where constantBox.HasConstant
                               select constantBox.GetConstant();
            return constantList;
        }

        public override int Index
        {
            get
            {
                return this.index;
            }
            set
            {
                this.index = value;
                this.UpdateIndex();
            }
        }

        private void UpdateIndex()
        {
            string text = GetVariableLabelText();
            this.SetVariableLabelText(text);
        }

        public override void Clear()
        {
            this.SuspendLayout();

            this.clearingText = true;
            this.equationBox.Clear();
            this.RemoveConstantBoxes(
                Enumerable.Range(0,
                    this.constantBoxes.Count).ToList());
            this.ResumeLayout();
        }

        private void RemoveConstantBoxes(List<int> indexes)
        {
            if (indexes == null)
            {
                return;
            }

            int count = indexes.Count;
            if (count > 1)
            {
                indexes.Sort((x, y) => x > y ? -1 : x < y ? 1 : 0);
            }

            for (int i = 0; i < count; i++)
            {
                int index = indexes[i];
                var constantBox = this.constantBoxes[index];
            }
        }

    }

    public class ValidationEventArgs<T> : EventArgs
    {
        private T value;
        private T replacementValue;
        private bool success;

        public ValidationEventArgs(T valueToValidate)
        {
            this.value = valueToValidate;
            this.replacementValue = valueToValidate;
            this.success = true;
        }

        public T Value
        {
            get
            {
                return this.value;
            }
        }

        public bool Success
        {
            get
            {
                return this.success;
            }
        }

        public T ReplacementValue
        {
            get
            {
                return this.replacementValue;
            }
        }

        public void SetResults(bool success, T replacementValue)
        {
            this.success = success;
            if (!success)
            {
                this.replacementValue = replacementValue;
            }
        }
    }

    public abstract class ConstantBoxBase : Panel
    {
        public abstract event EventHandler HasConstantChanged;
        public abstract event EventHandler ConstantTextChanged;

        public abstract bool HasConstant { get; }

        public abstract Constant GetConstant();

        public static ConstantBoxBase Create(string constantShorthand)
        {
            return new ConstantBox(constantShorthand);
        }

        public event EventHandler<ValidationEventArgs<string>> ValidateShorthand;

        /// <summary>
        /// Raises the ValidateShorthand event with the passed EventArgs argument. If there are no subscribers, the PerformDefaultShorthandValidation method is called instead.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnValidateShorthand(ValidationEventArgs<string> e)
        {
            var validationEvent = this.ValidateShorthand;
            if (validationEvent != null && validationEvent.GetInvocationList().Count() > 0)
            {
                validationEvent(this, e);
            }
            else
            {
                PerformDefaultShorthandValidation(e);
            }
        }

        protected virtual void PerformDefaultShorthandValidation(ValidationEventArgs<string> e)
        {
            string text = e.Value.Replace(" ", string.Empty);
            if (string.IsNullOrWhiteSpace(text) || !string.Equals(e.Value, text))
            {
                e.SetResults(false, text);
            }
        }
    }

    public class ConstantBox : ConstantBoxBase
    {
        public override event EventHandler ConstantTextChanged;
        public override event EventHandler HasConstantChanged;

        private const int defaultBoxHeight = 22;
        private ExpressionRichTextBox constantTextBox;
        private RichTextBox shorthandTextBox;
        private string lastValidShorthand;
        private bool hasConstant;

        public ConstantBox(string constantShorthand)
        {
            this.lastValidShorthand = constantShorthand;

            this.shorthandTextBox = new RichTextBox();
            this.shorthandTextBox.Parent = this;
            this.shorthandTextBox.Left = 0;
            this.shorthandTextBox.Top = 0;
            this.shorthandTextBox.Width = defaultBoxHeight;
            this.shorthandTextBox.Height = defaultBoxHeight;
            this.shorthandTextBox.Multiline = false;
            this.shorthandTextBox.Text = lastValidShorthand;
            this.shorthandTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.shorthandTextBox.LostFocus += constantShorthandTextBox_LostFocus;
            this.shorthandTextBox.TextChanged += shorthandTextBox_TextChanged;

            this.constantTextBox = new ExpressionRichTextBox();
            this.constantTextBox.Parent = this;
            this.constantTextBox.Top = 0;
            this.constantTextBox.Height = defaultBoxHeight;
            this.constantTextBox.TextChanged += constantTextBox_TextChanged;
        }

        private void shorthandTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateHasConstantStatus();
        }

        private void constantTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateHasConstantStatus();
            this.ConstantTextChanged.SafeRaise(this);
        }

        private void UpdateHasConstantStatus()
        {
            bool hasConstant = (!string.IsNullOrWhiteSpace(this.constantTextBox.Text) && !string.IsNullOrWhiteSpace(this.shorthandTextBox.Text));
            
            if (this.hasConstant != hasConstant)
            {
                this.hasConstant = hasConstant;
                this.HasConstantChanged.SafeRaise(this);
            }
        }

        void constantShorthandTextBox_LostFocus(object sender, EventArgs e)
        {
            var validationEventArgs = new ValidationEventArgs<string>(this.shorthandTextBox.Text);
            this.OnValidateShorthand(validationEventArgs);
            if (!validationEventArgs.Success)
            {
                this.shorthandTextBox.Text = string.IsNullOrWhiteSpace(validationEventArgs.ReplacementValue) ?
                    this.lastValidShorthand :
                    validationEventArgs.ReplacementValue;
            }
            this.lastValidShorthand = this.shorthandTextBox.Text;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            OnFormatControlPositions();
        }

        protected void OnFormatControlPositions()
        {
            this.constantTextBox.Left = this.shorthandTextBox.Right + 2;
            this.constantTextBox.Width = this.ClientSize.Width - this.constantTextBox.Left;
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (this.Parent != null)
            {
                this.shorthandTextBox.BackColor = this.Parent.BackColor;
            }
        }

        public override Constant GetConstant()
        {
            if (!this.hasConstant)
            {
                return Constant.Empty;
            }

            var constant = Constant.Create(this.shorthandTextBox.Text, this.constantTextBox.Text.Trim());
            return constant;
        }

        public override bool HasConstant
        {
            get { return hasConstant; }
        }
    }
}
