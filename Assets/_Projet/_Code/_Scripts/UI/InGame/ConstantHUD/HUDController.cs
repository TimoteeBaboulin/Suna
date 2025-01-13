using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [SerializeField] UIDocument healthArmorDocument;
    [SerializeField] UIDocument ammoDocument;

    VisualElement healthArmor;
    VisualElement ammor;

    private void Awake()
    {
        healthArmor = healthArmorDocument.rootVisualElement;
        ammor = ammoDocument.rootVisualElement;
    }
}
