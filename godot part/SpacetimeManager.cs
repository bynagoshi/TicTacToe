using Godot;
using System.Threading.Tasks;
using SpacetimeDB;
using SpacetimeDB.Types;      


public partial class SpacetimeManager : Node
{
	private DbConnection _conn;

	public override void _Ready()
	{
		GD.Print(">>> SpacetimeManager READY");
		ConnectToDb();
	}
	
	public override void _Process(double delta)
	{
		_conn?.FrameTick();
	}

	private void ConnectToDb()
	{
		const string HOST = "wss://maincloud.spacetimedb.com";
		const string DB_NAME = "languid-cord-9829"; 
		GD.Print("🚀 Connection initiated...");

		_conn = DbConnection
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
	}
}
