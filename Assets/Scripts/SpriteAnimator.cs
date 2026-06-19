using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimator : MonoBehaviour {
    [Header("Idle Frames")]
    public Sprite idleFrame1;
    public Sprite idleFrame2;

    [Header("Thinking Frames")]
    public Sprite thinkingFrame1;
    public Sprite thinkingFrame2;

    [Header("Extra UI Element")]
    public GameObject thoughtBubbleObject; // <-- Drag your ThoughtBubble GameObject here

    [Header("Settings")]
    public float frameRate = 0.35f; 

    private Image _uiImage;
    private bool _isThinking = false;
    private float _timer;
    private int _currentFrame = 0;

    void Awake() {
        _uiImage = GetComponent<Image>();
        if (_uiImage == null) {
            Debug.LogError($"SpriteAnimator on {gameObject.name} requires a UI Image component!", gameObject);
        }
    }

    void Start() {
        if (_uiImage != null && idleFrame1 != null) {
            _uiImage.sprite = idleFrame1;
        }
        
        // Ensure the bubble starts hidden on launch
        if (thoughtBubbleObject != null) {
            thoughtBubbleObject.SetActive(false);
        }
    }

    void Update() {
        if (_uiImage == null) return;

        _timer += Time.deltaTime;
        if (_timer >= frameRate) {
            _timer = 0f;
            _currentFrame = (_currentFrame == 0) ? 1 : 0;
            UpdateAnimationSprite();
        }
    }

    private void UpdateAnimationSprite() {
        if (_isThinking) {
            if (thinkingFrame1 != null && thinkingFrame2 != null) {
                _uiImage.sprite = (_currentFrame == 0) ? thinkingFrame1 : thinkingFrame2;
            }
        } else {
            if (idleFrame1 != null && idleFrame2 != null) {
                _uiImage.sprite = (_currentFrame == 0) ? idleFrame1 : idleFrame2;
            }
        }
    }

    public void StartThinkingAnimation() {
        _isThinking = true;
        _timer = 0f;
        _currentFrame = 0;
        UpdateAnimationSprite();

        // Show the thought bubble when the AI begins thinking
        if (thoughtBubbleObject != null) {
            thoughtBubbleObject.SetActive(true);
        }
    }

    public void StopThinkingAnimation() {
        _isThinking = false;
        _timer = 0f;
        _currentFrame = 0;
        UpdateAnimationSprite();

        // Hide the thought bubble when the AI finishes its sentence
        if (thoughtBubbleObject != null) {
            thoughtBubbleObject.SetActive(false);
        }
    }
}

