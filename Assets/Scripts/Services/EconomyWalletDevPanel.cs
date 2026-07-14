using UnityEngine;

// ⚠️ 개발 전용 임시 검증 패널. GEM 서버화(1단계) 파이프라인 확인용.
//    아무 씬의 빈 GameObject에 붙이면 좌상단에 OnGUI 버튼이 뜬다.
//    검증 끝나면(또는 라이브 전) 이 컴포넌트와 파일을 삭제할 것.
public class EconomyWalletDevPanel : MonoBehaviour
{
    async void Start()
    {
        // 로그인 완료까지 잠깐 대기 후 첫 잔액 읽기.
        float t = 0f;
        while (!ServicesBootstrap.IsSignedIn && t < 10f)
        {
            await System.Threading.Tasks.Task.Yield();
            t += Time.unscaledDeltaTime;
        }
        await EconomyWallet.RefreshAsync();
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 250, 140), "Economy GEM (DEV)");
        GUI.Label(new Rect(20, 34, 230, 22), "Signed in : " + ServicesBootstrap.IsSignedIn);
        GUI.Label(new Rect(20, 56, 230, 22), "GEM (cached) : " + EconomyWallet.CachedGem);

        if (GUI.Button(new Rect(20, 82, 110, 30), "새로고침"))
            _ = EconomyWallet.RefreshAsync();

        if (GUI.Button(new Rect(140, 82, 110, 30), "+1000 (DEV)"))
            _ = EconomyWallet.DevGrantAsync(1000);

        // 치트 차단 검증용: 클라가 직접 increment 시도 → Access Control Deny면 실패해야 정상.
        if (GUI.Button(new Rect(20, 116, 230, 26), "클라 직접 +500 (막혀야 정상)"))
            _ = TryClientIncrement(500);
    }

    // 플레이어 권한으로 직접 잔액 증액을 시도한다. Access Control 정책이 제대로
    // 걸려 있으면 예외(403)로 실패해야 한다. 성공하면 정책이 안 걸린 것.
    async System.Threading.Tasks.Task TryClientIncrement(int amount)
    {
        try
        {
            await Unity.Services.Economy.EconomyService.Instance.PlayerBalances
                .IncrementBalanceAsync(EconomyWallet.GemCurrencyId, amount);
            Debug.LogWarning("[DEV] 클라 직접 증액이 성공함 → Access Control 정책이 아직 안 걸림! " +
                             "ugs access upsert-project-policy 로 Player increment/decrement Deny 필요.");
            await EconomyWallet.RefreshAsync();
        }
        catch (System.Exception e)
        {
            Debug.Log("[DEV] 클라 직접 증액 차단됨(정상): " + e.Message);
        }
    }
}
