using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayer;

    [SerializeField] private string _activateAnimationID = "Base Layer.Activate";
    [SerializeField] private string _deactivateAnimationID = "Base Layer.Deactivate";

    [SerializeField] private AudioSource _sound;
    
    public static Checkpoint CurrentCheckpoint;
    public static Action CheckpointChanged;
    public static string CurrentCheckpointName;

    private bool _active;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        CheckpointChanged += UpdateAnimator;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        CheckpointChanged -= UpdateAnimator;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (CurrentCheckpoint == this) CurrentCheckpoint = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (CurrentCheckpoint == null && CurrentCheckpointName == name)
        {
            SetCheckpoint();
        }
    }

    private void UpdateAnimator()
    {
        var active = CurrentCheckpoint == this;
        if (active && !_active)
        {
            _animator.Play(_activateAnimationID);
            _sound.Play();
        }
        else if (!active && _active)
        {
            _animator.Play(_deactivateAnimationID);
        }

        _active = active;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            SetCheckpoint();
        }
    }

    private void SetCheckpoint()
    {
        CurrentCheckpoint = this;
        CurrentCheckpointName = name;
        CheckpointChanged?.Invoke();
    }
}
