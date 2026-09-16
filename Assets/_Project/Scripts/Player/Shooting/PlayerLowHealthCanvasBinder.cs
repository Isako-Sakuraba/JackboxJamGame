using Game.Services;
using PurrNet;
using UnityEngine;

namespace Game.Player
{
    public class PlayerLowHealthCanvasBinder : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _health;

        private PlayerLowHealthCanvas _canvas;
        private bool _isBound;

        private void Awake()
        {
            _health ??= GetComponentInParent<PlayerHealth>();
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
            if (_health == null)
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

            _canvas.Bind(_health);
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound)
                return;

            if (_canvas != null)
                _canvas.Unbind(_health);

            _isBound = false;
        }

        private bool IsLocalPlayer()
        {
            if (!_health.owner.HasValue || NetworkManager.main == null || !NetworkManager.main.isLocalPlayerReady)
                return false;

            return _health.owner.Value.Equals(NetworkManager.main.localPlayer);
        }
    }
}
