using VapSRServer.Data.Players;
using VapSRServer.Networking.Common;
using VapSRServer.Networking.PacketTypes;

namespace VapSRServer.Data.Rooms;

public class Rooms 
{
	private Random random = new();
	private const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-+[]_=;";
	public Dictionary<string, PrivateRoom> rooms = [];
	public bool RoomCodeExists(string code) 
	{
		return GetRoom(code) != null;
	}
	
	public void RemoveRoom(string code) 
	{
		rooms = (Dictionary<string, PrivateRoom>)rooms.Where((pair) => pair.Key != code);
	}
	
	public PrivateRoom GetRoom(string code) 
	{
    if (rooms.TryGetValue(code, out PrivateRoom room))
      return room;
    return null;
	}
	
	public PrivateRoom CreateRoom(ref Player player) 
	{
		PrivateRoom room = new() 
		{
			host = player,
			connected = [],
			code = GenerateCode()
		};
		player.SendResponse(S2CTypes.PrivateRoomCreated, new RoomDataCommon() { code = room.code });
    rooms.Add(room.code, room);
    return room;
	}
	
	private string GenerateCode() 
	{
		string code = new(Enumerable.Repeat(chars, 8)
			.Select(s => s[random.Next(s.Length)]).ToArray());
		if (RoomCodeExists(code))
			return GenerateCode();
		return code;
	}
}