namespace VapSRServer;

public static class Rooms 
{
	private static Random random = new();
	private const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-+[]_=;";
	public static PrivateRoom[] rooms = [];
	public static bool RoomCodeExists(string code) 
	{
		return GetRoom(code) != null;
	}
	
	public static void RemoveRoom(string code) 
	{
		rooms = rooms.Where((room) => room.code != code).ToArray();
	}
	
	public static PrivateRoom GetRoom(string code) 
	{
		foreach (PrivateRoom room in rooms) 
		{
			if (room.code == code)
				return room;
		}
		return null;
	}
	
	public static PrivateRoom CreateRoom(ref Player player) 
	{
		PrivateRoom room = new() 
		{
			host = player,
			connected = [],
			code = GenerateCode()
		};
		player.SendResponse(SendingMessageType.PrivateRoomCreated, new RoomData() { code = room.code });
		rooms = [ .. rooms, room ];
		return room;
	}
	
	private static string GenerateCode() 
	{
		string code = new(Enumerable.Repeat(chars, 8)
			.Select(s => s[random.Next(s.Length)]).ToArray());
		if (RoomCodeExists(code))
			return GenerateCode();
		return code;
	}
}