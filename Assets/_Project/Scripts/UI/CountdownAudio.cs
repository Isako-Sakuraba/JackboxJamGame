using Game.Environment;
using Game.Player;
using UnityEngine;

namespace Game.UI
{
    public sealed class CountdownAudio : MonoBehaviour
    {
        private enum CountdownType
        {
            InitialSpawn,
            MapChange
        }

        [SerializeField] private CountdownType _countdownType;
        [SerializeField] private InitialSpawnTimer _initialSpawnTimer;
        [SerializeField] private MapChanger _mapChanger;
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _tickClip;
        [SerializeField] private AudioClip _finalTickClip;
        [SerializeField] private AudioClip _changeStartClip;

        private int _lastSecond = -1;
        private bool _wasInitialCountdownActive;
        private bool _wasInitialSpawned;
        private bool _mapChangeStarted;

        private void OnEnable()
        {
            if (_countdownType != CountdownType.MapChange || !_mapChanger)
                return;

            _mapChanger.MapChangeAnnounced += OnMapChangeStarted;
            _mapChanger.MapChanged += OnMapChanged;
        }

        private void OnDisable()
        {
            if (!_mapChanger)
                return;

            _mapChanger.MapChangeAnnounced -= OnMapChangeStarted;
            _mapChanger.MapChanged -= OnMapChanged;
        }

        private void Update()
        {
            if (_countdownType == CountdownType.InitialSpawn)
                UpdateInitialSpawn();
            else
                UpdateMapChange();
        }

        private void UpdateInitialSpawn()
        {
            if (!_initialSpawnTimer)
                return;

            bool active = _initialSpawnTimer.ViewCountdownActive;
            if (active)
                PlayTickWhenSecondChanges(_initialSpawnTimer.ViewTime);

            if (_initialSpawnTimer.ViewFired && !_wasInitialSpawned)
                Play(_finalTickClip);

            if (active && !_wasInitialCountdownActive)
                _lastSecond = Mathf.CeilToInt(_initialSpawnTimer.ViewTime);

            _wasInitialCountdownActive = active;
            _wasInitialSpawned = _initialSpawnTimer.ViewFired;
        }

        private void UpdateMapChange()
        {
            if (_mapChanger && _mapChanger.ViewIsChangingMap)
                PlayTickWhenSecondChanges(_mapChanger.ViewMapChangeTimeRemaining);
        }

        private void PlayTickWhenSecondChanges(float time)
        {
            int second = Mathf.Max(1, Mathf.CeilToInt(time));
            if (second == _lastSecond)
                return;

            _lastSecond = second;
            Play(_tickClip);
        }

        private void OnMapChangeStarted(int mapIndex, float delay)
        {
            _mapChangeStarted = true;
            _lastSecond = Mathf.CeilToInt(delay);
            Play(_changeStartClip);
        }

        private void OnMapChanged(int mapIndex)
        {
            if (!_mapChangeStarted)
                return;

            _mapChangeStarted = false;
            _lastSecond = -1;
            Play(_finalTickClip);
        }

        private void Play(AudioClip clip)
        {
            if (!_source || !clip)
                return;

            _source.clip = clip;
            _source.Play();
        }
    }
}
