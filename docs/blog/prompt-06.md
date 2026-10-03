# 6편 자료 — 서버 붙이고 세상에 내놓기

> 내 PC에서만 돌던 게임에 계정·랭킹·가챠를 붙이고,
> 브라우저에서 돌아가게 만들어 배포한 단계.

---

## 왜 서버가 필요했나

가챠를 만들려니 막혔다.

**확률을 클라이언트가 굴리면 안 된다.** 게임 안에서 난수를 돌리면, 마음만 먹으면 조작할 수 있다. 레전드 확률 3%를 100%로 바꾸는 게 어렵지 않다.

게다가 이런 것들도 로컬에만 있으면 의미가 없었다.

- **계정** — 기기를 바꾸면 모은 오브가 사라진다
- **랭킹** — 내 점수만 보이면 랭킹이 아니다
- **소유 정보** — 어떤 오브를 가졌는지

---

## Unity Gaming Services (UGS)

Unity가 제공하는 백엔드다. 서버를 직접 세울 필요가 없다.

쓴 것:

```
com.unity.services.authentication   3.7.2   로그인
com.unity.services.cloudcode        2.10.3  서버 로직
com.unity.services.cloudsave        3.4.1   플레이어 데이터
com.unity.services.economy          3.5.3   재화(GEM)
com.unity.services.leaderboards     2.3.4   랭킹
```

### 로그인 — 익명 먼저

```csharp
await AuthenticationService.Instance.SignInAnonymouslyAsync();
```

가입 절차 없이 바로 플레이할 수 있다. 계정을 만들라고 하면 그 자리에서 나가는 사람이 많다.

나중에 Google 로그인을 붙여서, **게스트로 놀다가 원할 때 계정에 연결**할 수 있게 했다.

---

## 서버 권위 — 이 편의 핵심

**중요한 판단은 전부 서버에서 한다.** 클라이언트는 호출하고 결과를 연출할 뿐이다.

가챠 서버 코드의 첫 줄에 이렇게 적어뒀다.

> 클라는 결과 marbleName으로 SO를 찾아 연출만. RNG/재화/소유는 전적으로 서버.

### 서버가 하는 일

```
① GEM 100 차감
② 등급 추첨      Gold 80 / Diamond 17 / Legend 3
③ 그 등급에서 오브 하나 추첨
④ 이미 가진 것이면 → 조각으로 환산
⑤ 소유 목록에 추가
```

이 다섯 단계가 **한 번의 서버 호출** 안에서 끝난다. 중간에 끼어들 틈이 없다.

### 재화도 잠갔다

GEM은 Economy의 통화로 두고, **플레이어가 직접 증감하지 못하게** 접근 권한을 막았다. 서버 로직만 건드릴 수 있다.

### 서버 스크립트 9개

```
GachaPull            뽑기
ExchangeMarble       조각으로 오브 교환
GetProfile           프로필 조회
GetGemPackages       상품 목록
CreatePaypalOrder    결제 주문 생성
CapturePaypalOrder   결제 확정 + 지급
GetPendingOrder      미완료 주문 조회
GetPurchaseHistory   구매 내역
WithdrawPurchase     청약철회(환불)
```

뒤의 다섯 개는 결제용이다. **그 얘기는 다음 편에서.**

---

## WebGL — "에디터에서 되니까 되겠지"가 안 통했다

브라우저에서 돌리려면 WebGL로 빌드해야 한다. 여기서 함정이 줄줄이 나왔다.

### ① 한글만 투명하게 사라졌다

에디터에서는 멀쩡한데 브라우저 빌드에서 **한글만** 안 보였다. 영문과 숫자는 나온다.

원인: Unity 기본 폰트에 한글 글리프가 없다. 에디터와 윈도우에서는 **OS 폰트가 대신 그려주는데**, 브라우저는 OS 폰트에 접근할 수 없다.

해결: 한글 폰트를 `Resources/` 에 넣고 코드에서 명시적으로 불러온다.

```csharp
Resources.Load<Font>("Fonts/Pretendard-Regular")
```

지금 **14곳**에서 이렇게 부른다. UI를 코드로 만드는 프로젝트라 텍스트를 만드는 곳마다 필요했다.

> 이 함정은 나중에 또 밟았다. 경로를 `Fonts/Pretendard`로 한 글자 틀리게 썼더니
> 조용히 `null`이 되고 기본 폰트로 폴백돼서, **그 UI만 한글이 깨졌다.**
> 숫자와 영문은 멀쩡해서 에디터에서는 알아채기 어려웠다.

### ② `Task.Delay`가 안 돈다

WebGL에는 스레드가 없다. `await Task.Delay(1000)` 같은 걸 쓰면 그냥 멈춘다.

프레임 단위로 기다리는 헬퍼를 만들어 다섯 군데를 바꿨다.

```csharp
public static async Task WaitSignedInAsync(float timeout = 10f)
{
    float deadline = Time.realtimeSinceStartup + timeout;
    while (!IsSignedIn && Time.realtimeSinceStartup < deadline)
        await Awaitable.NextFrameAsync();
}
```

### ③ `Ctrl`이 브라우저에게 먹혔다

스펠 벨트를 여는 키가 원래 `Ctrl`이었다. 그런데 브라우저에서는 **`Ctrl+W`가 탭을 닫는다.**

게임을 하다가 탭이 꺼진다. `Shift`로 바꿨다.

> 게임 안에서만 생각하면 안 된다. **브라우저가 먼저 가져가는 키**가 있다.

### ④ 압축 설정

Brotli로 압축하고 **압축 해제 폴백**을 켰다.

```
webGLCompressionFormat: 0        Brotli
webGLDecompressionFallback: 1    폴백 켬
```

폴백을 켜면 `Content-Encoding` 헤더를 안 보내는 서버에서도 돌아간다. 서버 설정에 신경 쓸 필요가 없어진다.

`runInBackground`도 켰다. 탭을 잠깐 벗어나도 서버 통신이 끊기지 않는다.

---

## 배포 — Netlify

빌드 결과 폴더를 통째로 올리면 끝이다.

```bash
netlify deploy --prod --dir=Builds/WebGL
```

### 드래프트 배포가 유용했다

`--prod`를 빼면 **추측할 수 없는 임시 주소**가 나온다.

```
실 사이트   subjectnull.netlify.app
드래프트    6a7ac463...--subjectnull.netlify.app
```

실제 유저는 계속 이전 버전을 하고, 새 버전은 임시 주소로 혼자 확인한다. 특히 **휴대폰에서 테스트할 때** 이게 아니면 방법이 마땅찮았다.

### 커스텀 템플릿

Unity 기본 템플릿 대신 직접 만들었다. 게임 화면이 창 크기에 맞춰 **16:9로 유지**되도록 하고, 파비콘과 전체화면 버튼을 넣었다.

---

## 결과

**https://subjectnull.netlify.app**

주소만 있으면 누구나 브라우저에서 바로 플레이할 수 있게 됐다. 설치도, 다운로드도 없다.

| | |
|---|---|
| 서버 스크립트 | 9개 |
| 빌드 크기 | 약 31MB |
| 저장되는 것 | 계정·소유 오브·조각·덱·최고점수 |

---

## 자료에 없는 것 (본인만 아는 부분)

- UGS를 고른 이유 (다른 후보를 봤는지)
- 처음 배포하고 주소를 열었을 때
- 한글이 사라진 걸 발견했을 때 어떻게 원인을 좁혔는지
- 남에게 주소를 처음 보냈을 때
