using Game.Services;
using PurrNet;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerScoreKeeper : MonoBehaviour
    {
        private PlayerHealth _health;
        private PlayerScore _score;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        private void Start()
        {
            _score = ServiceLocator.Get<PlayerScore>();
        }

        private void OnEnable()
        {
            _health.Sim_Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            _health.Sim_Died -= OnPlayerDied;
        }

        private void OnPlayerDied(PlayerID id)
        {
            if (_health.owner.HasValue)
                _score.Sim_AddDeath(_health.owner.Value);

            if (!_health.owner.HasValue || !id.Equals(_health.owner.Value))
                _score.Sim_AddKill(id);
        }
    }
}
