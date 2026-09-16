using Game.Player;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class InitialSpawnCountdownUI : MonoBehaviour
    {
        [SerializeField] private InitialSpawnTimer _timer;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _text;

        private int _lastSecond = -1;

        private void Awake()
        {
            _panel.SetActive(false);
        }

        private void Update()
        {
            bool visible = _timer && _timer.ViewCountdownActive;
            _panel.SetActive(visible);
            if (!visible)
                return;

            int second = Mathf.Max(1, Mathf.CeilToInt(_timer.ViewTime));
            if (second == _lastSecond)
                return;

            _lastSecond = second;
            _text.text = second.ToString();
            _text.transform.localScale = Vector3.one * 1.45f;
            _text.transform.localRotation = Quaternion.Euler(0f, 0f, second % 2 == 0 ? -10f : 10f);

            LMotion.Create(1.45f, 1f, 0.45f)
                .WithEase(Ease.OutElastic)
                .Bind(scale =>
                {
                    if (!_text)
                        return;

                    _text.transform.localScale = Vector3.one * scale;
                    _text.transform.localRotation = Quaternion.Euler(0f, 0f, (scale - 1f) * 22f);
                });
        }
    }
}
