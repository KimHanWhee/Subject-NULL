using UnityEngine;

// ♥ Overheal 상태 — 최대 체력을 초과하는 임시 체력. 받는 피해를 임시 체력에서 먼저 차감.
// 소진되면 자동 해제(지속시간 제한 없음).
public class OverhealStatus : MonoBehaviour, IPlayerDamageModifier, IBuffDisplay
{
    private float tempHp;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return false; } }               // 남은 임시 HP를 숫자로
    public float BuffRemaining { get { return 0f; } }
    public int BuffCharges { get { return Mathf.CeilToInt(tempHp); } }

    public static void Apply(GameObject player, float amount, SpellMarble marble = null)
    {
        OverhealStatus s = player.GetComponent<OverhealStatus>();
        if (s == null) s = player.AddComponent<OverhealStatus>();
        s.marble = marble;
        s.tempHp = Mathf.Max(s.tempHp, amount); // 재적용은 더 큰 값으로 갱신(중첩 없음)
    }

    public float ModifyIncomingDamage(float damage, GameObject attacker)
    {
        if (tempHp <= 0f) return damage;
        float absorbed = Mathf.Min(tempHp, damage);
        tempHp -= absorbed;
        damage -= absorbed;
        if (tempHp <= 0f) Destroy(this); // 소진 → 해제
        return damage;
    }
}
