using Game.Services;
using PurrNet;
using PurrNet.Lobby;
using PurrNet.Prediction;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player
{
    public sealed class InitialSpawnTimer : PredictedIdentity<InitialSpawnTimer.TimerState>
    {
        [SerializeField] private float _spawnTime = 3f;

        [SerializeField] private int _expectedPlayerCountFallback = 2;

        public UnityEvent SimAllJoined = new();

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
                state.Time = _spawnTime;

            if (state.Time > 0f)
            {
                state.Time = Mathf.Max(0f, state.Time - delta);

                if (state.Time <= 0f)
                {
                    SimAllJoined.Invoke();
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
