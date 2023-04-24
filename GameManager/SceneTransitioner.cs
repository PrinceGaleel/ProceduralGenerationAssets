using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using TMPro;

public class SceneTransitioner : MonoBehaviour
{
    public static SceneTransitioner Instance;

    public RectTransform MainCanvas;
    public TextMeshProUGUI TipText;
    public TextMeshProUGUI ProgressText;
    public Slider Bar;

    private const float LerpAmount = 2.5f;

    private static readonly string[] Tips = new string[2] { "Tea > Coffee", "This is a tip!" };

    private static bool IsPreLoad;
    private static bool IsManualLoad;
    public static void AdvancePreLoad()
    {
        if (IsPreLoad)
        {
            IsPreLoad = false;
            LoadScene();
        }
    }

    private static AsyncOperation LoadProgress;
    private static string CurrentScene;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("Error: Multiple Scene transitioners detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;

            Animator[] anims = GetComponentsInChildren<Animator>();
            foreach (Animator anim in anims)
            {
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        GlobalSettings.LoadSettings();
        CurrentScene = "MainMenu";
        IsManualLoad = false;
        IsPreLoad = false;
        LoadScene();
    }

    public static void LoadScene(string sceneName, bool isPreLoad, bool isManual)
    {
        if (LoadProgress.isDone)
        {
            CurrentScene = sceneName;
            IsManualLoad = isManual;
            IsPreLoad = isPreLoad;
            if (!IsPreLoad) LoadScene();
            else { ToggleScreen(true); }
        }
    }

    private static void LoadScene()
    {
        ToggleScreen(true);

        if (Tips.Length > 0)
        {
            Instance.TipText.text = Tips[Random.Range(0, Tips.Length)];
        }
        else
        {
            Instance.TipText.text = "";
        }

        Instance.StartCoroutine(Instance.SetSceneLoadProgress());
    }

    private IEnumerator SetSceneLoadProgress()
    {
        yield return new WaitForEndOfFrame();

        LoadProgress = SceneManager.LoadSceneAsync(CurrentScene);

        float laggingTotal = 0;
        float actualTotal;
        float numStages = 1;
        float currentStage = 1;

        if(IsManualLoad)
        {
            numStages += 1;
        }

        while (!LoadProgress.isDone)
        {
            actualTotal = LoadProgress.progress / numStages;
            laggingTotal = Mathf.Clamp(laggingTotal + (Time.unscaledDeltaTime * LerpAmount), 0, actualTotal);
            ProgressText.text = Mathf.Ceil(laggingTotal * 100) + "%";
            Bar.value = laggingTotal;

            yield return new WaitForEndOfFrame();
        }

        actualTotal = (currentStage / numStages);

        while (laggingTotal != actualTotal)
        {
            laggingTotal = Mathf.Clamp(laggingTotal + (Time.unscaledDeltaTime * LerpAmount), 0, actualTotal);
            ProgressText.text = Mathf.Ceil(laggingTotal * 100) + "%";
            Bar.value = laggingTotal;
            yield return null;
        }

        if (!IsManualLoad)
        {
            ToggleScreen(false);
        }
    }

    public static void ToggleScreen(bool isEnabled)
    {
        if (isEnabled) 
        {
            Time.timeScale = 0;

            Instance.Bar.value = 0;
            Instance.ProgressText.text = "0%";
        }
        else Time.timeScale = 1;
        Instance.gameObject.SetActive(isEnabled);
    }
}