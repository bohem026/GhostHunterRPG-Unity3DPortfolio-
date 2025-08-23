using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FBFirestoreManager
{
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

    private FirebaseFirestore db;
    private FirebaseUser user;

    //public FirebaseFirestore DB => db;
    public FirebaseFirestore DB => db;
    public FirebaseUser USER => user;
    public string UserID => user?.UserId;
    public bool IsInitialized => db != null && user != null;

    public Action<bool> OnInitialized;

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

    public async Task<string> GetJsonField(string fieldName)
    {
        if (!IsInitialized) return null;

        try
        {
            DocumentSnapshot snapshot = await db.Collection("users").Document(UserID).GetSnapshotAsync();
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
