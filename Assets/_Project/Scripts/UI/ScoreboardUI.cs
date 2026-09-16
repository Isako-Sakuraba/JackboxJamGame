using Game.Core;
using Game.Player;
using Game.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    public sealed class ScoreboardUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _content;
        [SerializeField] private ScoreboardPlayerEntry _entryPrefab;
        [SerializeField] private PlayerScore _playerScore;

        private readonly List<ScoreboardPlayerEntry> _entries = new();
        private IInputService _input;

        private void Start()
        {
            _input = ServiceLocator.Get<IInputService>();
            _panel.SetActive(false);
        }

        private void Update()
        {
            bool visible = _input.Scoreboard.Held;
            _panel.SetActive(visible);
            if (!visible)
                return;

            List<PlayerScore.ScoreResult> scores = _playerScore.GetViewScores();
            EnsureEntryCount(scores.Count);

            for (int i = 0; i < _entries.Count; i++)
            {
                bool active = i < scores.Count;
                _entries[i].gameObject.SetActive(active);
                if (!active)
                    continue;

                PlayerScore.ScoreResult result = scores[i];
                string name = PlayerDataManager.TryGetPlayerData(result.Player, out PlayerData data)
                    ? data.DisplayName
                    : result.Player.ToString();
                _entries[i].SetData(name, result.Score.Kills, result.Score.Deaths);
            }
        }

        private void EnsureEntryCount(int count)
        {
            while (_entries.Count < count)
                _entries.Add(Instantiate(_entryPrefab, _content));
        }
    }
}
