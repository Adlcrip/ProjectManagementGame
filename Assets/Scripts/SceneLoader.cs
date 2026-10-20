using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Hook a button's OnClick to this and type the scene name in the box, e.g. "LevelSelect"
    public void LoadScene(string sceneName)
    {
        // In case the game was paused, make sure time runs again in the next scene
        Time.timeScale = 1f;

        SceneManager.LoadScene("Main Menu");
    }

    // Reloads the current level (handy for a Retry button)
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}