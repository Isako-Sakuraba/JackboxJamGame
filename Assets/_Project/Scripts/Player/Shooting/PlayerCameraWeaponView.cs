using LitMotion;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Player
{
    public class PlayerCameraWeaponView : MonoBehaviour
    {
        [System.Serializable]
        public struct CameraLightSettings
        {
            public Color color;
            public float Intensity;
            public float transitionIntensity;
            public float transitionTime;
            public float buildupCoefficient;
            public float stayCoefficient;
        }

        [Header("Dependencies")]
        [SerializeField] private PlayerCameraWeapon _weapon;
        [SerializeField] private Light2D _shutterLight;
        [SerializeField] private AudioSource _audioSource;

        [Header("Visual Settings")]
        [SerializeField] private CameraLightSettings _focusStartedLightSettings;
        [SerializeField] private CameraLightSettings _focusedLightSettings;
        [SerializeField] private CameraLightSettings _overfocusedLightSettings;
        [SerializeField] private CameraLightSettings _shootLightSettings;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _focusStartClip;
        [SerializeField] private AudioClip _focusedClip;
        [SerializeField] private AudioClip _overfocusedClip;
        [SerializeField] private AudioClip _shotClip;

        private MotionHandle _lightIntensityMotionHandle;
        private MotionHandle _lightColorMotionHandle;
        private MotionHandle _shutterReleaseMotionHandle;

        private void Start()
        {
            _shutterLight.enabled = false;

            _weapon.CameraStartedFocus.AddListener(OnCameraStartedFocus);
            _weapon.CameraFocused.AddListener(OnCameraFocused);
            _weapon.CameraOverfocused.AddListener(OnCameraOverfocused);
            _weapon.CameraShot.AddListener(OnCameraShot);
        }

        private void OnDisable()
        {
            CancelAllMotions();
        }

        private void Update()
        {
            UpdateView(_weapon.viewState, _weapon.verifiedState);
        }

        private void UpdateView(PlayerCameraWeapon.WeaponState viewState, PlayerCameraWeapon.WeaponState? verified)
        {
            if (viewState.State == PlayerCameraWeapon.FocusState.None)
                return;

            float current = viewState.FocusTimer;
            float max = _weapon.FocusTime;
            float t = current / max; // t is 1 means just started focus, t is 0 means finished
            t = Mathf.Clamp01(1 - t);
            float outerRadius = Mathf.Lerp(_weapon.FocusRadius.x, _weapon.FocusRadius.y, t);
            float outerSpotAngle = Mathf.Lerp(_weapon.FocusSpotAngle.x, _weapon.FocusSpotAngle.y, t);
            float innerRadius = _weapon.RadiusMultiplier * outerRadius;
            float innerSpotAngle = _weapon.SpotAngleMultiplier * outerSpotAngle;
            Vector2 direction = viewState.Direction;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            _shutterLight.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            _shutterLight.pointLightInnerAngle = innerSpotAngle;
            _shutterLight.pointLightOuterAngle = outerSpotAngle;
            _shutterLight.pointLightInnerRadius = innerRadius;
            _shutterLight.pointLightOuterRadius = outerRadius;
        }

        private void OnCameraStartedFocus()
        {
            _shutterLight.enabled = true;
            ApplyLightSettings(_focusStartedLightSettings);
            PlaySound(_focusStartClip);
        }

        private void OnCameraFocused()
        {
            PlayLightFlash(_focusedLightSettings);
            PlaySound(_focusedClip);
        }

        private void OnCameraOverfocused()
        {
            PlayLightFlash(_overfocusedLightSettings);
            PlaySound(_overfocusedClip);
        }

        private void OnCameraShot()
        {
            PlayLightFlash(_shootLightSettings);
            PlayShutterRelease(_shootLightSettings.transitionTime);
            PlaySound(_shotClip);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null)
                return;

            _audioSource.clip = clip;
            _audioSource.Play();
        }

        private void ApplyLightSettings(CameraLightSettings settings)
        {
            CancelAllMotions();

            _shutterLight.color = settings.color;
            _shutterLight.intensity = settings.Intensity;
        }

        private void PlayLightFlash(CameraLightSettings settings)
        {
            CancelLightFlash();

            Color startColor = _shutterLight.color;

            if (settings.transitionTime <= 0f)
            {
                _shutterLight.color = settings.color;
                _shutterLight.intensity = settings.Intensity;
                return;
            }

            GetFlashTimes(settings, out float buildupTime, out float stayTime, out float winddownTime);

            var sequence = LSequence.Create();

            if (buildupTime > 0f)
            {
                sequence.Append(LMotion.Create(_shutterLight.intensity, settings.transitionIntensity, buildupTime)
                    .Bind(value => _shutterLight.intensity = value));

                _lightColorMotionHandle = LMotion.Create(startColor, settings.color, buildupTime)
                    .Bind(value => _shutterLight.color = value);
            }
            else
            {
                _shutterLight.intensity = settings.transitionIntensity;
                _shutterLight.color = settings.color;
            }

            if (stayTime > 0f)
                sequence.AppendInterval(stayTime);

            if (winddownTime > 0f)
            {
                sequence.Append(LMotion.Create(settings.transitionIntensity, settings.Intensity, winddownTime)
                    .Bind(value => _shutterLight.intensity = value));
            }

            _lightIntensityMotionHandle = sequence.Run(builder =>
                builder.WithOnComplete(() => _shutterLight.intensity = settings.Intensity));
        }

        private void PlayShutterRelease(float duration)
        {
            CancelShutterRelease();

            if (duration <= 0f)
            {
                _shutterLight.pointLightOuterAngle = 0f;
                _shutterLight.pointLightInnerAngle = 0f;
                _shutterLight.enabled = false;
                return;
            }

            float startAngle = _shutterLight.pointLightOuterAngle;

            _shutterReleaseMotionHandle = LMotion.Create(startAngle, 0f, duration)
                .WithOnComplete(() => _shutterLight.enabled = false)
                .Bind(angle =>
                {
                    _shutterLight.pointLightOuterAngle = angle;
                    _shutterLight.pointLightInnerAngle = 0.92f * angle;
                });
        }

        private static void GetFlashTimes(CameraLightSettings settings, out float buildupTime, out float stayTime, out float winddownTime)
        {
            float buildupCoefficient = Mathf.Clamp01(settings.buildupCoefficient);
            float stayCoefficient = Mathf.Clamp(settings.stayCoefficient, 0f, 1f - buildupCoefficient);

            buildupTime = settings.transitionTime * buildupCoefficient;
            stayTime = settings.transitionTime * stayCoefficient;
            winddownTime = Mathf.Max(0f, settings.transitionTime - buildupTime - stayTime);
        }

        private void CancelAllMotions()
        {
            CancelLightFlash();
            CancelShutterRelease();
        }

        private void CancelLightFlash()
        {
            if (_lightIntensityMotionHandle.IsActive())
                _lightIntensityMotionHandle.Cancel();

            if (_lightColorMotionHandle.IsActive())
                _lightColorMotionHandle.Cancel();
        }

        private void CancelShutterRelease()
        {
            if (_shutterReleaseMotionHandle.IsActive())
                _shutterReleaseMotionHandle.Cancel();
        }
    }
}
