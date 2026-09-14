using Game.Player;
using PurrNet.Logging;
using PurrNet.Packing;
using PurrNet.Pooling;
using PurrNet.Utils;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace PurrNet.Prediction
{
    public class AdvancedPlayerSpawner : DeterministicIdentity<AdvancedPlayerSpawner.CustomSpawnerState>
    {
        public struct CustomSpawnerState : IPredictedData<CustomSpawnerState>, IPredictedData, IDisposable, IPackedAuto, IMath<CustomSpawnerState>, IDuplicate<CustomSpawnerState>, IPurrEquatable<CustomSpawnerState>
        {
            public bool spawned;
            public int spawnPointIndex;

            public DisposableList<PlayerWithObject> values;

            public PredictedObjectID this[PlayerID player]
            {
                set
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        PlayerWithObject value2 = values[i];
                        if (value2.playerID == player)
                        {
                            value2.objectID = value;
                            values[i] = value2;
                            return;
                        }
                    }

                    values.Add(new PlayerWithObject
                    {
                        objectID = value,
                        playerID = player
                    });
                }
            }

            public void Dispose()
            {
                values.Dispose();
            }

            public CustomSpawnerState Duplicate()
            {
                return new CustomSpawnerState
                {
                    values = DisposableList<PlayerWithObject>.Create(values),
                    spawnPointIndex = spawnPointIndex
                };
            }

            public bool TryGetValue(PlayerID player, out PredictedObjectID o)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    PlayerWithObject playerWithObject = values[i];
                    if (playerWithObject.playerID == player)
                    {
                        o = playerWithObject.objectID;
                        return true;
                    }
                }

                o = default(PredictedObjectID);
                return false;
            }

            public bool ContainsKey(PlayerID player)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i].playerID == player)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Remove(PlayerID player)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i].playerID == player)
                    {
                        values.RemoveAt(i);
                        break;
                    }
                }
            }

            public CustomSpawnerState Add(CustomSpawnerState P_0, CustomSpawnerState P_1)
            {
                return P_0;
            }

            public CustomSpawnerState Negate(CustomSpawnerState P_0)
            {
                return P_0;
            }

            public CustomSpawnerState Scale(CustomSpawnerState P_0, float P_1)
            {
                return P_0;
            }

            public bool PurrEquals(CustomSpawnerState other)
            {
                if (other.spawnPointIndex != spawnPointIndex || !PurrEquality<DisposableList<PlayerWithObject>>.Equals(other.values, values))
                {
                    return false;
                }

                return true;
            }
        }

        [SerializeField] private GameObject _playerPrefab;
        [SerializeField, PurrLock] private bool _destroyOnDisconnect;
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private bool _waitForSpawnCall;

        [Header("Camera Settings")]
        [SerializeField] private CinemachineTargetGroup _targetGroup;
        [SerializeField] private float _playerRadius = 0.5f;
        [SerializeField] private float _playerWeight = 1f;

        /// <summary>
        /// Prefab instantiated for each player. Assign before the network starts to configure the spawner at runtime.
        /// </summary>
        public GameObject playerPrefab
        {
            get => _playerPrefab;
            set => _playerPrefab = value;
        }

        private void Awake() => CleanupSpawnPoints();

        protected override void LateAwake()
        {
            if (predictionManager.players)
            {
                predictionManager.players.onPlayerAdded += OnPlayerLoadedScene;
                predictionManager.players.onPlayerRemoved += OnPlayerUnloadedScene;
            }
        }

        protected override void SimulationStart()
        {
            if (!predictionManager.players)
                return;

            var players = predictionManager.players.players;
            for (var i = 0; i < players.Count; i++)
                OnPlayerLoadedScene(players[i]);
        }

        protected override CustomSpawnerState GetInitialState()
        {
            return new CustomSpawnerState
            {
                spawnPointIndex = 0,
                values = DisposableList<PlayerWithObject>.Create()
            };
        }

        protected override void Destroyed()
        {
            if (predictionManager && predictionManager.players)
            {
                predictionManager.players.onPlayerAdded -= OnPlayerLoadedScene;
                predictionManager.players.onPlayerRemoved -= OnPlayerUnloadedScene;
            }
        }

        protected override CustomSpawnerState Interpolate(CustomSpawnerState from, CustomSpawnerState to, float t)
            => to;

        private void CleanupSpawnPoints()
        {
            bool hadNullEntry = false;
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (!spawnPoints[i])
                {
                    hadNullEntry = true;
                    spawnPoints.RemoveAt(i);
                    i--;
                }
            }

            if (hadNullEntry)
                PurrLogger.LogWarning($"Some spawn points were invalid and have been cleaned up.", this);
        }

        private void OnPlayerUnloadedScene(PlayerID player)
        {
            if (!_destroyOnDisconnect)
                return;

            if (currentState.TryGetValue(player, out var playerID))
            {
                hierarchy.Delete(playerID);
                currentState.Remove(player);
            }
        }

        private void OnPlayerLoadedScene(PlayerID player)
        {
            if (!enabled)
                return;

            if (_waitForSpawnCall)
                return;

            if (currentState.ContainsKey(player))
                return;

            SpawnPlayerInternal(player);
        }

        private void SpawnPlayerInternal(PlayerID player)
        {
            PredictedObjectID? newPlayer;

            CleanupSpawnPoints();

            if (spawnPoints.Count > 0)
            {
                var spawnPoint = spawnPoints[currentState.spawnPointIndex];
                currentState.spawnPointIndex = (currentState.spawnPointIndex + 1) % spawnPoints.Count;
                newPlayer = hierarchy.Create(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);
            }
            else
            {
                newPlayer = hierarchy.Create(_playerPrefab, owner: player);
            }

            if (!newPlayer.HasValue)
                return;

            currentState[player] = newPlayer.Value;
            predictionManager.SetOwnership(newPlayer, player);

            if (_targetGroup && newPlayer.TryGetGameObject(predictionManager, out var playerObject))
                AddPlayerToTargetGroup(playerObject);
        }

        private void AddPlayerToTargetGroup(GameObject playerObject)
        {
            var references = playerObject.GetComponent<PlayerReferences>();

            if (!references)
            {
                PurrLogger.LogWarning($"Spawned player '{playerObject.name}' has no {nameof(PlayerReferences)} assigned.", this);
                return;
            }

            var controller = references.SimplePlayerController;

            if (!controller)
            {
                PurrLogger.LogWarning($"Spawned player '{playerObject.name}' has no {nameof(SimplePlayerController)} reference assigned.", this);
                return;
            }

            if (!controller.Origin)
            {
                PurrLogger.LogWarning($"Spawned player '{playerObject.name}' has no camera origin assigned.", this);
                return;
            }

            _targetGroup.AddMember(controller.Origin, _playerWeight, _playerRadius);
        }

        public void SpawnPlayer(PlayerID player)
        {
            if (!enabled) return;

            if (currentState.ContainsKey(player))
                return;

            SpawnPlayerInternal(player);
        }

        public void SpawnAllPlayers()
        {
            if (!enabled || predictionManager.players == null)
                return;

            var players = predictionManager.players.players;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!currentState.ContainsKey(player))
                    SpawnPlayerInternal(player);
            }

            currentState.spawned = true;
        }

        public void RespawnPlayer(PlayerID player)
        {
            if (!enabled) return;

            if (currentState.TryGetValue(player, out var playerID))
            {
                hierarchy.Delete(playerID);
                currentState.Remove(player);
            }

            SpawnPlayerInternal(player);
        }
    }
}
