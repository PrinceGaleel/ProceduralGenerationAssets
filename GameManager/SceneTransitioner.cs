using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTransitioner : MonoBehaviour
{
    public static SceneTransitioner Instance;

    public RectTransform MainCanvas;
    public TextMeshProUGUI TipText;
    public TextMeshProUGUI ProgressText;
    public Slider Bar;

    private const float LerpAmount = 2.5f;

    private static readonly string[] Tips = new string[2] { "Tea > Coffee", "This is a tip!" };

    private static bool IsLoading;
    private static bool IsManualLoad;

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

            IsLoading = false;
            IsManualLoad = false;

            Animator[] anims = GetComponentsInChildren<Animator>();
            foreach (Animator anim in anims)
            {
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            DontDestroyOnLoad(gameObject);
        }
    }

    public static void LoadScene(string sceneName, bool isManual = false)
    {
        if (!IsLoading)
        {
            ToggleScreen(true);
            CurrentScene = sceneName;
            IsManualLoad = isManual;

            IsLoading = true;
            Time.timeScale = 0;

            Instance.Bar.value = 0;
            Instance.ProgressText.text = "0%";
            
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
    }

    private IEnumerator SetSceneLoadProgress()
    {
        yield return new WaitForEndOfFrame();

        AsyncOperation sceneLoading = SceneManager.LoadSceneAsync(CurrentScene);

        float laggingTotal = 0;
        float actualTotal;
        float numStages = 1;
        float currentStage = 1;

        if(IsManualLoad)
        {
            numStages += 1;
        }

        while (!sceneLoading.isDone)
        {
            actualTotal = sceneLoading.progress / numStages;
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
        Time.timeScale = 1;
        IsLoading = isEnabled;
        Instance.gameObject.SetActive(isEnabled);
    }
}