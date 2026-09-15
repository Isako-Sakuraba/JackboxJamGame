using Game.Services;
using PurrNet;
using PurrNet.Prediction;
using System;
using UnityEngine;

namespace Game.Player
{
    public class PlayerRespawner : PredictedIdentity<PlayerRespawner.RespawnState>
    {
        public struct RespawnState : IPredictedData<RespawnState>
        {
            public float RespawnTimer;

            public void Dispose() { }
        }

        [SerializeField] private PlayerReferences _references;
        [SerializeField] private float _respawnTime = 3f;

        private AdvancedPlayerSpawner _spawner;

        public event Action Sim_Respawned = delegate { };
        public event Action View_Respawned = delegate { };


        [NonSerialized] public PredictedEvent Respawned;

        private bool _wasViewRespawning;

        protected override void LateAwake()
        {
            base.LateAwake();

            _references.PlayerHealth.Sim_Died += Sim_OnDied;
            _references.PlayerHealth.Sim_RoundDied += Sim_OnRoundDied;
            _references.PlayerHealth.Sim_GlobalDied += Sim_OnGlobalDied;

            _spawner = ServiceLocator.Get<AdvancedPlayerSpawner>();

            Respawned = new PredictedEvent(predictionManager, this);
        }

        protected override void Destroyed()
        {
            _references.PlayerHealth.Sim_Died -= Sim_OnDied;
            _references.PlayerHealth.Sim_RoundDied -= Sim_OnRoundDied;
            _references.PlayerHealth.Sim_GlobalDied -= Sim_OnGlobalDied;
        }

        protected override void Simulate(ref RespawnState state, float delta)
        {
            if (state.RespawnTimer > 0f)
            {
                state.RespawnTimer = Mathf.Max(0f, state.RespawnTimer - delta);

                if (state.RespawnTimer <= 0f)
                {
                    Sim_Respawn();
                }
            }
        }

        protected override void UpdateView(RespawnState viewState, RespawnState? verified)
        {
            if (!verified.HasValue)
                return;

            bool isViewRespawning = verified.Value.RespawnTimer > 0f;

            if (!isViewRespawning && _wasViewRespawning)
                View_Respawned.Invoke();

            _wasViewRespawning = isViewRespawning;
        }

        private void Sim_OnDied(PlayerID iD)
        {
            currentState.RespawnTimer = _respawnTime;
        }

        private void Sim_OnRoundDied()
        {
            currentState.RespawnTimer = _respawnTime;
        }

        private void Sim_OnGlobalDied()
        {

        }

        public void Sim_Respawn(bool fromDeath = true)
        {
            Vector2 position = _spawner.GetSafestPosition();
            _references.PlayerHealth.Respawn();
            _references.PlayerCameraWeapon.Respawn();
            _references.SimplePlayerController.Respawn();
            _references.SimplePlayerController.Sim_SetPosition(position);

            Sim_Respawned.Invoke();
            Respawned.Invoke();
        }
    }
}
