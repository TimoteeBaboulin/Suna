using UnityEngine;


public abstract class State
{
    public void OnStateEnter()
    {
        OnEnter();
    }

    protected virtual void OnEnter()
    {
    }

    public void OnStateTriggerEnter(Collider collision)
    {
        TriggerEnter(collision);
    }

    protected virtual void TriggerEnter(Collider collision)
    {
    }

    public void OnStateTriggerExit(Collider collision)
    {
        TriggerExit(collision);
    }

    protected virtual void TriggerExit(Collider collision)
    {
    }

    public void OnStateCollisionEnter(Collision collision)
    {
        CollisionEnter(collision);
    }

    protected virtual void CollisionEnter(Collision collision)
    {
    }

    public void OnStateUpdate()
    {
        OnUpdate();
    }

    protected virtual void OnUpdate()
    {
    }

    public void OnStateFixedUpdate()
    {
        OnFixedUpdate();
    }

    protected virtual void OnFixedUpdate()
    {
    }

    public void OnStateExit()
    {
        OnExit();
    }

    protected virtual void OnExit()
    {
    }

    public void OnStateBeginDrag()
    {
        OnBeginDrag();
    }
    protected virtual void OnBeginDrag()
    {
    }

    public void OnStateDrag(Vector3 dragPos)
    {
        OnDrag(dragPos);
    }
    protected virtual void OnDrag(Vector3 dragPos)
    {
    }

    public void OnStateEndDrag()
    {
        OnEndDrag();
    }
    protected virtual void OnEndDrag()
    {
    }

    public void OnStateMouseButtonDown()
    {

        OnMouseButtonDown();
    }
    protected virtual void OnMouseButtonDown()
    {
    }

    public void OnStateMouseButtonUp()
    {

        OnMouseButtonUp();
    }
    protected virtual void OnMouseButtonUp()
    {
    }

    public bool CheckState<T>() where T : State
    {
        return this is T;
    }

    //public T GetComponent<T>()
    //{
        //return build.GetComponent<T>();
    //}

    //public GameObject gameObject { get => build.gameObject; }
    //public Transform transform { get => build.transform; }
}
