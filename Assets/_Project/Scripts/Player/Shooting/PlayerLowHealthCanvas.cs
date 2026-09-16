using Game.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player
{
    public class PlayerLowHealthCanvas : MonoBehaviour
    {
        public static PlayerLowHealthCanvas Instance { get; private set; }

        [SerializeField] private Image _vignetteImage;
        [SerializeField] private Vector2 _alphaRange = new(0f, 0.8f);
        [SerializeField] private float _healthThreshold = 30f;
        [SerializeField] private float _lerpSpeed = 10f;

        private PlayerHealth _health;

        private void Awake()
        {
            if (_vignetteImage != null)
            {
                Color color = _vignetteImage.color;
                color.a = 0f;
                _vignetteImage.color = color;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null;
            ServiceLocator.Unregister<PlayerLowHealthCanvas>();
        }

        private void Update()
        {
            if (_vignetteImage == null)
                return;

            float targetAlpha = GetTargetAlpha();
            Color color = _vignetteImage.color;
            color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * _lerpSpeed);
            _vignetteImage.color = color;
        }

        public void Bind(PlayerHealth health)
        {
            _health = health;
        }

        public void Unbind(PlayerHealth health)
        {
            if (_health == health)
                _health = null;
        }

        private float GetTargetAlpha()
        {
            if (_health == null)
                return 0f;

            PlayerHealth.HealthState state = _health.viewState;

            if (state.IsDead || state.CurrentHealth >= _healthThreshold)
                return 0f;

            float healthT = _healthThreshold > 0f
                ? Mathf.InverseLerp(_healthThreshold, 0f, state.CurrentHealth)
                : 1f;

            return Mathf.Lerp(_alphaRange.x, _alphaRange.y, healthT);
        }
    }
}
