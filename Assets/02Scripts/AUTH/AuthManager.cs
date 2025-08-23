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
    //[SerializeField] private Text text_FBLog;

    bool isAuthenticated = false;

    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    private void Start()
    {
        // 1) 로그인 상태 구독
        FBAuthManager.Instance.LoginState += OnLoginStateChanged;
        // 2) 초기화
        FBAuthManager.Instance.Init(doBroadcast: false);
        // 3) 부팅 시 세션 정리
        FBAuthManager.Instance.Logout();
        // 4) (선택) GPGS 자동 로그아웃도 같이
//#if UNITY_ANDROID || UNITY_IOS
//        PlayGamesPlatform.Activate();
//        PlayGamesPlatform.Instance.SignOut();
//#endif

        TryGPGSLogin();
    }

    private void OnDestroy()
    {
        FBAuthManager.Instance.LoginState -= OnLoginStateChanged;
    }

    // 로그인 상태 변경 시
    private void OnLoginStateChanged(bool signedIn)
    {
        if (signedIn)
        {
            if (isAuthenticated) return;

            Debug.Log("Auth Success");
            isAuthenticated = true;

            //1. Firestore 초기화
            FBFirestoreManager.Instance.Init();

            //2. 메인 씬 진입 등 후속 동작
            OnLoginSuccess();
        }
        else
        {
            isAuthenticated = false;
        }
    }

    private void TryGPGSLogin()
    {
#if UNITY_ANDROID || UNITY_IOS
        // GPGS 활성화(필요 시)
        PlayGamesPlatform.Activate();

        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                // 서버 인증 코드 → Firebase Credential로 전환
                PlayGamesPlatform.Instance.RequestServerSideAccess(false, authCode =>
                {
                    if (!string.IsNullOrEmpty(authCode))
                    {
                        var credential = PlayGamesAuthProvider.GetCredential(authCode);

                        FBAuthManager.Instance.SignInWithCredential(
                            credential,
                            onFail: err =>
                            {
                                StartCoroutine(ShowEmailLoginUI());
                            }
                        );
                    }
                    else
                    {
                        StartCoroutine(ShowEmailLoginUI());
                    }
                });
            }
            else
            {
                StartCoroutine(ShowEmailLoginUI());
            }
        });
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN
        StartCoroutine(ShowEmailLoginUI());
#endif
    }

    IEnumerator ShowEmailLoginUI()
    {
        yield return new WaitUntil(() =>
        TitleUIManager.Inst && TitleUIManager.Inst.IsTitleUIReady);

        if (!body_FBLogin.activeSelf)
            body_FBLogin.SetActive(true);
    }

    // 이메일 로그인 버튼에서 호출
    public void OnEmailLogin()
    {
        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);

        string email = input_Email.text?.Trim();
        string password = input_Password.text;

        // 1차: 로그인 시도
        FBAuthManager.Instance.Login(email, password);
    }

    private void SetEmailUIInteractable(bool command)
    {
        if (input_Email) input_Email.interactable = command;
        if (input_Password) input_Password.interactable = command;
    }

    private void OnLoginSuccess()
    {
        // 게임 메인 씬 진입 등
        if (body_FBLogin.activeSelf)
            body_FBLogin.SetActive(false);

        // UID 가져오기
        var uid = FBAuthManager.Instance.USER?.UserId;
        if (text_UID)
            text_UID.text = uid != null ? $"UID {uid}" : "UID (unknown)";

        Debug.LogError("Login flow complete. Enter main scene.");
    }

    public void ActivateIDAlert(bool command)
    {
        if (command)
        {
            input_Email.text = "";
            input_Password.text = "";
        }

        alert_Email.SetActive(command);
    }

    public void ActivatePWAlert(bool command)
    {
        if (command)
        {
            input_Password.text = "";
        }

        alert_Password.SetActive(command);
    }
}
