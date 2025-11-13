using Godot;
using System;
using System.Threading.Tasks;
using SpacetimeDB;
using SpacetimeDB.Types;

public partial class SpacetimeManager : Node
{
	public static SpacetimeManager I { get; private set; }
	public DbConnection Client { get; private set; }
	public event Action<string> OnBoardChanged;
	public ulong CurrentGameId { get; set; } = 0;
	public bool IsSubscribed { get; private set; } = false;

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
		// const string HOST = "ws://127.0.0.1:3000";
		const string DB_NAME = "languid-cord-9829";

		GD.Print("Connection initiated");

		Client = DbConnection
			.Builder()
			.WithUri(HOST)
			.WithModuleName(DB_NAME)
			.OnConnect((conn, identity, token) =>
			{

				conn.SubscriptionBuilder()
					.OnApplied(ctx => { GD.Print("Subscription applied"); IsSubscribed = true; })
					.OnError((ctx, ex) => GD.PrintErr($"Subscription error: {ex}"))
					.Subscribe(new string[] { "SELECT * FROM Game" });
					//.SubscribeToAllTables();
				GD.Print($"Connected, Identity={identity}");
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

		Client.OnUnhandledReducerError += (ctx, ex) =>
		{
			GD.PrintErr($"Reducer error: {ex}");
		};

		Client.Db.Game.OnInsert += (EventContext ctx, Game row) =>
		{
			GD.Print($"[Game inserted] id={row.Id} board={row.Board}");
			CurrentGameId = row.Id;
			OnBoardChanged?.Invoke(row.Board);
		};

		Client.Db.Game.OnUpdate += (EventContext ctx, Game oldRow, Game newRow) =>
		{
			GD.Print($"[Game updated] id={newRow.Id} board={newRow.Board} (was {oldRow.Board})");

			if (CurrentGameId == 0)
				CurrentGameId = newRow.Id;

			if (newRow.Id == CurrentGameId)
				OnBoardChanged?.Invoke(newRow.Board);
		};

		
	}

	public ulong GetLatestGameIdFromCache()
	{
		if (Client == null)
		{
			GD.PrintErr("GetLatestGameIdFromCache: Client is null");
			return 0;
		}

		ulong latest = 0;
		int count = 0;

		foreach (var g in Client.Db.Game.Iter())
		{
			GD.Print($"[Cache] Game row: id={g.Id}, board={g.Board}");
			count++;
			if (g.Id > latest) latest = g.Id;
		}

		GD.Print($"GetLatestGameIdFromCache: rows={count}, latest={latest}");
		return latest;
	}

	public void UseLatestGameId()
	{
		var id = GetLatestGameIdFromCache();
		if (id != 0)
		{
			GD.Print($"UseLatestGameId: using {id}");
			CurrentGameId = id;
		}
		else
		{
			GD.Print("UseLatestGameId: no games in cache");
		}
	}

	public void JoinLatest()
	{
		UseLatestGameId();
		if (CurrentGameId != 0)
		{
			GD.Print($"Calling reducer JoinGame({CurrentGameId})");
			Client.Reducers.JoinGame(CurrentGameId);
		}
		else
		{
			GD.PrintErr("JoinLatest: no game id to join");
		}
	}
}
