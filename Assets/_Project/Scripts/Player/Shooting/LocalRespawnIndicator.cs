using System.Collections;
using PurrNet;
using UnityEngine;

namespace Game.Player
{
    public sealed class LocalRespawnIndicator : MonoBehaviour
    {
        [SerializeField] private PlayerReferences _references;
        [SerializeField] private GameObject _indicator;
        [SerializeField, Min(0f)] private float _visibleDuration = 1.5f;
        [SerializeField, Min(0f)] private float _rotationSmoothing = 12f;
        [SerializeField] private float _rotationOffset;

        private Coroutine _hideRoutine;

        private void Awake()
        {
            _indicator.SetActive(false);
        }

        private void OnEnable()
        {
            _references.PlayerHealth.Verified_Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            _references.PlayerHealth.Verified_Respawned -= OnRespawned;

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _hideRoutine = null;
            _indicator.SetActive(false);
        }

        private void Update()
        {
            if (!_indicator.activeSelf)
                return;

            Vector2 direction = _references.SimplePlayerController.transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _rotationOffset;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
            float t = 1f - Mathf.Exp(-_rotationSmoothing * Time.deltaTime);
            _indicator.transform.rotation = Quaternion.Slerp(_indicator.transform.rotation, targetRotation, t);
        }

        private void OnRespawned()
        {
            if (!_references.PlayerHealth.owner.HasValue ||
                NetworkManager.main == null ||
                _references.PlayerHealth.owner.Value != NetworkManager.main.localPlayer)
            {
                return;
            }

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _indicator.SetActive(true);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_visibleDuration);
            _indicator.SetActive(false);
            _hideRoutine = null;
        }
    }
}
