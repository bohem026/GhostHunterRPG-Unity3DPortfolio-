using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Firestore ���� �Ŵ���(��-MonoBehaviour).
/// - ���� �α����� Firebase ����� �������� users/{UserID} ���� ����
/// - JSON ����ȭ�� �ʵ带 ���� Ű�� ����/�ε�
/// - �ʱ�ȭ ���� ��ε�ĳ��Ʈ(OnInitialized)
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
    /// �ʱ�ȭ ���(true/false)�� �˸��� �ݹ�.
    /// </summary>
    public Action<bool> OnInitialized;

    /// <summary>
    /// Firestore �ڵ�� ���� �α��� ����� �������� �����Ѵ�.
    /// ������ ������ �ʱ�ȭ ���и� ��ε�ĳ��Ʈ�Ѵ�.
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
    /// users/{UserID} �������� ���� �ʵ���� ���ڿ�(JSON)�� �о�´�.
    /// ���� �� null ��ȯ.
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
    /// ��ü�� JSON���� ����ȭ�Ͽ� users/{UserID} ������ ���� �ʵ�� �����Ѵ�.
    /// ���� ������ ���� ����(SetOptions.MergeAll).
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
