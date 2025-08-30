using MessagePack;
using MessagePack.Formatters;
using VapSRServer.Networking.Base;
using VapSRServer.Networking.C2S;
using VapSRServer.Networking.Common;
using VapSRServer.Networking.PacketTypes;
using VapSRServer.Networking.S2C;

namespace VapSRServer;

public class ServerResponseFormatter : IMessagePackFormatter<ServerResponse> 
{
	public ServerResponse Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) 
	{
		ServerResponse request = new();
		reader.ReadMapHeader();
		reader.ReadString();
		request.type = (S2CTypes)reader.ReadInt16();
    reader.ReadString();
    request.data = request.type switch
    {
			S2CTypes.RngSeed => MessagePackSerializer.Deserialize<RngDataCommon>(ref reader, options),
			S2CTypes.OpponentForfeit => MessagePackSerializer.Deserialize<PlayerResultCommon>(ref reader, options),
			S2CTypes.MatchFound => MessagePackSerializer.Deserialize<PlayerResultCommon>(ref reader, options),
			S2CTypes.PrivateRoomJoinAttempt => MessagePackSerializer.Deserialize<RoomJoinAttemptS2C>(ref reader, options),
			S2CTypes.PlayerFinishedStage => MessagePackSerializer.Deserialize<PlayerCompletedStageS2C>(ref reader, options),
			S2CTypes.PrivateRoomCreated => MessagePackSerializer.Deserialize<RoomDataCommon>(ref reader, options),
			S2CTypes.ReplicateRoomData => MessagePackSerializer.Deserialize<RoomReplicationDataS2C>(ref reader, options),
			S2CTypes.OtherPlayerForfeit => MessagePackSerializer.Deserialize<PlayerResultCommon>(ref reader, options),
			S2CTypes.PrivateRoomRunFinished => MessagePackSerializer.Deserialize<PlayerCompletedStageS2C>(ref reader, options),
			S2CTypes.PrivateRoomBatchRunsFinished => MessagePackSerializer.Deserialize<BatchRoomRunsFinishedS2C>(ref reader, options),
			S2CTypes.PrivateRoomNewHost => MessagePackSerializer.Deserialize<PrivateRoomNewHostS2C>(ref reader, options),
			_ => null,
    };
    return request;
  }
	
	public void Serialize(ref MessagePackWriter writer, ServerResponse value, MessagePackSerializerOptions options) 
	{
		MessagePackSerializer.Serialize(ref writer, value, options);
	}
}