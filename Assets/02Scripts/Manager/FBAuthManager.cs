using System;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Firebase Auth 상태를 래핑하고, 로그인 상태 변화를 브로드캐스트하는 매니저(논-MonoBehaviour).
/// - GPGS 등 외부 Credential을 받아 Firebase 로그인
/// - 이메일/비밀번호 로그인/가입 지원
/// - StateChanged 이벤트를 통해 로그인/로그아웃 전파
/// </summary>
public class FBAuthManager
{
    // --- Singleton ---
    private static FBAuthManager instance = null;
    public static FBAuthManager Instance
    {
        get
        {
            if (instance == null) instance = new FBAuthManager();
            return instance;
        }
    }

    // --- Firebase Handles ---
    private FirebaseAuth auth;
    private FirebaseUser user;

    // --- Public Accessors ---
    public FirebaseAuth AUTH => auth;
    public FirebaseUser USER => user;
    public string UserID => user?.UserId;
    public bool IsLoggedin => USER != null;

    /// <summary>
    /// 로그인 상태 변경 알림(true: signed in, false: signed out).
    /// </summary>
    public Action<bool> LoginState;

    /// <summary>
    /// FirebaseAuth 핸들 설정 및 StateChanged 구독을 초기화한다.
    /// 필요 시 초기 상태를 외부로 브로드캐스트한다.
    /// </summary>
    public void Init(bool doBroadcast = true)
    {
        auth = FirebaseAuth.DefaultInstance;

        // 중복 구독 방지
        auth.StateChanged -= OnChanged;
        auth.StateChanged += OnChanged;

        // 현재 유저 스냅샷
        user = auth.CurrentUser;

        // 초기 상태 브로드캐스트
        if (doBroadcast)
            OnChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Firebase Auth 상태 변경 시 호출되는 핸들러.
    /// 이전/현재 로그인 상태를 비교하여 변화가 있을 때만 브로드캐스트한다.
    /// </summary>
    private void OnChanged(object sender, EventArgs e)
    {
        if (auth.CurrentUser != user)
        {
            bool signedInNow = (auth.CurrentUser != null);
            bool signedInAlready = (user != null);

            Debug.Log($"[FBAuthManager] Auth state changed. wasSignedIn={signedInAlready}, nowSignedIn={signedInNow}");

            user = auth.CurrentUser;

            if (signedInNow && !signedInAlready)
            {
                Debug.Log($"[FBAuthManager] <SIGNED IN> UserId={user?.UserId}, Email={user?.Email}");
                LoginState?.Invoke(true);
            }
            else if (!signedInNow && signedInAlready)
            {
                Debug.Log("[FBAuthManager] <SIGNED OUT>");
                LoginState?.Invoke(false);
            }
        }
        else
        {
            Debug.Log("[FBAuthManager] StateChanged fired but CurrentUser ref unchanged.");
        }
    }

    /// <summary>
    /// 외부 공급자(GPGS 등)에서 받은 Credential로 Firebase 로그인한다.
    /// 성공/실패 콜백을 메인 스레드에서 호출한다.
    /// </summary>
    public void SignInWithCredential(
        Credential credential,
        Action<FirebaseUser> onSuccess = null,
        Action<string> onFail = null)
    {
        Debug.Log("[FBAuthManager] SignInWithCredential() start.");
        auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted || t.IsCanceled)
            {
                onFail?.Invoke(t.Exception?.Message);
                return;
            }

            Debug.Log("[FBAuthManager] SignInWithCredential SUCCESS.");
            onSuccess?.Invoke(t.Result);
        });
    }

    /// <summary>
    /// 이메일/비밀번호로 신규 계정을 생성한다.
    /// </summary>
    public void Create(string ID, string PW)
    {
        auth.CreateUserWithEmailAndPasswordAsync(ID, PW).ContinueWithOnMainThread(t =>
        {
            if (t.IsCanceled)
            {
                Debug.LogError("회원가입 취소");
                return;
            }
            if (t.IsFaulted)
            {
                Debug.LogError("회원가입 실패");
                return;
            }

            FirebaseUser newUser = t.Result.User;
            Debug.LogError("회원가입 완료");
        });
    }

    /// <summary>
    /// 이메일/비밀번호로 로그인 시도 후,
    /// 오류 코드를 1차 분류하고 필요 시 Create 시도로 보조 분류/가입을 수행한다.
    /// </summary>
    public void Login(string ID, string PW)
    {
        auth.SignInWithEmailAndPasswordAsync(ID, PW).ContinueWithOnMainThread(t =>
        {
            if (!t.IsFaulted && !t.IsCanceled)
            {
                FirebaseUser newUser = t.Result.User;
                Debug.LogError("로그인 완료");
                return;
            }

            // 1차: 로그인 에러 코드 분류
            var code = ExtractAuthError(t.Exception);
            Debug.LogWarning($"[FBAuthManager] Login FAILED. code={code}");

            switch (code)
            {
                // ── 케이스 1: 아이디(이메일) 공백 오류
                case AuthError.MissingEmail:
                    Debug.LogError("[FBAuthManager] 분류: MissingEmail (아이디 공백 오류)");
                    AuthManager.Inst.ActivateIDAlert(true);
                    AuthManager.Inst.ActivatePWAlert(false);
                    return;

                // ── 케이스 2: 비밀번호 공백 오류
                case AuthError.MissingPassword:
                    Debug.LogError("[FBAuthManager] 분류: MissingPassword (비밀번호 공백 오류)");
                    AuthManager.Inst.ActivateIDAlert(false);
                    AuthManager.Inst.ActivatePWAlert(true);
                    return;

                // ── 케이스 3: 아이디(이메일) 형식 오류
                case AuthError.InvalidEmail:
                    Debug.LogError("[FBAuthManager] 분류: InvalidEmail (아이디 형식 오류)");
                    AuthManager.Inst.ActivateIDAlert(true);
                    AuthManager.Inst.ActivatePWAlert(false);
                    return;

                default:
                    Debug.LogError($"[FBAuthManager] 분류 실패(기타): {code}");
                    break;
            }

            // 여기부터는 Failure/Internal/None/Network 등 “모호” → Create로 보조판단
            Debug.Log("[FBAuthManager] Create() probe to classify (신규 계정이면 자동 가입).");
            auth.CreateUserWithEmailAndPasswordAsync(ID, PW).ContinueWithOnMainThread(ct =>
            {
                if (!ct.IsFaulted && !ct.IsCanceled)
                {
                    // ── 신규 이메일 → 자동 회원가입 성공
                    var created = ct.Result.User;
                    Debug.LogError("[FBAuthManager] 분류: NewUser (자동 회원가입 완료)");
                    return;
                }

                // Create 실패 코드로 최종 분류
                var ccode = ExtractAuthError(ct.Exception);
                Debug.LogWarning($"[FBAuthManager] Create FAILED for classification. code={ccode}");

                switch (ccode)
                {
                    // ── 케이스 1: 중복 아이디(이미 존재하는 이메일)
                    case AuthError.EmailAlreadyInUse:
                        Debug.LogError("[FBAuthManager] 분류: EmailAlreadyInUse (중복 아이디로 로그인 시도)");
                        AuthManager.Inst.ActivateIDAlert(false);
                        AuthManager.Inst.ActivatePWAlert(true);
                        break;

                    // ── 케이스 4: 비밀번호 형식 오류(약한 비밀번호)
                    case AuthError.WeakPassword:
                        Debug.LogError("[FBAuthManager] 분류: WeakPassword (비밀번호 형식/강도 오류)");
                        AuthManager.Inst.ActivateIDAlert(false);
                        AuthManager.Inst.ActivatePWAlert(true);
                        break;

                    default:
                        Debug.LogError($"[FBAuthManager] 분류 실패(기타): {ccode}");
                        break;
                }
            });
        });
    }

    /// <summary>
    /// Firebase 예외에서 AuthError 코드를 추출한다.
    /// AggregateException까지 재귀적으로 펼쳐 검사한다.
    /// </summary>
    private static AuthError ExtractAuthError(Exception ex)
    {
        if (ex == null) return AuthError.None;

        if (ex is FirebaseException fe) return (AuthError)fe.ErrorCode;

        if (ex is AggregateException agg)
        {
            agg = agg.Flatten();
            foreach (var inner in agg.InnerExceptions)
            {
                if (inner is FirebaseException ife) return (AuthError)ife.ErrorCode;
                var nested = ExtractAuthError(inner);
                if (nested != AuthError.None) return nested;
            }
        }

        var baseEx = ex.GetBaseException();
        if (baseEx is FirebaseException bfe) return (AuthError)bfe.ErrorCode;

        return AuthError.None;
    }

    /// <summary>
    /// Firebase 세션을 로그아웃하고 내부 상태를 초기화한 뒤,
    /// 외부에도 signed-out 상태를 통지한다.
    /// </summary>
    public void Logout()
    {
        try
        {
            auth?.SignOut();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FBAuthManager] SignOut threw: {e}");
        }

        user = null;
        LoginState?.Invoke(false);
    }
}
