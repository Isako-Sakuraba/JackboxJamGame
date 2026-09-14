using UnityEngine;

namespace Game.Player
{
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
