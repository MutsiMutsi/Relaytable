using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relaytable.Models
{
	// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
	public class NodeStatusModel
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

	public class NodeStatusResult
	{
		public string id { get; set; }
		public string jsonrpc { get; set; }
		public NodeStatusModel result { get; set; }
	}
}
