using MessagePack;
using MessagePack.Formatters;
using VapSRServer.Networking.Base;
using VapSRServer.Networking.C2S;
using VapSRServer.Networking.Common;
using VapSRServer.Networking.PacketTypes;

namespace VapSRServer;

public class ServerResponseFormatter : IMessagePackFormatter<ServerResponse> 
{
	public ServerResponse Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) 
	{
		ServerResponse request = new();
		reader.ReadMapHeader();
		reader.ReadString();
		request.type = (S2CTypes)reader.ReadInt16();
    return request;
  }
	
	public void Serialize(ref MessagePackWriter writer, ServerResponse value, MessagePackSerializerOptions options) 
	{
		MessagePackSerializer.Serialize(ref writer, value, options);
	}
}