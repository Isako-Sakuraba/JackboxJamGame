using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

namespace Game.Player
{
    public class AudioVolumeOnOwnership : StatelessPredictedIdentity
    {
        [SerializeField] private AudioSource[] _sources;
        [SerializeField] private float _nonOwnerVolume;

        public override void OnViewOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner)
        {
            if (!newOwner.HasValue)
                return;

            if (!predictionManager.localPlayer.HasValue)
                return;

            PlayerID local = predictionManager.localPlayer.Value;
            PlayerID owner = newOwner.Value;

            if (local == owner)
                return;

            foreach (var source in _sources)
                source.volume = _nonOwnerVolume;
        }
    }
}
