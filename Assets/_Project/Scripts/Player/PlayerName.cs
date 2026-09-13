using PurrNet;
using PurrNet.Lobby;
using TMPro;
using UnityEngine;

namespace Game.Player 
{
    public class PlayerName : NetworkIdentity
    {
        [SerializeField] private TMP_Text _nameText;

        private string _displayName;

        protected override void OnSpawned()
        {
            if (!isOwner)
                return;

            string displayName = localPlayer.ToString();

            if (GameOrchestrator.active != null 
                && GameOrchestrator.active.sessionProvider != null)
            {
                displayName = GameOrchestrator.active.sessionProvider.playerName;
            }

            SetDisplayName(displayName);
        }

        [ServerRpc]
        private void SetDisplayName(string name)
        {
            _displayName = name;
            SetDisplayNameOnClients(_displayName);
        }

        [ObserversRpc(bufferLast: true)]
        private void SetDisplayNameOnClients(string name)
        {
            _displayName = name;
            UpdateDisplayName();
        }

        private void UpdateDisplayName()
        {
            _nameText.text = _displayName;
        }
    }
}
