# 6편 추가분 — Unity Cloud 대시보드 설정

> 이미 쓴 6편(서버와 배포) 글에 **끼워 넣을 부분**입니다.
> 위치: "UGS 패키지를 추가했다" 다음, "서버 권위" 앞.

---

## 패키지만 추가하면 되는 게 아니었다

Unity 쪽에서 패키지를 넣는 건 시작일 뿐이다. **웹 대시보드에서 서비스를 하나씩 켜고 설정해야** 실제로 동작한다.

**https://cloud.unity.com**

여기서 프로젝트를 만들면 Unity 에디터와 연결된다. 연결되면 에디터의 프로젝트 설정에 이런 값이 박힌다.

```
projectName      Subject:NULL
organizationId   hanwhee2
cloudProjectId   d23d2b4d-...
```

---

## 제품 탭에서 켠 것들

대시보드에는 **제품(Products)** 목록이 있고, 쓸 서비스를 골라 켜는 방식이다. 켜지 않으면 코드에서 호출해도 실패한다.

### Authentication — 로그인

- **익명 로그인** 활성화
- Google 로그인을 위해 **Identity Provider 등록**
  - 공급자 이름을 `oidc-google` 로 만들었다
  - **이 이름이 코드와 정확히 같아야 한다.** 코드에 이렇게 박혀 있다:
    ```csharp
    public const string ProviderName = "oidc-google";
    ```

### Cloud Code — 서버 로직

- 스크립트 9개를 올린다
- 대시보드에서 직접 붙여넣어도 되고, CLI로 한 번에 올려도 된다
  ```bash
  ugs deploy CloudCode
  ```

### Economy — 재화

- 통화(Currency)를 하나 만든다
  - **ID: `GEM`** (코드가 이 문자열로 찾는다)
  - 표시 이름: 젬
  - 최대 보유량: 2,147,483,647
- **Access Control에서 플레이어의 직접 증감을 막았다.** 이게 핵심이다. 막지 않으면 클라이언트에서 재화를 늘릴 수 있다. 서버 로직(Cloud Code)만 건드릴 수 있게 잠근다.

### Leaderboards — 랭킹

- 리더보드를 하나 만든다
  - **ID: `HIGH_SCORE`**
- 코드에도 같은 문자열이 있다:
  ```csharp
  public const string LeaderboardId = "HIGH_SCORE";
  ```

### Cloud Save — 플레이어 데이터

- 따로 만들 게 없다. 켜기만 하면 키-값으로 바로 쓴다
- 쓰고 있는 키: `ownedMarbles`(보유 오브), `shards`(조각), `deck`(덱), `passives`(패시브 칸), `highScore`

---

## 여기서 배운 것 — 이름이 곧 계약이다

대시보드에서 만든 **ID와 코드의 문자열이 정확히 같아야** 한다.

| 대시보드 | 코드 |
|---|---|
| Currency ID `GEM` | `GemCurrencyId = "GEM"` |
| Leaderboard ID `HIGH_SCORE` | `LeaderboardId = "HIGH_SCORE"` |
| Provider `oidc-google` | `ProviderName = "oidc-google"` |

한 글자만 틀려도 **컴파일은 멀쩡하게 되고 실행할 때 조용히 실패한다.** 에러 메시지도 "없는 리소스"라고만 나와서, 오타인지 설정을 안 한 건지 구분이 안 된다.

그래서 코드에 주석으로 적어뒀다.

```csharp
// 글로벌 랭킹(UGS Leaderboards) — 대시보드에 ID "HIGH_SCORE" 리더보드 필요
```

---

## 환경(Environment) 개념

대시보드에는 **환경**이 나뉘어 있다. 기본으로 `production` 하나가 있고, 개발용을 따로 둘 수도 있다.

설정값이 **환경마다 따로** 저장된다. 나중에 결제를 붙일 때 이것 때문에 크게 헤맸다. (7편에서)

---

## 구성원 추가

<!-- ✍️ 여기는 직접 채워야 합니다 -->
<!--
  - 왜 구성원을 추가했는지 (테스트를 맡기려고? 같이 만드는 사람이 있어서?)
  - 어떤 권한을 줬는지
  - 추가한 뒤에 달라진 점이 있었는지
  대시보드 → 조직(Organization) → Members 에서 이메일로 초대하는 방식입니다.
-->

---

<!--
📌 사실관계
- 주소, 프로젝트/조직 정보, 서비스 ID, 코드 상수는 전부 실제 프로젝트 설정에서 확인한 값
- "구성원 추가" 항목만 자료가 없어 비워둠
-->
