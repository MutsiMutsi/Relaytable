using Relaytable.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Relaytable.Helpers
{
	[JsonSerializable(typeof(Dictionary<string, string>))]
	[JsonSerializable(typeof(Results<NodeNeighbour>))]
	[JsonSerializable(typeof(Result<NodeStatusModel>))]
	internal partial class AppJsonSerializerContext : JsonSerializerContext
	{
	}
}
