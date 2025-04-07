using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerController _player;
    [SerializeField] private Vector2Int _gridPosition = Vector2Int.zero;
    [SerializeField] private Vector2 _gridSize = new(24, 18);
    [SerializeField] private Vector2 _gridOffset = Vector2.zero;
    [SerializeField] private float _transitionDuration = 0.5f;
    
    bool _transitioning;

    private Vector2Int GetGridPosition(Vector2 worldPosition)
    {
        var adjusted = (worldPosition - _gridOffset) / _gridSize;
        return new (Mathf.FloorToInt(adjusted.x), Mathf.FloorToInt(adjusted.y));
    }

    private Vector2 GetWorldPosition(Vector2Int gridPosition)
    {
        return gridPosition * _gridSize + _gridOffset;
    }

    private void Start()
    {
        _gridPosition = GetGridPosition(_player.transform.position);
        transform.position = GetWorldPosition(_gridPosition) + _gridSize * 0.5f;
    }

    private void OnValidate()
    {
        transform.position = GetWorldPosition(_gridPosition) + _gridSize * 0.5f;
    }

    private void Update()
    {
        if (_transitioning) return;
        
        var playerGridPos = GetGridPosition(_player.transform.position);

        if (playerGridPos != _gridPosition)
        {
            _gridPosition = playerGridPos;
            StartCoroutine(ScreenTransitionCoroutine());
        }
    }

    IEnumerator ScreenTransitionCoroutine()
    {
        _transitioning = true;
        var prevTimescale = Time.timeScale;
        var prevFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = 0;
        Time.fixedDeltaTime = 0f;

        var target = GetWorldPosition(_gridPosition) + _gridSize * 0.5f;
        yield return transform.DOMove(target, _transitionDuration).SetEase(Ease.OutQuad).SetUpdate(true).WaitForCompletion();

        Time.timeScale = prevTimescale;
        Time.fixedDeltaTime = prevFixedDeltaTime;
        _transitioning = false;
    }
}
