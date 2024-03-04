namespace Linq3Sql
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			(string table, string column) ignoreFks = ("BattleParticipant", "ParticipantId");
			string connectionString = @"Data Source=C:\Users\darcy\AppData\Local\AotT\AotT.db";

			var dbContextService = new DbContextService(connectionString, @"C:\Users\darcy\source\repos\_Experiments\Linq3Sql\Linq3Sql\GeneratedModel", ignoreFks);

			var playerDefault = await dbContextService.GetFirstAsync<Player>(3);

			var playerNone = await dbContextService.GetFirstAsync<Player>(3, ReferenceFetchMode.None);

			var playerSinglevel = await dbContextService.GetFirstAsync<Player>(3, ReferenceFetchMode.SingleLevel);

			var playerRecursive = await dbContextService.GetFirstAsync<Player>(3, ReferenceFetchMode.Recursive);

			var playerByName = await dbContextService.GetFirstAsync<Player>(3, ReferenceFetchMode.ByName, ["User"]);

			var playersAll = await dbContextService.GetAllAsync<Player>();

			var playersByIds = await dbContextService.GetAllAsync<Player>(new List<object> { 2, 3 });


		}
	}
}
