using PurrNet;
using PurrNet.Prediction;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    public sealed class PlayerStorage : StatelessPredictedIdentity
    {
        public static Dictionary<PlayerID, PredictedObjectID> All = new();

        protected override void LateAwake()
        {
            base.LateAwake();

            if (owner.HasValue)
                All[owner.Value] = id.objectId;
        }

        protected override void Destroyed()
        {
            base.Destroyed();

            if (owner.HasValue)
                All.Remove(owner.Value);
        }
    }

    public class PlayerReferences : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private SimplePlayerController _simplePlayerController;
        [SerializeField] private PlayerCameraWeapon _playerCameraWeapon;

        public PlayerHealth PlayerHealth => _playerHealth;
        public SimplePlayerController SimplePlayerController => _simplePlayerController;
        public PlayerCameraWeapon PlayerCameraWeapon => _playerCameraWeapon;
    }
}
