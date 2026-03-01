using UnityEngine;
using UnityEngine.SceneManagement;

namespace Youstianus
{
    public class TestSceneChange : MonoBehaviour
    {
        public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}
