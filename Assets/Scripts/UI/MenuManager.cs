using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void OnPressStartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnPressExit()
    {
        Application.Quit();
    }
}
