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

        request.data = request.type switch
        {
            "UserInfo" => MessagePackSerializer.Deserialize<UserInfo>(ref reader, options),
            "RouteStageFinished" => MessagePackSerializer.Deserialize<PlayerCompletedStageInfo>(ref reader, options),
            "RngSeed" => MessagePackSerializer.Deserialize<RngData>(ref reader, options),
            "RunFinished" => MessagePackSerializer.Deserialize<RunFinishedInfo>(ref reader, options),
            "JoinPrivateRoom" => MessagePackSerializer.Deserialize<RoomData>(ref reader, options),
            _ => null,
        };
        return request;
	}
	
	public void Serialize(ref MessagePackWriter writer, Request value, MessagePackSerializerOptions options) 
	{
		MessagePackSerializer.Serialize(ref writer, value, options);
	}
}