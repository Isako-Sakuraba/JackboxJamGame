using PurrNet;
using PurrNet.Prediction;
using System;
using UnityEngine;

namespace Game.Player
{
    public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>, IRespawnable
    {
        public struct HealthState : IPredictedData<HealthState>
        {
            public float CurrentHealth;
            public int GlobalLives;
            public int RoundLives;

            public bool IsDead => CurrentHealth <= 0f;
            public bool IsRoundDead => IsDead && RoundLives <= 0;
            public bool IsGlobalDead => IsRoundDead && GlobalLives <= 0;

            public void Dispose() { }
        }

        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private int _maxGlobalLives = 3;
        [SerializeField] private int _maxRoundLives = 3;

        public event Action<PlayerID, float> Sim_Damaged = delegate { };
        public event Action<PlayerID> Sim_Died = delegate { };
        public event Action Sim_RoundDied = delegate { };
        public event Action Sim_GlobalDied = delegate { };

        [NonSerialized] public PredictedEvent<float> Damaged;
        [NonSerialized] public PredictedEvent Died;

        public int FullLives => currentState.GlobalLives * _maxGlobalLives + currentState.RoundLives;

        public float MaxHealth => _maxHealth;
        public bool IsDead => currentState.CurrentHealth <= 0;

        protected override void LateAwake()
        {
            base.LateAwake();

            Damaged = new(predictionManager, this);
            Died = new(predictionManager, this);
        }

        protected override HealthState GetInitialState()
        {
            return new HealthState() { 
                CurrentHealth = _maxHealth,
                RoundLives = _maxRoundLives,
                GlobalLives = _maxGlobalLives
            };
        }

        public void Sim_Damage(PlayerID from, float damage)
        {
            if (IsDead) 
                return;

            currentState.CurrentHealth -= damage;
            Sim_Damaged.Invoke(from, damage);
            Damaged.Invoke(damage);

            currentState.CurrentHealth = Mathf.Max(0f, currentState.CurrentHealth);

            if (IsDead)
            {
                Sim_HandleJustDied();
                Sim_Died.Invoke(from);
                Died.Invoke();
            }
        }

        private void Sim_HandleJustDied()
        {
            currentState.RoundLives = Mathf.Max(0, currentState.RoundLives - 1);

            if (currentState.IsRoundDead)
            {
                currentState.GlobalLives = Mathf.Max(0, currentState.GlobalLives - 1);

                Sim_RoundDied.Invoke();

                if (currentState.IsGlobalDead)
                {
                    Sim_GlobalDied.Invoke();
                }
            }
        }

        protected override HealthState Interpolate(HealthState from, HealthState to, float t)
        {
            var interpolated = to;
            interpolated.CurrentHealth = to.CurrentHealth;

            return interpolated;
        }

        public void Respawn()
        {
            currentState.CurrentHealth = MaxHealth;
        }
    }
}
