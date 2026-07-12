using UnityEngine;

// 근접 추적 적(슬라임) — 대상을 향해 직진. 공통 규약(스폰/피격/사망)은 EnemyBase 소유.
public class EnemyController : EnemyBase
{
    protected override void Tick(Vector2 toTarget, float dt)
    {
        transform.Translate(toTarget.normalized * (speed * dt));
        if (toTarget.x != 0f) sr.flipX = toTarget.x < 0f;
    }
}
