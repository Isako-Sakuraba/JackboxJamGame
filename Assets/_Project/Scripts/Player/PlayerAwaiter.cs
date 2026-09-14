using PurrNet;
using PurrNet.Lobby;
using PurrNet.Prediction;
using System;
using UnityEngine;

namespace Game.Player
{
    public sealed class PlayerAwaiter : MonoBehaviour
    {
        [SerializeField] private PredictionManager _predictionManager;

        public event Action OnAllPlayersJoined;

        private bool _invoked;

        private void Start()
        {
            Debug.Log($"PM is null: {_predictionManager == null}");
            Debug.Log($"PPs is null: {_predictionManager.players == null}");

            _predictionManager.players.onPlayerAdded += OnPlayerChanged;
            _predictionManager.players.onPlayerRemoved += OnPlayerChanged;

            CheckPlayers();
        }

        private void OnDestroy()
        {
            if (!_predictionManager || !_predictionManager.players)
                return;

            _predictionManager.players.onPlayerAdded -= OnPlayerChanged;
            _predictionManager.players.onPlayerRemoved -= OnPlayerChanged;
        }

        private void OnPlayerChanged(PlayerID _)
        {
            CheckPlayers();
        }

        private void CheckPlayers()
        {
            if (_invoked)
                return;

            int expectedPlayers = GetExpectedPlayerCount();

            if (_predictionManager.players.players.Count < expectedPlayers)
                return;

            _invoked = true;
            OnAllPlayersJoined?.Invoke();
        }

        private int GetExpectedPlayerCount()
        {
            if (GameOrchestrator.active != null && GameOrchestrator.active.activeLobby != null)
                return GameOrchestrator.active.activeLobby.players.Count;

            return 2; // For local UDP testing
        }
    }
}
