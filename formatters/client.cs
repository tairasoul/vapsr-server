using MessagePack;
using MessagePack.Formatters;
using VapSRServer.Networking.Base;
using VapSRServer.Networking.C2S;
using VapSRServer.Networking.Common;
using VapSRServer.Networking.PacketTypes;

namespace VapSRServer;

public class ClientRequestFormatter : IMessagePackFormatter<ClientRequest> 
{
	public ClientRequest Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) 
	{
		ClientRequest request = new();
		reader.ReadMapHeader();
		reader.ReadString();
		request.type = (C2STypes)reader.ReadInt16();
		reader.ReadString();

        request.data = request.type switch
        {
            C2STypes.UserInfo => MessagePackSerializer.Deserialize<UserInfoC2S>(ref reader, options),
            C2STypes.RouteStageFinished => MessagePackSerializer.Deserialize<PlayerCompletedStageC2S>(ref reader, options),
            C2STypes.RngSeed => MessagePackSerializer.Deserialize<RngDataCommon>(ref reader, options),
            C2STypes.RunFinished => MessagePackSerializer.Deserialize<RunFinishedC2S>(ref reader, options),
            C2STypes.JoinPrivateRoom => MessagePackSerializer.Deserialize<RoomDataCommon>(ref reader, options),
            _ => null,
        };
        return request;
	}
	
	public void Serialize(ref MessagePackWriter writer, ClientRequest value, MessagePackSerializerOptions options) 
	{
		MessagePackSerializer.Serialize(ref writer, value, options);
	}
}