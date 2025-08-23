using System;
using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class FBAuthManager
{
    private static FBAuthManager instance = null;

    public static FBAuthManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new FBAuthManager();
            }
            return instance;
        }
    }

    private FirebaseAuth auth;
    private FirebaseUser user;

    public FirebaseAuth AUTH => auth;
    public FirebaseUser USER => user;
    public string UserID => user?.UserId;
    public bool IsLoggedin => USER != null;
    public Action<bool> LoginState; // true: signed in, false: signed out

    public void Init(bool doBroadcast = true)
    {
        auth = FirebaseAuth.DefaultInstance;

        // Remove previous sign in event.
        auth.StateChanged -= OnChanged;

        // Called if authentication state changed.
        auth.StateChanged += OnChanged;

        user = auth.CurrentUser;

        // 초기 상태 브로드캐스트
        if (doBroadcast)
            OnChanged(this, EventArgs.Empty);
    }

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

            // 1차: 로그인 에러 코드
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

            //// ── 케이스 2: 등록된 계정 비밀번호 오류
            //if (code == AuthError.WrongPassword)
            //{
            //    Debug.LogError("[FBAuthManager] 분류: WrongPassword (등록된 계정 비밀번호 오류)");
            //    return;
            //}

            //// ── 케이스 3: 아이디(이메일) 형식 오류
            //if (code == AuthError.InvalidEmail)
            //{
            //    Debug.LogError("[FBAuthManager] 분류: InvalidEmail (아이디 형식 오류)");
            //    return;
            //}

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


    /*
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

            // 실패 → 에러 코드 확인
            var code = ExtractAuthError(t.Exception);
            Debug.LogWarning($"[FBAuthManager] Login FAILED. code={code}");

            if (code == AuthError.UserNotFound)
            {
                // 진짜 미등록이면 그때만 자동 가입
                Debug.Log("[FBAuthManager] UserNotFound → Create()");
                Create(ID, PW);
                return;
            }

            // --- 보조판단: Failure/Internal 등 애매한 코드일 때 이메일 공급자 조회 ---
            Debug.Log("[FBAuthManager] FetchProvidersForEmailAsync()");
            auth.FetchProvidersForEmailAsync(ID).ContinueWithOnMainThread(m =>
            {
                if (m.IsFaulted || m.IsCanceled)
                {
                    Debug.LogError($"[FBAuthManager] FetchProviders FAILED. ex={m.Exception}");
                    Debug.LogError("로그인 실패");
                    return;
                }

                IList<string> providers = m.Result.ToList(); // IList<string>
                var hasAny = providers != null && providers.Count > 0;
                Debug.Log($"[FBAuthManager] providers: {(hasAny ? string.Join(",", providers) : "(none)")}");

                if (!hasAny)
                {
                    // 공급자 자체가 없음 = 사실상 미가입 → 생성
                    Debug.Log("[FBAuthManager] No providers → Create()");
                    Create(ID, PW);
                }
                else
                {
                    // using System.Linq; 있으면 Contains 그대로 사용 가능
                    bool hasPassword = providers.Contains("password");
                    if (hasPassword)
                        Debug.LogError("로그인 실패(기존 이메일로 등록되어 있으나 비밀번호 오류 가능).");
                    else
                        Debug.LogError($"로그인 실패(다른 공급자로 등록됨: {string.Join(",", providers)})");
                }
            });
        });
    }
    */

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

        // 내부 상태 초기화 & 외부에도 signed-out 통지
        user = null;
        LoginState?.Invoke(false);
    }
}
