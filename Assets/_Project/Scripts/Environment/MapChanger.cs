using Game.Services;
using Game.Player;
using PurrNet.Prediction;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Environment
{
    public sealed class MapChanger : MonoBehaviour
    {
        [SerializeField] private List<Map> _maps = new();

        private AdvancedPlayerSpawner _spawner;
        private int _activeMapIndex = -1;

        public event Action<int, float> MapChangeAnnounced = delegate { };
        public event Action<int> MapChanged = delegate { };
        public event Action RoundFinished = delegate { };
        public event Action RoundExitReady = delegate { };

        public int MapCount => _maps.Count;
        public int CurrentMapIndex => _activeMapIndex;
        public bool IsChangingMap => ServiceLocator.TryGet<PlayerLifeManager>(out var manager)
            && manager.IsChangingMap;
        public bool IsRoundFinished => ServiceLocator.TryGet<PlayerLifeManager>(out var manager)
            && manager.IsRoundFinished;
        public bool IsRoundExitReady => ServiceLocator.TryGet<PlayerLifeManager>(out var manager)
            && manager.IsRoundExitReady;
        public float MapChangeTimeRemaining => ServiceLocator.TryGet<PlayerLifeManager>(out var manager)
            ? manager.MapChangeTimer
            : 0f;
        public bool ViewIsChangingMap => ServiceLocator.TryGet<PlayerLifeManager>(out var manager)
            && manager.ViewIsChangingMap;
        public bool ViewIsRoundFinished => ServiceLocator.TryGet<PlayerLifeManager>(out var manager)
            && manager.ViewIsRoundFinished;
        public float ViewMapChangeTimeRemaining => ServiceLocator.TryGet<PlayerLifeManager>(out var manager)
            ? manager.ViewMapChangeTimer
            : 0f;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<MapChanger>(out var registered) && ReferenceEquals(registered, this))
                ServiceLocator.Unregister<MapChanger>();
        }

        public void Initialize(AdvancedPlayerSpawner spawner, int initialMapIndex)
        {
            _spawner = spawner;
            Sim_ActivateMap(initialMapIndex);
        }

        public void Sim_ActivateMap(int mapIndex)
            => ApplyMap(mapIndex, true);

        public void RestoreMap(int mapIndex)
            => ApplyMap(mapIndex, false);

        private void ApplyMap(int mapIndex, bool resetSpawnIndex)
        {
            if (mapIndex < 0 || mapIndex >= _maps.Count)
                return;

            Map targetMap = _maps[mapIndex];
            if (!targetMap)
            {
                Debug.LogError($"Map at index {mapIndex} is not assigned.", this);
                return;
            }

            if (_activeMapIndex == mapIndex)
                return;

            for (int i = 0; i < _maps.Count; i++)
            {
                if (_maps[i])
                    _maps[i].gameObject.SetActive(i == mapIndex);
            }

            _activeMapIndex = mapIndex;

            if (_spawner)
                _spawner.SetSpawnPoints(targetMap.SpawnPoints, resetSpawnIndex);
        }

        internal void NotifyMapChangeAnnounced(int nextMapIndex, float delay)
            => MapChangeAnnounced.Invoke(nextMapIndex, delay);

        internal void NotifyMapChanged(int mapIndex)
            => MapChanged.Invoke(mapIndex);

        internal void NotifyRoundFinished()
            => RoundFinished.Invoke();

        internal void NotifyRoundExitReady()
            => RoundExitReady.Invoke();
    }
}
