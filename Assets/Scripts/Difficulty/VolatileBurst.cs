using System.Collections;
using UnityEngine;

// 고난 "자폭 조직" — 적이 죽은 자리에서 잠깐 뒤 폭발한다.
// 적 본체는 사망 애니메이션 후 풀로 반환되므로, 폭발은 별도 GameObject가 수명을 갖고 처리한다.
//
// 피해는 PlayerController.ApplyRangedHit 경유 — 대시 무적·실드·안전지대 등
// 기존 방어 수단이 모두 그대로 적용된다(별도 예외 처리 불필요).
public class VolatileBurst : MonoBehaviour
{
    static readonly Color WarnColor = new Color(1f, 0.6f, 0.2f, 1f);

    public static void Spawn(Vector3 pos)
    {
        GameObject go = new GameObject("VolatileBurst");
        go.transform.position = pos;
        VolatileBurst vb = go.AddComponent<VolatileBurst>();
        vb.StartCoroutine(vb.Run(pos));
    }

    IEnumerator Run(Vector3 pos)
    {
        // 예고 — 터질 자리를 먼저 보여줘야 회피가 가능하다(즉발이면 반응 불가 = 불합리)
        SpellVfx.SpawnRing(pos, HardshipSystem.VolatileRadius, WarnColor, HardshipSystem.VolatileDelay);

        yield return new WaitForSeconds(HardshipSystem.VolatileDelay);

        SpellParticleVfx.SpawnBurst(pos, HardshipSystem.VolatileRadius, WarnColor, 16, 0.35f);

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            float d = Vector2.Distance(pc.transform.position, pos);
            if (d <= HardshipSystem.VolatileRadius)
                pc.ApplyRangedHit(HardshipSystem.VolatileDamage, null);
        }

        Destroy(gameObject);
    }
}
