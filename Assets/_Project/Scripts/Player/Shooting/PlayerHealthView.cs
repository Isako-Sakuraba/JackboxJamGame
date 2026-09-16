using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Player
{
    public class PlayerHealthView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private Transform _target;
        [SerializeField, FormerlySerializedAs("_blinkRenderer")] private SpriteRenderer[] _blinkRenderers;

        [Header("Invincibility Blink")]
        [SerializeField] private float _blinkFrequency = 10f;
        [SerializeField, Range(0f, 1f)] private float _blinkAlpha = 0.35f;

        private float[] _defaultBlinkAlphas;

        public float MaxHealth => _playerHealth != null ? _playerHealth.MaxHealth : 0f;

        private void Awake()
        {
            _playerHealth ??= GetComponentInParent<PlayerHealth>();

            _defaultBlinkAlphas = new float[_blinkRenderers.Length];

            for (int i = 0; i < _blinkRenderers.Length; i++)
                _defaultBlinkAlphas[i] = _blinkRenderers[i] != null ? _blinkRenderers[i].color.a : 1f;
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

            UpdateBlink(viewState);
        }

        private void UpdateBlink(PlayerHealth.HealthState viewState)
        {
            if (_blinkRenderers == null || _blinkRenderers.Length == 0)
                return;

            bool visible = !viewState.IsInvincible || Mathf.FloorToInt(Time.time * _blinkFrequency) % 2 == 0;

            for (int i = 0; i < _blinkRenderers.Length; i++)
            {
                SpriteRenderer blinkRenderer = _blinkRenderers[i];

                if (blinkRenderer == null)
                    continue;

                float defaultAlpha = i < _defaultBlinkAlphas.Length ? _defaultBlinkAlphas[i] : 1f;
                Color color = blinkRenderer.color;
                color.a = visible ? defaultAlpha : _blinkAlpha;
                blinkRenderer.color = color;
            }
        }
    }
}
