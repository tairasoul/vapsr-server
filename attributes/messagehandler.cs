using VapSRServer.Networking.PacketTypes;

namespace VapSRServer.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class MessageHandler : Attribute 
{
	public C2STypes type;
	
	public MessageHandler(C2STypes type) 
	{
		this.type = type;
	}
}