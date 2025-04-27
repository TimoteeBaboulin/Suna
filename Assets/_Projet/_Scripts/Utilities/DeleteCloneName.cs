using UnityEngine;

public class Dellet : MonoBehaviour
{
    [SerializeField] private string _name;

    void Start()
    {
        Animator animator = GetComponentInParent<Animator>();
        
        name = _name;

        if (animator != null)
        {
            animator.Rebind();
        }
    }
}
