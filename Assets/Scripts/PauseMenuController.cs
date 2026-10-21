using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public GameObject pauseMenuPanel;

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("Main Menu");
        Time.timeScale = 1f;
    }

    public void NextLevel()
    {
        SceneManager.LoadScene("Level 2");
        Time.timeScale = 1f;
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}