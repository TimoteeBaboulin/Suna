using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

[UxmlElement]
public partial class DamageIndicatorElement : SafeVisualElement
{
    private UITextureData texData;

    private VisualElement _image;

    private float _timer = 0f;
    private float _destroyEndTimer = 1.5f;

    public float Timer { get => _timer; private set => _timer = value; }

    [UxmlAttribute]
    public float DestroyTimer
    {
        get => _destroyEndTimer;
        set => _destroyEndTimer = value;
    }

    protected override void OnInit()
    {
        texData = Addressables.LoadAssetAsync<UITextureData>("UITextureData").WaitForCompletion();
        if (texData == null)
        {
            Debug.LogError("UITextureData not found.");
            return;
        }

        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;

        // Set up the backgrounds according to the team side
        _image = new()
        {
            name = "damage-indicator-image"
        };
        _image.AddToClassList(_image.name);
        _image.style.backgroundImage = new StyleBackground(texData.GetTexture("hud_hit_marker"));
        _image.style.width = 380;
        _image.style.height = 380;
        Add(_image);

        // Add Update function
        schedule.Execute(Update).Every(16);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (_timer < _destroyEndTimer)
        {
            _timer += Time.deltaTime;

            float t = 1 - _timer / _destroyEndTimer;

            style.opacity = t < 0 ? 0 : t;

            if (_timer >= _destroyEndTimer)
            {
                _timer = _destroyEndTimer;
                RemoveFromHierarchy();
            }
        }
    }
}
