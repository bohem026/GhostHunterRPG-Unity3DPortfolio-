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
    /// �̱��� ���۷����� �����Ѵ�.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// �α��� ���� �̺�Ʈ�� �����ϰ�, Firebase Auth �ʱ�ȭ �� ���� ���� ��
    /// GPGS �α��� �õ��� �����Ѵ�.
    /// </summary>
    private void Start()
    {
        // 1) �α��� ���� ����
        FBAuthManager.Instance.LoginState += OnLoginStateChanged;

        // 2) Firebase Auth �ʱ�ȭ(��ε�ĳ��Ʈ�� Start���� ���� ����)
        FBAuthManager.Instance.Init(doBroadcast: false);

        // 3) ���� �� ���� ���� ����
        FBAuthManager.Instance.Logout();

        // 4) GPGS �α��� �õ� (�����), �� �� �÷����� �̸��� UI�� ����
        TryGPGSLogin();
    }

    /// <summary>
    /// ���� ����.
    /// </summary>
    private void OnDestroy()
    {
        FBAuthManager.Instance.LoginState -= OnLoginStateChanged;
    }

    /// <summary>
    /// �α��� ���� ��ȭ �ݹ�. ���� �� Firestore �ʱ�ȭ �� �ļ� ó��.
    /// </summary>
    private void OnLoginStateChanged(bool signedIn)
    {
        if (signedIn)
        {
            if (isAuthenticated) return; // �ߺ� ó�� ����
            isAuthenticated = true;

            Debug.Log("Auth Success");

            // 1) Firestore �ʱ�ȭ
            FBFirestoreManager.Instance.Init();

            // 2) ���� �� ���� �� �ļ� ����
            OnLoginSuccess();
        }
        else
        {
            isAuthenticated = false;
        }
    }

    /// <summary>
    /// �����: GPGS �α��� �� ���� ���� �ڵ� ȹ�� �� Firebase Credential ����.
    /// ������/PC: �̸��� �α��� UI ����.
    /// </summary>
    private void TryGPGSLogin()
    {
#if UNITY_ANDROID || UNITY_IOS
        PlayGamesPlatform.Activate();

        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                // ���� ���� �ڵ� �� Firebase Credential ��ȯ
                PlayGamesPlatform.Instance.RequestServerSideAccess(false, authCode =>
                {
                    if (!string.IsNullOrEmpty(authCode))
                    {
                        var credential = PlayGamesAuthProvider.GetCredential(authCode);

                        FBAuthManager.Instance.SignInWithCredential(
                            credential,
                            onFail: err =>
                            {
                                // GPGS��Firebase ���� �� �̸��� �α��� UI�� ����
                                StartCoroutine(ShowEmailLoginUI());
                            }
                        );
                    }
                    else
                    {
                        // ���� �ڵ� ȹ�� ���� �� �̸��� �α��� UI
                        StartCoroutine(ShowEmailLoginUI());
                    }
                });
            }
            else
            {
                // GPGS �α��� ���� �� �̸��� �α��� UI
                StartCoroutine(ShowEmailLoginUI());
            }
        });
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN
        // ������/PC: �ٷ� �̸��� �α��� UI ����
        StartCoroutine(ShowEmailLoginUI());
#endif
    }

    /// <summary>
    /// Ÿ��Ʋ UI �غ� �ϷḦ ��ٸ� �� �̸��� �α��� �г��� ǥ���Ѵ�.
    /// </summary>
    private IEnumerator ShowEmailLoginUI()
    {
        yield return new WaitUntil(() =>
            TitleUIManager.Inst && TitleUIManager.Inst.IsTitleUIReady);

        if (!body_FBLogin.activeSelf)
            body_FBLogin.SetActive(true);
    }

    /// <summary>
    /// �̸��� �α��� ��ư���� ȣ��. �Է� ���� �о� Firebase �α��� �õ�.
    /// </summary>
    public void OnEmailLogin()
    {
        // Ŭ�� SFX
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

        string email = input_Email.text?.Trim();
        string password = input_Password.text;

        // 1��: �α��� �õ�
        FBAuthManager.Instance.Login(email, password);
    }

    /// <summary>
    /// �α��� ���� �� UI ���� �� UID ǥ��.
    /// </summary>
    private void OnLoginSuccess()
    {
        if (body_FBLogin.activeSelf)
            body_FBLogin.SetActive(false);

        // UID ���
        var uid = FBAuthManager.Instance.USER?.UserId;
        if (text_UID)
            text_UID.text = uid != null ? $"UID {uid}" : "UID (unknown)";

        Debug.LogError("Login flow complete. Enter main scene.");
    }

    /// <summary>
    /// ID ��ȿ�� ��� ǥ��/����. ǥ�� �� �Է� �ʵ� �ʱ�ȭ.
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
    /// PW ��ȿ�� ��� ǥ��/����. ǥ�� �� �н����� �ʱ�ȭ.
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
