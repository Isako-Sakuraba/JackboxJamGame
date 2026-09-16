using Game.Environment;
using Game.Player;
using Game.Services;
using PurrNet;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class RoundFlowUI : MonoBehaviour
    {
        [SerializeField] private GameObject _mapChangePanel;
        [SerializeField] private TMP_Text _mapChangeText;
        [SerializeField] private GameObject _roundEndPanel;
        [SerializeField] private TMP_Text _roundEndText;
        [SerializeField] private PlayerScore _playerScore;
        [SerializeField] private AudioSource _winAudioSource;
        [SerializeField] private AudioClip _winClip;
        [SerializeField] private List<ParticleSystem> _winParticles = new();

        private readonly StringBuilder _text = new();
        private MapChanger _mapChanger;
        private bool _wasRoundFinished;

        private void Start()
        {
            _mapChanger = ServiceLocator.Get<MapChanger>();
            _mapChangeText.richText = false;
            _roundEndText.richText = false;
            _mapChangePanel.SetActive(false);
            _roundEndPanel.SetActive(false);
        }

        private void Update()
        {
            bool roundFinished = _mapChanger.ViewIsRoundFinished;
            bool changingMap = _mapChanger.ViewIsChangingMap && !roundFinished;

            _mapChangePanel.SetActive(changingMap);
            if (changingMap)
            {
                int seconds = Mathf.CeilToInt(_mapChanger.ViewMapChangeTimeRemaining);
                _mapChangeText.text = $"Changing map in {seconds}...";
            }

            if (roundFinished && !_wasRoundFinished)
                OnRoundFinished();

            _wasRoundFinished = roundFinished;
        }

        private void OnRoundFinished()
        {
            if (_roundEndPanel.activeSelf)
                return;

            _mapChangePanel.SetActive(false);
            _roundEndPanel.SetActive(true);
            _roundEndText.text = "Calculating results...";

            if (_winAudioSource && _winClip)
            {
                _winAudioSource.clip = _winClip;
                _winAudioSource.Play();
            }

            foreach (ParticleSystem particle in _winParticles)
            {
                if (particle)
                    particle.Play();
            }

            StartCoroutine(UpdateWinnerTextNextFrame());
        }

        private IEnumerator UpdateWinnerTextNextFrame()
        {
            List<PlayerScore.ScoreResult> winners;
            while (!_playerScore.TryGetVerifiedWinners(out winners))
                yield return null;

            _roundEndText.text = BuildWinnerText(winners);
        }

        private string BuildWinnerText(List<PlayerScore.ScoreResult> winners)
        {
            if (winners.Count == 0)
                return "No winner.";

            PlayerScore.ScoreData score = winners[0].Score;
            string kills = score.Kills == 1 ? "kill" : "kills";
            string deaths = score.Deaths == 1 ? "death" : "deaths";

            if (winners.Count == 1)
            {
                return $"The winner is {GetDisplayName(winners[0].Player)} with " +
                    $"{score.Kills} {kills} and {score.Deaths} {deaths}";
            }

            _text.Clear();
            _text.Append("The winners are ");

            for (int i = 0; i < winners.Count; i++)
            {
                if (i > 0)
                {
                    if (i == winners.Count - 1)
                        _text.Append(winners.Count == 2 ? " and " : ", and ");
                    else
                        _text.Append(", ");
                }

                _text.Append(GetDisplayName(winners[i].Player));
            }

            _text.Append($" with {score.Kills} {kills} and {score.Deaths} {deaths}");
            return _text.ToString();
        }

        private static string GetDisplayName(PlayerID player)
        {
            if (PlayerDataManager.TryGetPlayerData(player, out PlayerData data) &&
                !string.IsNullOrWhiteSpace(data.DisplayName))
            {
                return data.DisplayName;
            }

            return player.ToString();
        }

    }
}
