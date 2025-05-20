using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class LoadManager : Singleton<LoadManager>
{
    #region Fields
    [Header("Load Reference")]
    [SerializeField] private Image loadingImage;

    [Header("Load Settings")]
    [SerializeField] private AnimationCurve fadeCurve;

    private Coroutine currentCoroutine;
    #endregion


    #region PublicMethods
    public void LoadLevel(string sceneName, Action onLoaded = null)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(LoadSceneWithFade(sceneName, onLoaded));
    }
    #endregion

    #region PrivateMethods
    private IEnumerator LoadSceneWithFade(string sceneName, Action onLoaded)
    {
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;

        // Fade In 
        yield return StartCoroutine(Fade(loadingImage, 1.0f, 1.0f));

        yield return new WaitWhile(() => async.progress < 0.9f);
        async.allowSceneActivation = true;

        // Fade out
        yield return StartCoroutine(Fade(loadingImage, 0.0f, 1.0f));

        onLoaded?.Invoke();
    }

    private IEnumerator Fade(Image fadeImage, float targetAlpha, float duration)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            float startAlpha = color.a;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float curveProgress = fadeCurve.Evaluate(elapsedTime / duration);
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, curveProgress);
                SetAlpha(ref fadeImage, alpha);

                yield return null;
            }

            SetAlpha(ref fadeImage, targetAlpha);
        }
    }

    private void SetAlpha(ref Image loadingImage, float alpha)
    {
        Color color = loadingImage.color;
        color.a = alpha;
        loadingImage.color = color;
    }
    #endregion
}
