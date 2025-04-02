using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using UI = UIDocumentUtils;


public class ShopController : MonoBehaviour
{
    private UIDocument document;
    private VisualElement root;
    private VisualElement shop;
    private VisualElement shopmenu;

    [SerializeField] private RangedWeaponData m1a1;
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
        CreateShopButton(out Button button, 30); line.Add(button);
        CreateShopButton(out button, 30); line.Add(button); weaponDict[button] = m1a1;
        CreateShopButton(out button, 30); line.Add(button);
        CreateShopLine(out line); shopmenu.Add(line);
        CreateShopButton(out button, 30); line.Add(button);
        CreateShopButton(out button, 30); line.Add(button);
        CreateShopButton(out button, 30); line.Add(button);
        CreateShopLine(out line); shopmenu.Add(line);
        CreateShopButton(out button, 30); line.Add(button);
        CreateShopButton(out button, 30); line.Add(button);
        CreateShopButton(out button, 30); line.Add(button);

        List<Button> buttonList = shopmenu.Query<Button>().ToList();

        foreach (Button btn in buttonList)
        {
            if (weaponDict.ContainsKey(btn))
            {
                Button btnRef = btn;
                AddProductLabelsToShopButton(ref btnRef, weaponDict[btn].entityName, weaponDict[btn].price.ToString() + " $");


                btn.clicked += () => /*Debug.Log(weaponDict[btn].entityName);*/
                {
                    ShopCommand sc = new ShopCommand
                    {
                        weaponData = weaponDict[btn].entityName,
                    };

                    RpcUtils.SendClientToServerRpc(ref sc);
                };

                // On hover Debug Log entity name
                btn.RegisterCallback<PointerEnterEvent>(evt => OnShopButtonEnter(btn));
                btn.RegisterCallback<PointerLeaveEvent>(evt => OnShopButtonLeave());
            }
            else
            {
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.LogError("Coucou");
            UI.ToggleActive(ref root);
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
        statsElement.Add(UI.Label($"Firerate: {weapon.firerate}", 20, Color.white));
    }

    private void OnShopButtonLeave()
    {
        root.Q<VisualElement>("StatsElement")?.RemoveFromHierarchy();
    }
}
