using UnityEngine;

namespace Game.Player
{
    public class PlayerDeathAudio : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _clip;

        private void Start()
        {
            _health.Verified_Died += OnVerifiedDied;
        }

        private void OnEnable()
        {
            if (didStart)
                _health.Verified_Died += OnVerifiedDied;
        }

        private void OnDisable()
        {
            if (didStart)
                _health.Verified_Died -= OnVerifiedDied;
        }

        private void OnVerifiedDied()
        {
            if (_audioSource == null || _clip == null)
                return;

            _audioSource.clip = _clip;
            _audioSource.Play();
        }
    }
}
