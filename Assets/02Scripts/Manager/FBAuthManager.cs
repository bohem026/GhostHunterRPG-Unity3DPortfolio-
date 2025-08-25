using System;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Firebase Auth ���¸� �����ϰ�, �α��� ���� ��ȭ�� ��ε�ĳ��Ʈ�ϴ� �Ŵ���(��-MonoBehaviour).
/// - GPGS �� �ܺ� Credential�� �޾� Firebase �α���
/// - �̸���/��й�ȣ �α���/���� ����
/// - StateChanged �̺�Ʈ�� ���� �α���/�α׾ƿ� ����
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
    /// �α��� ���� ���� �˸�(true: signed in, false: signed out).
    /// </summary>
    public Action<bool> LoginState;

    /// <summary>
    /// FirebaseAuth �ڵ� ���� �� StateChanged ������ �ʱ�ȭ�Ѵ�.
    /// �ʿ� �� �ʱ� ���¸� �ܺη� ��ε�ĳ��Ʈ�Ѵ�.
    /// </summary>
    public void Init(bool doBroadcast = true)
    {
        auth = FirebaseAuth.DefaultInstance;

        // �ߺ� ���� ����
        auth.StateChanged -= OnChanged;
        auth.StateChanged += OnChanged;

        // ���� ���� ������
        user = auth.CurrentUser;

        // �ʱ� ���� ��ε�ĳ��Ʈ
        if (doBroadcast)
            OnChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Firebase Auth ���� ���� �� ȣ��Ǵ� �ڵ鷯.
    /// ����/���� �α��� ���¸� ���Ͽ� ��ȭ�� ���� ���� ��ε�ĳ��Ʈ�Ѵ�.
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
    /// �ܺ� ������(GPGS ��)���� ���� Credential�� Firebase �α����Ѵ�.
    /// ����/���� �ݹ��� ���� �����忡�� ȣ���Ѵ�.
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
    /// �̸���/��й�ȣ�� �ű� ������ �����Ѵ�.
    /// </summary>
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

    /// <summary>
    /// �̸���/��й�ȣ�� �α��� �õ� ��,
    /// ���� �ڵ带 1�� �з��ϰ� �ʿ� �� Create �õ��� ���� �з�/������ �����Ѵ�.
    /// </summary>
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

            // 1��: �α��� ���� �ڵ� �з�
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

    /// <summary>
    /// Firebase ���ܿ��� AuthError �ڵ带 �����Ѵ�.
    /// AggregateException���� ��������� ���� �˻��Ѵ�.
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
    /// Firebase ������ �α׾ƿ��ϰ� ���� ���¸� �ʱ�ȭ�� ��,
    /// �ܺο��� signed-out ���¸� �����Ѵ�.
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
