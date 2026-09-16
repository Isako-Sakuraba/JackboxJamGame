using Game.Services;
using PurrNet;
using UnityEngine;

namespace Game.Player
{
    public class RespawnCanvasBinder : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private PlayerRespawner _respawner;

        private RespawnCanvas _canvas;
        private bool _isBound;

        private void Awake()
        {
            _health ??= GetComponentInParent<PlayerHealth>();
            _respawner ??= GetComponentInParent<PlayerRespawner>();
        }

        private void OnEnable()
        {
            UpdateBinding();
        }

        private void Update()
        {
            UpdateBinding();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void UpdateBinding()
        {
            if (_health == null || _respawner == null)
                return;

            if (!IsLocalPlayer())
            {
                Unbind();
                return;
            }

            if (_isBound)
                return;

            if (_canvas == null && !ServiceLocator.TryGet(out _canvas))
                return;

            _canvas.Bind(_health, _respawner);
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound)
                return;

            if (_canvas != null)
                _canvas.Unbind(_health, _respawner);

            _isBound = false;
        }

        private bool IsLocalPlayer()
        {
            if (NetworkManager.main == null || !NetworkManager.main.isLocalPlayerReady)
                return false;

            if (_health.owner.HasValue)
                return _health.owner.Value.Equals(NetworkManager.main.localPlayer);

            if (_respawner.owner.HasValue)
                return _respawner.owner.Value.Equals(NetworkManager.main.localPlayer);

            return false;
        }
    }
}
