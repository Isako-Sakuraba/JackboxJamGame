using System.Collections;
using PurrNet;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player
{
    public class RespawnIndicator : MonoBehaviour
    {
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private Transform _indicatorRoot;
        [SerializeField] private float _visibleDuration = 3f;
        [SerializeField] private float _rotationOffset = -90f;

        private Renderer[] _renderers;
        private Graphic[] _graphics;
        private Coroutine _hideCoroutine;
        private Coroutine _showWhenReadyCoroutine;
        private bool _isVisible;

        private void Awake()
        {
            _respawner ??= GetComponentInParent<PlayerRespawner>();
            _health ??= GetComponentInParent<PlayerHealth>();
            _indicatorRoot ??= transform;
            _renderers = _indicatorRoot.GetComponentsInChildren<Renderer>(true);
            _graphics = _indicatorRoot.GetComponentsInChildren<Graphic>(true);

            Hide();
        }

        private void OnEnable()
        {
            if (_respawner != null)
            {
                _respawner.Sim_Respawned += OnRespawned;
                _respawner.View_Respawned += OnRespawned;
            }

            if (_health != null)
                _health.Verified_Respawned += OnRespawned;

            Hide();
        }

        private void OnDisable()
        {
            if (_respawner != null)
            {
                _respawner.Sim_Respawned -= OnRespawned;
                _respawner.View_Respawned -= OnRespawned;
            }

            if (_health != null)
                _health.Verified_Respawned -= OnRespawned;

            CancelHide();
            CancelShowWhenReady();
            Hide();
        }

        private void Update()
        {
            if (!_isVisible)
                return;

            RotateTowardsWorldOrigin();
        }

        private void OnRespawned()
        {
            if (IsLocalPlayer())
            {
                Show();
                return;
            }

            CancelShowWhenReady();
            _showWhenReadyCoroutine = StartCoroutine(ShowWhenLocalPlayerReady());
        }

        private IEnumerator ShowWhenLocalPlayerReady()
        {
            float timeout = Time.time + 1f;

            while (Time.time < timeout)
            {
                yield return null;

                if (!IsLocalPlayer())
                    continue;

                _showWhenReadyCoroutine = null;
                Show();
                yield break;
            }

            _showWhenReadyCoroutine = null;
            Hide();
        }

        private void Show()
        {
            if (_indicatorRoot == null)
                return;

            CancelHide();
            _isVisible = true;
            _indicatorRoot.gameObject.SetActive(true);
            SetRenderersEnabled(true);
            RotateTowardsWorldOrigin();
            _hideCoroutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_visibleDuration);
            _hideCoroutine = null;
            Hide();
        }

        private void Hide()
        {
            _isVisible = false;
            SetRenderersEnabled(false);
        }

        private void SetRenderersEnabled(bool enabled)
        {
            if (_renderers == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].enabled = enabled;
            }

            if (_graphics == null)
                return;

            for (int i = 0; i < _graphics.Length; i++)
            {
                if (_graphics[i] != null)
                    _graphics[i].enabled = enabled;
            }
        }

        private void CancelHide()
        {
            if (_hideCoroutine == null)
                return;

            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        private void CancelShowWhenReady()
        {
            if (_showWhenReadyCoroutine == null)
                return;

            StopCoroutine(_showWhenReadyCoroutine);
            _showWhenReadyCoroutine = null;
        }

        private void RotateTowardsWorldOrigin()
        {
            Vector3 direction = Vector3.zero - _indicatorRoot.position;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _rotationOffset;
            _indicatorRoot.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private bool IsLocalPlayer()
        {
            if (NetworkManager.main == null || !NetworkManager.main.isLocalPlayerReady)
                return false;

            if (_health != null && _health.owner.HasValue)
                return _health.owner.Value.Equals(NetworkManager.main.localPlayer);

            if (_respawner != null && _respawner.owner.HasValue)
                return _respawner.owner.Value.Equals(NetworkManager.main.localPlayer);

            return false;
        }
    }
}
