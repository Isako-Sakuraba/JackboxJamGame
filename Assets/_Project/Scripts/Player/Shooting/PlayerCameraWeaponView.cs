using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Player
{
    public class PlayerCameraWeaponView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerCameraWeapon _weapon;
        [SerializeField] private Light2D _stutterLight;
        [SerializeField] private AudioSource _audioSource;

        [Header("Visual Settings")]
        [SerializeField] private float _stutterReleaseTime = 0.4f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _focusStartClip;
        [SerializeField] private AudioClip _focusedClip;
        [SerializeField] private AudioClip _overfocusedClip;
        [SerializeField] private AudioClip _shotClip;

        private float _stutterReleaseTimer = 0f;
        private float _stutterStartAngle = 0f;

        private bool StutterReleaseTimerAvailable => _stutterReleaseTimer > 0f;

        private void Start()
        {
            _stutterLight.enabled = false;

            _weapon.CameraStartedFocus.AddListener(OnCameraStartedFocus);
            _weapon.CameraFocused.AddListener(OnCameraFocused);
            _weapon.CameraOverfocused.AddListener(OnCameraOverfocused);
            _weapon.CameraShot.AddListener(OnCameraShot);
        }

        private void Update()
        {
            if (StutterReleaseTimerAvailable)
            {
                _stutterLight.intensity = 12f;

                _stutterReleaseTimer = Mathf.Max(0f, _stutterReleaseTimer - Time.deltaTime);

                float t = Mathf.Clamp01(_stutterReleaseTimer / _stutterReleaseTime);
                t = 1 - t; // 1 - stutter released;

                float releaseSpeed = _stutterStartAngle / _stutterReleaseTime;

                _stutterLight.pointLightOuterAngle = Mathf.MoveTowards(
                    _stutterLight.pointLightOuterAngle, 0f, releaseSpeed * Time.deltaTime);

                _stutterLight.pointLightInnerAngle = 0.92f * _stutterLight.pointLightOuterAngle;

                if (!StutterReleaseTimerAvailable)
                {
                    _stutterLight.enabled = false;
                    _stutterLight.intensity = 8f;
                }
            }
        }

        private void OnCameraStartedFocus()
        {
            _stutterLight.enabled = true;
            PlaySound(_focusStartClip);
        }

        private void OnCameraFocused()
        {
            PlaySound(_focusedClip);
        }

        private void OnCameraOverfocused()
        {
            PlaySound(_overfocusedClip);
        }

        private void OnCameraShot()
        {
            _stutterStartAngle = _stutterLight.pointLightOuterAngle;
            _stutterReleaseTimer = _stutterReleaseTime;
            PlaySound(_shotClip);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }
    }
}
