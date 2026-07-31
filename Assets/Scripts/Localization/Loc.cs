using System.Collections.Generic;
using UnityEngine;

public enum Language { Ko = 0, En = 1 }

// 다국어 문자열 테이블 — 키 → (한국어, 영어).
//
// 설계 원칙
//  - 저장 데이터에는 절대 표시 문자열을 쓰지 않는다(마블 저장키 marbleName처럼).
//    여기 있는 건 "화면에 보이는 글자"뿐이라 언어를 바꿔도 세이브/서버 데이터는 그대로다.
//  - 키가 없으면 키 자체를 반환한다 → 누락돼도 화면이 비지 않고 어디가 빠졌는지 바로 보인다.
//
// 현재 범위: 핵심 UI(메인메뉴·설정·일시정지·게임오버·고난 선택).
// 스펠 오브 이름/설명, 플레이 방법 본문, 가챠·덱 화면은 아직 한국어 고정 — 단계적으로 확장.
public static class Loc
{
    public static Language Current { get; private set; } = Language.Ko;

    // 언어가 바뀌면 이미 그려진 UI가 스스로 다시 그리도록 알린다.
    public static event System.Action OnChanged;

    public static void SetLanguage(Language lang)
    {
        if (Current == lang) return;
        Current = lang;
        if (OnChanged != null) OnChanged();
    }

    // 키 존재 여부 — 번역이 없을 때 원본(에셋 문자열)으로 폴백할지 판단하는 데 쓴다.
    public static bool Has(string key) { return table.ContainsKey(key); }

    public static string T(string key)
    {
        string[] v;
        if (!table.TryGetValue(key, out v)) return key; // 누락 감지용
        int i = (int)Current;
        if (i < 0 || i >= v.Length || string.IsNullOrEmpty(v[i])) return v[0];
        return v[i];
    }

    // 서식 문자열용 — T(key)를 포맷 템플릿으로 사용
    public static string T(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>
    {
        // ── 메인 메뉴 ──
        { "menu.start",      new[] { "게임 시작", "START" } },
        { "menu.deck",       new[] { "덱 구성", "DECK" } },
        { "menu.gacha",      new[] { "오브 뽑기", "GACHA" } },
        { "menu.howto",      new[] { "플레이 방법", "HOW TO PLAY" } },
        { "menu.ranking",    new[] { "랭킹", "RANKING" } },
        { "menu.settings",   new[] { "설정", "SETTINGS" } },
        { "common.back",     new[] { "← 뒤로", "← BACK" } },
        { "common.close",    new[] { "닫기", "Close" } },

        // ── 설정 ──
        { "settings.title",     new[] { "설정", "SETTINGS" } },
        { "settings.volume",    new[] { "전체 음량", "Master Volume" } },
        { "settings.language",  new[] { "언어", "Language" } },
        { "settings.mute",      new[] { "음소거", "Mute" } },
        { "settings.hint",      new[] { "변경 사항은 자동 저장됩니다", "Changes are saved automatically" } },

        // ── 일시정지 ──
        { "pause.title",     new[] { "일시정지", "PAUSED" } },
        { "pause.sub",       new[] { "── 실험 일시 중단 ──", "── EXPERIMENT SUSPENDED ──" } },
        { "pause.resume",    new[] { "재개", "RESUME" } },
        { "pause.restart",   new[] { "다시시작", "RESTART" } },
        { "pause.mainmenu",  new[] { "메인메뉴", "MAIN MENU" } },

        // ── 고난 선택 ──
        { "hardship.title",  new[] { "실험 프로토콜 투여", "PROTOCOL INJECTION" } },
        { "hardship.sub",    new[] { "덱을 모두 소진했습니다 — 개체 강화 항목 1개를 선택하십시오",
                                     "Deck depleted — select one specimen enhancement" } },
        { "hardship.pick",   new[] { "선택", "SELECT" } },
        { "hardship.foot",   new[] { "고난은 누적됩니다 · 스펠을 아껴 쓸수록 덱이 늦게 돌아갑니다",
                                     "Hardships stack · Spend spells sparingly to cycle slower" } },
        { "hardship.none",   new[] { "미적용", "None" } },
        { "hardship.stacks", new[] { "현재 {0}중첩", "{0} stacks" } },
        { "hardship.hud",    new[] { "고난", "HARDSHIP" } },

        // 고난 10종 — 이름/효과
        { "hs.muscle.n",   new[] { "근섬유 강화", "Muscle Fiber" } },
        { "hs.muscle.d",   new[] { "적 최대 체력 +25%", "Enemy max HP +25%" } },
        { "hs.nerve.n",    new[] { "신경 가속", "Neural Boost" } },
        { "hs.nerve.d",    new[] { "적 이동·공격속도 +15%", "Enemy move & attack speed +15%" } },
        { "hs.instinct.n", new[] { "공격 본능", "Killer Instinct" } },
        { "hs.instinct.d", new[] { "적 공격력 +25%", "Enemy damage +25%" } },
        { "hs.crowd.n",    new[] { "과밀 배양", "Overcrowding" } },
        { "hs.crowd.d",    new[] { "동시 등장 수 +1", "+1 simultaneous spawn" } },
        { "hs.rapid.n",    new[] { "급속 배양", "Rapid Culture" } },
        { "hs.rapid.d",    new[] { "등장 간격 -15%", "Spawn interval -15%" } },
        { "hs.hard.n",     new[] { "경화 외피", "Hardened Shell" } },
        { "hs.hard.d",     new[] { "받는 피해 -20%, 넉백 저항", "Damage taken -20%, knockback resist" } },
        { "hs.volatile.n", new[] { "자폭 조직", "Volatile Tissue" } },
        { "hs.volatile.d", new[] { "적 사망 시 폭발", "Enemies explode on death" } },
        { "hs.regen.n",    new[] { "재생 조직", "Regeneration" } },
        { "hs.regen.d",    new[] { "적이 초당 체력 1% 회복", "Enemies heal 1% HP per second" } },
        { "hs.frenzy.n",   new[] { "광폭화", "Frenzy" } },
        { "hs.frenzy.d",   new[] { "빈사(30%) 시 속도 2배", "Double speed below 30% HP" } },
        { "hs.velocity.n", new[] { "탄속 개선", "Muzzle Velocity" } },
        { "hs.velocity.d", new[] { "적 탄속 +25%", "Enemy bullet speed +25%" } },

        // ── 스펠 오브 설명 (키 = "spell." + SpellAbility.id + ".d") ──
        // 이름(abilityName)은 이미 영문 고유명이라 번역 대상이 아니다.
        { "spell.Adrenaline.d", new[] { "10초간 체력이 낮을수록 이동속도와 공격속도가 증가한다.",
            "For 10s, the lower your HP the faster you move and attack." } },
        { "spell.Apocalypse.d", new[] { "모든 적에게 즉시 대량 피해를 입힌다.",
            "Instantly deals massive damage to every enemy." } },
        { "spell.BlackHole.d", new[] { "드롭한 위치에 블랙홀을 생성해 범위 내 적을 강하게 끌어당기고, 3초 뒤 폭발하며 범위 안의 적을 소멸시킨다.",
            "Creates a black hole that violently pulls in nearby enemies, then detonates after 3s, annihilating everything caught inside." } },
        { "spell.BladeStorm.d", new[] { "플레이어 주변을 칼날이 회전하며 닿는 적에게 피해를 준다.",
            "Blades orbit the player, damaging any enemy they touch." } },
        { "spell.VoidSlash.d", new[] { "10초간 대시가 공허를 가르는 참격으로 바뀐다. 대시 방향으로 파고들며 경로의 적을 베고, 그동안 무적이며 적과 충돌하지 않는다.",
            "For 10s, your dash becomes a void-cutting slash: cut down every enemy along the path, invulnerable and non-colliding." } },
        { "spell.ChainLightning.d", new[] { "10초간 총알이 적에게 명중하면 주변 적에게 번개가 연쇄적으로 전가한다.",
            "For 10s, bullet hits arc lightning to nearby enemies." } },
        { "spell.Confusion.d", new[] { "드롭한 위치 주변 범위의 모든 적이 5초간 방향 감각을 잃고 무작위로 움직인다.",
            "Enemies in the area lose their bearings and wander randomly for 5s." } },
        { "spell.Decoy.d", new[] { "플레이어 위치에 분신을 생성한다. 적들은 분신을 우선 공격한다.",
            "Spawns a decoy at your position. Enemies target it first." } },
        { "spell.EMField.d", new[] { "6초간 자신 주변으로 들어오는 모든 원거리 공격을 폭발시켜 제거한다.",
            "For 6s, incoming projectiles detonate before they reach you." } },
        { "spell.FlameField.d", new[] { "드롭한 위치에 불꽃 장판을 만들어 범위 내 적에게 지속 피해를 입힌다.",
            "Creates a burning field that damages enemies standing in it." } },
        { "spell.Fortress.d", new[] { "5초간 이동할 수 없는 대신 받는 모든 피해를 무시한다.",
            "For 5s you cannot move, but you ignore all incoming damage." } },
        { "spell.Freeze.d", new[] { "범위 내 적을 2초간 얼린다.",
            "Freezes enemies in the area for 2s." } },
        { "spell.Gatling.d", new[] { "10초간 한 번의 발사가 빠른 3연사로 바뀐다.",
            "For 10s, each shot becomes a rapid 3-round burst." } },
        { "spell.GhostStep.d", new[] { "3초간 이동속도가 증가하고 모든 피해를 받지 않지만, 공격할 수 없다.",
            "For 3s, move faster and take no damage — but you cannot attack." } },
        { "spell.Grenade.d", new[] { "드롭한 위치에 폭발을 일으켜 범위 내 적에게 피해를 준다.",
            "Detonates at the target location, damaging nearby enemies." } },
        { "spell.Heal.d", new[] { "체력을 3 회복한다.", "Restores 3 HP." } },
        { "spell.IronSkin.d", new[] { "10초간 받는 피해가 50% 감소한다.",
            "For 10s, damage taken is reduced by 50%." } },
        { "spell.Lifesteal.d", new[] { "5초간 적에게 기본 공격이 명중할 때마다 체력을 0.5 회복한다.",
            "For 5s, restore 0.5 HP each time a basic shot connects." } },
        { "spell.MirrorWorld.d", new[] { "10초간 완전 무적이 되며, 받은 피해를 주변 모든 적에게 전부 반사한다.",
            "For 10s, become fully invulnerable and reflect all damage taken onto every nearby enemy." } },
        { "spell.PiercingShot.d", new[] { "10초간 발사하는 총알이 적을 관통한다.",
            "For 10s, your bullets pierce through enemies." } },
        { "spell.Railgun.d", new[] { "다음 3번의 공격이 레일건으로 바뀐다. 커서 방향으로 레일건을 발사해 강력한 피해를 주고 적중한 모든 적을 넉백시킨다.",
            "Your next 3 attacks become railgun shots — a piercing beam toward the cursor that deals heavy damage and knocks back every enemy hit." } },
        { "spell.Reflect.d", new[] { "3초간 받는 피해를 공격한 적에게도 반사시킨다.",
            "For 3s, damage you take is also reflected onto the attacker." } },
        { "spell.RepulseNova.d", new[] { "주변의 적을 강하게 밀쳐내고 잠깐 기절시킨다.",
            "Violently knocks back nearby enemies and briefly stuns them." } },
        { "spell.Resurrection.d", new[] { "사망 시 체력 50%로 부활한다 (1회).",
            "Revive once at 50% HP upon death." } },
        { "spell.SafeZone.d", new[] { "지정한 위치에 안전지대를 펼친다. 범위 안에서는 받는 피해가 모두 무효화되며 적은 내부로 들어오지 못한다.",
            "Deploys a safe zone. Inside it all damage is nullified and enemies cannot enter." } },
        { "spell.SecondWind.d", new[] { "10초간 대시가 스태미너를 소모하지 않으며, 대시 쿨타임이 대폭 감소한다.",
            "For 10s, dashing costs no stamina and its cooldown is greatly reduced." } },
        { "spell.Shield.d", new[] { "짧은 시간 피해를 막는 보호막을 생성한다.",
            "Creates a barrier that blocks damage for a short time." } },
        { "spell.SlowAura.d", new[] { "10초간 플레이어 주변 범위의 적과 원거리 공격 속도를 크게 감소시킨다.",
            "For 10s, greatly slows enemies and enemy projectiles around you." } },
        { "spell.SlowField.d", new[] { "범위 내 적의 이동속도를 3초간 50% 감소시킨다.",
            "Slows enemies in the area by 50% for 3s." } },
        { "spell.SniperMode.d", new[] { "10초간 총알 속도와 데미지가 3배가 되지만 이동속도가 느려진다.",
            "For 10s, bullet speed and damage are tripled, but you move slower." } },
        { "spell.SpeedUp.d", new[] { "5초간 이동속도가 증가한다.",
            "Increases movement speed for 5s." } },
        { "spell.Teleport.d", new[] { "지정한 위치로 플레이어가 순간이동한다.",
            "Teleports the player to the target location." } },
        { "spell.ThunderBomb.d", new[] { "10초간 기본 공격이 비격진천뢰 투하로 바뀐다. 커서 위치에 폭탄이 떨어져 주변 적에게 폭발 피해를 입힌다.",
            "For 10s, your basic attack becomes a bomb drop. Bombs fall at the cursor and explode, damaging nearby enemies." } },
        { "spell.TimeStop.d", new[] { "5초간 플레이어를 제외한 모든 것을 정지시킨다. 정지 시간 속 플레이어의 공격력이 매우 증가한다.",
            "For 5s, everything but the player freezes. Your attacks deal massively increased damage while time is stopped." } },

        // ── 등급 ──
        { "grade.normal",  new[] { "일반", "Normal" } },
        { "grade.gold",    new[] { "골드", "Gold" } },
        { "grade.diamond", new[] { "다이아", "Diamond" } },
        { "grade.legend",  new[] { "레전드", "Legend" } },

        // ── 덱 편성 ──
        { "deck.title",       new[] { "스펠 오브 덱 편성", "SPELL ORB DECK" } },
        { "deck.catalog",     new[] { "오브 카탈로그", "ORB CATALOG" } },
        { "deck.catalogHint", new[] { "카드 클릭=덱 추가 · <b>스킬마다</b> 개별 중복 제한(등급 높을수록 적게: 일반6/골드3/다이아2/레전드1)",
            "Click a card to add · <b>per-skill</b> copy limit (higher grade = fewer: Normal 6 / Gold 3 / Diamond 2 / Legend 1)" } },
        { "deck.mydeck",      new[] { "내 덱", "MY DECK" } },
        { "deck.mydeckHint",  new[] { "클릭하면 제거됩니다", "Click to remove" } },
        { "deck.save",        new[] { "덱 저장", "SAVE" } },
        { "deck.autofill",    new[] { "자동 채우기", "AUTO FILL" } },
        { "deck.clear",       new[] { "비우기", "CLEAR" } },
        { "deck.locked",      new[] { "미보유", "LOCKED" } },
        { "deck.lockedHint",  new[] { "뽑기로 획득", "Unlock via gacha" } },
        { "deck.msgNotOwned", new[] { "미보유 오브입니다 — 뽑기로 획득하세요",
            "You don't own this marble — get it from the gacha" } },
        { "deck.msgFull",     new[] { "덱이 가득 찼습니다 (최대 {0}개)", "Deck is full (max {0})" } },
        { "deck.msgMaxCopies",new[] { "'{0}'은(는) 최대 {1}개까지 ({2} 등급)",
            "'{0}' is limited to {1} copies ({2})" } },
        { "deck.msgSize",     new[] { "덱은 {0}~{1}개여야 합니다", "Deck must contain {0}–{1} marbles" } },
        { "deck.msgSaved",    new[] { "덱 저장 완료!", "Deck saved!" } },
        { "deck.msgFilled",   new[] { "남은 칸을 무작위로 채웠습니다", "Filled the remaining slots at random" } },

        // ── 가챠 / 도감 ──
        { "gacha.title",       new[] { "오브 뽑기", "ORB GACHA" } },
        { "gacha.charge",      new[] { "＋ GEM 충전", "＋ BUY GEM" } },
        { "gacha.pullPanel",   new[] { "뽑기", "PULL" } },
        { "gacha.resultHint",  new[] { "여기에 결과가 표시됩니다", "Results appear here" } },
        { "gacha.rateBtn",     new[] { "확률 상세 보기", "Drop Rates" } },
        { "gacha.rateTitle",   new[] { "뽑기 확률 상세", "DROP RATES" } },
        { "gacha.rateSub",     new[] { "등급을 먼저 추첨한 뒤, 같은 등급 안에서는 균등 확률로 결정됩니다",
            "Grade is rolled first, then evenly among the skills of that grade" } },
        { "gacha.colGrade",    new[] { "등급", "Grade" } },
        { "gacha.colSkill",    new[] { "스킬명", "Skill" } },
        { "gacha.colRate",     new[] { "확률(%)", "Rate (%)" } },
        { "gacha.rateSum",     new[] { "등급 합계 — ", "Totals — " } },
        { "gacha.rateNote",    new[] { "일반 등급 14종은 기본 보유로 뽑기 대상이 아닙니다 · 중복 획득 시 조각으로 환급됩니다",
            "The 14 Normal skills are owned by default and excluded · duplicates refund shards" } },
        { "gacha.collection",  new[] { "도감 · 조각 교환", "COLLECTION · SHARD EXCHANGE" } },
        { "gacha.loading",     new[] { "로딩 중", "Loading" } },
        { "gacha.multiTitle",  new[] { "{0}연 뽑기", "{0}× PULL" } },
        { "gacha.revealAll",   new[] { "전체 공개", "REVEAL ALL" } },
        { "gacha.multiSum",    new[] { "신규 <color=#7FE08A>{0}종</color>  ·  <color=#C8A0FF>+조각 {1}</color>",
            "New <color=#7FE08A>{0}</color>  ·  <color=#C8A0FF>+{1} shards</color>" } },
        { "gacha.confirm",     new[] { "확인", "OK" } },
        { "gacha.dupShard",    new[] { "중복 +조각 {0}", "DUP +{0} shards" } },
        { "gacha.owned",       new[] { "보유중", "OWNED" } },
        { "gacha.exchange",    new[] { "조각 {0} 교환", "{0} shards" } },
        { "gacha.shards",      new[] { "조각", "Shards" } },
        { "gacha.pull1",       new[] { "1회 뽑기", "PULL ×1" } },
        { "gacha.pull10",      new[] { "10회 뽑기", "PULL ×10" } },
        { "gacha.chargeTitle", new[] { "GEM 충전", "BUY GEM" } },
        { "gacha.chargeSub",   new[] { "결제하면 서버 계정에 GEM이 즉시 지급됩니다.",
            "GEM is credited to your account immediately after payment." } },
        { "gacha.pkgLoading",  new[] { "상품 불러오는 중...", "Loading products..." } },
        { "gacha.pkgFail",     new[] { "상품을 불러오지 못했습니다", "Failed to load products" } },
        { "gacha.buy",         new[] { "구매", "BUY" } },

        { "gacha.msgNoGem",      new[] { "GEM이 부족합니다 (필요 {0})", "Not enough GEM (need {0})" } },
        { "gacha.msgNoGemShort", new[] { "GEM이 부족합니다", "Not enough GEM" } },
        { "gacha.msgPullFail",   new[] { "뽑기에 실패했습니다", "Pull failed" } },
        { "gacha.msgPullNet",    new[] { "네트워크 오류로 뽑기 실패", "Pull failed — network error" } },
        { "gacha.msgNoShard",    new[] { "조각이 부족합니다 (필요 {0})", "Not enough shards (need {0})" } },
        { "gacha.msgExchanged",  new[] { "'{0}' 교환 완료!", "'{0}' unlocked!" } },
        { "gacha.msgExchFail",   new[] { "교환 실패", "Exchange failed" } },
        { "gacha.msgExchNet",    new[] { "네트워크 오류로 교환 실패", "Exchange failed — network error" } },
        { "gacha.msgNeedAccount",new[] { "구매하려면 계정이 필요합니다 — 계정 화면으로 이동합니다",
            "An account is required to purchase — opening the account screen" } },
        { "gacha.msgPaymentSoon",new[] { "결제 서비스 준비 중입니다 ({0})", "Payment service coming soon ({0})" } },
        { "gacha.msgPurchased",  new[] { "결제 완료! GEM {0} 지급되었습니다", "Purchase complete! {0} GEM added" } },
        // ── 청약철회 ──
        { "wd.open",       new[] { "결제 내역 · 청약철회", "Purchases · Withdrawal" } },
        { "wd.title",      new[] { "결제 내역", "PURCHASES" } },
        { "wd.empty",      new[] { "결제 내역이 없습니다.", "No purchases yet." } },
        { "wd.loading",    new[] { "불러오는 중…", "Loading…" } },
        { "wd.button",     new[] { "청약철회", "Withdraw" } },
        { "wd.note",       new[] { "결제일로부터 7일 이내, GEM을 사용하지 않은 결제만 철회할 수 있습니다.",
            "Only purchases made within 7 days with unused GEM can be withdrawn." } },
        // 자동 철회 대상이 아닌 건(사용함·기간 만료·오류)의 출구. 없으면 사용자가 연락할 방법이 없다.
        { "wd.support",    new[] { "철회가 불가능한 건은 <b>subjectnull.game@gmail.com</b> 으로 문의해 주세요.\n계정 ID는 위 [계정] 화면에서 복사할 수 있습니다.",
            "For purchases that cannot be withdrawn here, contact <b>subjectnull.game@gmail.com</b>.\nYou can copy your account ID from the [Account] screen." } },
        // 비활성 사유 — 왜 안 되는지 보여줘야 문의가 줄어든다
        { "wd.r.used",     new[] { "GEM 사용됨", "GEM used" } },
        { "wd.r.expired",  new[] { "기간 만료(7일)", "Period expired (7d)" } },
        { "wd.r.refunded", new[] { "환불 완료", "Refunded" } },
        { "wd.r.processing", new[] { "처리 중", "Processing" } },
        { "wd.r.nocapture",new[] { "자동 철회 불가 — 문의 바랍니다", "Not eligible — please contact support" } },
        { "wd.confirmTitle", new[] { "청약철회 하시겠습니까?", "Withdraw this purchase?" } },
        { "wd.confirmBody",  new[] { "<b>{0}</b> 결제를 철회합니다.\nGEM {1} 가 회수되고 결제 금액이 환불됩니다.",
            "Withdrawing <b>{0}</b>.\n{1} GEM will be removed and the payment refunded." } },
        { "wd.done",       new[] { "청약철회 완료 — {0} 환불 처리되었습니다", "Withdrawal complete — {0} refunded" } },
        { "wd.failed",     new[] { "청약철회에 실패했습니다 ({0})", "Withdrawal failed ({0})" } },

        // ── 자산 소모 확인 팝업 ──
        { "confirm.yes",        new[] { "확인", "Confirm" } },
        { "confirm.no",         new[] { "취소", "Cancel" } },
        { "confirm.pullTitle",  new[] { "오브를 뽑으시겠습니까?", "Pull orbs?" } },
        { "confirm.pullBody",   new[] { "GEM {0} 를 사용해 {1}회 뽑기를 진행합니다.\n보유 GEM: {2}",
            "This will spend {0} GEM for {1} pull(s).\nYour GEM: {2}" } },
        { "confirm.exchTitle",  new[] { "조각으로 교환하시겠습니까?", "Exchange shards?" } },
        { "confirm.exchBody",   new[] { "구슬 조각 {0} 를 사용해\n<b>{1}</b> 를 획득합니다.\n보유 조각: {2}",
            "This will spend {0} shards to obtain\n<b>{1}</b>.\nYour shards: {2}" } },
        { "confirm.buyTitle",   new[] { "결제하시겠습니까?", "Confirm purchase" } },
        { "confirm.buyBody",    new[] { "<b>{0}</b>\n결제 금액 <b>{1}</b> · GEM <b>{2}</b> 지급",
            "<b>{0}</b>\nAmount <b>{1}</b> · <b>{2}</b> GEM" } },
        // 결제 전 고지 — 이 안내를 미리 보여줘야 "사용한 GEM 환불 제한"이 효력을 갖는다.
        // ⚠️ 실제 구현(WithdrawPurchase)과 문구가 어긋나면 안 된다.
        //    구현 규칙: 7일 이내 + 결제 후 GEM 잔액이 그대로일 때만 전액 철회.
        //    한 번이라도 사용하면 그 결제는 통째로 철회 불가(부분 환불 없음).
        { "confirm.buyNotice",  new[] {
            "• GEM은 게임 내에서만 사용할 수 있으며 현금으로 환전되지 않습니다.\n" +
            "• 결제일로부터 <b>7일 이내</b>, GEM을 <b>사용하지 않은 경우에 한해</b> 청약철회할 수 있습니다.\n" +
            "• <color=#FFAF6A>결제 후 GEM을 한 번이라도 사용하면 해당 결제는 청약철회할 수 없습니다.</color>\n" +
            "• 철회는 [계정] 화면의 [결제 내역]에서 직접 하실 수 있습니다.\n" +
            "• 확인을 누르면 위 내용에 동의하는 것으로 봅니다.",
            "• GEM can only be used in-game and cannot be exchanged for cash.\n" +
            "• Withdrawal is available <b>within 7 days</b>, and only if the GEM is <b>still unused</b>.\n" +
            "• <color=#FFAF6A>Once you spend any GEM, this purchase can no longer be withdrawn.</color>\n" +
            "• You can withdraw it yourself from [Account] → [Purchases].\n" +
            "• Pressing Confirm means you agree to the above." } },

        // 실패 코드를 함께 보여준다 — 원인이 전부 한 문구로 뭉개지면 문의가 와도 특정할 수 없다.
        { "gacha.msgPayFail",    new[] { "결제에 실패했습니다 ({0})", "Payment failed ({0})" } },
        { "gacha.msgLoadFail",   new[] { "정보를 불러오지 못했습니다 — 잠시 후 다시 열어 주세요",
            "Couldn't load your data — please reopen in a moment" } },
        { "gacha.msgPayCaptureFail", new[] { "결제는 완료됐지만 지급에 실패했습니다 — 잠시 후 다시 시도해 주세요",
            "Payment went through but the reward failed — please try again shortly" } },

        // ── 슈트(타입) 역할 ──
        { "suit.spade",   new[] { "공격", "Attack" } },
        { "suit.heart",   new[] { "회복", "Heal" } },
        { "suit.club",    new[] { "유틸", "Utility" } },
        { "suit.diamond", new[] { "방어", "Defense" } },

        // ── 플레이 방법 ──
        { "howto.title", new[] { "게임 방법", "HOW TO PLAY" } },
        { "howto.tab0",  new[] { "조작", "Controls" } },
        { "howto.tab1",  new[] { "스펠 오브", "Spell Orbs" } },
        { "howto.tab2",  new[] { "조커", "Joker" } },

        { "howto.body0", new[] {
            "<b>이동</b>   WASD / 방향키\n\n" +
            "<b>공격</b>   마우스 좌클릭 (커서 방향)\n\n" +
            "<b>대시</b>   SPACE\n" +
            "     스태미너 소모\n" +
            "     원거리 공격 회피 · 쿨타임 1초\n\n" +
            "<b>Shift</b>   스펠 오브",

            "<b>Move</b>   WASD / Arrow Keys\n\n" +
            "<b>Attack</b>   Left Click (toward cursor)\n\n" +
            "<b>Dash</b>   SPACE\n" +
            "     Consumes stamina\n" +
            "     Dodges ranged attacks · 1s cooldown\n\n" +
            "<b>Shift</b>   Spell Orbs" } },

        { "howto.body1", new[] {
            "스펠 오브는 특수 능력을 발동시키는 마법 구슬입니다.\n" +
            "덱에서 직접 원하는 스펠 오브를 편성하여 사용할 수 있습니다.\n\n" +
            "<b>타입</b>\n" +
            "   <color=#7FB0FF>스페이드 (공격)</color>   <color=#FF7F8A>하트 (버프)</color>\n" +
            "   <color=#7FE08A>클로버 (유틸)</color>   <color=#C8A0FF>다이아몬드 (방어)</color>\n\n" +
            "<b>등급</b>   일반 → <color=#FFD24A>골드</color> → <color=#66D0FF>다이아</color> → <color=#FF8AF0>레전드</color>\n" +
            "   높은 등급일수록 강한 능력.\n" +
            "   골드 이상은 <b>오브 뽑기</b> 및 구슬 조각 교환으로 획득.\n\n" +
            "<b>사용</b>   Shift 홀드 시 스펠 오브 벨트 팝업\n" +
            "        Shift 홀드 + 오브 드래그 & 드롭 시 사용",

            "Spell marbles are orbs that trigger special abilities.\n" +
            "Build your own deck from the marbles you own.\n\n" +
            "<b>Types</b>\n" +
            "   <color=#7FB0FF>Spade (Attack)</color>   <color=#FF7F8A>Heart (Buff)</color>\n" +
            "   <color=#7FE08A>Club (Utility)</color>   <color=#C8A0FF>Diamond (Defense)</color>\n\n" +
            "<b>Grades</b>   Normal → <color=#FFD24A>Gold</color> → <color=#66D0FF>Diamond</color> → <color=#FF8AF0>Legend</color>\n" +
            "   Higher grades hold stronger abilities.\n" +
            "   Gold and above come from <b>Orb Gacha</b> or shard exchange.\n\n" +
            "<b>Use</b>   Hold Shift to open the marble belt\n" +
            "        Hold Shift + drag & drop a marble to cast" } },

        { "howto.body2", new[] {
            "덱에 편성된 스펠 오브를 <b>모두 사용</b>하면\n" +
            "<color=#FFD24A><b>JOKER</b></color> 가 발동됩니다.\n\n" +
            "<b>조커의 장난</b>   적을 강화합니다\n" +
            "   플레이어는 제시된 <b>3가지 중 하나</b>를 직접 고를 수 있습니다.\n" +
            "   효과는 <color=#FF8A7A>판이 끝날 때까지 누적</color>됩니다.\n\n" +
            "<b>조커의 심판</b>   선택 직후 발동\n" +
            "   · <color=#FF7A7A>대숙청</color> — 안전지대 밖 전멸\n" +
            "   · <color=#FFE066>번개 폭풍</color> — 낙뢰 회피\n" +
            "   <b>플레이어와 적 모두에게</b> 적용되니\n" +
            "   잘 이용해 적을 효율적으로 물리쳐보세요!",

            "Use <b>every marble</b> in your deck and\n" +
            "<color=#FFD24A><b>JOKER</b></color> is triggered.\n\n" +
            "<b>Joker's Prank</b>   Empowers the enemy\n" +
            "   You choose <b>one of three</b> enhancements yourself.\n" +
            "   Their effects <color=#FF8A7A>stack for the rest of the run</color>.\n\n" +
            "<b>Joker's Judgment</b>   Fires right after your pick\n" +
            "   · <color=#FF7A7A>Purge</color> — everything outside the safe zone dies\n" +
            "   · <color=#FFE066>Lightning Storm</color> — dodge the strikes\n" +
            "   It hits <b>you and the enemies alike</b>,\n" +
            "   so turn it against them!" } },

        // ── 로그인 게이트(진입 씬) ──
        { "login.connecting", new[] { "연결 중...", "Connecting..." } },
        { "login.ready",      new[] { "Press Any Key to Start", "Press Any Key to Start" } },
        { "login.fail",       new[] { "연결 실패 — 아무 키나 눌러 재시도",
            "Connection failed — press any key to retry" } },
        { "login.guest",      new[] { "게스트로 시작", "PLAY AS GUEST" } },
        { "login.google",     new[] { "Google로 시작", "SIGN IN WITH GOOGLE" } },

        // ── 계정 화면 ──
        { "acc.title",        new[] { "계정", "ACCOUNT" } },
        { "acc.connecting",   new[] { "서버 연결 중...", "Connecting to server..." } },
        { "acc.failed",       new[] { "서버에 연결하지 못했습니다", "Could not connect to the server" } },
        { "acc.guestNotice",  new[] { "게스트 진행상황(GEM·오브)은 이 브라우저에만 저장됩니다.\nGoogle 계정을 연결하면 어떤 기기에서든 이어할 수 있습니다.",
            "Guest progress (GEM & marbles) is stored only in this browser.\nLink a Google account to continue on any device." } },
        { "acc.linked",       new[] { "현재 계정: <b><color=#7FE08A>Google 연결됨</color></b> (영구 · 어느 기기서든 복구 가능)",
            "Account: <b><color=#7FE08A>Google linked</color></b> (permanent · restorable on any device)" } },
        { "acc.guest",        new[] { "현재 계정: <color=#E0A06A>게스트</color> — 아직 이 브라우저에만 저장됩니다",
            "Account: <color=#E0A06A>Guest</color> — stored only in this browser for now" } },
        { "acc.linkGoogle",   new[] { "Google 계정 연결", "Link Google Account" } },
        { "acc.linkGoogleWeb",new[] { "Google 계정 연결 (웹 빌드 전용)", "Link Google Account (web build only)" } },
        { "acc.switch",       new[] { "다른 계정으로 로그인", "Sign in with another account" } },
        { "acc.logout",       new[] { "로그아웃", "Sign out" } },
        { "acc.change",       new[] { "변경", "Edit" } },
        { "acc.copy",         new[] { "복사", "Copy" } },
        { "acc.save",         new[] { "저장", "Save" } },
        { "acc.playerId",     new[] { "플레이어 ID: ", "Player ID: " } },
        { "acc.nickname",     new[] { "닉네임: ", "Nickname: " } },
        { "acc.msgNickSaved", new[] { "닉네임이 변경되었습니다", "Nickname updated" } },
        { "acc.msgIdCopied",  new[] { "플레이어 ID를 복사했습니다", "Player ID copied" } },
        { "acc.warnSwitch",   new[] { "지금 게스트 진행상황을 잃습니다 — 한 번 더 클릭",
            "You will lose this guest progress — click once more" } },
        { "acc.warnSwitch2",  new[] { "이 브라우저의 게스트 계정(GEM·오브)은 복구할 수 없게 됩니다.",
            "This browser's guest account (GEM & marbles) becomes unrecoverable." } },
        { "acc.msgLinked",    new[] { "Google 계정 연결 완료! 이제 어느 기기에서든 이어할 수 있어요.",
            "Google account linked! You can now continue on any device." } },

        // ── 게임 오버 ──
        { "over.score",    new[] { "점수  {0}", "SCORE  {0}" } },
        { "over.best",     new[] { "최고기록  {0}", "BEST  {0}" } },
        { "over.newBest",  new[] { "★ 신기록! ★", "★ NEW RECORD! ★" } },

        // ── 인게임 알림 ──
        { "game.powerUp",  new[] { "공격력 강화!", "POWER UP!" } },

        // ── 랭킹 ──
        { "rank.title",    new[] { "글로벌 랭킹", "GLOBAL RANKING" } },
        { "rank.loading",  new[] { "불러오는 중...", "Loading..." } },
        { "rank.failed",   new[] { "랭킹을 불러오지 못했습니다", "Could not load the ranking" } },
        { "rank.empty",    new[] { "아직 기록이 없습니다", "No records yet" } },
        { "rank.needLogin",new[] { "로그인 후 이용할 수 있습니다", "Sign in to view the ranking" } },
        { "rank.myRank",   new[] { "내 순위  {0}위 · {1}", "YOUR RANK  #{0} · {1}" } },
        { "rank.anon",     new[] { "실험체-", "Subject-" } },
        { "rank.errNotSignedIn", new[] { "로그인 상태가 아닙니다", "You are not signed in" } },
        { "rank.errNameLen",     new[] { "닉네임은 2~16자로 입력하세요", "Nickname must be 2–16 characters" } },
        { "rank.errNameSpace",   new[] { "닉네임에 공백은 쓸 수 없습니다", "Nickname cannot contain spaces" } },
        { "rank.errNameFail",    new[] { "닉네임 변경에 실패했습니다", "Failed to update the nickname" } },
        { "rank.errFetch",       new[] { "불러오지 못했습니다", "Failed to load" } },
    };
}
