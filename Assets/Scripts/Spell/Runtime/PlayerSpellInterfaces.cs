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

// 발사되는 총알 속성 수정(Piercing 관통, Sniper 탄속 등) — Shoot에서 발사 직후 적용
public interface IPlayerBulletModifier
{
    void ModifyBullet(Bullet bullet);
}

// ♠ Railgun(충전)·비격진천뢰(폭탄) — 기본 발사를 통째로 대체(true 반환 시 일반 총알 생성 생략).
// 여러 개면 먼저 성공한 하나만 발동.
public interface IPlayerShotOverride
{
    bool TryOverrideShot(Vector2 origin, Vector2 direction);

    // 직전 TryOverrideShot이 "실제로 발사했는지".
    // true를 돌려줬어도 쿨다운/충전 중이라 클릭만 흡수한 경우가 있어서 구분이 필요하다.
    // (이걸 안 보고 연사를 이어붙이면 쿨다운 중 연타로 무한 발사가 된다)
    bool DidFire { get; }

    // ♠ Gatling 연사용 추가 발사. 클릭 1회가 3연발이 되므로 대체 발사도 같은 횟수로 반복한다.
    // 실제 발사가 성립한 뒤에만 호출되므로, 여기서는 연타 방지 게이트를 건너뛴다.
    void FireBurstShot(Vector2 origin, Vector2 direction);
}

// 버프 HUD 표시용 — 플레이어 자기버프 상태 컴포넌트가 구현하면 PlayerBuffHUD가 GetComponents로
// 수집해 HP바 아래에 아이콘+숫자로 표시. (허브 없이 인터페이스만 — 기존 규약과 동일)
public interface IBuffDisplay
{
    SpellMarble BuffMarble { get; } // 아이콘 해석용(HUD가 icon→ability.icon→skinTable 폴백)
    bool BuffTimed { get; }         // true=남은 초 표시, false=남은 횟수 표시
    float BuffRemaining { get; }    // 남은 초(Timed일 때)
    int BuffCharges { get; }        // 남은 횟수(Timed 아닐 때)
}

// 플레이어 총알의 적 명중 알림 허브 — Bullet이 호출, Lifesteal/Chain Lightning 등이 구독.
// (static 이벤트: 플레이어 1인 전제. 구독 해제는 상태 컴포넌트 OnDisable 책임)
public static class PlayerBulletEvents
{
    public static System.Action<GameObject, float> EnemyHit; // (맞은 적, 데미지)

    public static void NotifyEnemyHit(GameObject enemy, float damage)
    {
        if (EnemyHit != null) EnemyHit(enemy, damage);
    }

    // 기본 공격 1회당 1번(개틀링 추가 발 제외) — ♠ 비격진천뢰 등이 구독. 인자 = 커서 월드 좌표.
    public static System.Action<Vector2> PlayerShot;

    public static void NotifyPlayerShot(Vector2 cursorWorld)
    {
        if (PlayerShot != null) PlayerShot(cursorWorld);
    }
}
