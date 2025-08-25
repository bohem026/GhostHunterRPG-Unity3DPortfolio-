using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Firestore 연동 매니저(논-MonoBehaviour).
/// - 현재 로그인한 Firebase 사용자 기준으로 users/{UserID} 문서 접근
/// - JSON 직렬화된 필드를 단일 키로 저장/로드
/// - 초기화 상태 브로드캐스트(OnInitialized)
/// </summary>
public class FBFirestoreManager
{
    // --- Singleton ---
    private static FBFirestoreManager instance = null;
    public static FBFirestoreManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new FBFirestoreManager();
            }
            return instance;
        }
    }

    // --- Firebase Handles ---
    private FirebaseFirestore db;
    private FirebaseUser user;

    // --- Public Accessors ---
    public FirebaseFirestore DB => db;
    public FirebaseUser USER => user;
    public string UserID => user?.UserId;
    public bool IsInitialized => db != null && user != null;

    /// <summary>
    /// 초기화 결과(true/false)를 알리는 콜백.
    /// </summary>
    public Action<bool> OnInitialized;

    /// <summary>
    /// Firestore 핸들과 현재 로그인 사용자 스냅샷을 설정한다.
    /// 유저가 없으면 초기화 실패를 브로드캐스트한다.
    /// </summary>
    public void Init()
    {
        db = FirebaseFirestore.DefaultInstance;
        user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogWarning("FirestoreManager: No users logged in.");
            OnInitialized?.Invoke(false);
            return;
        }

        Debug.Log("FirestoreManager initialized.");
        OnInitialized?.Invoke(true);
    }

    /// <summary>
    /// users/{UserID} 문서에서 지정 필드명을 문자열(JSON)로 읽어온다.
    /// 실패 시 null 반환.
    /// </summary>
    public async Task<string> GetJsonField(string fieldName)
    {
        if (!IsInitialized) return null;

        try
        {
            DocumentSnapshot snapshot = await db
                .Collection("users").Document(UserID)
                .GetSnapshotAsync();

            if (snapshot.Exists && snapshot.TryGetValue(fieldName, out string json))
            {
                return json;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"FirestoreManager: Data load failed. - {fieldName}, {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 객체를 JSON으로 직렬화하여 users/{UserID} 문서의 지정 필드명에 저장한다.
    /// 기존 문서와 병합 저장(SetOptions.MergeAll).
    /// </summary>
    public async Task SaveJsonField(string fieldName, object data)
    {
        if (!IsInitialized) return;

        try
        {
            string json = JsonUtility.ToJson(data, true);
            var dict = new Dictionary<string, object> { { fieldName, json } };

            await db.Collection("users").Document(UserID)
                .SetAsync(dict, SetOptions.MergeAll);

            Debug.Log($"FirestoreManager: [{fieldName}] saved.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"FirestoreManager: [{fieldName}] save failed. - {ex.Message}");
        }
    }
}
