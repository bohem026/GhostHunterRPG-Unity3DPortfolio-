using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingSceneManager : MonoBehaviour
{
    private const string NAME_LOADING_SCENE = "LoadingScene";
    public static string mainSceneName;
    public static string addSceneName;

    private static string[] tips =
    {
        "���� ��ų�� E Ű�� ����� �� �־��.",
        "��� ��ų�� Q Ű�� ����� �� �־��.",
        "���� �ð� �ȿ� ������ ��ǥġ��ŭ ��ġ�ϼ���.",
        "���͸��� ���� �Ӽ��� �����ؿ�.",
        "���� ���� �Ӽ��� �غ��ϴ� ���� ȿ�����̿���!",
        "�κ񿡼� ��ų�� �����ϰ� ������ �� �־��.",
        "�κ񿡼� ������ ��ȭ�� �� �־��.",
        "�������� ���� ���� �κ񿡼� ������ �� �־��."
    };

    [Header("BODY")]
    public Text text_Tip;
    [Header("FOOT")]
    public Slider slider_Progress;
    public Text text_Progress;

    void Start()
    {
        StartCoroutine(LoadSceneCoroutine());
        InitTipText();
    }

    private void InitTipText()
    {
        text_Tip.text = tips[Random.Range(0, tips.Length)];
    }

    IEnumerator LoadSceneCoroutine()
    {
        yield return null; // ù ������ ���

        // 1�ܰ�: ���� �� ��(Level01Scene) �ε�
        AsyncOperation mainSceneOp
            = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
        mainSceneOp.allowSceneActivation = false;

        while (mainSceneOp.progress < 0.75f)
        {
            slider_Progress.value = mainSceneOp.progress;
            text_Progress.text = (mainSceneOp.progress * 100).ToString("F2") + "%";
            yield return null;
        }

        slider_Progress.value = 1f;
        text_Progress.text = "100.00%";

        // �ణ ��� �� Ȱ��ȭ
        yield return new WaitForSeconds(1.5f);
        mainSceneOp.allowSceneActivation = true;

        // ���: ���� ������ Ȱ��ȭ�� ������
        while (!mainSceneOp.isDone)
        {
            yield return null;
        }

        // 2�ܰ�: ���̽� ��(LevelBaseScene) �ε� (UI �� �μ� ���)
        if (addSceneName != null)
        {
            AsyncOperation baseSceneOp
                = SceneManager.LoadSceneAsync(addSceneName, LoadSceneMode.Additive);
            while (!baseSceneOp.isDone)
            {
                yield return null;
            }
        }

        // 3�ܰ�: �ε� �� ����
        SceneManager.UnloadSceneAsync(NAME_LOADING_SCENE);
    }

    public static void LoadSceneWithLoading
        (
        string mainScene,
        string addScene = null
        )
    {
        mainSceneName = mainScene;
        addSceneName = addScene;
        SceneManager.LoadScene(NAME_LOADING_SCENE);
    }
}
