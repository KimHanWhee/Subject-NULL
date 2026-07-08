using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

// UGS(백엔드) 진입점: 게임 시작 시 1회 초기화 + 익명 로그인.
// 로그인 성공 시 playerId를 콘솔에 남긴다. 이후 Economy/Cloud Code(가챠) 통신의 전제.
// 여러 씬에 있어도 중복 없이 유지(싱글턴 + DontDestroyOnLoad).
public class ServicesBootstrap : MonoBehaviour
{
    public static ServicesBootstrap Instance { get; private set; }

    // 다른 스크립트에서 로그인 완료 여부 확인용
    public static bool IsSignedIn =>
        UnityServices.State == ServicesInitializationState.Initialized &&
        AuthenticationService.Instance.IsSignedIn;

    async void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        await InitAndSignInAsync();
    }

    async Task InitAndSignInAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
                Debug.Log("[UGS] 로그인 성공 · playerId=" + AuthenticationService.Instance.PlayerId);
            AuthenticationService.Instance.SignInFailed += (err) =>
                Debug.LogError("[UGS] 로그인 실패: " + err);

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("[UGS] 초기화/로그인 예외: " + e);
        }
    }
}
