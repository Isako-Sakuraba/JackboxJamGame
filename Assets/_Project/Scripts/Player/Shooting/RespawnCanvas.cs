using System.Globalization;
using Game.Services;
using TMPro;
using UnityEngine;

namespace Game.Player
{
    public class RespawnCanvas : MonoBehaviour
    {
        public static RespawnCanvas Instance { get; private set; }

        [SerializeField] private Canvas _canvas;
        [SerializeField] private TMP_Text _respawnText;
        [SerializeField] private string _respawnTextFormat = "Respawning in {0}";

        private PlayerHealth _health;
        private PlayerRespawner _respawner;
        private PlayerLifeManager _lifeManager;

        private void Awake()
        {
            _canvas ??= GetComponent<Canvas>();

            SetVisible(false);

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
            ServiceLocator.Unregister<RespawnCanvas>();
        }

        private void Update()
        {
            _lifeManager ??= ServiceLocator.TryGet<PlayerLifeManager>(out var manager) ? manager : null;
            bool gameplayPaused = _lifeManager != null &&
                (_lifeManager.ViewIsChangingMap || _lifeManager.ViewIsRoundFinished);
            bool isDead = _health != null && _health.viewState.IsDead;
            SetVisible(isDead && !gameplayPaused);

            if (!isDead || gameplayPaused || _respawnText == null || _respawner == null)
                return;

            float respawnTimer = Mathf.Max(0f, _respawner.viewState.RespawnTimer);
            string timerText = respawnTimer.ToString("0.0", CultureInfo.InvariantCulture);
            _respawnText.text = string.Format(CultureInfo.InvariantCulture, _respawnTextFormat, timerText);
        }

        public void Bind(PlayerHealth health, PlayerRespawner respawner)
        {
            _health = health;
            _respawner = respawner;
        }

        public void Unbind(PlayerHealth health, PlayerRespawner respawner)
        {
            if (_health == health)
                _health = null;

            if (_respawner == respawner)
                _respawner = null;

            if (_health == null || _respawner == null)
                SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_canvas != null)
                _canvas.enabled = visible;
        }
    }
}
