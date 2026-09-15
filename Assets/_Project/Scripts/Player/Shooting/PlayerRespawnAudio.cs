using UnityEngine;

namespace Game.Player
{
    public class PlayerRespawnAudio : MonoBehaviour
    {
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _playChance = 1f;

        private void Start()
        {
            _respawner.View_Respawned += OnRespawned;
        }

        private void OnEnable()
        {
            if (didStart)
                _respawner.View_Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            if (didStart)
                _respawner.View_Respawned -= OnRespawned;
        }

        private void OnRespawned()
        {
            if (_audioSource == null || _clip == null)
                return;

            if (Random.value > _playChance)
                return;

            _audioSource.clip = _clip;
            _audioSource.Play();
        }
    }
}
