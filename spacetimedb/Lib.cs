using SpacetimeDB;
using System.Linq;

public static partial class Module
{
    [SpacetimeDB.Table(Public = true)]
    public partial struct Game
    {
        [SpacetimeDB.AutoInc]
        [SpacetimeDB.PrimaryKey]
        public ulong Id;
        public string Board;
        public string Status;
        public Identity? Player1;
        public Identity? Player2;
        public Identity? CurrentPlayer;
    }

    [SpacetimeDB.Reducer]
    public static void CreateGame(ReducerContext ctx)
    {
        var board = "---------";
        var status = "waiting";
        Identity? player1 = ctx.Sender;
        Identity? player2 = null;
        Identity? currentPlayer = null;

        var game = ctx.Db.Game.Insert(new Game { Board = board, Status = status, Player1 = player1, Player2 = player2, CurrentPlayer = currentPlayer });
        Log.Info($"Inserted {game.Board} under #{game.Id}");
    }

    [SpacetimeDB.Reducer]
    public static void JoinGame(ReducerContext ctx, ulong gameId)
    {
        Game? found = null;
        foreach (var game in ctx.Db.Game.Iter())
        {
            if (game.Id == gameId)
            {
                found = game;
                break;
            }
        }
        if (found == null)
        {
            Log.Error($"Game {gameId} not found");
            return;
        }

        var g = found.Value;
        if (g.Status != "waiting") 
        { 
            Log.Error("Game already started"); 
            return; 
        }
        if (g.Player1 == ctx.Sender) 
        { 
            Log.Error("You are already Player1"); 
            return; 
        }
        if (g.Player2 != null) 
        { 
            Log.Error("Player2 already taken"); 
            return; 
        }
        g.Player2 = ctx.Sender;
        g.Status = "in progress";
        ctx.Db.Game.Id.Update(g);
        Log.Info($"Player2 joined game {gameId}");
    }
    
    [SpacetimeDB.Reducer]
    public static void MakeMove(ReducerContext ctx, ulong gameId, int position)
    {
        Game? found = null;
        foreach (var game in ctx.Db.Game.Iter())
        {
            if (game.Id == gameId)
            {
                found = game;
                break;
            }
        }
        if (found == null)
        {
            Log.Error($"Game {gameId} not found");
            return;
        }

        var g = found.Value;

        if (g.CurrentPlayer != ctx.Sender)
        {
            Log.Error($"It's not {ctx.Sender}'s turn");
            return;
        }
        if (g.Board[position] != '-')
        {
            Log.Error($"Position {position} is already taken");
            return;
        }
        var chars = g.Board.ToCharArray();
        chars[position] = (g.CurrentPlayer == g.Player1) ? 'X' : 'O';
        g.Board = new string(chars);
        g.CurrentPlayer = (g.CurrentPlayer == g.Player1) ? g.Player2 : g.Player1;

        var winner = CheckWin(g.Board);
        if (winner != '-')
        {
            g.Status = $"{winner} won";
        }
        else if (g.Board.All(c => c != '-'))
        {
            g.Status = "draw";
        } else {
            g.Status = "in progress";
        }
        ctx.Db.Game.Id.Update(g);
        Log.Info($"Made move {position} for {ctx.Sender}");
    }

    [SpacetimeDB.Reducer]
    public static void ResetGame(ReducerContext ctx, ulong gameId)
    {
        Game? found = null;
        foreach (var game in ctx.Db.Game.Iter())
        {
            if (game.Id == gameId)
            {
                found = game;
                break;
            }
        }
        if (found == null)
        {
            Log.Error($"Game {gameId} not found");
            return;
        }

        var g = found.Value;

        g.Board = "---------";
        g.Status = "waiting";
        ctx.Db.Game.Id.Update(g);
        Log.Info($"Reset game {gameId}");
    }


    // Helper functions
    private static char CheckWin(string board)
    {
        int[][] lines = {
        new[]{0,1,2}, new[]{3,4,5}, new[]{6,7,8},
        new[]{0,3,6}, new[]{1,4,7}, new[]{2,5,8},
        new[]{0,4,8}, new[]{2,4,6}
        };
        foreach (var l in lines)
        {
            var a = board[l[0]];
            if (a != '-' && a == board[l[1]] && a == board[l[2]])
                return a;
        }
        return '-'; 
    }
}
