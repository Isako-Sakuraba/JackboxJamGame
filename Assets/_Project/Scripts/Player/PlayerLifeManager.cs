using Game.Environment;
using Game.Services;
using PurrNet.Lobby;
using PurrNet.Prediction;
using UnityEngine;

namespace Game.Player
{
    public sealed class PlayerLifeManager : PredictedIdentity<PlayerLifeManager.LifeState>
    {
        public struct LifeState : IPredictedData<LifeState>
        {
            public int DeathsThisMap;
            public int CurrentMapIndex;
            public float MapChangeTimer;
            public float RoundEndTimer;
            public bool IsChangingMap;
            public bool IsRoundFinished;
            public bool IsRoundExitReady;

            public void Dispose() { }
        }

        [SerializeField, Min(1)] private int _deathsPerMap = 5;
        [SerializeField, Min(1)] private int _deathsPerMapPerPlayer = 3;
        [SerializeField, Min(0f)] private float _mapChangeDelay = 3f;
        [SerializeField, Min(0f)] private float _roundEndDelay = 5f;
        [SerializeField] private MapChanger _mapChanger;

        private AdvancedPlayerSpawner _spawner;
        private PlayerScore _playerScore;
        private bool _hasVerifiedState;
        private LifeState _lastVerifiedState;

        public int DeathsThisMap => currentState.DeathsThisMap;
        public int CurrentMapIndex => currentState.CurrentMapIndex;
        public float MapChangeTimer => currentState.MapChangeTimer;
        public float RoundEndTimer => currentState.RoundEndTimer;
        public bool IsChangingMap => currentState.IsChangingMap;
        public bool IsRoundFinished => currentState.IsRoundFinished;
        public bool IsRoundExitReady => currentState.IsRoundExitReady;
        public float ViewMapChangeTimer => viewState.MapChangeTimer;
        public bool ViewIsChangingMap => viewState.IsChangingMap;
        public bool ViewIsRoundFinished => viewState.IsRoundFinished;

        private int _calculatedDeathsPerMap = 2;

        public override void OnPreSetup()
        {
            base.OnPreSetup();
            ServiceLocator.Register(this);

            if (GameOrchestrator.active != null && GameOrchestrator.active.activeLobby != null)
                _calculatedDeathsPerMap = GameOrchestrator.active.activeLobby.players.Count;
            else
                _calculatedDeathsPerMap = _deathsPerMap;
        }

        protected override void LateAwake()
        {
            base.LateAwake();

            _spawner = ServiceLocator.Get<AdvancedPlayerSpawner>();
            _playerScore = ServiceLocator.Get<PlayerScore>();
            if (!_mapChanger)
                _mapChanger = ServiceLocator.Get<MapChanger>();

            _mapChanger.Initialize(_spawner, currentState.CurrentMapIndex);
        }

        protected override LifeState GetInitialState()
        {
            return new LifeState
            {
                CurrentMapIndex = 0
            };
        }

        protected override void Simulate(ref LifeState state, float delta)
        {
            _mapChanger.Sim_ActivateMap(state.CurrentMapIndex);

            if (state.IsRoundFinished)
            {
                if (!state.IsRoundExitReady)
                {
                    state.RoundEndTimer = Mathf.Max(0f, state.RoundEndTimer - delta);
                    state.IsRoundExitReady = state.RoundEndTimer <= 0f;
                }

                return;
            }

            if (!state.IsChangingMap)
                return;

            state.MapChangeTimer = Mathf.Max(0f, state.MapChangeTimer - delta);
            if (state.MapChangeTimer > 0f)
                return;

            state.CurrentMapIndex++;
            state.DeathsThisMap = 0;
            state.IsChangingMap = false;

            _mapChanger.Sim_ActivateMap(state.CurrentMapIndex);
            _spawner.Sim_RespawnAllPlayers();
        }

        public void Sim_RecordDeath()
        {
            if (currentState.IsChangingMap || currentState.IsRoundFinished)
                return;

            currentState.DeathsThisMap++;
            if (currentState.DeathsThisMap < _calculatedDeathsPerMap)
                return;

            if (currentState.CurrentMapIndex >= _mapChanger.MapCount - 1)
            {
                _playerScore.Sim_FinalizeRound();
                currentState.IsRoundFinished = true;
                currentState.RoundEndTimer = _roundEndDelay;
                currentState.IsRoundExitReady = _roundEndDelay <= 0f;
                return;
            }

            currentState.IsChangingMap = true;
            currentState.MapChangeTimer = _mapChangeDelay;
        }

        protected override void UpdateView(LifeState viewState, LifeState? verified)
        {
            if (!verified.HasValue)
                return;

            LifeState next = verified.Value;
            if (!_hasVerifiedState)
            {
                _mapChanger.NotifyMapChanged(next.CurrentMapIndex);
                if (next.IsChangingMap)
                    _mapChanger.NotifyMapChangeAnnounced(next.CurrentMapIndex + 1, next.MapChangeTimer);
                if (next.IsRoundFinished)
                    _mapChanger.NotifyRoundFinished();
                if (next.IsRoundExitReady)
                    _mapChanger.NotifyRoundExitReady();

                _lastVerifiedState = next;
                _hasVerifiedState = true;
                return;
            }

            if (next.IsChangingMap && !_lastVerifiedState.IsChangingMap)
                _mapChanger.NotifyMapChangeAnnounced(next.CurrentMapIndex + 1, next.MapChangeTimer);

            if (next.CurrentMapIndex != _lastVerifiedState.CurrentMapIndex)
                _mapChanger.NotifyMapChanged(next.CurrentMapIndex);

            if (next.IsRoundFinished && !_lastVerifiedState.IsRoundFinished)
                _mapChanger.NotifyRoundFinished();

            if (next.IsRoundExitReady && !_lastVerifiedState.IsRoundExitReady)
                _mapChanger.NotifyRoundExitReady();

            _lastVerifiedState = next;
        }

        protected override void SetUnityState(LifeState state)
        {
            if (_mapChanger)
                _mapChanger.RestoreMap(state.CurrentMapIndex);
        }

        protected override void Destroyed()
        {
            if (ServiceLocator.TryGet<PlayerLifeManager>(out var registered) && ReferenceEquals(registered, this))
                ServiceLocator.Unregister<PlayerLifeManager>();
        }
    }
}
