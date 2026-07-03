# Archive Index — 2026-07

| Feature | Phase | Match Rate | Archived | Documents |
|---------|-------|:----------:|:--------:|-----------|
| player-dash | completed | 100% | 2026-07-01 | [plan](player-dash/player-dash.plan.md) · [design](player-dash/player-dash.design.md) · [analysis](player-dash/player-dash.analysis.md) · [report](player-dash/player-dash.report.md) |
| enemy-ranged-attack | completed | ~98% | 2026-07-03 | [plan](enemy-ranged-attack/enemy-ranged-attack.plan.md) · [design](enemy-ranged-attack/enemy-ranged-attack.design.md) · [analysis](enemy-ranged-attack/enemy-ranged-attack.analysis.md) · [report](enemy-ranged-attack/enemy-ranged-attack.report.md) |

## player-dash 요약
- **기능**: 플레이어 대시 v1 (Space + 이동 방향, dashSpeed 24 / 0.2s / 쿨다운 1s) + 대시 사운드
- **결과**: Success Criteria 5/5, Critical/Important 갭 0, 벽 관통 이슈 해결(MovePosition + Continuous)
- **연기**: 스태미너, 니어미스 슬로우모션(적 원거리 공격 선행 필요) — Claude.md 추가 기능
- **후속 완료**: 대시 v2 스태미너(연속 풀 100/34/25) + UI 바 — 별도 세션에서 구현

## enemy-ranged-attack 요약
- **기능**: 원거리 적 신규 유형(사거리 유지 히스테리시스 near4/far7 + 주기 사격 1.5s) + `"EnemyBullet"` 진영 분리 + 플레이어 총알 피격(TakeHit)
- **결과**: Success Criteria 8/8, Match ~98%, Critical/Important 갭 0
- **산출물**: EnemyBullet.cs(신규), RangedEnemyController.cs(신규), PlayerController·GameManager·EnemyController(수정)
- **학습**: Rigidbody2D 회전 잠금 패턴 확립(freezeRotation + 스폰 회전 초기화), 프리팹↔씬 참조 제약(FindObjectOfType)
- **후속**: 니어미스 슬로우모션(EnemyBullet 식별자 활용), 화살/레이저, 스펠 마블
