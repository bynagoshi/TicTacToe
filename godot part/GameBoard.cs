using Godot;
using System.Threading.Tasks;
using SpacetimeDB;
using System;

	public partial class GameBoard : Control
	{
	//private ulong gameId = 1; 
	private Button[] cells = new Button[9];
	private string currentStatus = "";
	public override void _Ready()
	{
		GD.Print("GameBoard Ready");
		GetNode<Button>("ControlContainer/CreateGame").Text = "Create Game";
		GetNode<Button>("ControlContainer/JoinGame").Text = "Join Game";
		GetNode<Button>("ControlContainer/ResetGame").Text = "Reset Game";
		for (int i = 0; i < 9; i++)
		{
			var button = GetNode<Button>($"GridContainer/Cell{i + 1}");
			cells[i] = button;
			int index = i;
			button.Text = "-";

			button.Connect(Button.SignalName.Pressed,
				Callable.From(() =>
				{
					GD.Print($"Cell {index} clicked");
					OnCellPressed(index);
				}));
			
		}

		GetNode<Button>("ControlContainer/CreateGame").Connect(
			Button.SignalName.Pressed,
			Callable.From(() =>
			{
				GD.Print("Create clicked");
				OnCreateGame();
			})
		);

		GetNode<Button>("ControlContainer/JoinGame").Connect(
			Button.SignalName.Pressed,
			Callable.From(() =>
			{
				GD.Print("Join clicked");
				OnJoinGame();
			})
		);

		GetNode<Button>("ControlContainer/ResetGame").Connect(
			Button.SignalName.Pressed,
			Callable.From(() =>
			{
				GD.Print("Reset clicked");
				OnResetGame();
			})
		);


		GD.Print("Buttons wired via Connect()");
		
		// Subscribe to board changes
		if (SpacetimeManager.I != null)
		{
			SpacetimeManager.I.OnBoardChanged += Render;
			SpacetimeManager.I.OnStatusChanged += OnStatusChanged;
		}

	}

	private void OnCreateGame()
	{
		if (!SpacetimeManager.I.IsSubscribed)
		{
			GD.Print("Waiting for subscription to be applied...");
			return;
		}
		
		var id = SpacetimeManager.I.GetLatestGameIdFromCache();
		if (id != 0)
		{
			//SpacetimeManager.I.CurrentGameId = id;
			GD.Print($"Using existing game {id}");
			return;
		}
		
		SpacetimeManager.I.Client.Reducers.CreateGame();
		GD.Print("New game created");

	}

	private void OnJoinGame()
	{
		
		SpacetimeManager.I.UseLatestGameId();
		var id = SpacetimeManager.I.CurrentGameId;

		if (id == 0)
		{
			GD.PrintErr("No game found, create one");
			return;
		}

		SpacetimeManager.I.Client.Reducers.JoinGame(id);
		GD.Print($"JoinGame({id})");
	}

	private void OnResetGame()
	{
		var id = SpacetimeManager.I.CurrentGameId;
		SpacetimeManager.I.Client.Reducers.ResetGame(id);
		GD.Print("Game reset");
	}

	private void OnCellPressed(int pos)
	{
		// Check if game is still in progress
		if (currentStatus == "X won" || currentStatus == "O won" || currentStatus == "draw")
		{
			GD.Print($"Game is over (status: {currentStatus}), cannot make move");
			return;
		}

		var id = SpacetimeManager.I.CurrentGameId;
		if (id == 0) id = SpacetimeManager.I.GetLatestGameIdFromCache();
		
		if (id != 0)
		{
			SpacetimeManager.I.Client.Reducers.MakeMove(id, pos);
			GD.Print($"Pressed cell {pos}");
		}
	}

	private void OnStatusChanged(string status)
	{
		currentStatus = status;
		GD.Print($"Game status changed to: {status}");
		UpdateCellButtonsEnabled();
	}

	private void UpdateCellButtonsEnabled()
	{
		bool enabled = currentStatus == "in progress";
		for (int i = 0; i < cells.Length; i++)
		{
			if (cells[i] != null)
			{
				cells[i].Disabled = !enabled;
			}
		}
	}

	public void Render(string board)
	{
		if (string.IsNullOrEmpty(board) || board.Length < 9) return;
		GD.Print($"Rendering board: {board}");
		
		// Update status from cache if available
		if (SpacetimeManager.I != null)
		{
			currentStatus = SpacetimeManager.I.GetCurrentGameStatus();
			UpdateCellButtonsEnabled();
		}
		
		for (int i = 0; i < 9; i++)
		{
			if (cells[i] != null)
			{
				cells[i].Text = board[i].ToString();
			}
		}
	}
}
