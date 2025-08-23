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
        "공격 스킬은 E 키로 사용할 수 있어요.",
        "방어 스킬은 Q 키로 사용할 수 있어요.",
        "제한 시간 안에 유령을 목표치만큼 퇴치하세요.",
        "몬스터마다 약한 속성이 존재해요.",
        "상성을 갖는 속성을 준비하는 것이 효과적이에요!",
        "로비에서 스킬을 연구하고 장착할 수 있어요.",
        "로비에서 스탯을 강화할 수 있어요.",
        "전투에서 얻은 장비는 로비에서 장착할 수 있어요."
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
        yield return null; // 첫 프레임 대기

        // 1단계: 먼저 본 씬(Level01Scene) 로드
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

        // 약간 대기 후 활성화
        yield return new WaitForSeconds(1.5f);
        mainSceneOp.allowSceneActivation = true;

        // 대기: 씬이 완전히 활성화될 때까지
        while (!mainSceneOp.isDone)
        {
            yield return null;
        }

        // 2단계: 베이스 씬(LevelBaseScene) 로드 (UI 등 부속 요소)
        if (addSceneName != null)
        {
            AsyncOperation baseSceneOp
                = SceneManager.LoadSceneAsync(addSceneName, LoadSceneMode.Additive);
            while (!baseSceneOp.isDone)
            {
                yield return null;
            }
        }

        // 3단계: 로딩 씬 제거
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
