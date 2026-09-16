using PurrNet;
using PurrNet.Lobby;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{

    [Serializable]
    public struct PlayerData
    {
        public string DisplayName;
        public PlayerID PlayerID;

        public PlayerData(string displayName, PlayerID playerID)
        {
            DisplayName = displayName;
            PlayerID = playerID;
        }
    }

    public sealed class PlayerDataManager : NetworkIdentity
    {
        private static PlayerDataManager _instance;

        [SerializeField]
        private SyncDictionary<PlayerID, PlayerData> _players = new(ownerAuth: false);

        public static IDictionary<PlayerID, PlayerData> Players => _instance._players;

        public static bool TryGetPlayers(out IDictionary<PlayerID, PlayerData> players)
        {
            players = _instance ? _instance._players : null;
            return players != null;
        }

        public static bool TryGetPlayerData(PlayerID player, out PlayerData data)
        {
            data = default;
            return _instance && _instance._players.TryGetValue(player, out data);
        }

        public static PlayerData LocalPlayerData
        {
            get
            {
                PlayerID localPlayer = NetworkManager.main.localPlayer;

                if (!_instance._players.TryGetValue(localPlayer, out PlayerData data))
                    throw new InvalidOperationException(
                        $"Player data for local player {localPlayer} is not available yet.");

                return data;
            }
        }

        public static bool TryGetLocalPlayerData(out PlayerData data)
        {
            data = default;

            if (_instance == null ||
                NetworkManager.main == null ||
                !NetworkManager.main.isLocalPlayerReady)
            {
                return false;
            }

            return _instance._players.TryGetValue(
                NetworkManager.main.localPlayer,
                out data);
        }

        protected override void OnSpawned(bool asServer)
        {
            _instance = this;

            if (asServer)
                return;

            if (GameOrchestrator.active && GameOrchestrator.active.sessionProvider.isLoggedIn)
                RegisterPlayer(GameOrchestrator.active.sessionProvider.playerName);
            else
                RegisterPlayer(localPlayer.HasValue ? localPlayer.ToString() : "Unknown ID");
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!asServer && _instance == this)
                _instance = null;
        }

        [ServerRpc(requireOwnership: false)]
        private void RegisterPlayer(string displayName, RPCInfo info = default)
        {
            PlayerID playerID = info.sender;

            _players[playerID] = new PlayerData(displayName,playerID);
        }

    }
}
