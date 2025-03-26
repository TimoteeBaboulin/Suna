using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ShopController : MonoBehaviour
{
    private UIDocument document;
    private VisualElement root;
    private VisualElement shop;
    private VisualElement shopmenu;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        root = document.rootVisualElement;
        shop = root.Q<VisualElement>("Shop");
        shopmenu = shop.Q<VisualElement>("ShopMenu");
        root.style.opacity = 0;
        document.sortingOrder = -1;
        root.SetEnabled(false);
    }

    private void Start()
    {
        Button button = new();
        button.AddToClassList("shop_buy_slot_button");
        button.style.position = Position.Absolute;
        button.style.width = 100;
        button.style.height = 100;
        button.style.marginBottom = 10;
        button.style.marginTop = 10;
        button.style.marginLeft = 10;
        button.style.marginRight = 10;
        button.style.paddingBottom = 10;
        button.style.paddingTop = 10;
        button.style.paddingLeft = 10;
        button.style.paddingRight = 10;
        shopmenu.Add(button);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            root.style.opacity = root.style.opacity.value == 1 ? 0 : 1;
            root.SetEnabled(!root.enabledInHierarchy);
            document.sortingOrder = root.enabledInHierarchy ? 1 : -1;
        }
    }

}
