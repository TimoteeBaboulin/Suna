using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

[UxmlElement]
public partial class KillFeedElement : SafeVisualElement
{
    private UITextureData texData;

    private VisualElement _background;
    private VisualElement _killerBackground;

    private TeamSideType _killerTeamSide = TeamSideType.Corpo;
    private TeamSideType _victimTeamSide = TeamSideType.Natif;

    [UxmlAttribute]
    public TeamSideType KillerTeamSide
    {
        get => _killerTeamSide;
        set
        {
            _killerTeamSide = value;
            if (_killerBackground != null)
                _killerBackground.style.backgroundImage = texData.Textures.First(t => t.Name == "killfeed_" + _killerTeamSide.ToString().ToLower() + "_killer_background").Texture;
        }
    }

    [UxmlAttribute]
    public TeamSideType VictimTeamSide
    {
        get => _victimTeamSide;
        set
        {
            _victimTeamSide = value;
            if (_background != null)
                _background.style.backgroundImage = texData.Textures.First(t => t.Name == "killfeed_" + _victimTeamSide.ToString().ToLower() + "_background").Texture;
        }
    }

    private float _timer = 0f;
    private float _destroyEndTimer = 4f;
    private float _fadeStart = 3f;

    public float Timer { get => _timer; private set => _timer = value; }

    [UxmlAttribute]
    public float DestroyTimer
    {
        get => _destroyEndTimer;
        set => _destroyEndTimer = value;
    }

    [UxmlAttribute]
    public float FadeStart
    {
        get => _fadeStart;
        set => _fadeStart = value;
    }

    private Label _killerNameLabel;
    private Label _victimNameLabel;
    private string _killerName = "Killer";
    private string _victimName = "Victim";

    [UxmlAttribute]
    public string KillerName
    {
        get => _killerName;
        set
        {
            _killerName = value;
            if (_killerNameLabel != null)
                _killerNameLabel.text = _killerName;
        }
    }
    [UxmlAttribute]
    public string VictimName
    {
        get => _victimName;
        set
        {
            _victimName = value;
            if (_victimNameLabel != null)
                _victimNameLabel.text = _victimName;
        }
    }

    protected override void OnInit()
    {
        texData = Addressables.LoadAssetAsync<UITextureData>("UITextureData").WaitForCompletion();
        if (texData == null)
        {
            Debug.LogError("UITextureData not found.");
            return;
        }

        StyleSheet killFeedStyleSheet = Addressables.LoadAssetAsync<StyleSheet>("style-sheet-killfeed").WaitForCompletion();
        if (killFeedStyleSheet != null)
        {
            styleSheets.Add(killFeedStyleSheet);
        }
        else
        {
            Debug.LogError("KillFeed StyleSheet not found.");
        }

        // Set up the KillFeed element
        style.unityFontDefinition = FontDefinition.FromFont(texData.DefaultFont);
        AddToClassList("killfeed");

        // Set up the backgrounds according to the team side
        _background = new()
        {
            name = "killfeed-background"
        };
        _background.AddToClassList(_background.name);
        Add(_background);

        _killerBackground = new()
        {
            name = "killfeed-killer-background"
        };
        _killerBackground.AddToClassList(_killerBackground.name);
        _background.Add(_killerBackground);

        // Set up the Labels
        _killerNameLabel = new()
        {
            name = "killfeed-label-killer-name"
        };
        _killerNameLabel.AddToClassList(_killerNameLabel.name);
        _killerNameLabel.AddToClassList("killfeed-label");
        _killerBackground.Add(_killerNameLabel);

        _victimNameLabel = new()
        {
            name = "killfeed-label-victim-name"
        };
        _victimNameLabel.AddToClassList(_victimNameLabel.name);
        _victimNameLabel.AddToClassList("killfeed-label");
        _background.Add(_victimNameLabel);

        // Add Update function
        schedule.Execute(Update).Every(16);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (_timer < _destroyEndTimer)
        {
            _timer += Time.deltaTime;

            if (_timer > _fadeStart)
            {
                float timerFromFade = _timer - _fadeStart;
                float timerRemainingBeforeDestroy = _destroyEndTimer - _fadeStart;
                style.opacity = timerFromFade / timerRemainingBeforeDestroy;
            }

            if (_timer >= _destroyEndTimer)
            {
                _timer = _destroyEndTimer;
                RemoveFromHierarchy();
            }
        }
    }
}
