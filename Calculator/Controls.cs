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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using JonUtility;
    using U = JonUtility.Utility;

    public enum GraphMode : int
    {
        Function = 0
    }

    [DebuggerDisplay("{x}, {y}")]
    public struct PointD
    {
        private double x;
        private double y;

        public PointD(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public double X
        {
            get
            {
                return this.x;
            }
        }

        public double Y
        {
            get
            {
                return this.y;
            }
        }

        public override string ToString()
        {
            return this.x.ToString() + "," + this.y.ToString();
        }
    }

    public abstract class EquationBoxBase
    {
        private RichTextBox equationTextBox;

        public RichTextBox EquationTextBox
        {
            get
            {
                return this.equationTextBox;
            }

            set
            {
                this.equationTextBox = value;
            }
        }

        public virtual string Variable
        {
            get
            {
                return "X";
            }
        }

        public virtual string Text
        {
            get
            {
                return this.equationTextBox.Text;
            }

            set
            {
                this.equationTextBox.Text = value;
            }
        }

        public virtual bool HasEquation
        {
            get
            {
                return !string.IsNullOrEmpty(this.equationTextBox.Text);
            }
        }
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
            this.Text = "Calculator";
            this.calculatorPanel = new CalculatorPanel(this, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            this.calculatorPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            this.calculatorPanel.BackColor = Color.WhiteSmoke;
        }

        protected override void OnLoad(EventArgs e)
        {
            this.CenterForm(500, 500);
            base.OnLoad(e);
        }
    }

    public class CalculatorPanel : Panel
    {
        private GraphBox graphBox;

        public CalculatorPanel()
            : this(null, 0, 0, 300, 200)
        {
        }

        public CalculatorPanel(Control parent, int left, int top, int width, int height)
        {
            this.Parent = parent;
            this.SetBounds(left, top, width, height);
            this.graphBox = U.NewControl<GraphBox>(this, string.Empty, 25, 25, 301, 301);
            Task t = Task.Run(() =>
            {
                string eqText = "x^2+1 - (.5x^3)";
                Equation eq = Equation.Create(eqText);
                Delegate del = EquationParser.ParseEquation(eq, null);
                object result = del.DynamicInvoke(new object[] { 5.5D });
                this.graphBox.Graph(del, null);
            });
        }
    }

    // tomorrow: fix the tick marks to represent the correct offsets instead of doing it by count
    //           allow decimal numbers without the leading 0, such as ".5" instead of "0.5"

    public class GraphBox : PictureBox
    {
        private GraphSettings settings = new GraphSettings();
        private Bitmap image;
        private Graphics graphics;
        private Pen ticksPen;
        private Pen axisPen;
        private Pen graphLinePen;
        private Point origin;
        private PointD scaleFactor;

        public GraphBox()
        {
            this.SizeMode = PictureBoxSizeMode.Normal;
            this.BackColor = Color.White;
            this.ticksPen = Pens.DarkRed;
            this.axisPen = Pens.Black;
            this.graphLinePen = Pens.Blue;
        }

        public GraphBox(Control parent, int left, int top, int width, int height)
            : this()
        {
            this.Parent = parent;
            this.SetBounds(left, top, width, height);
        }

        public GraphSettings Settings
        {
            get
            {
                return this.settings;
            }
        }

        public void Graph(Delegate function, object graphData = null)
        {
            this.OnGraph(function, graphData);
        }

        protected async virtual void OnGraph(Delegate function, object graphData = null)
        {
            this.SetOrigin();
            Task t = Task.Run(() => this.DrawBaseGraph());
            PointD[] points = await Task.Run(() => this.GetGraphPoints(function, graphData)).ConfigureAwait(false);
            await t.ConfigureAwait(false);
            await Task.Run(() => this.DrawGraphPoints(points)).ConfigureAwait(false);
            this.Invoke(new Action(() => { this.Image = this.image; }));
        }

        protected override void OnResize(EventArgs e)
        {
            if ((this.ClientSize.Width > 0) && (this.ClientSize.Height > 0))
            {
                this.image = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
                this.graphics = Graphics.FromImage(this.image);
            }

            base.OnResize(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (this.graphics != null)
            {
                this.graphics.Dispose();
                this.graphics = null;
            }

            if (this.image != null)
            {
                this.image.Dispose();
                this.image = null;
            }

            base.Dispose(disposing);
        }

        private void SetOrigin()
        {
            int originX = 0, originY = 0;

            double rangeTotalX = this.settings.XAxisRange.Y - this.settings.XAxisRange.X;
            double rangeTotalY = this.settings.YAxisRange.Y - this.settings.YAxisRange.X;

            if (this.settings.XAxisRange.X < 0)
            {
                if (this.settings.XAxisRange.Y < 0)
                {
                    originX = this.ClientSize.Width;
                }
                else
                {
                    double offset = Math.Abs(this.settings.XAxisRange.X / rangeTotalX);
                    originX = (int)(offset * this.ClientSize.Width);
                }
            }

            if (this.settings.YAxisRange.X < 0)
            {
                if (this.settings.YAxisRange.Y < 0)
                {
                    originX = this.ClientSize.Height;
                }
                else
                {
                    double offset = Math.Abs(this.settings.YAxisRange.X / rangeTotalY);
                    originY = (int)(offset * this.ClientSize.Height);
                }
            }

            this.scaleFactor = new PointD(this.ClientSize.Width / rangeTotalX, this.ClientSize.Height / rangeTotalY);
            this.origin = new Point(originX, originY);
        }

        private void DrawBaseGraph()
        {
            int halfLineThickness = (this.settings.LineThickness - 1) / 2;
            int upperLineX = this.origin.X - halfLineThickness;
            int upperLineY = this.origin.Y - halfLineThickness;

            this.graphics.Clear(this.BackColor);

            if (this.settings.ShowXAxis)
                for (int i = 0; i < this.settings.LineThickness; i++)
                    this.graphics.DrawLine(this.axisPen, 0, upperLineY + i, this.ClientSize.Width, upperLineY + i);
            if (this.settings.ShowYAxis)
                for (int i = 0; i < this.settings.LineThickness; i++)
                    this.graphics.DrawLine(this.axisPen, upperLineX + i, 0, upperLineX + i, this.ClientSize.Height);
            if (this.settings.ShowTickMarks)
                this.DrawTickMarks();
        }

        private void DrawTickMarks()
        {
            int tickHalfWidth = ((this.settings.LineThickness - 1) / 2) + this.settings.TickMarkWidth;
            int tickDistanceX = this.ClientSize.Width / this.settings.XTickMarkCount;
            int lowerY = this.origin.Y + tickHalfWidth;
            int upperY = this.origin.Y - tickHalfWidth;
            int tickOffsetX = this.origin.X % tickDistanceX;
            for (int tickX = 0, currentX = tickOffsetX; tickX < this.settings.XTickMarkCount + 1; tickX++, currentX += tickDistanceX)
            {
                if (currentX == this.origin.X) continue;
                this.graphics.DrawLine(this.ticksPen, currentX, lowerY, currentX, upperY);
            }

            int tickDistanceY = this.ClientSize.Height / this.settings.YTickMarkCount;
            int lowerX = this.origin.X + tickHalfWidth;
            int upperX = this.origin.X - tickHalfWidth;
            int tickOffsetY = this.origin.Y % tickDistanceX;
            for (int tickY = 0, currentY = tickOffsetY; tickY < this.settings.YTickMarkCount + 1; tickY++, currentY += tickDistanceY)
            {
                if (currentY == this.origin.Y) continue;
                this.graphics.DrawLine(this.ticksPen, lowerX, currentY, upperX, currentY);
            }
        }

        private PointD[] GetGraphPoints(Delegate function, object graphData)
        {
            var functionToEvaluate = (Func<double, double>)function;
            int pointCount = this.settings.PointsToGraph;
            double min = (double)this.settings.XValueRange.X;
            double max = (double)this.settings.XValueRange.Y;
            double range = max - min;
            double increment = range / pointCount;

            var points = new PointD[pointCount];
            double valueX = min;
            for (int i = 0; i < pointCount; i++)
            {
                double valueY = functionToEvaluate(valueX);
                points[i] = new PointD(valueX, valueY);
                valueX += increment;
            }

            return points;
        }
        
        private void DrawGraphPoints(PointD[] points)
        {
            int pointCount = points.Length;
            int linesDrawn = 0;
            for (int i = 1; i < pointCount; i++)
            {
                var firstPoint = points[i - 1];
                var secondPoint = points[i];

                int x1 = this.origin.X + (int)(this.scaleFactor.X * firstPoint.X);
                int y1 = this.origin.Y - (int)(this.scaleFactor.Y * firstPoint.Y);
                int x2 = this.origin.X + (int)(this.scaleFactor.X * secondPoint.X);
                int y2 = this.origin.Y - (int)(this.scaleFactor.Y * secondPoint.Y);

                // check to see whether x1,y1 is in bounds, or x2,y2 is in bounds; if either is true, draw the line
                bool inBounds = ((x1 > -1 && x1 < this.ClientSize.Width) &&
                                 (y1 > -1 && y1 < this.ClientSize.Height)) ||
                                 ((x2 > -1 && x2 < this.ClientSize.Width) &&
                                 (y2 > -1 && y2 < this.ClientSize.Height));
                if (inBounds)
                {
                    this.graphics.DrawLine(this.graphLinePen, x1, y1, x2, y2);
                    linesDrawn++;
                }
            }
            Console.WriteLine(linesDrawn);
        }
    }

    public class GraphSettings
    {
        public GraphSettings()
        {
            this.XAxisRange = new PointF(-10f, 10f);
            this.YAxisRange = new PointF(-10f, 10f);
            this.XValueRange = new PointF(-100, 100);
            this.PointsToGraph = 800;
            this.LineThickness = 1;
            this.XTickMarkCount = 20;
            this.YTickMarkCount = 20;
            this.TickMarkWidth = 2;
            this.ShowTickMarks = true;
            this.ShowXAxis = true;
            this.ShowYAxis = true;
        }

        public PointF XAxisRange { get; set; }

        public PointF YAxisRange { get; set; }

        public PointF XValueRange { get; set; }

        public int PointsToGraph { get; set; }

        public int LineThickness { get; set; }

        public bool ShowTickMarks { get; set; }

        public int TickMarkWidth { get; set; }

        public int XTickMarkCount { get; set; }

        public int YTickMarkCount { get; set; }

        public bool ShowGridLines { get; set; }

        public bool ShowXAxis { get; set; }

        public bool ShowYAxis { get; set; }
    }
}
