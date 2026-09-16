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
        private PlayerLifeManager _lifeManager;

        public event Action Sim_Respawned = delegate { };
        public event Action View_Respawned = delegate { };


        [NonSerialized] public PredictedEvent Respawned;

        private bool _wasViewRespawning;

        protected override void LateAwake()
        {
            base.LateAwake();

            _references.PlayerHealth.Sim_Died += Sim_OnDied;

            _spawner = ServiceLocator.Get<AdvancedPlayerSpawner>();
            _lifeManager = ServiceLocator.Get<PlayerLifeManager>();

            Respawned = new PredictedEvent(predictionManager, this);
        }

        protected override void Destroyed()
        {
            _references.PlayerHealth.Sim_Died -= Sim_OnDied;
        }

        protected override void Simulate(ref RespawnState state, float delta)
        {
            if (_lifeManager.IsChangingMap || _lifeManager.IsRoundFinished)
                return;

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
            _lifeManager.Sim_RecordDeath();
            currentState.RespawnTimer = _lifeManager.IsRoundFinished ? 0f : _respawnTime;
        }

        public void Sim_Respawn(bool fromDeath = true)
        {
            currentState.RespawnTimer = 0f;
            Vector2 position = _spawner.GetSafestPosition();
            _references.PlayerHealth.Respawn();
            _references.PlayerCameraWeapon.Respawn();
            _references.SimplePlayerController.Respawn();
            _references.SimplePlayerController.Sim_SetPosition(position);

            if (fromDeath)
            {
                Sim_Respawned.Invoke();
                Respawned.Invoke();
            }
        }
    }
}
