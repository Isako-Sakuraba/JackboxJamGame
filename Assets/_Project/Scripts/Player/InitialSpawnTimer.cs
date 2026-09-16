using Game.Services;
using PurrNet;
using PurrNet.Lobby;
using PurrNet.Prediction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Game.Player
{
    public sealed class InitialSpawnTimer : PredictedIdentity<InitialSpawnTimer.TimerState>
    {
        [SerializeField] private float _spawnTime = 3f;

        [SerializeField] private int _expectedPlayerCountFallback = 2;

        [FormerlySerializedAs("SimAllJoined")]
        public UnityEvent Sim_AllJoinedAfterTimer = new();
        public UnityEvent Sim_AllJoinedBeforeTimer = new();

        public float ViewTime => viewState.Time;
        public bool ViewFired => viewState.Fired;
        public bool ViewCountdownActive => viewState.Time > 0f && !viewState.Fired;

        protected override TimerState GetInitialState()
        {
            TimerState initial = new TimerState
            {
                ExpectedPlayers = GetNumberOfExpectedPlayers(),
                CurrentPlayers = 0,
                Time = 0,
                Fired = false
            };

            return initial;
        }

        private int GetNumberOfExpectedPlayers()
        {
            if (GameOrchestrator.active != null && GameOrchestrator.active.activeLobby != null)
                return GameOrchestrator.active.activeLobby.players.Count;

            return _expectedPlayerCountFallback;
        }

        protected override void Simulate(ref TimerState state, float delta)
        {
            if (state.Fired)
                return;

            state.CurrentPlayers = predictionManager.players.players.Count;

            if (state.CurrentPlayers == state.ExpectedPlayers && state.Time <= 0f)
            {
                state.Time = _spawnTime;
                Sim_AllJoinedBeforeTimer.Invoke();
            }

            if (state.Time > 0f)
            {
                state.Time = Mathf.Max(0f, state.Time - delta);

                if (state.Time <= 0f)
                {
                    Sim_AllJoinedAfterTimer.Invoke();
                    state.Fired = true;
                }
            }
        }

        public struct TimerState : IPredictedData<TimerState>
        {
            public int CurrentPlayers;
            public int ExpectedPlayers;

            public float Time;

            public bool Fired;

            public void Dispose() { }
        }
    }
}
