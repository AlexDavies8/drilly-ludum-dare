using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader i;

    [SerializeField] private RectTransform _screenSweep;
    [SerializeField] private float _sweepDuration = 0.3f;

    [SerializeField] private Vector2 _sweepEnterPos = Vector2.zero;
    [SerializeField] private Vector2 _coveredPos = Vector2.zero;
    [SerializeField] private Vector2 _sweepLeavePos = Vector2.zero;

    private bool _loading = false;
    
    private void Awake()
    {
        if (i) Destroy(gameObject);
        else
        {
            i = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (i == this) i = null;
    }

    public void ReloadScene()
    {
        StartCoroutine(ReloadSceneCoroutine());
    }

    IEnumerator ReloadSceneCoroutine()
    {
        if (_loading) yield break;

        _loading = true;
        
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        
        _screenSweep.anchoredPosition = _sweepEnterPos;
        yield return _screenSweep.DOAnchorPos(_coveredPos, _sweepDuration).SetEase(Ease.InQuad).SetUpdate(true).WaitForCompletion();
        
        var asyncSceneLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
        yield return new WaitUntil(() => asyncSceneLoad?.isDone ?? true);
        
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        yield return _screenSweep.DOAnchorPos(_sweepLeavePos, _sweepDuration).SetEase(Ease.OutQuad).SetUpdate(true).WaitForCompletion();
        
        _loading = false;
    }
}