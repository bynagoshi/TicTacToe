using Godot;
using System.Threading.Tasks;
using SpacetimeDB;
using SpacetimeDB.Types;     


public partial class SpacetimeManager : Node
{
	public static SpacetimeManager I {get; private set;}
	public DbConnection Client {get; private set;}
	public event System.Action<string> OnBoardChanged;
	public ulong CurrentGameId { get; private set; } = 0;

	public override void _Ready()
	{
		I = this;
		GD.Print(">>> SpacetimeManager READY");
		ConnectToDb();
	}
	
	public override void _Process(double delta)
	{
		Client?.FrameTick();
	}

	private void ConnectToDb()
	{
		const string HOST = "wss://maincloud.spacetimedb.com";
		const string DB_NAME = "languid-cord-9829"; 
		GD.Print("🚀 Connection initiated...");

		Client = DbConnection
			.Builder()
			.WithUri(HOST)
			.WithModuleName(DB_NAME)  
			.OnConnect((conn, identity, token) =>
			{
				GD.Print($"Connected! Identity={identity}");
			})
			.OnConnectError(e =>
			{
				GD.PrintErr($"Connect error: {e}");
			})
			.OnDisconnect((conn, ex) =>
			{
				GD.Print("Disconnected");
			})
			.Build();    
		Client.Db.Game.OnInsert += (EventContext ctx, Game row) =>
			{
				GD.Print($"[Game inserted] id={row.Id} board={row.Board}");
				CurrentGameId = row.Id;
				OnBoardChanged?.Invoke(row.Board);
			};			

		Client.Db.Game.OnUpdate += (EventContext ctx, Game oldRow, Game newRow) =>
			{
				if (newRow.Id == CurrentGameId)
				{
					GD.Print($"[Game updated] id={newRow.Id} board={newRow.Board} (was {oldRow.Board})");
					OnBoardChanged?.Invoke(newRow.Board);
				}
				
			};
		Client.SubscriptionBuilder()
			.OnApplied(ctx => GD.Print("Subscription applied!"))
			.OnError((ctx, ex) => GD.PrintErr($"Subscription error: {ex.Message}"))
			//.OnData((ctx, rows) =>
			//{
				//foreach (var row in rows)
				//{
					//GD.Print($"Game update received: id={row.Id}, board={row.Board}");
					//OnBoardChanged?.Invoke(row.Board);
				//}
			//})
			.Subscribe(new string[] { "SELECT * FROM Game" });
					   
	}
}
