using PurrNet;
using UnityEngine;

namespace Game.Player
{
    public class PlayerRespawnerView : MonoBehaviour
    {
        [SerializeField] private PlayerReferences _references;
        [SerializeField] private ParticleSystem _particles;
        [SerializeField] private GameObject _viewRoot;

        private PlayerHealth _health => _references.PlayerHealth;
        private PlayerRespawner _respawner => _references.PlayerRespawner;

        private void Subscribe()
        {
            _health.Sim_Died += OnDied;
            _respawner.Sim_Respawned += OnRespawned;
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
        }

        private void Unsubscribe()
        {
            _health.Sim_Died -= OnDied;
            _respawner.Sim_Respawned -= OnRespawned;
        }

        private void OnRespawned()
        {
            _particles.gameObject.SetActive(false);
            _viewRoot.SetActive(true);
        }

        private void OnDied(PlayerID id)
        {
            _particles.gameObject.SetActive(true);
            _particles.transform.position = _viewRoot.transform.position;
            _particles.Play();
            _viewRoot.SetActive(false);
        }
    }
}
