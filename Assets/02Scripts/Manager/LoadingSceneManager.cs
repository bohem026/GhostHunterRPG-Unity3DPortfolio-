using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingSceneManager : MonoBehaviour
{
    // --- Constants / Static state ---
    private const string NAME_LOADING_SCENE = "LoadingScene";
    public static string mainSceneName;
    public static string addSceneName;

    // �ε� �� �޽��� ���
    private static readonly string[] tips =
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

    // --- UI References ---
    [Header("BODY")]
    public Text text_Tip;

    [Header("FOOT")]
    public Slider slider_Progress;
    public Text text_Progress;

    /// <summary>
    /// �ε� �ڷ�ƾ ���� �� �� �ؽ�Ʈ �ʱ�ȭ.
    /// </summary>
    private void Start()
    {
        StartCoroutine(LoadSceneCoroutine());
        InitTipText();
    }

    /// <summary>
    /// �� �ؽ�Ʈ�� �������� ������ ǥ���Ѵ�.
    /// </summary>
    private void InitTipText()
    {
        text_Tip.text = tips[Random.Range(0, tips.Length)];
    }

    /// <summary>
    /// ���� ���� Additive�� �ε��Ͽ� Ȱ��ȭ�ϰ�(���൵ ǥ��),
    /// �ʿ� �� �߰� ���� �ε��� �� �ε� ���� ��ε��Ѵ�.
    /// </summary>
    private IEnumerator LoadSceneCoroutine()
    {
        yield return null; // ù ������ ���(������ �ε� ������ ����)

        // 1�ܰ�: ���� �� �ε�(��: Level01Scene)
        AsyncOperation mainSceneOp =
            SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
        mainSceneOp.allowSceneActivation = false;

        // ���൵ ǥ��(���� Ư���� 0.9 �������� progress�� 0~0.9 ����)
        while (mainSceneOp.progress < 0.75f)
        {
            slider_Progress.value = mainSceneOp.progress;
            text_Progress.text = (mainSceneOp.progress * 100).ToString("F2") + "%";
            yield return null;
        }

        // ����� ���൵ 100% ���� ǥ��
        slider_Progress.value = 1f;
        text_Progress.text = "100.00%";

        // �ణ ��� �� Ȱ��ȭ
        yield return new WaitForSeconds(1.5f);
        mainSceneOp.allowSceneActivation = true;

        // ���� ���� ������ Ȱ��ȭ�� ������ ���
        while (!mainSceneOp.isDone)
        {
            yield return null;
        }

        // 2�ܰ�: �μ�(additive) �� �ε�(UI ��)
        if (addSceneName != null)
        {
            AsyncOperation baseSceneOp =
                SceneManager.LoadSceneAsync(addSceneName, LoadSceneMode.Additive);
            while (!baseSceneOp.isDone)
            {
                yield return null;
            }
        }

        // 3�ܰ�: �ε� �� ��ε�
        SceneManager.UnloadSceneAsync(NAME_LOADING_SCENE);
    }

    /// <summary>
    /// �ε� ���� ���� ������ ��(�� ������ �߰� ��)�� �ε��Ѵ�.
    /// </summary>
    public static void LoadSceneWithLoading(string mainScene, string addScene = null)
    {
        mainSceneName = mainScene;
        addSceneName = addScene;
        SceneManager.LoadScene(NAME_LOADING_SCENE);
    }
}
