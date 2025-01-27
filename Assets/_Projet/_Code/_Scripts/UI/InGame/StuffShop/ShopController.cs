using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.WSA;

public class ShopController : MonoBehaviour
{
    private VisualElement root;
    private VisualElement shop;
    [SerializeField] VisualTreeAsset line;
    [SerializeField] VisualTreeAsset stuff;
    [SerializeField] List<string> items = new();

    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        shop = root.Q<VisualElement>("Shop");
    }

    private void Start()
    {
        for (int i = 0; i < Mathf.CeilToInt(items.Count / 3f); i++)
        {
            shop.Add(line.Instantiate().Children().First());
        }

        Button element;
        int amount = 0;
        foreach (var child in shop.Children())
        {
            if (amount < items.Count - 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    element = stuff.Instantiate().Children().First() as Button;
                    element.text = items[amount];
                    child.Add(element);
                    amount++;
                }
            }
            else
            {
                for (int i = 0; i < items.Count - amount; i++)
                {
                    element = stuff.Instantiate().Children().First() as Button;
                    element.text = items[amount + i];
                    child.Add(element);
                }
            }
        }
        
        foreach (var child in shop.Children())
        {
            foreach (Button button in child.Children())
            {
                button.clicked += () => {
                    EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    EntityQuery query = entityManager.CreateEntityQuery(typeof(TestPlayerData));
                    Entity entity = query.ToEntityArray(Allocator.Temp).First();
                    TestPlayerData data = query.ToComponentDataArray<TestPlayerData>(Allocator.Temp).First();
                    if (data.Cash >= 100)
                    {
                        data.Cash -= 100;
                        Debug.Log("Bought " + button.text);
                    }
                    entityManager.SetComponentData(entity, data);
                    TestPlayerDataSystem system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TestPlayerDataSystem>();
                    system.InvokeCashUpdate(data.Cash);
                };
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            root.visible = !root.visible;
        }
    }

}
