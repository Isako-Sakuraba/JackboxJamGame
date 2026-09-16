using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class ScoreboardPlayerEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _deathsText;

        public void SetData(string playerName, int kills, int deaths)
        {
            _nameText.text = playerName;
            _killsText.text = kills.ToString();
            _deathsText.text = deaths.ToString();
        }
    }
}
