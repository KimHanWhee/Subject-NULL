using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{

    public void OnPressPlayAgain()
    {
        SceneLoader.Load("GameScene");
    }

    public void OnPressMainMenu()
    {
        SceneLoader.Load("MainMenuScene");
    }
}
