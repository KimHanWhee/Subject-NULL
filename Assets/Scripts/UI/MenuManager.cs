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
        SceneLoader.Load("GameScene");
    }

    public void OnPressDeckBuilding()
    {
        SceneLoader.Load("DeckBuildingScene");
    }

    public void OnPressHowToPlay()
    {
        SceneLoader.Load("HowToPlayScene");
    }

    public void OnPressGacha()
    {
        SceneLoader.Load("GachaScene");
    }

    public void OnPressAccount()
    {
        SceneLoader.Load("AccountScene");
    }

    public void OnPressExit()
    {
        Application.Quit();
    }
}
