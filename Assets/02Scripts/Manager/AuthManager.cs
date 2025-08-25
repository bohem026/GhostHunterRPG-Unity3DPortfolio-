using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID || UNITY_IOS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Firebase.Auth;
using Firebase.Extensions;
#endif

public class AuthManager : MonoBehaviour
{
    public static AuthManager Inst;

    [Header("UI")]
    [SerializeField] private GameObject body_FBLogin;
    [SerializeField] private InputField input_Email;
    [SerializeField] private GameObject alert_Email;
    [SerializeField] private InputField input_Password;
    [SerializeField] private GameObject alert_Password;

    [Header("LOG")]
    [SerializeField] private Text text_UID;

    private bool isAuthenticated = false;

    /// <summary>
    /// 싱글톤 레퍼런스를 설정한다.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// 로그인 상태 이벤트를 구독하고, Firebase Auth 초기화 및 세션 정리 후
    /// GPGS 로그인 시도를 시작한다.
    /// </summary>
    private void Start()
    {
        // 1) 로그인 상태 구독
        FBAuthManager.Instance.LoginState += OnLoginStateChanged;

        // 2) Firebase Auth 초기화(브로드캐스트는 Start에서 직접 제어)
        FBAuthManager.Instance.Init(doBroadcast: false);

        // 3) 부팅 시 기존 세션 정리
        FBAuthManager.Instance.Logout();

        // 4) GPGS 로그인 시도 (모바일), 그 외 플랫폼은 이메일 UI로 폴백
        TryGPGSLogin();
    }

    /// <summary>
    /// 구독 해제.
    /// </summary>
    private void OnDestroy()
    {
        FBAuthManager.Instance.LoginState -= OnLoginStateChanged;
    }

    /// <summary>
    /// 로그인 상태 변화 콜백. 성공 시 Firestore 초기화 후 후속 처리.
    /// </summary>
    private void OnLoginStateChanged(bool signedIn)
    {
        if (signedIn)
        {
            if (isAuthenticated) return; // 중복 처리 방지
            isAuthenticated = true;

            Debug.Log("Auth Success");

            // 1) Firestore 초기화
            FBFirestoreManager.Instance.Init();

            // 2) 메인 씬 진입 등 후속 동작
            OnLoginSuccess();
        }
        else
        {
            isAuthenticated = false;
        }
    }

    /// <summary>
    /// 모바일: GPGS 로그인 → 서버 인증 코드 획득 → Firebase Credential 연동.
    /// 에디터/PC: 이메일 로그인 UI 노출.
    /// </summary>
    private void TryGPGSLogin()
    {
#if UNITY_ANDROID || UNITY_IOS
        PlayGamesPlatform.Activate();

        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                // 서버 인증 코드 → Firebase Credential 전환
                PlayGamesPlatform.Instance.RequestServerSideAccess(false, authCode =>
                {
                    if (!string.IsNullOrEmpty(authCode))
                    {
                        var credential = PlayGamesAuthProvider.GetCredential(authCode);

                        FBAuthManager.Instance.SignInWithCredential(
                            credential,
                            onFail: err =>
                            {
                                // GPGS→Firebase 실패 시 이메일 로그인 UI로 폴백
                                StartCoroutine(ShowEmailLoginUI());
                            }
                        );
                    }
                    else
                    {
                        // 인증 코드 획득 실패 → 이메일 로그인 UI
                        StartCoroutine(ShowEmailLoginUI());
                    }
                });
            }
            else
            {
                // GPGS 로그인 실패 → 이메일 로그인 UI
                StartCoroutine(ShowEmailLoginUI());
            }
        });
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN
        // 에디터/PC: 바로 이메일 로그인 UI 노출
        StartCoroutine(ShowEmailLoginUI());
#endif
    }

    /// <summary>
    /// 타이틀 UI 준비 완료를 기다린 뒤 이메일 로그인 패널을 표시한다.
    /// </summary>
    private IEnumerator ShowEmailLoginUI()
    {
        yield return new WaitUntil(() =>
            TitleUIManager.Inst && TitleUIManager.Inst.IsTitleUIReady);

        if (!body_FBLogin.activeSelf)
            body_FBLogin.SetActive(true);
    }

    /// <summary>
    /// 이메일 로그인 버튼에서 호출. 입력 값을 읽어 Firebase 로그인 시도.
    /// </summary>
    public void OnEmailLogin()
    {
        // 클릭 SFX
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

        string email = input_Email.text?.Trim();
        string password = input_Password.text;

        // 1차: 로그인 시도
        FBAuthManager.Instance.Login(email, password);
    }

    /// <summary>
    /// 로그인 성공 후 UI 정리 및 UID 표시.
    /// </summary>
    private void OnLoginSuccess()
    {
        if (body_FBLogin.activeSelf)
            body_FBLogin.SetActive(false);

        // UID 출력
        var uid = FBAuthManager.Instance.USER?.UserId;
        if (text_UID)
            text_UID.text = uid != null ? $"UID {uid}" : "UID (unknown)";

        Debug.LogError("Login flow complete. Enter main scene.");
    }

    /// <summary>
    /// ID 유효성 경고 표시/해제. 표시 시 입력 필드 초기화.
    /// </summary>
    public void ActivateIDAlert(bool command)
    {
        if (command)
        {
            input_Email.text = "";
            input_Password.text = "";
        }

        alert_Email.SetActive(command);
    }

    /// <summary>
    /// PW 유효성 경고 표시/해제. 표시 시 패스워드 초기화.
    /// </summary>
    public void ActivatePWAlert(bool command)
    {
        if (command)
        {
            input_Password.text = "";
        }

        alert_Password.SetActive(command);
    }
}
