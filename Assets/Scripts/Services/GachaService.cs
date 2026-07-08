using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;

// 가챠: 서버(pullGacha Cloud Code)에서 마블 1개 추첨 결과를 받아온다.
// RNG는 전적으로 서버. 클라는 결과 id로 SO를 찾아 연출만 한다.
public static class GachaService
{
    [System.Serializable]
    public class GachaResult
    {
        public string id;     // 뽑힌 마블/능력 식별 key (SpellAbility.id)
        public string grade;  // 등급 문자열(연출/로그용)
    }

    public static async Task<GachaResult> PullAsync()
    {
        var args = new Dictionary<string, object>();
        return await CloudCodeService.Instance.CallEndpointAsync<GachaResult>("pullGacha", args);
    }
}
