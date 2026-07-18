using System.Threading.Tasks;
using UnityEngine;

// 임시 검증용: 로그인 완료 후 pullGacha 1회 호출해 결과를 콘솔에 남긴다.
// 파이프라인 확인이 끝나면 이 컴포넌트/스크립트는 제거 예정.
public class GachaTest : MonoBehaviour
{
    async void Start()
    {
        // 로그인 완료까지 대기(최대 ~10초) — WebGL 안전(Task.Delay 금지)
        await ServicesBootstrap.WaitSignedInAsync();

        if (!ServicesBootstrap.IsSignedIn)
        {
            Debug.LogError("[Gacha] 로그인 안 됨 — pullGacha 생략");
            return;
        }

        try
        {
            var r = await GachaService.PullAsync();
            if (!string.IsNullOrEmpty(r.error)) Debug.Log("[Gacha] 뽑기 거부: " + r.error);
            else Debug.Log("[Gacha] 뽑기 성공 · " + r.marbleName + " (" + r.grade + ") new=" + r.isNew + " gem=" + r.gem + " shards=" + r.shards);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Gacha] GachaPull 호출 실패: " + e.Message);
        }
    }
}
