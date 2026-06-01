// step 0: adapt target framework to net10.0 in the project file: net 10.0 windows 10.0.19041.0
// step 2: import RawGameController with using;

using Timer = System.Windows.Forms.Timer;
using Windows.Gaming.Input;

namespace _28_Snake;

public partial class Form1 : Form
{
    const int Rows = 29;
    const int Cols = 29;
    const int ElemSize = 20;
    const int StartMovingInterval = 100;
    const int StepsToFirstApple = Cols / 4;

    readonly static Random rng = new();
    // Snake
    Panel head = new Panel() { BackColor = Color.Green, Size = new Size(ElemSize, ElemSize) };
    Queue<Panel> tail = new Queue<Panel>();
    MovingDirection SnakeMovingDirection = MovingDirection.None;
    
    // Futter
    Panel apple = new Panel() { BackColor = Color.Red, Size = new Size(ElemSize, ElemSize) };

    Timer tmrMoveSnake = new Timer() { Interval = StartMovingInterval, Enabled = true };

    // step 2: add field for game controller and timer for data input of controller
    RawGameController? controller = null;
    Timer tmrPollControllerStatus = new Timer() { Interval = 16, Enabled = true }; // 60 FPS
    public Form1()
    {
        // Raster des Spiels erzeugen --> Grösse des Soielfeldes an den Raster anpassen.
        Text = "Snake";
        Width = Cols * ElemSize + (Width - ClientSize.Width); // Rand muss zum Raster dazugezählt werden
        Height = Rows * ElemSize + (Height - ClientSize.Height);

        // Snake mittig Positionieren
        GameInit();

        KeyDown += Form1_KeyDown;
        tmrMoveSnake.Tick += TmrMoveSnake_Tick;

        // step 3: register for controller events and tmrPollControllerStatus: status
        RawGameController.RawGameControllerAdded += RawGameController_RawGameControllerAdded;
        RawGameController.RawGameControllerRemoved += RawGameController_RawGameControllerRemoved;

        tmrPollControllerStatus.Tick += TmrPollControllerStatus_Tick;
    }

    private void TmrPollControllerStatus_Tick(object? sender, EventArgs e)
    {
        if (controller != null)
        {
            int numOfAxis = Math.Max(0, controller.AxisCount);
            int numOfButtons = Math.Max(0, controller.ButtonCount);

            double[] axisValues = new double[numOfAxis];

            // auslesen des aktuellen Status des Controllers: Buttons, Switches, Achsen-Werte auslesen
            controller.GetCurrentReading(null, null, axisValues);

            if (numOfAxis > 1)
            {
                axisValues[0] = NormalizeAxisValue(axisValues[0]);  // -1 ... 1
                axisValues[1] = NormalizeAxisValue(axisValues[1]);
                // 0 --> 1 d.h. nach links
                // -0.3 bis 0.3 --> 0 d.h. nichts gedrückt
                // 1 --> 1 d.h. nach rechts
                double deadzone = 0.3;
                //if (axisValues[0] > -deadzone && axisValues[0] < deadzone)
                //{
                //    axisValues[0] = 0;
                //}
                //if (axisValues[1] > -deadzone && axisValues[1] < deadzone)
                //{
                //    axisValues[1] = 0;
                //}

                if (axisValues[0] < -deadzone)
                    SnakeMovingDirection = MovingDirection.Left;
                if (axisValues[0] > deadzone)
                    SnakeMovingDirection = MovingDirection.Right;
                if (axisValues[1] < -deadzone)
                    SnakeMovingDirection = MovingDirection.Up;
                if (axisValues[1] > deadzone)
                    SnakeMovingDirection = MovingDirection.Down;
            }
        }
    }

    private double NormalizeAxisValue(double value)
    {
        if (double.IsNaN(value))    // isNaN = Is not a Number, kein gültiger double Wert
        {
            return 0;
        }
        if (value >= 0 && value <= 1)
        {
            return value * 2 - 1;  // 0 ... 1 --> -1 ... 1
        }
        if (value < 0)
        {
            return -1;
        }
        if (value > 1)
        {
            return 1;
        }
        return value;
    }

    private void RawGameController_RawGameControllerRemoved(object? sender, RawGameController e)
    {
        controller = null;
    }

    private void RawGameController_RawGameControllerAdded(object? sender, RawGameController e)
    {
        controller = e;
    }

    private void GameInit()
    {
        head.Location = new Point((Cols / 2) * ElemSize, (Rows / 2) * ElemSize);
        SnakeMovingDirection = MovingDirection.None;

        apple.Location = new Point((Cols / 2 + StepsToFirstApple) * ElemSize, (Rows / 2) * ElemSize);

        Controls.Add(head);
        Controls.Add(apple);
    }

    private void TmrMoveSnake_Tick(object? sender, EventArgs e) // Game loop
    {
        // Gameover: prüfen ob der Kopf der Schlange gegen eine Wand fahren würde
        if (head.Top == 0 && SnakeMovingDirection == MovingDirection.Up ||
            head.Bottom == ClientSize.Height && SnakeMovingDirection == MovingDirection.Down ||
            head.Left == 0 && SnakeMovingDirection == MovingDirection.Left ||
            head.Right == ClientSize.Width && SnakeMovingDirection == MovingDirection.Right)
        {
            GameOver();
        }
        // prüfen, ob sich die Schlange auf dem Apfel befindet
        if (head.Location == apple.Location)
        {
            // ein neues Schwanzelement erzeugen
            Panel tailElem = new Panel() {
                BackColor = Color.LightGreen,
                Size = new Size(ElemSize, ElemSize),
                Location = head.Location
            };
            tail.Enqueue(tailElem);
            Controls.Add(tailElem);

            // Futter an eine neue Position setzen
            apple.Location = new Point(rng.Next(0, Cols) * ElemSize, rng.Next(0, Rows) * ElemSize);
        }

        // Letztes Schwanzelement an die Position des Kopfes bewegen
        if (tail.Count > 0)
        {
            Panel lastTailElement = tail.Dequeue();
            lastTailElement.Location = head.Location;
            tail.Enqueue(lastTailElement);
        }

        MoveSnakeHead();

        // GameOver prüfen, ob der Kopf der Schlange sich über einem Schlangenelement befindet
        foreach (Panel tailElem in new Queue<Panel>(tail))
        {
            if (tailElem.Location == head.Location)
            {
                GameOver();
            }
        }
    }

    private void GameOver()
    {
        tmrMoveSnake.Stop();
        DialogResult result = MessageBox.Show("Nochmals?", "Game Over", MessageBoxButtons.YesNo);
        if (result == DialogResult.Yes)
        {
            Controls.Clear();
            tail.Clear();

            GameInit();
            tmrMoveSnake.Start();
        }
        else
        {
            Close();
        }
    }

    private void MoveSnakeHead()
    {
        switch (SnakeMovingDirection)
        {
            case MovingDirection.Up:
                head.Top -= ElemSize;
                break;
            case MovingDirection.Down:
                head.Top += ElemSize;
                break;
            case MovingDirection.Left:
                head.Left -= ElemSize;
                break;
            case MovingDirection.Right:
                head.Left += ElemSize;
                break;
            default:
                break;
        }
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
                SnakeMovingDirection = MovingDirection.Up;
                break;
            case Keys.Down:
                SnakeMovingDirection = MovingDirection.Down;
                break;
            case Keys.Left:
                SnakeMovingDirection = MovingDirection.Left;
                break;
            case Keys.Right:
                SnakeMovingDirection = MovingDirection.Right;
                break;
        }
    }
}