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

        // �ʱ� ���� ��ε�ĳ��Ʈ
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
                Debug.LogError("ȸ������ ���");
                return;
            }
            if (t.IsFaulted)
            {
                Debug.LogError("ȸ������ ����");
                return;
            }

            FirebaseUser newUser = t.Result.User;
            Debug.LogError("ȸ������ �Ϸ�");
        });
    }

    public void Login(string ID, string PW)
    {
        auth.SignInWithEmailAndPasswordAsync(ID, PW).ContinueWithOnMainThread(t =>
        {
            if (!t.IsFaulted && !t.IsCanceled)
            {
                FirebaseUser newUser = t.Result.User;
                Debug.LogError("�α��� �Ϸ�");
                return;
            }

            // 1��: �α��� ���� �ڵ�
            var code = ExtractAuthError(t.Exception);
            Debug.LogWarning($"[FBAuthManager] Login FAILED. code={code}");

            switch (code)
            {
                // ���� ���̽� 1: ���̵�(�̸���) ���� ����
                case AuthError.MissingEmail:
                    Debug.LogError("[FBAuthManager] �з�: MissingEmail (���̵� ���� ����)");
                    AuthManager.Inst.ActivateIDAlert(true);
                    AuthManager.Inst.ActivatePWAlert(false);
                    return;

                // ���� ���̽� 2: ��й�ȣ ���� ����
                case AuthError.MissingPassword:
                    Debug.LogError("[FBAuthManager] �з�: MissingPassword (��й�ȣ ���� ����)");
                    AuthManager.Inst.ActivateIDAlert(false);
                    AuthManager.Inst.ActivatePWAlert(true);
                    return;

                // ���� ���̽� 3: ���̵�(�̸���) ���� ����
                case AuthError.InvalidEmail:
                    Debug.LogError("[FBAuthManager] �з�: InvalidEmail (���̵� ���� ����)");
                    AuthManager.Inst.ActivateIDAlert(true);
                    AuthManager.Inst.ActivatePWAlert(false);
                    return;

                default:
                    Debug.LogError($"[FBAuthManager] �з� ����(��Ÿ): {code}");
                    break;
            }

            //// ���� ���̽� 2: ��ϵ� ���� ��й�ȣ ����
            //if (code == AuthError.WrongPassword)
            //{
            //    Debug.LogError("[FBAuthManager] �з�: WrongPassword (��ϵ� ���� ��й�ȣ ����)");
            //    return;
            //}

            //// ���� ���̽� 3: ���̵�(�̸���) ���� ����
            //if (code == AuthError.InvalidEmail)
            //{
            //    Debug.LogError("[FBAuthManager] �з�: InvalidEmail (���̵� ���� ����)");
            //    return;
            //}

            // ������ʹ� Failure/Internal/None/Network �� ����ȣ�� �� Create�� �����Ǵ�
            Debug.Log("[FBAuthManager] Create() probe to classify (�ű� �����̸� �ڵ� ����).");
            auth.CreateUserWithEmailAndPasswordAsync(ID, PW).ContinueWithOnMainThread(ct =>
            {
                if (!ct.IsFaulted && !ct.IsCanceled)
                {
                    // ���� �ű� �̸��� �� �ڵ� ȸ������ ����
                    var created = ct.Result.User;
                    Debug.LogError("[FBAuthManager] �з�: NewUser (�ڵ� ȸ������ �Ϸ�)");
                    return;
                }

                // Create ���� �ڵ�� ���� �з�
                var ccode = ExtractAuthError(ct.Exception);
                Debug.LogWarning($"[FBAuthManager] Create FAILED for classification. code={ccode}");

                switch (ccode)
                {
                    // ���� ���̽� 1: �ߺ� ���̵�(�̹� �����ϴ� �̸���)
                    case AuthError.EmailAlreadyInUse:
                        Debug.LogError("[FBAuthManager] �з�: EmailAlreadyInUse (�ߺ� ���̵�� �α��� �õ�)");
                        AuthManager.Inst.ActivateIDAlert(false);
                        AuthManager.Inst.ActivatePWAlert(true);
                        break;

                    // ���� ���̽� 4: ��й�ȣ ���� ����(���� ��й�ȣ)
                    case AuthError.WeakPassword:
                        Debug.LogError("[FBAuthManager] �з�: WeakPassword (��й�ȣ ����/���� ����)");
                        AuthManager.Inst.ActivateIDAlert(false);
                        AuthManager.Inst.ActivatePWAlert(true);
                        break;

                    default:
                        Debug.LogError($"[FBAuthManager] �з� ����(��Ÿ): {ccode}");
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
                Debug.LogError("�α��� �Ϸ�");
                return;
            }

            // ���� �� ���� �ڵ� Ȯ��
            var code = ExtractAuthError(t.Exception);
            Debug.LogWarning($"[FBAuthManager] Login FAILED. code={code}");

            if (code == AuthError.UserNotFound)
            {
                // ��¥ �̵���̸� �׶��� �ڵ� ����
                Debug.Log("[FBAuthManager] UserNotFound �� Create()");
                Create(ID, PW);
                return;
            }

            // --- �����Ǵ�: Failure/Internal �� �ָ��� �ڵ��� �� �̸��� ������ ��ȸ ---
            Debug.Log("[FBAuthManager] FetchProvidersForEmailAsync()");
            auth.FetchProvidersForEmailAsync(ID).ContinueWithOnMainThread(m =>
            {
                if (m.IsFaulted || m.IsCanceled)
                {
                    Debug.LogError($"[FBAuthManager] FetchProviders FAILED. ex={m.Exception}");
                    Debug.LogError("�α��� ����");
                    return;
                }

                IList<string> providers = m.Result.ToList(); // IList<string>
                var hasAny = providers != null && providers.Count > 0;
                Debug.Log($"[FBAuthManager] providers: {(hasAny ? string.Join(",", providers) : "(none)")}");

                if (!hasAny)
                {
                    // ������ ��ü�� ���� = ��ǻ� �̰��� �� ����
                    Debug.Log("[FBAuthManager] No providers �� Create()");
                    Create(ID, PW);
                }
                else
                {
                    // using System.Linq; ������ Contains �״�� ��� ����
                    bool hasPassword = providers.Contains("password");
                    if (hasPassword)
                        Debug.LogError("�α��� ����(���� �̸��Ϸ� ��ϵǾ� ������ ��й�ȣ ���� ����).");
                    else
                        Debug.LogError($"�α��� ����(�ٸ� �����ڷ� ��ϵ�: {string.Join(",", providers)})");
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

        // ���� ���� �ʱ�ȭ & �ܺο��� signed-out ����
        user = null;
        LoginState?.Invoke(false);
    }
}
