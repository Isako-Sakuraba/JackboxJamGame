using UnityEngine;

namespace Game.Utilities
{
    [ExecuteAlways]
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteRendererColorPropertyBlock : MonoBehaviour
    {
        private static readonly int SpriteRendererColorId = Shader.PropertyToID("_SpriteRendererColor");

        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _propertyBlock;

        private void OnEnable()
        {
            ResolveReferences();
            ApplyColor();
        }

        private void LateUpdate()
        {
            ResolveReferences();
            ApplyColor();
        }

        private void OnValidate()
        {
            ResolveReferences();
            ApplyColor();
        }

        private void OnDidApplyAnimationProperties()
        {
            ResolveReferences();
            ApplyColor();
        }

        private void ResolveReferences()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            _propertyBlock ??= new MaterialPropertyBlock();
        }

        private void ApplyColor()
        {
            if (_spriteRenderer == null)
                return;

            _spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(SpriteRendererColorId, _spriteRenderer.color);
            _spriteRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
