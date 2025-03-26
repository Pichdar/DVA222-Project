using System;
using GLib;
using Gtk;
using Cairo;
using System.Security.Cryptography.X509Certificates;
namespace Gtk;

//Code by Vidar Ersson & Maverick I. N.

public class GameLogic {    
    
    public bool is_human_turn = true, game_on = false, winner = false;
    private bool move_made = false, loop = false;
    private uint timeout_id = 0;
    static private Random rnd = new Random();
    public BoardRenderer Board{get;}
    
    
    private GUI gui;
    public GameLogic(BoardRenderer board) {
        this.Board = board;
        System.Console.WriteLine($"{Board.rows}, {Board.cols}, {Board.cell_size}");
        gui = new GUI(Board.rows, Board.cols, Board.cell_size, this);
        
    }
    
    public void MoveHandler(int column)
    {
        if(is_human_turn)
        {
            if(FindBottom(column))
            {
                move_made = true;
            }
        }
    }
    
    private bool GameHandler()
    {
        if(is_human_turn)
        {
            if(CheckWinner())
            {
                gui.WinScreen(is_human_turn);
                Console.WriteLine("You won");
                winner = true;
                Board.drawingarea.QueueDraw();
                game_on = false;
                timeout_id = 0;
                return false;
            }
            else if(move_made)
            {
                is_human_turn = false;
                move_made = false;
            }
            return true;
        }
        else
        {
            if(FindBottom(AiMove()))
            {
                if(CheckWinner())
                {
                    gui.WinScreen(is_human_turn);
                    Console.WriteLine("You lost");
                    winner = true;
                    Board.drawingarea.QueueDraw();
                    game_on = false;
                    timeout_id = 0;
                    return false;
                }
                else
                {
                    is_human_turn = true;
                }
            }
            return true;
        }
    }
    private bool RandomMove()
    {
        int rand_col, rand_row;
        while(loop){
            do
            {
                rand_col = rnd.Next(Board.cols);
                rand_row = rnd.Next(1,Board.rows);
            }
            while(Board.grid[rand_row,rand_col] != 0 && !IsFilled());
            if(!IsFilled())
            {
                Board.grid[rand_row,rand_col] = rnd.Next(1,3);
                return true;
            }
            Board.ClearGrid();
        }
        timeout_id = 0;
        return false;
        
    }
    private bool IsFilled()
    {
        for(int i = 1; i < Board.rows; i++)
        {
            for(int j = 0; j < Board.cols; j++)
            {
                if(Board.grid[i,j] == 0) // 0 means unoccupied
                {
                    return false;
                }
            }
        }
        return true;
    }
    private int AiMove()
    {
        return rnd.Next(Board.cols);
    }
    private bool CheckWinner()
    {
        int player;
        if(is_human_turn){player = 2;}
        else
        player = 1;
        for(int i = 0; i < Board.rows; i++)
        {
            for(int j = 0; j < Board.cols; j++)
            {
                if(Board.grid[i,j] == player)
                {   
                    // Check horizontal
                    if(j <= Board.cols - 4 && Board.grid[i,j +1] == player && Board.grid[i,j+2] == player && Board.grid[i,j+3] == player)
                    {
                        return true;
                    }
                    // Check vertical
                    if (i <= Board.rows - 4 && Board.grid[i + 1, j] == player && Board.grid[i + 2, j] == player && Board.grid[i + 3, j] == player)
                    {
                        return true;
                    }
                    // Check diagonal
                    if (i <= Board.rows - 4 && j <= Board.cols - 4 && Board.grid[i + 1, j + 1] == player && Board.grid[i + 2, j + 2] == player && Board.grid[i + 3, j + 3] == player)
                    {
                        return true;
                    }
                    // Check reverse diagonal
                    if (i >= 3 && j <= Board.cols - 4 && Board.grid[i - 1, j + 1] == player && Board.grid[i - 2, j + 2] == player && Board.grid[i - 3, j + 3] == player)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    private bool FindBottom(int which_column)
    {
        int value;
        for(int i = Board.rows-1; i >= 0; i--)
        {
            if(Board.grid[i, which_column] == 0 && i != 0)
            {
                value = is_human_turn ? 2:1;
                Board.PlacePieceInCell(Board.grid, i, which_column, value);
                
                Board.drawingarea.QueueDraw();
                return true;
            }
        }
        return false;
    }

    public void StartGameButton(object obj, EventArgs eventArgs)
    {
        loop = false;
        Board.ClearGrid();
        winner = false;
        gui.end_screen.Hide();
        Board.drawingarea.QueueDraw();
        if(timeout_id > 0)
        {
            if (GLib.Source.Remove(timeout_id))  // Ensure it exists before removing
            {
                timeout_id = 0;
            }
        }
        game_on = true;
        is_human_turn = true;
        move_made = false;
        timeout_id = GLib.Timeout.Add(20, GameHandler);
        
    }
    public void RandomMoveButton(object obj, EventArgs eventArgs)
    {
        game_on = false;
        loop = !loop;
        Board.ClearGrid();
        gui.end_screen.Hide();
    
        if(timeout_id > 0)
        {
            if (GLib.Source.Remove(timeout_id))  // Ensure it exists before removing
            {
                timeout_id = 0;
            }
        }
        timeout_id = GLib.Timeout.Add(200, () =>
        {
            Board.drawingarea.QueueDraw();
            return RandomMove();
        });
        
    
    }
}
public class BoardRenderer
{     
    public int rows, cols, cell_size;
    private string back_color, line_color;
    public DrawingArea drawingarea;
    public int [,] grid;
    
    Cell Cell{get;}
	public BoardRenderer(int rows, int cols, int cell_size, string back_color, string line_color, Cell cell)
	{
        this.rows = rows;
        this.cols = cols;
        this.cell_size = cell_size;
        this.back_color = back_color;
        this.line_color = line_color;
        this.Cell = cell;
        grid = new int[rows, cols];
	}
	public void DrawGrid(object sender, DrawnArgs args)
	{
        
        Context Cr = args.Cr;
		drawingarea = (DrawingArea)sender;
        Cr.SetSourceColor(StringToColor(back_color));
		Cr.Rectangle(0, 0, 800, 800); // Set position and size (clip if bigger than drawingarea)
		Cr.Fill();
        for(int i = 0; i < cols; i++)
        {
            // Draws numbers on top of grid
            Cr.SetSourceColor(StringToColor(line_color));
            Cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Normal);
            Cr.SetFontSize(20);
            TextExtents extents = Cr.TextExtents((i+1).ToString());
            double text_width = extents.Width;
            double text_height = extents.Height;
            Cr.MoveTo(i*cell_size+(cell_size/2)-text_width/2 + 10, (cell_size/2)+text_height/2 + 10);
            Cr.ShowText((i+1).ToString());
            Cr.Stroke();
            for(int j = 0; j < rows; j++)
            {
                Cr.SetSourceColor(StringToColor(line_color));
                Cr.Rectangle(i*cell_size + 10,j*cell_size + 10, cell_size, cell_size); // + 10 for padding
                Cr.Stroke();
            }
            
        }
        Cell.MakeCircle(Cr, rows, cols, cell_size, grid);
	}
    public void ClearGrid()
    {
        Array.Clear(grid);
    }
    static public Color StringToColor(string color)
            {
            switch (color.ToLower())
                    {
                    case "red": return new Color(1,0,0); 
                    case "green": return new Color(0, 1, 0); 
                    case "blue": return new Color(0, 0, 1); 
                    case "yellow": return new Color(1, 1, 0); 
                    case "cyan": return new Color(0, 1, 1); 
                    case "magenta": return new Color(1, 0, 1); 
                    case "black": return new Color(0, 0, 0);
                    case "white": return new Color(1, 1, 1);
                    case "gray": return new Color(0.5, 0.5, 0.5);
                    case "lightgray": return new Color(0.75, 0.75, 0.75); 
                    case "darkgray": return new Color(0.25, 0.25, 0.25); 
                    case "orange": return new Color(1, 0.5, 0); 
                    case "pink": return new Color(1, 0.75, 0.8); 
                    case "purple": return new Color(0.5, 0, 0.5); 
                    case "brown": return new Color(0.6, 0.3, 0.1); 
                    case "gold": return new Color(1, 0.84, 0); 
                    case "silver": return new Color(0.75, 0.75, 0.75); 
                    case "navy": return new Color(0, 0, 0.5); 
                    case "lime": return new Color(0.75, 1, 0); 
                    case "indigo": return new Color(0.29, 0, 0.51);
                    case "teal": return new Color(0, 0.5, 0.5); 
                    case "maroon": return new Color(0.5, 0, 0); 
                    case "olive": return new Color(0.5, 0.5, 0); 
                    case "violet": return new Color(0.93, 0.51, 0.93); 
                    case "turquoise": return new Color(0.25, 0.88, 0.82); 
                    case "coral": return new Color(1, 0.5, 0.31); 
                    case "salmon": return new Color(0.98, 0.5, 0.45); 
                    case "chocolate": return new Color(0.82, 0.41, 0.12); 
                default:
                        return new Color(0,0,0); // Default to black if unknown
                }
            }

    public void PlacePieceInCell(int [,] grid, int row, int col, int value){
        Cell.ChangeValue(grid, row, col, value);
    }
}
public class GUI : Window{
    public Fixed container;
    public Label end_screen;
    public Button play_game_button, fill_grid_button;
    GameLogic GameLogic{get;}
    public GUI(int rows, int cols, int cell_size, GameLogic gameLogic) : base("Connect 4"){
        this.GameLogic = gameLogic;
        
        DeleteEvent += (sender, args) => Application.Quit();

		// Create a Fixed container to manually position widgets
        container = new Fixed();
        container.Name = "container";
        this.Name = "background";
        ApplyCss(this, $"#background{{ background-image:none; background-color: black; border-radius: 0;}}");
        end_screen = new Label();
        
        Add(container);
        SetSizeRequest(cell_size*cols+20, cell_size*rows+150);

		// Create a Button widget and put it in the Fixed container
		
        play_game_button = new Button("Start Game");
        play_game_button.Clicked += GameLogic.StartGameButton;
		play_game_button.SetSizeRequest(cols*cell_size, 50);
        ApplyCss(play_game_button, $"button{{ background-image:none; background-color:green; border-radius: 50px; font-size: 28px;}}");
		container.Put(play_game_button, 10, cell_size*rows+50);

        fill_grid_button = new Button("Fill grid");
        fill_grid_button.Clicked += GameLogic.RandomMoveButton;
		fill_grid_button.SetSizeRequest(cols*cell_size, 50);
        ApplyCss(fill_grid_button, $"button{{ background-image:none; background-color:green; border-radius: 50px; font-size: 28px;}}");
		container.Put(fill_grid_button, 10, cell_size*rows+105);
        
		
		// Create a Label and put it in the Fixed container
		Label label = new Label("Connect 4");
        label.Name = "Connect4";
        ApplyCss(label, $"#Connect4{{ background-image:none; color: purple; padding: 10px; font-size: 30px;}}");
        label.SetSizeRequest(200, 100);
		container.Put(label, cell_size*cols/4+25, cell_size*rows - 20);

        end_screen.Name = "end_screen";      
        end_screen.SetSizeRequest(200, 100);
        container.Put(end_screen, cell_size*cols/2-100, cell_size*rows+75);
		
		// Create a DrawingArea and put it in the Fixed container
		GameLogic.Board.drawingarea = new DrawingArea();
        GameLogic.Board.drawingarea.SetSizeRequest(cell_size*cols + 20, cell_size*rows + 20);
		GameLogic.Board.drawingarea.Drawn += GameLogic.Board.DrawGrid;
		container.Put(GameLogic.Board.drawingarea, 0, 0);
        
		// Show all widgets
		ShowAll();
		
    }
    public void ApplyCss(Widget widget, string css)
    {
        CssProvider provider = new CssProvider();
        provider.LoadFromData(css);
        StyleContext context = widget.StyleContext;
        context.AddProvider(provider, Gtk.StyleProviderPriority.Application);
    }

    public void WinScreen(bool is_human_turn){
        
        if(is_human_turn)
        {
            end_screen.Text = "You won!";
            ApplyCss(end_screen, $"#end_screen{{ background-image:none; color: lime; padding: 10px; font-size: 60px; text-shadow: 5px 5px 5px white;}}");
            ShowAll();
        }
        else
        {
            end_screen.Text = "You lost!";
            ApplyCss(end_screen, $"#end_screen{{ background-image:none; color: rgb(255, 0, 0); padding: 10px; font-size: 60px; text-shadow: 5px 5px 5px white;}}");
            ShowAll();
        }
        
    }
    protected override bool OnKeyPressEvent(Gdk.EventKey ev)
    {
        if (ev.Key == Gdk.Key.Escape)
        {
            Application.Quit();
        }
        else if (ev.Key >= Gdk.Key.Key_1 && ev.Key <= Gdk.Key.Key_9)
        {
            int column = ev.Key - Gdk.Key.Key_1;
            Console.WriteLine($"{column}");
            if(GameLogic.game_on)
            {
                GameLogic.MoveHandler(column);
            }
        }
        return base.OnKeyPressEvent(ev);
    }

}
public class Cell{
    public void MakeCircle(Context Cr, int rows, int cols, int cell_size, int [,] grid)
    {
        for(int i = 0; i < rows; i++)
        {
            for(int j = 0; j < cols; j++)
            {
               if(grid[i,j] == 1)
               {
                    Cr.Arc(j * cell_size + cell_size / 2 + 10, i * cell_size + cell_size / 2 + 10, cell_size / 3, 0, 2 * Math.PI);
                    Cr.SetSourceColor(BoardRenderer.StringToColor("red"));
                    Cr.Fill();
               }
               else if(grid[i,j] == 2)
               {
                    Cr.Arc(j * cell_size + cell_size / 2 + 10, i * cell_size + cell_size / 2 + 10, cell_size / 3, 0, 2 * Math.PI);
                    Cr.SetSourceColor(BoardRenderer.StringToColor("yellow"));
                    Cr.Fill();
               }
            }
        }
    }
    public void ChangeValue(int [,] grid, int row, int column, int value){
        grid[row, column] = value;    
    }
}
public class Connect4 : GameLogic{
    public Connect4(int size, string back_color, string line_color) : base(new BoardRenderer(size + 1, size, 50, back_color, line_color, new Cell())) {}
}
public class Program{
    public static void Main()
    {
        Application.Init();
        Connect4 gridler = new(9,"black","purple");
        Application.Run();
    }
}