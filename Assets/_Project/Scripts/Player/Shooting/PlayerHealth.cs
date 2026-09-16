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
            public float InvincibilityTimer;
            public float CurrentHealth;
            public int CurrentLives;

            public bool IsInvincible => InvincibilityTimer > 0f;
            public bool IsDead => CurrentHealth <= 0f;
            public bool IsFullyDead => IsDead && CurrentLives <= 0;

            public override string ToString()
            {
                return $"CurrentHealth: {CurrentHealth} | Lives: {CurrentLives}";
            }

            public void Dispose() { }
        }

        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private int _maxLives = 3;
        [SerializeField] private float _invincibilityTime = 2f;

        public event Action<PlayerID, float> Sim_Damaged = delegate { };
        public event Action<PlayerID> Sim_Died = delegate { };
        public event Action Verified_Died = delegate { };
        public event Action Verified_FullyDied = delegate { };
        public event Action Verified_Respawned = delegate { };
        public event Action Sim_FullyDied = delegate { };

        [NonSerialized] public PredictedEvent<float> Damaged;
        [NonSerialized] public PredictedEvent Died;

        public float MaxHealth => _maxHealth;
        public float InvincibilityTime => _invincibilityTime;
        public bool IsDead => currentState.CurrentHealth <= 0;
        public bool IsFullyDead => IsDead & currentState.CurrentLives <= 0;

        bool _wasVerifiedDead = false;
        bool _wasVerifiedFullyDead = false;

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
                CurrentLives = _maxLives,
                InvincibilityTimer = _invincibilityTime
            };
        }

        protected override void Simulate(ref HealthState state, float delta)
        {
            if (state.IsInvincible)
            {
                state.InvincibilityTimer = Mathf.Max(0f, state.InvincibilityTimer - delta);
            }
        }

        public void Sim_Damage(PlayerID from, float damage)
        {
            if (IsDead || currentState.IsInvincible) 
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

        public void Sim_Heal(float heal)
        {
            if (IsDead)
                return;

            currentState.CurrentHealth += heal;

            currentState.CurrentHealth = Mathf.Min(_maxHealth, currentState.CurrentHealth);
        }

        private void Sim_HandleJustDied()
        {
            currentState.CurrentLives = Mathf.Max(0, currentState.CurrentLives - 1);

            if (currentState.IsFullyDead)
            {
                Sim_FullyDied.Invoke();
            }
        }

        protected override void UpdateView(HealthState viewState, HealthState? verified)
        {
            if (!verified.HasValue)
                return;

            bool isVerifiedDead = verified.Value.IsDead;

            if (isVerifiedDead && !_wasVerifiedDead)
            {
                Verified_Died.Invoke();
            }
            else if (!isVerifiedDead && _wasVerifiedDead)
            {
                Verified_Respawned.Invoke();
            }

            _wasVerifiedDead = isVerifiedDead;


            bool isVerifiedFullyDead = verified.Value.IsFullyDead;

            if (isVerifiedFullyDead && !_wasVerifiedFullyDead)
            {
                Verified_FullyDied.Invoke();
            }

            _wasVerifiedFullyDead = isVerifiedFullyDead;
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
            currentState.InvincibilityTimer = _invincibilityTime;
        }
    }
}
