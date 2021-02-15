using System.Drawing;

namespace Atomikh2
{
    public class Move
    {
        private Graphics Gfx { get; }

        // Tools
        private Pen Pen { get; }
        private SolidBrush Brush { get; }
        private string Text { get; }

        // Line properties
        private Point P1 { get; }
        private Point P2 { get; }

        // String properties
        private float P1F { get; }
        private float P2F { get; }
        private Point[] PointAr { get; }

        // Rectangle properties
        private Rectangle Rec { get; }
        private RectangleF RecF { get; }

        private int Shape { get; }

        /// <summary>
        /// Create a line
        /// </summary>
        public Move(Graphics gfx, Pen pen, Point p1, Point p2)
        {
            Pen = pen;
            P1 = p1;
            P2 = p2;
            Gfx = gfx;
            Shape = 0;
        }

        /// <summary>
        /// Create a rectangle
        /// </summary>
        public Move(Graphics gfx, Pen pen, Rectangle rec)
        {
            Pen = pen;
            Rec = rec;
            Gfx = gfx;
            Shape = 1;
        }

        /// <summary>
        /// Create a filled rectangle
        /// </summary>
        public Move(Graphics gfx, SolidBrush brush, Rectangle rec)
        {
            Brush = brush;
            Rec = rec;
            Gfx = gfx;
            Shape = 2;
        }

        /// <summary>
        /// Create a filled ellipse
        /// </summary>
        public Move(Graphics gfx, SolidBrush brush, RectangleF recf)
        {
            Brush = brush;
            RecF = recf;
            Gfx = gfx;
            Shape = 3;
        }

        /// <summary>
        /// Create a string
        /// </summary>
        public Move(Graphics gfx, SolidBrush brush, float f1, float f2, string text)
        {
            Text = text;
            Gfx = gfx;
            Brush = brush;
            P1F = f1;
            P2F = f2;
            Shape = 4;
        }

        /// <summary>
        /// Create a filled polygon
        /// </summary>
        public Move(Graphics gfx, SolidBrush brush, Point[] pointAr)
        {
            Gfx = gfx;
            Brush = brush;
            Shape = 5;
            PointAr = pointAr;
        }

        public void Draw()
        {
            if (Shape == 0) Gfx.DrawLine(Pen, P1, P2);
            else if (Shape == 1) Gfx.DrawRectangle(Pen, Rec);
            else if (Shape == 2) Gfx.FillRectangle(Brush, Rec);
            else if (Shape == 3) Gfx.FillEllipse(Brush, RecF);
            else if (Shape == 4) Gfx.DrawString(Text, new Font("Comic Sans MS", 15f), Brush, P1F, P2F);
            else if (Shape == 5) Gfx.FillPolygon(Brush, PointAr);
        }
    }
}