﻿using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuantumCore.API;
using QuantumCore.API.Game;
using QuantumCore.API.Game.World;
using QuantumCore.Auth;
using QuantumCore.Core.Networking;
using QuantumCore.Game.World.Entities;

namespace QuantumCore.Game
{
    public class GameConnection : Connection, IGameConnection
    {
        public IGameServer Server { get; }
        public Guid? AccountId { get; set; }
        public string Username { get; set; }
        public IPlayerEntity Player { get; set; }

        public GameConnection(GameServer server, TcpClient client, IPacketManager packetManager, ILogger<AuthConnection> logger, PluginExecutor pluginExecutor) 
            : base(logger, pluginExecutor, packetManager)
        {
            Server = server;
            Init(client);
        }

        protected override void OnHandshakeFinished()
        {
            GameServer.Instance.CallConnectionListener(this);
        }

        protected async override Task OnClose()
        {
            if (Player != null)
            {
                await World.World.Instance.DespawnEntity(Player);
            }

            await Server.RemoveConnection(this);
            
            // todo enable expiry on auth token
        }

        protected async override Task OnReceive(object packet)
        {
            await Server.CallListener(this, packet);
        }

        protected override long GetServerTime()
        {
            return Server.ServerTime;
        }
    }
}