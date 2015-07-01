
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
