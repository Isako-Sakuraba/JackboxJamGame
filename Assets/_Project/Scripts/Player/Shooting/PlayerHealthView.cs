using UnityEngine;

namespace Game.Player
{
    public class PlayerHealthView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private Transform _target;

        public float MaxHealth => _playerHealth != null ? _playerHealth.MaxHealth : 0f;

        private void Awake()
        {
            _playerHealth ??= GetComponentInParent<PlayerHealth>();
        }

        private void Update()
        {
            if (_playerHealth == null || _target == null)
                return;

            UpdateView(_playerHealth.viewState);
        }

        private void UpdateView(PlayerHealth.HealthState viewState)
        {
            float normalizedHealth = MaxHealth > 0f
                ? Mathf.Clamp01(viewState.CurrentHealth / MaxHealth)
                : 0f;

            Vector3 scale = _target.localScale;
            scale.x = normalizedHealth;
            _target.localScale = scale;
        }
    }
}
