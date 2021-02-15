using System.Drawing;
using System.Windows.Forms;

namespace Atomikh2
{
    public class Canvas : Panel
    {
        private Paint Form { get; }

        private PictureBox ResizeRight;
        private PictureBox ResizeDown;
        private PictureBox ResizeDiag;

        public Rectangle DrawRectangle { get; set; }
        public bool Resizing { get; set; }

        public Canvas(Paint form)
        {
            // Prevents flickering
            this.DoubleBuffered = true;

            Form = form;
            //FormGfx = form.CreateGraphics();
        }

        public void AddResizer()
        {
            // Create pictureBoxes and add them to the form
            ResizeRight = new PictureBox()
            {
                Size = new Size(5, 5),
                Location = new Point(this.Location.X + this.Width, this.Location.Y + (this.Height / 2)),
                Cursor = Cursors.SizeWE,
                BackColor = SystemColors.ControlLight,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "resizeRight"
            };
            ResizeRight.MouseDown += resize_MouseDown;
            ResizeRight.MouseUp += resize_MouseUp;
            ResizeRight.MouseMove += resize_MouseMove;
            Form.Controls.Add(ResizeRight);

            ResizeDown = new PictureBox()
            {
                Size = new Size(5, 5),
                Location = new Point(this.Location.X + (this.Width / 2), this.Location.Y + this.Height),
                Cursor = Cursors.SizeNS,
                BackColor = SystemColors.ControlLight,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "resizeDown"
            };
            ResizeDown.MouseDown += resize_MouseDown;
            ResizeDown.MouseUp += resize_MouseUp;
            ResizeDown.MouseMove += resize_MouseMove;
            Form.Controls.Add(ResizeDown);

            ResizeDiag = new PictureBox()
            {
                Size = new Size(5, 5),
                Location = new Point(this.Location.X + this.Width, this.Location.Y + this.Height),
                Cursor = Cursors.SizeNWSE,
                BackColor = SystemColors.ControlLight,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "resizeDiag"
            };
            ResizeDiag.MouseDown += resize_MouseDown;
            ResizeDiag.MouseUp += resize_MouseUp;
            ResizeDiag.MouseMove += resize_MouseMove;
            Form.Controls.Add(ResizeDiag);
        }

        private static Point startResize;
        private void resize_MouseDown(object sender, MouseEventArgs e)
        {
            Resizing = true;
            startResize = Cursor.Position;
        }

        private void resize_MouseMove(object sender, MouseEventArgs e)
        {
            if (Resizing)
            {
                Form.Invalidate();

                if (((PictureBox)sender).Name == "resizeRight")
                    DrawRectangle = new Rectangle(this.Location, new Size(this.Width + (Cursor.Position.X - startResize.X), this.Height));
                else if (((PictureBox)sender).Name == "resizeDown")
                    DrawRectangle = new Rectangle(this.Location, new Size(this.Width, this.Height + (Cursor.Position.Y - startResize.Y)));
                else DrawRectangle = new Rectangle(this.Location, new Size(this.Width + (Cursor.Position.X - startResize.X), this.Height + (Cursor.Position.Y - startResize.Y)));
            }
        }

        private void resize_MouseUp(object sender, MouseEventArgs e)
        {
            // Change Canvas size
            this.Size = DrawRectangle.Size;
            Form.Invalidate();
            Resizing = false;
        }

        public void UpdatePosition()
        {
            ResizeDown.Location = new Point(this.Location.X + (this.Width / 2), this.Location.Y + this.Height);
            ResizeRight.Location = new Point(this.Location.X + this.Width, this.Location.Y + (this.Height / 2));
            ResizeDiag.Location = new Point(this.Location.X + this.Width, this.Location.Y + this.Height);
        }
    }
}