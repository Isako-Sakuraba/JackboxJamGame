using UnityEngine;

namespace Game.Environment
{
    [ExecuteAlways]
    public sealed class MapRenderer : MonoBehaviour
    {
        [SerializeField] private MapPalette _palette;
        [SerializeField] private SpriteRenderer _obstacles;
        [SerializeField] private SpriteRenderer _middleground;
        [SerializeField] private SpriteRenderer _background;

        private void OnEnable()
        {
            ApplyPalette();
        }

        private void Update()
        {
            ApplyPalette();
        }

        private void OnValidate()
        {
            ApplyPalette();
        }

        private void ApplyPalette()
        {
            if (_palette == null)
                return;

            SetColor(_obstacles, _palette.BaseColor);
            SetColor(_middleground, _palette.SecondaryColor);
            SetColor(_background, _palette.BackgroundColor);
        }

        private static void SetColor(SpriteRenderer spriteRenderer, Color color)
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.color = color;
        }
    }
}
