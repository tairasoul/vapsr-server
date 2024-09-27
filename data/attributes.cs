namespace VapSRServer;

[AttributeUsage(AttributeTargets.Method)]
public class MessageHandler : Attribute 
{
	public string type;
	
	public MessageHandler(string type) 
	{
		this.type = type;
	}
	
	public MessageHandler(ReceivingMessageType type) 
	{
		this.type = type.ToString();
	}
}