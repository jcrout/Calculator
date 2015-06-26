﻿//-----------------------------------------------------------------------
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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using JonUtility;
    using U = JonUtility.Utility;
    using CS = CalculatorSettings;
    using System.Collections;

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
            var btn = U.NewControl<Button>(this, "lol", 0, 0, 80, 22);

            btn.Click += (sender, e) =>
            {
                double randDouble = JonUtility.Diagnostics.GetRandomDouble(4);
                Constant[] constants = new Constant[3];
                constants[0] = new Constant("A", "55.7");
                constants[1] = new Constant("B", "3)");
                constants[2] = new Constant("C", "33.21");
                Equation[] equations = new Equation[1];
                var builder = new StringBuilder();
                for (int i = 0; i < 5; i++)
                {
                    builder.Append(JonUtility.Diagnostics.GetRandomNumber(0, 10));
                }
                //equations[0] = Equation.Create(@"  2.2.2 + .53.4 - .33.333.333.55.3. + 3.24.52.35 .57X.6  x^2 - -1   5* ***6 + 2.22 - " + builder.ToString(), constants);
                //equations[0] = Equation.Create(randDouble.ToString() + @"x^2 3 -   4 (2 x * -5 / 0)", constants);
                //equations[0] = Equation.Create(randDouble.ToString() + @"x^2 - .25x^3 + max(x, 3) - -2", constants);
                //equations[1] = Equation.Create(@"3log10(" + randDouble.ToString() + "X)", constants);
                //equations[0] = Equation.Create(@".6023x^4 - x^3 + x^2 - x + 1", constants);
                //equations[0] = Equation.Create(@"xround(.2555555X^2,2) - 2", constants);
                //equations[0] = Equation.Create(@".5B.5BX5.5 + 1.(X/B)X5.2max(5,x(3max(4,5)XX))3", constants);
                equations[0] = Equation.Create(@"5..3X(.X(+23max(..,3)))", constants);
                //equations[4] = Equation.Create(randDouble.ToString() + @"X", constants);
                Delegate[] delegates = null;
                try
                {
                    delegates = equations.Select(eq => EquationParser.ParseEquation(eq, null)).ToArray();
                }
                catch (AggregateException ex)
                {
                    var exceptions = ex.Flatten().InnerExceptions;
                    foreach (ParsingException exception in exceptions)
                    {
                        Console.WriteLine(exception.ParsingErrorMessage);
                    }
                    throw;
                }
                this.graphBox.Graph(delegates);
            };
            var btn2 = U.NewControl<Button>(this, "Clear", btn.Right + 10, 0, 80, 22);
            btn2.Click += (sender, e) =>
            {
                this.graphBox.Clear(true);
            };
        }
    }

    public class GraphData
    {
        private Delegate function;
        private PointD[] points;

        internal GraphData(Delegate function, PointD[] points)
        {
            this.function = function;
            this.points = points;
        }

        private GraphData()
        {
        }

        public Delegate Function
        {
            get
            {
                return this.function;
            }
        }

        public PointD[] Points
        {
            get
            {
                return this.points;
            }
        }
    }

    public enum GraphStatus
    {
        Idle = 0,
        Drawing = 1
    }

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
        private GraphStatus graphStatus = GraphStatus.Idle;
        private object statusUpdateLock = new object();

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

        public GraphStatus CurrentStatus
        {
            get
            {
                lock (this.statusUpdateLock)
                {
                    return this.graphStatus;
                }
            }

            protected set
            {
                lock (this.statusUpdateLock)
                {
                    this.graphStatus = value;
                }
            }
        }

        public GraphSettings Settings
        {
            get
            {
                return this.settings;
            }
        }

        public void Clear(bool redrawGrid = false)
        {
            this.Clear(true, redrawGrid);
        }

        public void Graph(IEnumerable<Delegate> functions)
        {
            // Disallow multiple calls to Graph
            lock (this.statusUpdateLock)
            {
                if (this.graphStatus != GraphStatus.Idle)
                {
                    return;
                }
                this.graphStatus = GraphStatus.Drawing;
            }

            this.OnGraph(functions);
        }

        public void Graph(Delegate function)
        {
            this.Graph(new Delegate[1] { function });
        }

        protected async virtual void OnGraph(IEnumerable<Delegate> functions)
        {
            long time1 = 0, time2 = 0;
            JonUtility.Diagnostics.QueryPerformanceCounter(ref time1);

            this.SetOrigin();
            this.Clear(false, false);

            var drawBaseGraphTask = Task.Run(() => this.DrawBaseGraph());
            var tasks = new List<Task>(functions.Count() + 1) { drawBaseGraphTask };

            foreach (Delegate function in functions)
            {
                Delegate func = function;
                var t2 = Task.Run(() => this.GetGraphData(func))
                    .ContinueWith(f => this.DrawGraphPoints(f.Result.Points));
                tasks.Add(t2);
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (AggregateException ex)
            {
                CS.Log.TraceError(ex);
            }

            this.Invoke(new Action(() => { this.Image = this.image; }));
            this.graphStatus = GraphStatus.Idle;

            JonUtility.Diagnostics.QueryPerformanceCounter(ref time2);
            this.Parent.Parent.Invoke(new Action(() => this.Parent.Parent.Text = "Time taken: " + U.TicksToMS(time2 - time1, 4)));
        }

        protected override void OnResize(EventArgs e)
        {
            if ((this.ClientSize.Width > 0) && (this.ClientSize.Height > 0))
            {
                if (this.graphics == null)
                {
                    this.image = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
                    this.graphics = Graphics.FromImage(this.image);
                }
                else
                {
                    lock (this.graphics)
                    {
                        this.image = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
                        this.graphics = Graphics.FromImage(this.image);
                    }
                }
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

        private async void Clear(bool refresh, bool redrawGrid)
        {
            lock (this.graphics)
            {
                this.graphics.Clear(this.BackColor);
            }

            if (redrawGrid)
            {
                await Task.Run(() => this.DrawBaseGraph());
            }

            if (refresh)
            {
                this.Refresh();
            }
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
                    double offset = Math.Abs(this.settings.YAxisRange.Y / rangeTotalY);
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

            if (this.settings.ShowXAxis)
                for (int i = 0; i < this.settings.LineThickness; i++)
                    this.DrawLine(this.axisPen, 0, upperLineY + i, this.ClientSize.Width, upperLineY + i);
            if (this.settings.ShowYAxis)
                for (int i = 0; i < this.settings.LineThickness; i++)
                    this.DrawLine(this.axisPen, upperLineX + i, 0, upperLineX + i, this.ClientSize.Height);
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
                this.DrawLine(this.ticksPen, currentX, lowerY, currentX, upperY);
            }

            int tickDistanceY = this.ClientSize.Height / this.settings.YTickMarkCount;
            int lowerX = this.origin.X + tickHalfWidth;
            int upperX = this.origin.X - tickHalfWidth;
            int tickOffsetY = this.origin.Y % tickDistanceX;
            for (int tickY = 0, currentY = tickOffsetY; tickY < this.settings.YTickMarkCount + 1; tickY++, currentY += tickDistanceY)
            {
                if (currentY == this.origin.Y) continue;
                this.DrawLine(this.ticksPen, lowerX, currentY, upperX, currentY);
            }
        }

        private GraphData GetGraphData(Delegate function)
        {
            var functionToEvaluate = (Func<double, double>)function;
            int pointCount = this.settings.PointsToGraph;
            double min = this.settings.XValueRange.X.IsNaNorInfinity() ? this.settings.XAxisRange.X - 1 : this.settings.XValueRange.X;
            double max = this.settings.XValueRange.Y.IsNaNorInfinity() ? this.settings.XAxisRange.Y + 1 : this.settings.XValueRange.Y;
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

            return new GraphData(function, points);
        }

        private void DrawGraphPoints(PointD[] points)
        {
            int pointCount = points.Length;
            int linesDrawn = 0;
            for (int i = 1; i < pointCount; i++)
            {
                var firstPoint = points[i - 1];
                var secondPoint = points[i];
                if (firstPoint.X.IsNaNorInfinity() || firstPoint.Y.IsNaNorInfinity() || secondPoint.X.IsNaNorInfinity() || secondPoint.Y.IsNaNorInfinity())
                {
                    continue;
                }

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
                    this.DrawLine(this.graphLinePen, x1, y1, x2, y2);
                    linesDrawn++;
                }
            }
        }

        private void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            lock (this.graphics)
            {
                this.graphics.DrawLine(pen, x1, y1, x2, y2);
            }
        }
    }

    public class GraphSettings
    {
        public GraphSettings()
        {
            this.XAxisRange = new PointD(-10f, 10f);
            this.YAxisRange = new PointD(-10f, 10f);
            this.XValueRange = new PointD(Double.NaN, Double.NaN);
            this.PointsToGraph = 400;
            this.LineThickness = 1;
            this.XTickMarkCount = 20;
            this.YTickMarkCount = 20;
            this.TickMarkWidth = 2;
            this.ShowTickMarks = true;
            this.ShowXAxis = true;
            this.ShowYAxis = true;
        }

        public PointD XAxisRange { get; set; }

        public PointD YAxisRange { get; set; }

        public PointD XValueRange { get; set; }

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
