using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace Atomikh2
{
    public partial class Paint : Form
    {
        // Resizable Canvas
        private readonly Canvas _canvas;

        // Tools
        private Color _color;
        private Color _color2;
        private int _size;
        private Pen _pen;
        private SolidBrush _sbrush;

        // Drawing
        private bool isDown;
        private int initMouseX;
        private int initMouseY;
        private int px;
        private int py;

        // Shape preview
        private static RectangleF resizeEllipse;
        private static Rectangle resizeRectangle;
        private static Point resizeLine1;
        private static Point resizeLine2;

        // Ready shapes
        private List<Move> Shape;
        private List<Move> Face;
        private List<Move> House;
        private List<Move> Tree;
        private List<Move> Unipi;
        private int index;

        // Graphics
        private Graphics _gfx;

        // Image for saving and drawing graphics
        private Bitmap _image;

        // Undo / Redo
        private List<Bitmap> _undo;
        private List<Bitmap> _redo;

        // DataBase
        private readonly string _connectionString;

        public Paint()
        {
            InitializeComponent();
            _connectionString = @"Data Source=database.db; Version=3;";

            // Initialize Canvas
            panel.Visible = false;
            panel.Enabled = false;

            _canvas = new Canvas(this)
            {
                Name = "canvas",
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ContextMenuStrip = rightClick,
                MinimumSize = new Size(200, 200),
                Size = panel.Size,
                Location = panel.Location
            };

            // Add Resizing option
            _canvas.AddResizer();

            // Add events
            _canvas.Paint += canvas_Paint;
            _canvas.MouseDown += canvas_MouseDown;
            _canvas.MouseMove += canvas_MouseMove;
            _canvas.MouseUp += canvas_MouseUp;
            _canvas.Click += canvas_Click;
            _canvas.SizeChanged += canvas_SizeChanged;

            this.Controls.Add(_canvas);
        }

        private void Paint_Load(object sender, EventArgs e)
        {
            // Initialize bottom bar
            labelCsize.Text = $"{_canvas.Size.Width} ☓ {_canvas.Size.Height}px";
            labelCursor.Text = "0,0px";

            // Initialize tools
            _color = Color.Black;
            _color2 = Color.LightGray;
            _pen = new Pen(Color.Black, 2);
            _sbrush = new SolidBrush(Color.Black);

            // Initialize toolbox
            radioButtonPen.Select();
            sizeUpDown.Value = 10;
            checkBoxColor1.Checked = true;

            // Create image and graphics
            _image = new Bitmap(_canvas.Width, _canvas.Height);
            _gfx = Graphics.FromImage(_image);
            _gfx.Clear(Color.White);

            // initialize Undo / Redo lists
            _undo = new List<Bitmap>();
            _redo = new List<Bitmap>();
            SaveCanvas(); // save blank canvas

            // Initialize ready shape lists
            UpdateReady();

            // Smoother drawings
            _gfx.SmoothingMode = SmoothingMode.AntiAlias;
        }

        // Draw canvas resize rectangle
        private void Paint_Paint(object sender, PaintEventArgs e)
        {
            var gfx = e.Graphics;
            if (_canvas.Resizing)
            {
                gfx.DrawRectangle(new Pen(Color.Gray, 2), _canvas.DrawRectangle);
                labelCsize.Text = $"{_canvas.DrawRectangle.Width} ☓ {_canvas.DrawRectangle.Height}px";
            }
        }

        // Function that logs user's drawings
        private void SaveToDatabase(string shape)
        {
            var timeStamp = DateTime.Now;
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var insert = $@"Insert into Logs(Shape, Timestamp) Values('{shape}','{timeStamp:dd/MM/yyyy HH:mm:ss}');";
                var cmd = new SQLiteCommand(insert, conn);
                var changes = cmd.ExecuteNonQuery();
            }
        }

        /*
         * _gfx draws the graphics in an image at the size of the canvas. And draws the image to the canvas using onPaint's graphics
         * every time the panel invalidates or when we call DrawCanvas()
         * This makes it so the drawings the user drawn don't disappear when the application window goes out of screen or when the auto scroll needs to be used
         */
        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            var gfx = e.Graphics;
            gfx.SmoothingMode = SmoothingMode.AntiAlias;
            gfx.DrawImage(_image, 0, 0); // Draw canvas image

            // if the user is drawing a rectangle, a circle or a line show the drawing progress of the shape
            if (isDown)
            {
                if (radioButtonCircle.Checked) // circle
                {
                    if (checkBoxFill.Checked)
                    {
                        gfx.FillEllipse(new SolidBrush(_color2), resizeEllipse);
                        gfx.DrawEllipse(_pen, resizeEllipse);
                    }
                    else gfx.DrawEllipse(_pen, resizeEllipse);
                }
                else if (radioButtonLine.Checked) // line
                {
                    gfx.DrawLine(_pen, resizeLine1, resizeLine2);
                }
                else if (radioButtonRectangle.Checked) // Rectangle
                {
                    if (checkBoxFill.Checked)
                    {
                        gfx.FillRectangle(new SolidBrush(_color2), resizeRectangle);
                        gfx.DrawRectangle(_pen, resizeRectangle);
                    }
                    else gfx.DrawRectangle(_pen, resizeRectangle);
                }
            }
        }

        // Triggers onPaint event
        private void DrawCanvas()
        {
            _canvas.Invalidate();
        }

        private void canvas_SizeChanged(object sender, EventArgs e)
        {
            // Update canvas graphics
            labelCsize.Text = $"{_canvas.Width} ☓ {_canvas.Height}px";

            // Change Resizing buttons location
            _canvas.UpdatePosition();

            // Change image to be the same size as the canvas
            var temp = _image;
            _image = new Bitmap(_canvas.Width, _canvas.Height);
            _gfx = Graphics.FromImage(_image);
            _gfx.Clear(Color.White);
            _gfx.DrawImage(temp, 0, 0);
            _gfx.SmoothingMode = SmoothingMode.AntiAlias;

            UpdateReady();
        }

        /*
         * Color selection
         */

        // Color dialog
        private void pictureBoxColor_Click(object sender, EventArgs e)
        {
            if (SelectColor.ShowDialog() == DialogResult.OK)
            {
                pictureBoxColor.BackColor = SelectColor.Color;
                _color = SelectColor.Color;
                _pen.Color = SelectColor.Color;
                _sbrush.Color = SelectColor.Color;
            }
        }

        private void fillColorChange_click(object sender, EventArgs e)
        {
            if (SelectColor.ShowDialog() == DialogResult.OK)
            {
                fillColor.BackColor = SelectColor.Color;
                _color2 = SelectColor.Color;
            }
        }

        // Small ready colors
        private void change_Color(object sender, EventArgs e)
        {
            var colorBox = (PictureBox)sender;
            if (checkBoxColor1.Checked)
            {
                pictureBoxColor.BackColor = colorBox.BackColor;
                _color = colorBox.BackColor;
                _sbrush.Color = colorBox.BackColor;
                _pen.Color = colorBox.BackColor;
            }
            else
            {
                fillColor.BackColor = colorBox.BackColor;
                _color2 = colorBox.BackColor;
            }
        }

        /*
         * Drawing on canvas and image
         */
        private void canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return; // If key pressed isn't left click

            // Save the coordinates of the mouseDown
            initMouseX = e.X;
            initMouseY = e.Y;

            // FreeStyle Pen
            px = e.X;
            py = e.Y;

            isDown = true;
            if (radioButtonWords.Checked) // String typer
            {
                _gfx.DrawString(textBoxWord.Text, SelectFont.Font, _sbrush, e.X, e.Y); // Perm
                DrawCanvas();
            }
            else if (radioButtonPen.Checked) // Pen
            {
                _gfx.FillEllipse(_sbrush, new Rectangle(e.X - _size / 2, e.Y - _size / 2, _size, _size));
                DrawCanvas();
            }
            else if (radioButtonEraser.Checked) // Eraser
            {
                _gfx.FillEllipse(new SolidBrush(Color.White), new Rectangle(e.X - _size / 2, e.Y - _size / 2, _size, _size));
                DrawCanvas();
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            labelCursor.Text = $"{e.Location.X},{e.Location.Y}px";

            // Freestyle
            if (isDown)
            {
                if (radioButtonPen.Checked) //pen
                {
                    _gfx.FillEllipse(_sbrush, new Rectangle(e.X - _size / 2, e.Y - _size / 2, _size, _size));
                    _gfx.DrawLine(_pen, new Point(px, py), new Point(e.X, e.Y));
                    DrawCanvas();
                    px = e.X;
                    py = e.Y;
                }
                else if (radioButtonEraser.Checked) // Eraser
                {
                    _gfx.FillEllipse(new SolidBrush(Color.White), new Rectangle(e.X - _size / 2, e.Y - _size / 2, _size, _size));
                    _gfx.DrawLine(new Pen(Color.White, _size), new Point(px, py), new Point(e.X, e.Y));
                    DrawCanvas();
                    px = e.X;
                    py = e.Y;
                }
                else if (radioButtonCircle.Checked) // Circle
                {
                    DrawCanvas();
                    resizeEllipse = new RectangleF(new PointF(initMouseX, initMouseY),
                            new SizeF(e.X - initMouseX, e.Y - initMouseY));
                }
                else if (radioButtonLine.Checked) // line
                {
                    DrawCanvas();
                    resizeLine1 = new Point(initMouseX, initMouseY);
                    resizeLine2 = new Point(e.X, e.Y);
                }
                else if (radioButtonRectangle.Checked) // Rectangle
                {
                    DrawCanvas();
                    // Down && Right movement
                    if (initMouseX < e.X && initMouseY < e.Y) resizeRectangle = new Rectangle(new Point(initMouseX, initMouseY), new Size(e.X - initMouseX, e.Y - initMouseY));

                    // Down && Left movement
                    else if (initMouseX > e.X && initMouseY < e.Y) resizeRectangle = new Rectangle(new Point(e.X, e.Y - (e.Y - initMouseY)), new Size(initMouseX - e.X, e.Y - initMouseY));

                    // Up && Right movement
                    else if (initMouseX < e.X && initMouseY > e.Y) resizeRectangle = new Rectangle(new Point(initMouseX, initMouseY - (initMouseY - e.Y)), new Size(e.X - initMouseX, initMouseY - e.Y));

                    // Up && Left movement
                    else if (initMouseX > e.X && initMouseY > e.Y) resizeRectangle = new Rectangle(new Point(e.X, e.Y), new Size(initMouseX - e.X, initMouseY - e.Y));
                }
            }
        }

        private void canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return; // If key pressed isn't the left click

            if (isDown)
            {   // Draws rectangles depending the mouse movement the user made
                if (radioButtonRectangle.Checked)
                {
                    // Down && Right movement
                    if (initMouseX < e.X && initMouseY < e.Y)
                    {
                        if (checkBoxFill.Checked)
                        {
                            _gfx.FillRectangle(new SolidBrush(_color2), new Rectangle(new Point(initMouseX, initMouseY), new Size(e.X - initMouseX, e.Y - initMouseY)));
                            _gfx.DrawRectangle(_pen, new Rectangle(new Point(initMouseX, initMouseY), new Size(e.X - initMouseX, e.Y - initMouseY)));
                        }
                        else
                        {
                            _gfx.DrawRectangle(_pen, new Rectangle(new Point(initMouseX, initMouseY), new Size(e.X - initMouseX, e.Y - initMouseY)));
                        }
                    }
                    // Down && Left movement
                    else if (initMouseX > e.X && initMouseY < e.Y)
                    {
                        if (checkBoxFill.Checked)
                        {
                            _gfx.FillRectangle(new SolidBrush(_color2), new Rectangle(new Point(e.X, e.Y - (e.Y - initMouseY)), new Size(initMouseX - e.X, e.Y - initMouseY)));
                            _gfx.DrawRectangle(_pen, new Rectangle(new Point(e.X, e.Y - (e.Y - initMouseY)), new Size(initMouseX - e.X, e.Y - initMouseY)));
                        }
                        else
                        {
                            _gfx.DrawRectangle(_pen, new Rectangle(new Point(e.X, e.Y - (e.Y - initMouseY)), new Size(initMouseX - e.X, e.Y - initMouseY)));
                        }
                    }
                    // Up && Right movement
                    else if (initMouseX < e.X && initMouseY > e.Y)
                    {
                        if (checkBoxFill.Checked)
                        {
                            _gfx.FillRectangle(new SolidBrush(_color2), new Rectangle(new Point(initMouseX, initMouseY - (initMouseY - e.Y)), new Size(e.X - initMouseX, initMouseY - e.Y)));
                            _gfx.DrawRectangle(_pen, new Rectangle(new Point(initMouseX, initMouseY - (initMouseY - e.Y)), new Size(e.X - initMouseX, initMouseY - e.Y)));
                        }
                        else
                        {
                            _gfx.DrawRectangle(_pen, new Rectangle(new Point(initMouseX, initMouseY - (initMouseY - e.Y)), new Size(e.X - initMouseX, initMouseY - e.Y)));
                        }
                    }
                    // Up && Left movement
                    else if (initMouseX > e.X && initMouseY > e.Y)
                    {
                        if (checkBoxFill.Checked)
                        {
                            _gfx.FillRectangle(new SolidBrush(_color2), new Rectangle(new Point(e.X, e.Y), new Size(initMouseX - e.X, initMouseY - e.Y)));
                            _gfx.DrawRectangle(_pen, new Rectangle(new Point(e.X, e.Y), new Size(initMouseX - e.X, initMouseY - e.Y)));
                        }
                        else
                        {
                            _gfx.DrawRectangle(_pen, new Rectangle(new Point(e.X, e.Y), new Size(initMouseX - e.X, initMouseY - e.Y)));
                        }
                    }
                    SaveToDatabase("Rectangle");
                }
                else if (radioButtonCircle.Checked)
                {
                    if (checkBoxFill.Checked)
                    {
                        _gfx.FillEllipse(new SolidBrush(_color2), new RectangleF(new PointF(initMouseX, initMouseY), new SizeF(e.X - initMouseX, e.Y - initMouseY)));
                        _gfx.DrawEllipse(_pen, new RectangleF(new PointF(initMouseX, initMouseY), new SizeF(e.X - initMouseX, e.Y - initMouseY)));
                    }
                    else
                    {
                        _gfx.DrawEllipse(_pen, new RectangleF(new PointF(initMouseX, initMouseY), new SizeF(e.X - initMouseX, e.Y - initMouseY)));
                    }
                    SaveToDatabase("Circle/Ellipse");
                }
                else if (radioButtonLine.Checked)
                {
                    _gfx.DrawLine(_pen, new Point(initMouseX, initMouseY), new Point(e.X, e.Y));
                    SaveToDatabase("Line");
                }

                SaveCanvas();
                isDown = false;
            }
        }

        /*
         * MENU BAR
         */

        // Function that saves a copy of the current canvas image to the undo list
        private void SaveCanvas()
        {
            var save = new Bitmap(_image);
            _undo.Add(save);
        }

        // Undo / Redo
        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Text == "Undo")
            {
                _gfx.Clear(Color.White);
                try
                {
                    _redo.Add(_undo.Last());
                    _undo.RemoveAt(_undo.Count - 1);

                    _canvas.Size = _undo.Last().Size;
                    _gfx.DrawImage(_undo.Last(), new Point(0, 0));
                    DrawCanvas();
                }
                catch (Exception)
                {
                }
            }
            else if (e.ClickedItem.Text == "Redo")
            {
                _gfx.Clear(Color.White);
                try
                {
                    _undo.Add(_redo.Last());
                    _redo.RemoveAt(_redo.Count - 1);

                    _canvas.Size = _undo.Last().Size;
                    _gfx.DrawImage(_undo.Last(), new Point(0, 0));
                    DrawCanvas();
                }
                catch (Exception)
                {
                }
            }
        }

        // Save / Load
        private void fileToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var clicked = e.ClickedItem;
            if (clicked.Text == "Save")
            {
                if (SaveImage.ShowDialog() == DialogResult.OK)
                {
                    _image.Save(SaveImage.FileName, ImageFormat.Jpeg);
                }
            }
            else // Load
            {
                if (SelectPicture.ShowDialog() == DialogResult.OK)
                {
                    var imageTemp = new Bitmap(SelectPicture.FileName);
                    if (imageTemp.Width >= 1920 || imageTemp.Height >= 1080)
                    {
                        imageTemp = new Bitmap(imageTemp, new Size(1920, 1080));
                    }
                    _canvas.Size = imageTemp.Size;
                    _gfx.DrawImage(imageTemp, new Point(0, 0));
                    SaveCanvas();
                    DrawCanvas();
                }
            }
        }

        /*
         * TOOLS
         */
        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e) // Right click on canvas -> clear canvas
        {
            _gfx.Clear(Color.White);
            SaveCanvas();
            DrawCanvas();
        }

        // Bucket/Fill
        private void canvas_Click(object sender, EventArgs e)
        {
            if (radioButtonFill.Checked)
            {
                _gfx.Clear(_color);
                SaveCanvas();
                DrawCanvas();
            }
        }

        // Pen size
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            _size = (int)sizeUpDown.Value;
            _pen.Width = (int)sizeUpDown.Value;
        }

        // Deselect any shape checked & Draw string
        private void tool_Click(object sender, EventArgs e)
        {
            foreach (var radioButton in panelShapes.Controls.OfType<RadioButton>())
            {
                if (radioButton.Checked) radioButton.Checked = false;
            }

            var tool = (RadioButton)sender;
            if (tool.Name == "radioButtonWords")
            {
                if (SelectFont.ShowDialog() != DialogResult.OK)
                {
                    tool.Checked = false;
                }
            }
        }

        // Deselect any tool checked
        private void shape_click(object sender, EventArgs e)
        {
            foreach (var radioButton in panelTools.Controls.OfType<RadioButton>())
            {
                if (radioButton.Checked) radioButton.Checked = false;
            }
        }

        /*
         * Auto Draw
         */
        private void autoDraw_Click(object sender, EventArgs e)
        {
            if (((Button)sender).Name == "drawHouse") Shape = House;
            if (((Button)sender).Name == "drawFace") Shape = Face;
            if (((Button)sender).Name == "drawTree") Shape = Tree;
            if (((Button)sender).Name == "drawWord") Shape = Unipi;
            if (radioButtonWords.Checked)
            {
                radioButtonWords.Checked = false;
                radioButtonPen.Checked = true;
            }
            panelDraw.Enabled = false;
            timerDrawReady.Start();
        }

        private void timerDrawReady_Tick(object sender, EventArgs e)
        {
            Shape[index++].Draw();
            DrawCanvas();

            if (index >= Shape.Count)
            {
                index = 0;
                panelDraw.Enabled = true;

                SaveCanvas();
                timerDrawReady.Stop();
            }
        }

        private void UpdateReady()
        {
            var houseWidth = _canvas.Width / 3;
            House = new List<Move> // Scalable house
            {
                new Move(_gfx, new SolidBrush(Color.Yellow), new Rectangle(new Point(_canvas.Width / 2 - houseWidth / 2,_canvas.Height - _canvas.Height / 2),
                    new Size(houseWidth, _canvas.Height - (_canvas.Height - _canvas.Height / 2)))),
                new Move(_gfx, new SolidBrush(Color.Brown),new Point[]
                {
                    new Point(_canvas.Width / 2 - houseWidth / 2,_canvas.Height - _canvas.Height / 2),
                    new Point(_canvas.Width / 2 - houseWidth / 2 + houseWidth/2,_canvas.Height - _canvas.Height / 2 * 2),
                    new Point(_canvas.Width / 2 - houseWidth / 2 + houseWidth,_canvas.Height - _canvas.Height / 2),
                    new Point(_canvas.Width / 2 - houseWidth / 2,_canvas.Height - _canvas.Height / 2),
                    new Point(_canvas.Width / 2 - houseWidth / 2 + houseWidth,_canvas.Height - _canvas.Height / 2)
                }),
                new Move(_gfx, new SolidBrush(Color.Black), new Rectangle(new Point(_canvas.Width / 2 - houseWidth / 8,_canvas.Height - _canvas.Height / 4),
                    new Size(houseWidth / 4, _canvas.Height - (_canvas.Height - _canvas.Height / 2)))),
            };
            Tree = new List<Move>
            {
                new Move(_gfx, new SolidBrush(Color.Brown), new Rectangle(new Point(_canvas.Width / 2 - 10,_canvas.Height - 100), new Size(20, 100))),
                new Move(_gfx, new SolidBrush(Color.Green), new RectangleF(new PointF(_canvas.Width / 2 - 40,_canvas.Height - 200), new SizeF(80, 110))),
                new Move(_gfx, new Pen(Color.Brown, 4) , new Point(_canvas.Width / 2 - 10 , _canvas.Height - 60), new Point(_canvas.Width / 2 - 50, _canvas.Height - 80)),
            };
            Face = new List<Move>
            {
                new Move(_gfx, new SolidBrush(Color.Yellow), new RectangleF(new PointF(_canvas.Width/2 - 50,_canvas.Height/2 - 50), new SizeF(100, 100))),
                new Move(_gfx, new SolidBrush(Color.Black), new RectangleF(new PointF(_canvas.Width/2 - 40,_canvas.Height/2 - 20), new SizeF(20, 20))),
                new Move(_gfx, new SolidBrush(Color.Black), new RectangleF(new PointF(_canvas.Width/2 + 20 ,_canvas.Height/2 - 20), new SizeF(20, 20))),
                new Move(_gfx, new SolidBrush(Color.Red), new RectangleF(new PointF(_canvas.Width/2 - 25 ,_canvas.Height/2 + 10), new SizeF(50, 10))),
            };
            Unipi = new List<Move>
            {
                new Move(_gfx, new SolidBrush(Color.CornflowerBlue), _canvas.Width/2 - 40, _canvas.Height/2 - 20 , "U"),
                new Move(_gfx, new SolidBrush(Color.Red), _canvas.Width/2 - 20, _canvas.Height/2 - 20 ,            "N"),
                new Move(_gfx, new SolidBrush(Color.Green), _canvas.Width/2, _canvas.Height/2 - 20 ,               "I"),
                new Move(_gfx, new SolidBrush(Color.Orange), _canvas.Width/2 + 20,_canvas.Height/2 - 20 ,          "P"),
                new Move(_gfx, new SolidBrush(Color.Blue), _canvas.Width/2 + 40, _canvas.Height/2 - 20 ,           "I"),
            };
        }

        private void checkBoxColor1_Click(object sender, EventArgs e)
        {
            checkBoxColor2.Checked = false;
        }

        private void checkBoxColor2_Click(object sender, EventArgs e)
        {
            checkBoxColor1.Checked = false;
        }
    }
}