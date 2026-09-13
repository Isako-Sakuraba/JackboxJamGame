using Game.Services;
using UnityEngine;

namespace Game.Lifecycle
{
    /// <summary>
    /// Registers itself as the root bootstrap service.
    /// </summary>
    public sealed class BootstrapService : MonoBehaviour, IBootstrapService
    {
        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register<IBootstrapService>(this);
        }
    }
}
