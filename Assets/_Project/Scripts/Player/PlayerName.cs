using PurrNet;
using PurrNet.Prediction;
using TMPro;
using UnityEngine;

namespace Game.Player 
{
    public class PlayerName : StatelessPredictedIdentity
    {
        [SerializeField] private TMP_Text _nameText;

        protected override void OnOwnerAssigned(PlayerID? player)
        {
            base.OnOwnerAssigned(player);

            if (!owner.HasValue)
                return;

            if (PlayerDataManager.Players.TryGetValue(owner.Value, out var data))
            {
                _nameText.text = data.DisplayName;
            }
        }
    }
}
