using UnityEngine;

namespace Game.Environment
{
    [CreateAssetMenu(fileName = "MapPalette", menuName = "Game/Environment/Map Palette")]
    public sealed class MapPalette : ScriptableObject
    {
        [field: SerializeField] public Color BaseColor { get; private set; } = Color.white;
        [field: SerializeField] public Color SecondaryColor { get; private set; } = Color.gray;
        [field: SerializeField] public Color BackgroundColor { get; private set; } = Color.black;
        [field: SerializeField] public Color LinesColor { get; private set; } = Color.cyan;
        [field: SerializeField] public Color BordersColor { get; private set; } = Color.pink;
    }
}
