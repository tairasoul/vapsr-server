namespace VapSRServer;

public static class Rooms 
{
	private static Random random = new Random();
	private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
	public static PrivateRoom[] rooms = [];
	public static bool RoomCodeExists(string code) 
	{
		return GetRoom(code) != null;
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
			player1 = player,
			code = GenerateCode()
		};
		player.SendResponse(new(SendingMessageType.PrivateRoomCreated, new RoomData() { code = room.code }));
		rooms = [ .. rooms, room ];
		return room;
	}
	
	private static string GenerateCode() 
	{
		string code = new string(Enumerable.Repeat(chars, 6)
			.Select(s => s[random.Next(s.Length)]).ToArray());
		if (RoomCodeExists(code))
			return GenerateCode();
		return code;
	}
}