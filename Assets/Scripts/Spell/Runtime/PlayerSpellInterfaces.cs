using UnityEngine;

// ♦/♥ 스펠 마블 — 플레이어 피해 파이프라인 훅 인터페이스.
// 상태 컴포넌트가 구현하면 PlayerController가 피격/발사/사망 시 GetComponents로 수집해 적용.
// (허브 클래스 없이 인터페이스만 — 상태이상 규약(Apply+OnDisable 원복)과 자연 결합)

// 받는 피해 수정(Iron Skin 감소, Fortress 무효, Reflect 반사, Mirror World 분산 등)
public interface IPlayerDamageModifier
{
    // attacker는 접촉 피격 시 해당 적, 총알 피격 시 null일 수 있음
    float ModifyIncomingDamage(float damage, GameObject attacker);
}

// 주는 피해 수정(Counter 2배, 후일 Sniper Mode 등) — 발사 시점 적용
public interface IPlayerOutgoingModifier
{
    float ModifyOutgoingDamage(float damage);
}

// 피격 통지(실드/무효로 0이 된 피격은 통지 안 됨) — Counter 윈도우 시작 등
public interface IPlayerHitListener
{
    void OnPlayerHit(float damage);
}

// 사망 가로채기(Resurrection) — true 반환 시 사망 처리 취소(구현체가 부활 처리 책임)
public interface IPlayerDeathInterceptor
{
    bool TryInterceptDeath();
}
