using PurrNet;
using PurrNet.Prediction;
using TMPro;
using UnityEngine;

namespace Game.Player 
{
    public class PlayerName : StatelessPredictedIdentity
    {
        [SerializeField] private TMP_Text _nameText;

        private PlayerID? _lastOwner;

        public override void OnViewOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner)
        {
            base.OnViewOwnerChanged(oldOwner, newOwner);

            if (!newOwner.HasValue)
                return;

            if (PlayerDataManager.Players.TryGetValue(newOwner.Value, out var data))
            {
                _nameText.text = data.DisplayName;
            }
        }

        protected override void UpdateView()
        {
            if (owner == _lastOwner)
                return;

            _lastOwner = owner;

            Debug.Log($"Owner changed to {owner}");

            if (!owner.HasValue)
                return;

            if (PlayerDataManager.Players.TryGetValue(owner.Value, out var data))
                _nameText.text = data.DisplayName;
        }
    }
}
