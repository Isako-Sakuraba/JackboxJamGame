using PurrNet.Prediction;
using System;
using UnityEngine;

namespace Game.Player
{
    public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>
    {
        public struct HealthState : IPredictedData<HealthState>
        {
            public float CurrentHealth;

            public void Dispose() { }
        }

        [SerializeField] private float _maxHealth = 100f;

        public event Action<float> Sim_Damaged = delegate { };
        public event Action Sim_Died = delegate { };

        public PredictedEvent<float> Damaged;
        public PredictedEvent<float> Died;

        public bool IsDead => currentState.CurrentHealth <= 0;

        protected override void LateAwake()
        {
            base.LateAwake();

            Damaged = new(predictionManager, this);
            Died = new(predictionManager, this);
        }

        protected override HealthState GetInitialState()
        {
            return new HealthState() { CurrentHealth = _maxHealth };
        }

        public void Sim_Damage(float damage)
        {
            if (IsDead) 
                return;

            currentState.CurrentHealth -= damage;
            Sim_Damaged.Invoke(damage);
            Damaged.Invoke(damage);

            currentState.CurrentHealth = Mathf.Max(0f, currentState.CurrentHealth);

            if (IsDead)
            {
                Sim_Died.Invoke();
                Died.Invoke(damage);
            }
        }
    }
}
