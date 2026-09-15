using UnityEngine;

namespace Game.Player
{ 
    public class PlayerReferences : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private SimplePlayerController _simplePlayerController;
        [SerializeField] private PlayerCameraWeapon _playerCameraWeapon;
        [SerializeField] private PlayerRespawner _playerRespawner;

        public PlayerHealth PlayerHealth => _playerHealth;
        public SimplePlayerController SimplePlayerController => _simplePlayerController;
        public PlayerCameraWeapon PlayerCameraWeapon => _playerCameraWeapon;
        public PlayerRespawner PlayerRespawner => _playerRespawner;
    }
}
