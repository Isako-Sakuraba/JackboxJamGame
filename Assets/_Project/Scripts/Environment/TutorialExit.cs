using PurrNet;
using PurrNet.Lobby;
using PurrNet.Prediction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class TutorialExit : PredictedIdentity<TutorialExit.ExitState>
    {
        public struct ExitState : IPredictedData<ExitState>
        {
            public bool Triggered;

            public void Dispose() { }
        }

        [SerializeField] private GameOverBroadcaster _gameOverBroadcaster;
        [SerializeField, Min(0)] private int _fallbackSceneIndex;

        private bool _wasVerifiedTriggered;
        private bool _exiting;

        public void Sim_Trigger()
        {
            currentState.Triggered = true;
        }

        protected override void UpdateView(ExitState viewState, ExitState? verified)
        {
            if (!verified.HasValue)
                return;

            bool isVerifiedTriggered = verified.Value.Triggered;
            if (isVerifiedTriggered && !_wasVerifiedTriggered &&
                IsAllowedToExit())
            {
                Exit();
            }

            _wasVerifiedTriggered = isVerifiedTriggered;
        }

        private bool IsAllowedToExit()
        {
            return !_exiting && (NetworkManager.main == null || NetworkManager.main.isServer);
        }

        private void Exit()
        {
            _exiting = true;

            if (GameSession.instance != null)
            {
                _gameOverBroadcaster.EndGame();
                return;
            }

            NetworkManager manager = NetworkManager.main;
            if (manager != null)
            {
                if (manager.isClient)
                    manager.StopClient();

                if (manager.isServer)
                    manager.StopServer();
            }

            SceneManager.LoadSceneAsync(_fallbackSceneIndex);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
            _gameOverBroadcaster = FindFirstObjectByType<GameOverBroadcaster>();
        }
#endif
    }
}
