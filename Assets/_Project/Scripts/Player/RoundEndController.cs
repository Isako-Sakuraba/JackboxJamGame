using Game.Environment;
using Game.Services;
using PurrNet;
using PurrNet.Lobby;
using UnityEngine;

namespace Game.Player
{
    public sealed class RoundEndController : MonoBehaviour
    {
        [SerializeField] private GameOverBroadcaster _gameOverBroadcaster;

        private MapChanger _mapChanger;
        private bool _endingGame;

        private void Start()
        {
            _mapChanger = ServiceLocator.Get<MapChanger>();
            _mapChanger.RoundExitReady += OnRoundExitReady;

            if (_mapChanger.IsRoundExitReady)
                OnRoundExitReady();
        }

        private void OnDestroy()
        {
            if (_mapChanger)
                _mapChanger.RoundExitReady -= OnRoundExitReady;
        }

        private void OnRoundExitReady()
        {
            if (_endingGame || NetworkManager.main == null || !NetworkManager.main.isServer)
                return;

            _endingGame = true;

            if (GameSession.instance)
            {
                _gameOverBroadcaster.EndGame();
                return;
            }

            NetworkManager manager = NetworkManager.main;
            if (manager.isClient)
                manager.StopClient();
            if (manager.isServer)
                manager.StopServer();
        }
    }
}
