using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// 부트/타이틀 씬 게이트: UGS 로그인(ServicesBootstrap) 완료를 기다렸다가
// "Press Any Key to Start" → 아무 입력이면 MainMenuScene으로 전환. 실패 시 재시도.
public class LoginGate : MonoBehaviour
{
    [SerializeField] private string nextScene = "MainMenuScene";
    [SerializeField] private Text promptText;                 // 상태/안내 문구
    [SerializeField] private float failTimeout = 10f;         // 이 시간 넘게 로그인 안 되면 실패 처리

    [Header("Messages")]
    [SerializeField] private string connectingMsg = "연결 중...";
    [SerializeField] private string readyMsg = "Press Any Key to Start";
    [SerializeField] private string failMsg = "연결 실패 — 아무 키나 눌러 재시도";

    private bool ready;
    private bool failed;
    private float elapsed;

    void Update()
    {
        if (!ready && !failed)
        {
            if (ServicesBootstrap.IsSignedIn)
            {
                ready = true;
                SetPrompt(readyMsg);
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= failTimeout)
            {
                failed = true;
                SetPrompt(failMsg);
            }
            else
            {
                SetPrompt(connectingMsg);
            }
            return;
        }

        if (AnyInput())
        {
            if (failed)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // 부트 씬 재시작 = 재시도(로더 미경유)
            else
                SceneLoader.Load(nextScene);
        }
    }

    bool AnyInput()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        return (kb != null && kb.anyKey.wasPressedThisFrame) ||
               (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    void SetPrompt(string s)
    {
        if (promptText != null && promptText.text != s) promptText.text = s;
    }
}
