// 게임 전역 씬 전환 진입점. 반투명 로딩 오버레이(LoadingOverlay)를 띄운 채 대상 씬을 비동기 로드한다.
// 사용: SceneLoader.Load("GameScene");  (SceneManager.LoadScene 직접 호출 대신)
public static class SceneLoader
{
    public static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        LoadingOverlay.Get().LoadScene(sceneName);
    }
}
