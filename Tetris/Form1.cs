using System;
using System.Windows.Forms;
using System.Media;
using System.Drawing;

namespace Tetris
{
    public partial class Form1 : Form
    {
        Shape currentShape;
        Shape nextShape;
        Timer gameTimer = new Timer();
        private SoundPlayer gameOver;
        private SoundPlayer lineCleared;
        private SoundPlayer blockPlaced;
        public Form1()
        {
            InitializeComponent();
                        
            loadCanvas();

            lineCleared = new SoundPlayer(@"C:\Users\User\Downloads\Correct2.wav");
            gameOver = new SoundPlayer(@"C:\Users\User\Downloads\gameOver.wav");
            blockPlaced = new SoundPlayer(@"C:\Users\User\Downloads\blockPlaced.wav");

            currentShape = getRandomShapeWithCenterAligned();
            nextShape = getNextShape();

            gameTimer.Tick += Timer_Tick;
            gameTimer.Interval = 500;
            gameTimer.Start();
            retryLabel.Hide();
            exitLabel.Hide();
            gameOverLabel.Hide();   
            
            this.KeyDown += Form1_KeyDown;
            speedLabel.Text = "Interval: " + gameTimer.Interval.ToString() + " ms";
        }
        
        Bitmap canvasBitmap;
        Graphics canvasGraphics;
        int canvasWidth = 15;
        int canvasHeight = 20;
        int[,] canvasDotArray;
        int dotSize = 20;
        private void loadCanvas()
        {
            //Resize the picture box based on the dotsize and canvas size
            pictureBox1.Width = canvasWidth * dotSize;
            pictureBox1.Height = canvasHeight * dotSize;

            //Create Bitmap with picture box's size
            canvasBitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            canvasGraphics = Graphics.FromImage(canvasBitmap);

            canvasGraphics.FillRectangle(Brushes.Black, 0, 0, canvasBitmap.Width, canvasBitmap.Height);

            //Load bitmap into picture box
            pictureBox1.Image = canvasBitmap;
            
            //Initialize canvas dot array. Elements are Zero by default
            canvasDotArray = new int[canvasWidth, canvasHeight];
        }
        private void resetCanvas()
        {
            loadCanvas();
            gameTimer.Start();
            retryLabel.Hide();
            exitLabel.Hide();   
            gameOverLabel.Hide();
            linesLabel.Text = "Lines Cleared: " + "0";
            levelLabel.Text = "Current Level: " + "0";
            speedLabel.Text = "Interval: " + gameTimer.Interval.ToString() + " ms";
        }
        int currentX;
        int currentY;

        private Shape getRandomShapeWithCenterAligned()
        {
            var shape = ShapesHandler.GetRandomShape();
            
            //Calculate the x and y values as if the shape lies in the center
            currentX = 7;
            currentY = -shape.Height;

            return shape;
        }

        Bitmap workingBitmap;
        Graphics workingGraphics;
        private void Timer_Tick(object sender, EventArgs e)
        {
            var movePossible = moveShapeIfPossible(moveDown: 1);

            //If shape reached the bottom or touched any other shapes
            if (!movePossible)
            {
                //Copy working image
                canvasBitmap = new Bitmap(workingBitmap);

                updateCanvasDotArrayWithCurrentShape();

                //Get next shape
                currentShape = nextShape;
                nextShape = getNextShape();
                
                clearFilledRowsAndUpdateScore();
            }
        }

        //Updates blocks postition in array
        private void updateCanvasDotArrayWithCurrentShape()
        {
            try
            {
                for (int i = 0; i < currentShape.Width; i++)
                {
                    for (int j = 0; j < currentShape.Height; j++)
                    {
                        if (currentShape.Dots[j, i] == 1)
                        {
                            checkIfGameOver();

                            canvasDotArray[currentX + i, currentY + j] = 1;

                            blockPlaced.Play();
                        }
                    }
                }
            }
            catch 
            {
                return;
            }
        }

        private void checkIfGameOver()
        {
            if (currentY < 0)
            {
                gameTimer.Stop();
                gameOverLabel.Show();
                retryLabel.Show();
                exitLabel.Show();
                gameOver.Play();
            }
        }

        //Checks for invalid rotation or movements
        private bool moveShapeIfPossible(int moveDown = 0, int moveSide = 0)
        {
            var newX = currentX + moveSide;
            var newY = currentY + moveDown;

            //Checks sidebars
            if (newX < 0 || newX + currentShape.Width > canvasWidth
                || newY + currentShape.Height > canvasHeight)
                return false;

            //Checks for surrounding shapes
            for (int i = 0; i < currentShape.Width; i++)
            {
                for (int j = 0; j < currentShape.Height; j++)
                {
                    if (newY + j > 0 && canvasDotArray[newX + i, newY + j] == 1 && currentShape.Dots[j, i] == 1)
                        return false;
                }
            }

            currentX = newX;
            currentY = newY;

            drawShape();

            return true;
        }

        private Brush PickBrush()
        {
            Random rnd = new Random();
            Brush[] brushes = new Brush[]
            {
                Brushes.Red,
                Brushes.Blue,
                Brushes.Magenta,
                Brushes.Yellow,
                Brushes.Green,
                Brushes.Cyan
            };

            Brush brush = brushes[rnd.Next(brushes.Length)];

            return brush;

        }
        private void drawShape()
        {
            //Controls downwards movement of shapes
            workingBitmap = new Bitmap(canvasBitmap);
            workingGraphics = Graphics.FromImage(workingBitmap);

            for (int i = 0; i < currentShape.Width; i++)
            {
  
                for (int j = 0; j < currentShape.Height; j++)
                {
                    if (currentShape.Dots[j, i] == 1)
                    workingGraphics.FillRectangle(PickBrush(), (currentX + i) * dotSize, (currentY + j) * dotSize, dotSize, dotSize);
                }
            }

            pictureBox1.Image = workingBitmap;
        }
        
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            var verticalMove = 0;
            var horizontalMove = 0;

            switch (e.KeyCode)
            {
                //Move block left
                case Keys.Left:
                    verticalMove--;
                    break;
                
                //Move block right
                case Keys.Right:
                    verticalMove++;
                    break;

                //Accelerate block down
                case Keys.Down:
                    horizontalMove++;
                    break;

                //Rotate block clockwise
                case Keys.Up:
                    currentShape.turnClockwise();
                    break;
               
                //Reset Canvas
                case Keys.Enter:
                     resetCanvas();
                     break;

                //Exit
                case Keys.Escape:
                     Application.Exit();
                     break;

                default:
                    return;
            }

            var isMoveSuccess = moveShapeIfPossible(horizontalMove, verticalMove);

            //If Shape movement is not possible rollback to previous state
            if (!isMoveSuccess && e.KeyCode == Keys.Up)
                currentShape.rollback();
        }

        int score;
        public void clearFilledRowsAndUpdateScore()
        {
            //Checks each row
            for (int i = 0; i < canvasHeight; i++)
            {
                int j;
                for (j = canvasWidth - 1; j >= 0; j--)
                {
                    if (canvasDotArray[j, i] == 0)
                        break;
                }

                if (j == -1)
                {
                    score++;
                    linesLabel.Text = "Lines Cleared: " + score;
                    levelLabel.Text = "Current Level: " + score;

                    //Increase game timer for each line cleared
                    gameTimer.Interval -= 25;
                    speedLabel.Text = "Interval: " + gameTimer.Interval.ToString() +" ms";

                    //Update dot array based on check
                    for (j = 0; j < canvasWidth; j++)
                    {
                        for (int k = i; k > 0; k--)
                        {
                            canvasDotArray[j, k] = canvasDotArray[j, k - 1];
                        }

                        canvasDotArray[j, 0] = 0;
                        lineCleared.Play();
                    }
                }
            }

            //Draws panel based on updated array values
             for (int i = 0; i < canvasWidth; i++)
            {
                for (int j = 0; j < canvasHeight; j++)
                {
                    canvasGraphics = Graphics.FromImage(canvasBitmap);
                    canvasGraphics.FillRectangle(
                        canvasDotArray[i, j] == 1 ? PickBrush() : Brushes.Black,
                        i * dotSize, j * dotSize, dotSize, dotSize
                        );
                }
            }

            pictureBox1.Image = canvasBitmap;
        }

        Bitmap nextShapeBitmap;
        Graphics nextShapeGraphics;
        private Shape getNextShape()
        {
            var shape = getRandomShapeWithCenterAligned();

            //Display upcoming shape in side panel
            nextShapeBitmap = new Bitmap(7 * dotSize, 6 * dotSize);
            nextShapeGraphics = Graphics.FromImage(nextShapeBitmap);

            nextShapeGraphics.FillRectangle(Brushes.Black, 0, 0, nextShapeBitmap.Width, nextShapeBitmap.Height);

            //Center shape in side panel
            var startX = (7 - shape.Width) / 2;
            var startY = (6 - shape.Height) / 2;

            for (int i = 0; i < shape.Height; i++)
            {
                for (int j = 0; j < shape.Width; j++)
                {
                     nextShapeGraphics.FillRectangle(
                     shape.Dots[i, j] == 1 ? Brushes.White : Brushes.Black,
                     (startX + j) * dotSize, (startY + i) * dotSize, dotSize, dotSize);
                }
            }

            pictureBox2.Size = nextShapeBitmap.Size;
            pictureBox2.Image = nextShapeBitmap;

            return shape;
        }

        static class ShapesHandler
        {
            private static Shape[] shapesArray;

            static ShapesHandler()
            {
                //Create shapes add into the array.
                shapesArray = new Shape[]
                    {
                    new Shape {
                        Width = 2,
                        Height = 2,
                        Dots = new int[,]
                        {
                            { 1, 1 },
                            { 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 1,
                        Height = 4,
                        Dots = new int[,]
                        {
                            { 1 },
                            { 1 },
                            { 1 },
                            { 1 }
                        }
                    },
                    new Shape {
                        Width = 3,
                        Height = 2,
                        Dots = new int[,]
                        {
                            { 0, 1, 0 },
                            { 1, 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 2,
                        Height = 3,
                        Dots = new int[,]
                        {
                            { 0, 1,},
                            { 0, 1,},
                            { 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 2,
                        Height = 3,
                        Dots = new int[,]
                        {
                            { 1, 0 },
                            { 1, 0 },
                            { 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 3,
                        Height = 2,
                        Dots = new int[,]
                        {
                            { 1, 1, 0 },
                            { 0, 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 3,
                        Height = 2,
                        Dots = new int[,]
                        {
                            { 0, 1, 1 },
                            { 1, 1, 0 }
                        }
                    }
                    };
            }

            //Randomly obtain a shape in the array
            public static Shape GetRandomShape()
            {
                var shape = shapesArray[new Random().Next(shapesArray.Length)];

                return shape;
            }
        }
    }
}
