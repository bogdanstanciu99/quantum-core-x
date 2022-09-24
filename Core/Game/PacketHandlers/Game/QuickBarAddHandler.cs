﻿using System.Threading;
using System.Threading.Tasks;
using QuantumCore.Core.Networking;
using QuantumCore.Game.Packets.QuickBar;

namespace QuantumCore.Game.PacketHandlers.Game;

public class QuickBarAddHandler : IPacketHandler<QuickBarAdd>
{
    public async Task ExecuteAsync(PacketContext<QuickBarAdd> ctx, CancellationToken token = default)
    {
        var player = ctx.Connection.Player;
        if (player == null)
        {
            ctx.Connection.Close();
            return;
        }

        await player.QuickSlotBar.Add(ctx.Packet.Position, ctx.Packet.Slot);
    }
}