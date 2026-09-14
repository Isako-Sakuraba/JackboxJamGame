using PurrNet;
using UnityEngine;

namespace Game.Player
{
    public class PlayerColor : NetworkIdentity
    {
        private static readonly int PlayerColorId = Shader.PropertyToID("_PlayerColor");

        [SerializeField] private SpriteRenderer[] _renderers;
        [SerializeField] private Color[] _colors =
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta
        };

        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            ResolveRenderers();
        }

        protected override void OnSpawned()
        {
            ApplyColor();
        }

        protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
        {
            ApplyColor(newOwner);
        }

        private void ApplyColor()
        {
            ApplyColor(owner);
        }

        private void ApplyColor(PlayerID? ownerId)
        {
            if (_colors == null || _colors.Length == 0)
                return;

            ResolveRenderers();

            Color color = GetColor(ownerId);
            _propertyBlock ??= new MaterialPropertyBlock();

            for (int i = 0; i < _renderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = _renderers[i];

                if (spriteRenderer == null)
                    continue;

                spriteRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(PlayerColorId, color);
                spriteRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private Color GetColor(PlayerID? ownerId)
        {
            if (!ownerId.HasValue)
                return _colors[0];

            int hash = ownerId.Value.GetHashCode() & int.MaxValue;
            return _colors[hash % _colors.Length];
        }

        private void ResolveRenderers()
        {
            if (_renderers != null && _renderers.Length > 0)
                return;

            _renderers = GetComponentsInChildren<SpriteRenderer>();
        }
    }
}
