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
        [SerializeField] private SpriteRenderer _lines;
        [SerializeField] private SpriteRenderer[] _borders;

        private void OnEnable()
        {
            ApplyPalette();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyPalette();
        }
#endif

        private void ApplyPalette()
        {
            if (_palette == null)
                return;

            SetColor(_obstacles, _palette.BaseColor);
            SetColor(_middleground, _palette.SecondaryColor);
            SetColor(_background, _palette.BackgroundColor);
            SetColor(_lines, _palette.LinesColor);

            if (_borders != null)
                foreach (var border in  _borders)
                    SetColor(border, _palette.BordersColor);
        }

        private static void SetColor(SpriteRenderer spriteRenderer, Color color)
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.color = color;
        }
    }
}
