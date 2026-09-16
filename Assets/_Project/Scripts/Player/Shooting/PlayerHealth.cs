using Game.Services;
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

            public bool IsInvincible => InvincibilityTimer > 0f;
            public bool IsDead => CurrentHealth <= 0f;

            public override string ToString()
            {
                return $"CurrentHealth: {CurrentHealth}";
            }

            public void Dispose() { }
        }

        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _invincibilityTime = 2f;

        public event Action<PlayerID, float> Sim_Damaged = delegate { };
        public event Action<PlayerID> Sim_Died = delegate { };
        public event Action Verified_Died = delegate { };
        public event Action Verified_Respawned = delegate { };

        [NonSerialized] public PredictedEvent<float> Damaged;
        [NonSerialized] public PredictedEvent Died;

        public float MaxHealth => _maxHealth;
        public float InvincibilityTime => _invincibilityTime;
        public bool IsDead => currentState.CurrentHealth <= 0;

        bool _wasVerifiedDead = false;
        private PlayerLifeManager _lifeManager;

        public override void OnPreSetup()
        {
            base.OnPreSetup();
            _lifeManager = ServiceLocator.Get<PlayerLifeManager>();
        }

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
            if (IsDead || currentState.IsInvincible ||
                (_lifeManager && (_lifeManager.IsChangingMap || _lifeManager.IsRoundFinished)))
                return;

            currentState.CurrentHealth -= damage;
            Sim_Damaged.Invoke(from, damage);
            Damaged.Invoke(damage);

            currentState.CurrentHealth = Mathf.Max(0f, currentState.CurrentHealth);

            if (IsDead)
            {
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
