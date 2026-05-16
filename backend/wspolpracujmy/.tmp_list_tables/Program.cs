using Npgsql;
using System.Text;

var candidates = new[]
{
	"Host=localhost;Port=5432;Database=wspolpracujmy;Username=postgres;Password=postgres",
	"Host=db;Port=5432;Database=projekt_db;Username=postgres;Password=postgres"
};

Exception? lastEx = null;
foreach (var connString in candidates)
{
	try
	{
		await using var conn = new NpgsqlConnection(connString);
		await conn.OpenAsync();

		var sql = "SELECT table_schema, table_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema NOT IN ('pg_catalog','information_schema') ORDER BY table_schema, table_name;";
		await using var cmd = new NpgsqlCommand(sql, conn);
		await using var reader = await cmd.ExecuteReaderAsync();

		Console.WriteLine($"Connected using: {connString}");
		Console.WriteLine("Tables in database:");
		while (await reader.ReadAsync())
		{
			var schema = reader.GetString(0);
			var name = reader.GetString(1);
			Console.WriteLine($"- {schema}.{name}");
		}
		return;
	}
	catch (Exception ex)
	{
		lastEx = ex;
		Console.Error.WriteLine($"Connection failed for: {connString} -> {ex.Message}");
	}
}

if (lastEx != null)
{
	Console.Error.WriteLine($"All connection attempts failed. Last error: {lastEx.Message}");
	Environment.ExitCode = 2;
}
