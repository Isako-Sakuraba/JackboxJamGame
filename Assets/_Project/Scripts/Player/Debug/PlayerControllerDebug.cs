using TMPro;
using UnityEngine;

namespace Game.Player.Debugging
{
    public class PlayerControllerDebug : MonoBehaviour
    {
        [SerializeField] private PlayerController _controller;
        [SerializeField] private TMP_Text _text;

        private void Awake()
        {
            _controller ??= GetComponent<PlayerController>();
            _text ??= GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (_controller == null || _text == null)
                return;

            _text.text = _controller.currentState.ToString();
        }
    }
}
