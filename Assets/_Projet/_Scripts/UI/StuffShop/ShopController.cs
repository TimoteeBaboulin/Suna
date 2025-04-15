using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

using UI = UIDocumentUtils;


public class ShopController : MonoBehaviour
{
    private UIDocument document;
    private VisualElement root;
    private VisualElement shop;
    private VisualElement shopmenu;

    [SerializeField] private DefaultInputSystem input;
    bool inputFound = false;

    [SerializeField] private RangedWeaponData skar18;
    [SerializeField] private RangedWeaponData decimator;
    [SerializeField] private RangedWeaponData lp17;
    [SerializeField] private RangedWeaponData fakir;
    [SerializeField] private RangedWeaponData banduka;
    [SerializeField] private RangedWeaponData SMG;
    private Dictionary<Button, RangedWeaponData> weaponDict = new();

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        shop = root.Q<VisualElement>("Shop");
        shopmenu = shop.Q<VisualElement>("ShopMenu");
        UI.SetActive(ref root, false);
    }

    private void Start()
    {
        CreateShopLine(out VisualElement line); shopmenu.Add(line);
        CreateShopButton(out Button button, 30); line.Add(button); weaponDict[button] = lp17;
        CreateShopButton(out button, 30); line.Add(button); weaponDict[button] = banduka;
        CreateShopButton(out button, 30); line.Add(button); weaponDict[button] = skar18;
        CreateShopLine(out line); shopmenu.Add(line);
        CreateShopButton(out button, 30); line.Add(button); weaponDict[button] = fakir;
        CreateShopButton(out button, 30); line.Add(button); 
        CreateShopButton(out button, 30); line.Add(button); weaponDict[button] = decimator;
        CreateShopLine(out line); shopmenu.Add(line);
        CreateShopButton(out button, 30); line.Add(button); weaponDict[button] = SMG;
        CreateShopButton(out button, 30); line.Add(button);
        CreateShopButton(out button, 30); line.Add(button);

        List<Button> buttonList = shopmenu.Query<Button>().ToList();

        foreach (Button btn in buttonList)
        {
            if (weaponDict.ContainsKey(btn))
            {
                Button btnRef = btn;
                AddProductLabelsToShopButton(ref btnRef, weaponDict[btn].entityName, weaponDict[btn].price.ToString() + " $");
                //btn.style.backgroundImage = weaponDict[btn].UIImage;
                AddWeaponIcon(ref btnRef, weaponDict[btn].UIImage);

                btn.clicked += () =>
                {
                    ShopCommand sc = new ShopCommand
                    {
                        weaponData = weaponDict[btn].entityName,
                    };

                    RpcUtils.SendClientToServerRpc(ref sc);
                    UI.SetActive(ref root, false);
                    ActivateUIInput(false);
                };

                // On hover Debug Log entity name
                btn.RegisterCallback<PointerEnterEvent>(evt => OnShopButtonEnter(btn));
                btn.RegisterCallback<PointerLeaveEvent>(evt => OnShopButtonLeave());
            }
        }
    }

    private void Update()
    {
        CharacterInputSystem system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CharacterInputSystem>();

        if (system != null)
        {
            input = system.input;
            inputFound = true;
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            UI.ToggleActive(ref root);
            ActivateUIInput(UI.IsActive(ref root));
        }
    }

    private void CreateShopButton(out Button button, float widthPercent)
    {
        button = new();
        button.AddToClassList("shop_buy_slot_button");
        button.style.position = Position.Relative;
        button.style.width = UI.PercentLength(widthPercent);
        UI.SetMargin(ref button, 10);
        UI.SetPadding(ref button, 10);
    }

    private void CreateShopLine(out VisualElement line)
    {
        line = new();
        line.style.flexDirection = FlexDirection.Row;
        line.style.width = UI.PercentLength(100);
        line.style.height = UI.PercentLength(30);
        line.style.justifyContent = Justify.Center;
        line.style.alignSelf = Align.Center;
    }

    private void AddLabelToShopButton(ref Button button, string text, float fontSize, TextAnchor anchor, Vector2 shift)
    {
        Label label = new(text);
        label.style.color = Color.white;
        label.style.unityTextAlign = anchor;
        UI.SetMargin(ref label);
        UI.SetPadding(ref label);
        label.style.position = Position.Absolute;
        UI.SetPosition(ref label, anchor, shift);
        label.style.fontSize = fontSize;
        button.Add(label);
    }

    private void AddWeaponIcon(ref Button button, Texture2D icon)
    {
        VisualElement iconElement = new();
        iconElement.style.backgroundImage = icon;
        iconElement.style.position = Position.Absolute;
        UI.SetPosition(ref iconElement, TextAnchor.MiddleCenter, 0, 0);
        float ratio = (float)icon.height / icon.width;
        UI.SetSize(ref iconElement, new Length(80, LengthUnit.Percent), new Length(100 * ratio, LengthUnit.Percent));
        button.Add(iconElement);
    }

    private void AddProductLabelsToShopButton(ref Button button, string productName, string productPrice)
    {
        AddLabelToShopButton(ref button, productName, 30, TextAnchor.UpperRight, new(5, 5));
        AddLabelToShopButton(ref button, productPrice, 30, TextAnchor.LowerRight, new(2, 5));
    }

    private void OnShopButtonEnter(Button button)
    {
        VisualElement statsElement = new() { name = "StatsElement" };
        shopmenu.Add(statsElement);
        UI.SetSize(ref statsElement, 300f, shopmenu.resolvedStyle.height * 3 / 4f);
        statsElement.style.position = Position.Absolute;
        UI.SetPosition(ref statsElement, TextAnchor.MiddleRight, -300f, 0f);
        statsElement.style.backgroundColor = new Color(1, 1, 1, .1f);
        UI.SetBorderWidth(ref statsElement, 2);
        UI.SetBorderColor(ref statsElement, new Color(1, 1, 1, .5f));
        RangedWeaponData weapon = weaponDict[button];
        statsElement.Add(UI.Label($"Name: {weapon.entityName}", 20, Color.white));
        statsElement.Add(UI.Label($"Damage: {weapon.damage}", 20, Color.white));
        statsElement.Add(UI.Label($"Firerate: {weapon.firerate} RPM", 20, Color.white));
    }

    private void OnShopButtonLeave()
    {
        root.Q<VisualElement>("StatsElement")?.RemoveFromHierarchy();
    }

    private void ActivateUIInput(bool value)
    {
        if (value)
        {
            input.Player.Disable();
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            input.Player.Enable();
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }
}
