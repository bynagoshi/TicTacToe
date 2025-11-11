using Godot;
using System.Threading.Tasks;
using SpacetimeDB;

public partial class GameBoard : Control
{
	private ulong gameId = 1; // static test ID for now

	public override void _Ready()
	{
		// Hook up the cell buttons
		for (int i = 0; i < 9; i++)
		{
			var button = GetNode<Button>($"GridContainer/Cell{i}");
			int index = i;
			button.Pressed += () => OnCellPressed(index);
		}

		// Hook up action buttons
		GetNode<Button>("CreateGame").Pressed += () => OnCreateGame();
		GetNode<Button>("JoinGame").Pressed += () => OnJoinGame();
		GetNode<Button>("ResetGame").Pressed += () => OnResetGame();

		GD.Print("Game board ready");
	}

	private void OnCreateGame()
	{
		SpacetimeManager.I.Client.Reducers.CreateGame();
		GD.Print("Game created!");
	}

	private void OnJoinGame()
	{
		SpacetimeManager.I.Client.Reducers.JoinGame(gameId);
		GD.Print("Joined game!");
	}

	private void OnResetGame()
	{
		SpacetimeManager.I.Client.Reducers.ResetGame(gameId);
		GD.Print("Game reset!");
	}

	private void OnCellPressed(int pos)
	{
		SpacetimeManager.I.Client.Reducers.MakeMove(gameId, (byte)pos);
		GD.Print($"Pressed cell {pos}");
	}
}
