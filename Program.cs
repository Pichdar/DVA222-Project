using System;
using GLib;
using Gtk;
using Cairo;
namespace Gtk;

// Handle placing above board(done), Fix AI moves(done), Remake into non-God object

public class GridGame : Window
{
    private int rows, cols, cell_size;
    private string back_color, line_color;
    private bool is_human_turn = true, game_on = false, winner = false, move_made = false;
    static private Random rnd = new Random();
    private int [,] grid;
    private DrawingArea drawingarea;
    private uint timeout_id = 0;
    private Fixed container;
    private Label end_screen;
	public GridGame(int rows, int cols, int cell_size, string back_color, string line_color) : base("Connect Four")
	{
        this.rows = rows;
        this.cols = cols;
        this.cell_size = cell_size;
        this.back_color = back_color;
        this.line_color = line_color;
        grid = new int[rows, cols];
		
        DeleteEvent += (sender, args) => Application.Quit();

		// Create a Fixed container to manually position widgets
        container = new Fixed();
        container.Name = "container";
        this.Name = "background";
        ApplyCss(this, $"#background{{ background-image:none; background-color: black; border-radius: 0;}}");
        end_screen = new Label();
        
        Add(container);
        SetSizeRequest(cell_size*rows+20, cell_size*cols+150);

		// Create a Button widget and put it in the Fixed container
		Button fill_grid_button = new Button("Fill grid");
        fill_grid_button.Clicked += RandomMoveButton;
		fill_grid_button.SetSizeRequest(rows*cell_size, 50);
        ApplyCss(fill_grid_button, $"button{{ background-image:none; background-color:green; border-radius: 50px; font-size: 28;}}");
		container.Put(fill_grid_button, 10, cell_size*cols+105);

        Button play_game_button = new Button("Start Game");
        play_game_button.Clicked += StartGameButton;
		play_game_button.SetSizeRequest(rows*cell_size, 50);
        ApplyCss(play_game_button, $"button{{ background-image:none; background-color:green; border-radius: 50px; font-size: 28;}}");
		container.Put(play_game_button, 10, cell_size*cols+50);
		
		// Create a Label and put it in the Fixed container
		Label label = new Label("Connect 4");
        label.Name = "Connect4";
        ApplyCss(label, $"#Connect4{{ background-image:none; color: purple; padding: 10px; font-size: 30;}}");
        label.SetSizeRequest(200, 100);
		container.Put(label, cell_size*rows/4+25, cell_size*cols - 20);

        end_screen.Name = "end_screen";      
        end_screen.SetSizeRequest(200, 100);
        container.Put(end_screen, cell_size*rows/2-100, cell_size*cols+75);
		
		// Create a DrawingArea and put it in the Fixed container
		drawingarea = new DrawingArea();
        drawingarea.SetSizeRequest(cell_size*rows + 20, cell_size*cols + 20);
		drawingarea.Drawn += DrawGrid;
		container.Put(drawingarea, 0, 0);
        
		// Show all widgets
		ShowAll();

		// Run the GTK application
		Application.Run();
	}
	private void DrawGrid(object sender, DrawnArgs args)
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
            if(winner)
            {
                if(is_human_turn)
                {
                    end_screen.Text = "You won!";
                    ApplyCss(end_screen, $"#end_screen{{ background-image:none; color: lime; padding: 10px; font-size: 60; text-shadow: 5px 5px 5px white;}}");
                    ShowAll();
                }
                else
                {
                    end_screen.Text = "You lost!";
                    ApplyCss(end_screen, $"#end_screen{{ background-image:none; color: rgb(255, 0, 0); padding: 10px; font-size: 60; text-shadow: 5px 5px 5px white;}}");
                    ShowAll();
                }
            }
        }
        MakeCircle(Cr);
	}
    private void MakeCircle(Context Cr)
    {
        for(int i = 0; i < rows; i++)
        {
            for(int j = 0; j < cols; j++)
            {
               if(grid[i,j] == 1)
               {
                    Cr.Arc(j * cell_size + cell_size / 2 + 10, i * cell_size + cell_size / 2 + 10, cell_size / 3, 0, 2 * Math.PI);
                    Cr.SetSourceColor(StringToColor("red"));
                    Cr.Fill();
               }
               else if(grid[i,j] == 2)
               {
                    Cr.Arc(j * cell_size + cell_size / 2 + 10, i * cell_size + cell_size / 2 + 10, cell_size / 3, 0, 2 * Math.PI);
                    Cr.SetSourceColor(StringToColor("yellow"));
                    Cr.Fill();
               }
            }
        }
    }
    private void StartGameButton(object obj, EventArgs eventArgs)
    {
        ClearGrid();
        winner = false;
        end_screen.Hide();
        drawingarea.QueueDraw();
        if(timeout_id > 0)
        {
            GLib.Timeout.Remove(timeout_id);
            timeout_id = 0;
        }
        game_on = true;
        is_human_turn = true;
        move_made = false;
        timeout_id = GLib.Timeout.Add(20, GameHandler);
    }
    private void RandomMoveButton(object obj, EventArgs eventArgs)
    {
        ClearGrid();
        end_screen.Hide();
        if(timeout_id > 0)
        {
            GLib.Timeout.Remove(timeout_id);
            timeout_id = 0;
        }
        timeout_id = GLib.Timeout.Add(200, () =>
		{
			drawingarea.QueueDraw();
			return RandomMove();
		});
    }
    private bool RandomMove()
    {
        int rand_col, rand_row;
        do
        {
            rand_col = rnd.Next(cols);
            rand_row = rnd.Next(rows);
        }
        while(grid[rand_col,rand_row] != 0 && !IsFilled());
        if(!IsFilled())
        {
            grid[rand_col,rand_row] = rnd.Next(1,3);
            return true;
        }
        return false;
        
    }
    private bool IsFilled()
    {
        for(int i = 0; i < cols; i++)
        {
            for(int j = 0; j < rows; j++)
            {
                if(grid[i,j] == 0) // 0 means unoccupied
                {
                    return false;
                }
            }
        }
        return true;
    }
    private void ClearGrid()
    {
        grid = new int[cols,rows];
    }
    private void ApplyCss(Widget widget, string css)
            {
            CssProvider provider = new CssProvider();
            provider.LoadFromData(css);
            StyleContext context = widget.StyleContext;
            context.AddProvider(provider, Gtk.StyleProviderPriority.Application);
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
            if(game_on)
            {
                MoveHandler(column);
            }
        }
        return base.OnKeyPressEvent(ev);
    }
    private void MoveHandler(int column)
    {
        if(is_human_turn)
        {
            if(PlacePieceInBottom(column))
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
                Console.WriteLine("You won");
                winner = true;
                drawingarea.QueueDraw();
                game_on = false;
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
            if(PlacePieceInBottom(AiMove()))
            {
                if(CheckWinner())
                {
                    Console.WriteLine("You lost");
                    winner = true;
                    drawingarea.QueueDraw();
                    game_on = false;
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
    private bool PlacePieceInBottom(int which_column)
    {
        for(int i = rows-1; i >= 0; i--)
        {
            if(grid[i, which_column] == 0)
            {
                grid[i, which_column] = is_human_turn ? 2:1;
                drawingarea.QueueDraw();
                return true;
            }
        }
        return false;
    }
    private int AiMove()
    {
        return rnd.Next(cols);
    }
    private bool CheckWinner()
    {
        int player;
        if(is_human_turn){player = 2;}
        else
        player = 1;
        for(int i = 0; i < rows; i++)
        {
            for(int j = 0; j < cols; j++)
            {
                if(grid[i,j] == player)
                {   
                    // Check horizontal
                    if(j <= cols - 4 && grid[i,j +1] == player && grid[i,j+2] == player && grid[i,j+3] == player)
                    {
                        return true;
                    }
                    // Check vertical
                    if (i <= rows - 4 && grid[i + 1, j] == player && grid[i + 2, j] == player && grid[i + 3, j] == player)
                    {
                        return true;
                    }
                    // Check diagonal
                    if (i <= rows - 4 && j <= cols - 4 && grid[i + 1, j + 1] == player && grid[i + 2, j + 2] == player && grid[i + 3, j + 3] == player)
                    {
                        return true;
                    }
                    // Check reverse diagonal
                    if (i >= 3 && j <= cols - 4 && grid[i - 1, j + 1] == player && grid[i - 2, j + 2] == player && grid[i - 3, j + 3] == player)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    static private Color StringToColor(string color)
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

    public static void Main()
    {
        Application.Init();
        GridGame gridler = new(9, 9, 50, "black", "purple");
        
    }
}