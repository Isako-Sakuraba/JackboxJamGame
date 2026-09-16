using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public sealed class LoadTutorial : MonoBehaviour
    {
        [SerializeField, Min(0)] private int _sceneIndex;

        public void Load()
        {
            SceneManager.LoadSceneAsync(_sceneIndex);
        }
    }
}
