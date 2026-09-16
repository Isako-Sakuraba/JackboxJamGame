using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

namespace Game.Player
{
    public sealed class PlayerKillAudio : StatelessPredictedIdentity
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _clip;

        private void OnEnable()
        {
            PlayerHealth.Verified_Kill += OnVerifiedKill;
        }

        private void OnDisable()
        {
            PlayerHealth.Verified_Kill -= OnVerifiedKill;
        }

        private void OnVerifiedKill(PlayerID killer)
        {
            if (!owner.HasValue || !predictionManager.localPlayer.HasValue ||
                owner.Value != killer || predictionManager.localPlayer.Value != killer ||
                !_audioSource || !_clip)
            {
                return;
            }

            _audioSource.clip = _clip;
            _audioSource.Play();
        }
    }
}
