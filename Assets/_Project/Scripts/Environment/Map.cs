using System.Collections.Generic;
using UnityEngine;

namespace Game.Environment
{
    public sealed class Map : MonoBehaviour
    {
        [SerializeField] private List<Transform> _spawnPoints = new();

        public IReadOnlyList<Transform> SpawnPoints => _spawnPoints;
    }
}
