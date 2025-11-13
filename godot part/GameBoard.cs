using Godot;
using System.Threading.Tasks;
using SpacetimeDB;
using System;

public partial class GameBoard : Control
{
	//private ulong gameId = 1; 
	private Button[] cells = new Button[9];
	public override void _Ready()
	{
		GD.Print("GameBoard Ready");
		
		for (int i = 1; i < 10; i++)
		{
			var button = GetNode<Button>($"GridContainer/Cell{i}");
			int index = i;
			button.Text = "-";
			//button.Pressed += () => 
			//{
				//GD.Print($"Cell {index} clicked");
				//OnCellPressed(index);
				////OnCreateGame();
			//};
			button.Connect(Button.SignalName.Pressed,
				Callable.From(() =>
				{
					GD.Print($"Cell {index} clicked");
					OnCellPressed(index);
				}));
			
		}
		//void Wire(string name, Action a)
		//{
			//var b = GetNodeOrNull<Button>(name);
			//if (b == null) { GD.PrintErr($"Missing: {name}"); return; }
			//b.Connect(Button.SignalName.Pressed, Callable.From(() =>
			//{
				//GD.Print($"{name} clicked");
				//a();
			//}));
		//}

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
		
		//GetNode<Button>("ControlContainer/CreateGame").Pressed += () => { GD.Print("Create clicked"); OnCreateGame(); };
		//GetNode<Button>("ControlContainer/JoinGame").Pressed += () => { GD.Print("Join clicked"); OnJoinGame(); };
		//GetNode<Button>("ControlContainer/ResetGame").Pressed += () => { GD.Print("Reset clicked"); OnResetGame(); };
//
		//SpacetimeManager.I.OnBoardChanged += Render;

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
		//if (SpacetimeManager.I.CurrentGameId != 0)
		//{
			//SpacetimeManager.I.Client.Reducers.JoinGame(SpacetimeManager.I.CurrentGameId);
			//GD.Print($"Joined new game {SpacetimeManager.I.CurrentGameId}");
		//}
	}

	private void OnJoinGame()
	{
		//var id = SpacetimeManager.I.CurrentGameId;
		//if (id == 0) id = SpacetimeManager.I.GetLatestGameIdFromCache();
		//
		//if (id != 0)
		//{
			//SpacetimeManager.I.Client.Reducers.JoinGame(id);
			//GD.Print($"Joined game {id}");
		//}
		//else
		//{
			//GD.PrintErr("No game to join yet. Click Create Game first.");
		//}
		
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
		var id = SpacetimeManager.I.CurrentGameId;
		if (id == 0) id = SpacetimeManager.I.GetLatestGameIdFromCache();
		
		if (id != 0)
		{
			SpacetimeManager.I.Client.Reducers.MakeMove(id, (byte)pos);
			GD.Print($"Pressed cell {pos}");
		}
	}

	public void Render(string board)
	{
		if (string.IsNullOrEmpty(board) || board.Length < 10) return;
		for (int i = 1; i < 10; i++)
			cells[i].Text = board[i].ToString();
	}
}
