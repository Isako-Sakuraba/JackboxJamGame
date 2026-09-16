using UnityEngine;

namespace Game.UI
{
    public sealed class BackgroundMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _intro;
        [SerializeField] private AudioClip _loop;

        private bool _waitingForLoop;

        public void Begin()
        {
            if (!_audioSource)
                return;

            _audioSource.Stop();
            _audioSource.loop = false;

            if (_intro)
            {
                _audioSource.clip = _intro;
                _audioSource.Play();
                _waitingForLoop = true;
                return;
            }

            PlayLoop();
        }

        private void Update()
        {
            if (_waitingForLoop && !_audioSource.isPlaying)
                PlayLoop();
        }

        private void PlayLoop()
        {
            _waitingForLoop = false;
            if (!_audioSource || !_loop)
                return;

            _audioSource.clip = _loop;
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }
}
