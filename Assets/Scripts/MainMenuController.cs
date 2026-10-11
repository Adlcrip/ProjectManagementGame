using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject selectLevelPanel;

    public void PlayGame()
    {
        selectLevelPanel.SetActive(true);
    }

    public void CloseLevelPanel()
    {
        selectLevelPanel.SetActive(false);
    }

    public void LoadLevel1()
    {
        SceneManager.LoadScene("Level 1");
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene("Level 2");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}