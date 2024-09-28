using MessagePack;
using MessagePack.Formatters;

namespace VapSRServer;

public class RequestFormatter : IMessagePackFormatter<Request> 
{
	public Request Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) 
	{
		Request request = new();
		reader.ReadMapHeader();
		reader.ReadString();
		request.type = reader.ReadString();
		reader.ReadString();
		
		switch (request.type) 
		{
			case "UserInfo":
				request.data = MessagePackSerializer.Deserialize<UserInfo>(ref reader, options);
				break;
			case "RouteStageFinished":
				request.data = MessagePackSerializer.Deserialize<PlayerCompletedStageInfo>(ref reader, options);
				break;
			case "RngSeed":
				request.data = MessagePackSerializer.Deserialize<RngData>(ref reader, options);
				break;
			case "RunFinished":
				request.data = MessagePackSerializer.Deserialize<RunFinishedInfo>(ref reader, options);
				break;
			default:
				request.data = null;
				break;
		}
		
		return request;
	}
	
	public void Serialize(ref MessagePackWriter writer, Request value, MessagePackSerializerOptions options) 
	{
		MessagePackSerializer.Serialize(ref writer, value, options);
	}
}