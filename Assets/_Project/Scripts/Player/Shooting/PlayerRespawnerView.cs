using System.Collections;
using UnityEngine;

namespace Game.Player
{
    public class PlayerRespawnerView : MonoBehaviour
    {
        private static readonly WaitForSeconds ShowViewRootDelay = new(0.05f);

        [SerializeField] private PlayerReferences _references;
        [SerializeField] private ParticleSystem _particles;
        [SerializeField] private GameObject _viewRoot;

        private PlayerHealth _health => _references.PlayerHealth;
        private Coroutine _showViewRootCoroutine;

        private void Subscribe()
        {
            _health.Verified_Died += OnDied;
            _health.Verified_Respawned += OnRespawned;
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnEnable()
        {
            if (didStart)
                Subscribe();
        }

        private void OnDisable()
        {
            if (didStart)
                Unsubscribe();

            CancelShowViewRoot();
        }

        private void Unsubscribe()
        {
            _health.Verified_Died -= OnDied;
            _health.Verified_Respawned -= OnRespawned;
        }

        private void OnRespawned()
        {
            _particles.gameObject.SetActive(false);
            CancelShowViewRoot();
            _showViewRootCoroutine = StartCoroutine(ShowViewRootAfterDelay());
        }

        private void OnDied()
        {
            CancelShowViewRoot();
            _particles.gameObject.SetActive(true);
            _particles.transform.position = _viewRoot.transform.position;
            _particles.Play();
            _viewRoot.SetActive(false);
        }

        private IEnumerator ShowViewRootAfterDelay()
        {
            yield return ShowViewRootDelay;
            _viewRoot.SetActive(true);
            _showViewRootCoroutine = null;
        }

        private void CancelShowViewRoot()
        {
            if (_showViewRootCoroutine == null)
                return;

            StopCoroutine(_showViewRootCoroutine);
            _showViewRootCoroutine = null;
        }
    }
}
