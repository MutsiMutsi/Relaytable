using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relaytable.Models
{
	// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
	public record struct NodeStatusModel
	{
		public string addr { get; set; }
		public int currTimeStamp { get; set; }
		public int height { get; set; }
		public string id { get; set; }
		public int jsonRpcPort { get; set; }
		public int proposalSubmitted { get; set; }
		public int protocolVersion { get; set; }
		public string publicKey { get; set; }
		public long relayMessageCount { get; set; }
		public string syncState { get; set; }
		public int tlsJsonRpcPort { get; set; }
		public int tlsWebsocketPort { get; set; }
		public int uptime { get; set; }
		public string version { get; set; }
		public int websocketPort { get; set; }
	}

	public record struct NodeNeighbour
	{
		public string addr { get; set; }
		public int connTime { get; set; }
		public int height { get; set; }
		public string id { get; set; }
		public bool isOutbound { get; set; }
		public int jsonRpcPort { get; set; }
		public int protocolVersion { get; set; }
		public string publicKey { get; set; }
		public int roundTripTime { get; set; }
		public string syncState { get; set; }
		public int tlsJsonRpcPort { get; set; }
		public int tlsWebsocketPort { get; set; }
		public int websocketPort { get; set; }
		public string ledgerMode { get; set; }
	}

	public record struct Error
	{
		public int code { get; set; }
		public string message { get; set; }
		public string data { get; set; }
	}

	public record struct Result<T>
	{
		public string id { get; set; }
		public string jsonrpc { get; set; }
		public T? result { get; set; }

		public Error error { get; set; }
	}

	public record struct Results<T>
	{
		public string id { get; set; }
		public string jsonrpc { get; set; }
		public List<T> result { get; set; }
		public Error error { get; set; }
	}
}
