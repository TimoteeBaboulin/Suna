using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

using UI = UIDocumentUtils;


public class ShopController : MonoBehaviour, IUIController
{
    private UIDocument document;
    private VisualElement root;
    private VisualElement shopmenu;

    [SerializeField] private RangedWeaponData skar18;
    [SerializeField] private RangedWeaponData decimator;
    [SerializeField] private RangedWeaponData lp17;
    [SerializeField] private RangedWeaponData fakir;
    [SerializeField] private RangedWeaponData banduka;
    [SerializeField] private GrenadeData heGrenade;
    [SerializeField] private GrenadeData flashbang;
    [SerializeField] private GrenadeData smoke;
    [SerializeField] private GrenadeData gas;
    [SerializeField] private RangedWeaponData SMG;
    [SerializeField] private RangedWeaponData Sniper;
    private Dictionary<Button, RangedWeaponData> weaponDict = new();
    private Dictionary<Button, GrenadeData> grenadeDict = new();

    public UICentralController centralController { get => transform.parent.GetComponent<UICentralController>(); }

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        shopmenu = root.Q<VisualElement>("ShopMenu");
    }

    private void Start()
    {
        weaponDict.Add(shopmenu.Q<Button>("ButtonSkar18"), skar18);
        weaponDict.Add(shopmenu.Q<Button>("ButtonDecimator"), decimator);
        weaponDict.Add(shopmenu.Q<Button>("ButtonLP17"), lp17);
        weaponDict.Add(shopmenu.Q<Button>("ButtonFakir"), fakir);
        weaponDict.Add(shopmenu.Q<Button>("ButtonBanduka"), banduka);
        weaponDict.Add(shopmenu.Q<Button>("ButtonNelara"), SMG);
        weaponDict.Add(shopmenu.Q<Button>("ButtonLaksya"), Sniper);
        grenadeDict.Add(shopmenu.Q<Button>("ButtonHE"), heGrenade);
        grenadeDict.Add(shopmenu.Q<Button>("ButtonFlash"), flashbang);
        grenadeDict.Add(shopmenu.Q<Button>("ButtonFumi"), smoke);
        grenadeDict.Add(shopmenu.Q<Button>("ButtonGas"), gas);

        List<Button> buttonList = shopmenu.Query<Button>().ToList();

        foreach (Button btn in buttonList)
        {
            if (weaponDict.ContainsKey(btn))
            {
                Button btnRef = btn;
                AddProductLabelsToShopButton(ref btnRef, weaponDict[btn].entityName, weaponDict[btn].price.ToString() + " $");
                //AddWeaponIcon(ref btnRef, weaponDict[btn].UIImage);

                btn.clicked += () =>
                {
                    ShopCommand sc = new ShopCommand
                    {
                        weaponData = weaponDict[btn].entityName,
                    };

                    RpcUtils.SendClientToServerRpc(ref sc);
                    centralController.SetUIActive(this, false);
                    centralController.SetCursorActive(false);
                    centralController.SetInputActive(true);
                };

                // On hover Debug Log entity name
                btn.RegisterCallback<PointerEnterEvent>(evt => OnShopButtonEnter(btn));
                btn.RegisterCallback<PointerLeaveEvent>(evt => OnShopButtonLeave());
            }
            else if (grenadeDict.ContainsKey(btn))
            {
                Button btnRef = btn;
                AddProductLabelsToShopButton(ref btnRef, grenadeDict[btn].entityName, grenadeDict[btn].price.ToString() + " $");
                //AddWeaponIcon(ref btnRef, grenadeDict[btn].UIImage);
                btn.clicked += () =>
                {
                    ShopCommand sc = new ShopCommand
                    {
                        weaponData = grenadeDict[btn].entityName,
                    };
                    RpcUtils.SendClientToServerRpc(ref sc);
                    centralController.SetUIActive(this, false);
                    centralController.SetCursorActive(false);
                    centralController.SetInputActive(true);
                };
                // On hover Debug Log entity name
                //btn.RegisterCallback<PointerEnterEvent>(evt => OnShopButtonEnter(btn));
                //btn.RegisterCallback<PointerLeaveEvent>(evt => OnShopButtonLeave());
            }
            else
            {
                //That's when the player is buying an armor

                btn.clicked += () =>
                {
                    ShopCommand sc = new ShopCommand
                    {
                        isArmor = true,
                    };
                    RpcUtils.SendClientToServerRpc(ref sc);
                    centralController.SetUIActive(this, false);
                    centralController.SetCursorActive(false);
                    centralController.SetInputActive(true);
                };
            }
        }
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

    private void AddProductLabelsToShopButton(ref Button button, string productName, string productPrice)
    {
        AddLabelToShopButton(ref button, productName, 30, TextAnchor.LowerLeft, new(5, 5));
        AddLabelToShopButton(ref button, productPrice, 30, TextAnchor.LowerRight, new(2, 5));
    }

    private void OnShopButtonEnter(Button button)
    {
        VisualElement statsElement = new() { name = "StatsElement" };
        shopmenu.Add(statsElement);
        UI.SetSize(ref statsElement, 350f, shopmenu.resolvedStyle.height * 3 / 4f);
        statsElement.style.position = Position.Absolute;
        UI.SetPosition(ref statsElement, TextAnchor.LowerLeft, -360f, 0f);
        statsElement.style.backgroundColor = new Color(.58f, .58f, .58f, .25f);
        RangedWeaponData weapon = weaponDict[button];
        statsElement.Add(UI.Label($"Name: {weapon.entityName}", 30, Color.white));
        statsElement.Add(UI.Label($"Damage: {weapon.damage}", 30, Color.white));
        statsElement.Add(UI.Label($"Firerate: {weapon.firerate} RPM", 30, Color.white));
    }

    private void OnShopButtonLeave()
    {
        root.Q<VisualElement>("StatsElement")?.RemoveFromHierarchy();
    }

    public void SetUIActive(bool value)
    {
        UI.SetActive(ref root, value);
    }

    public bool IsUIActive()
    {
        return UI.IsActive(ref root);
    }

    public UICentralController.UIState GetUIState()
    {
        return UICentralController.UIState.SHOP;
    }
}
