using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using System.Reflection;
public class StateMachine : MonoBehaviour
{ 
    State currentState;
    Dictionary<Type, State> statesInstance = new Dictionary<Type, State>();

    void Awake()
    {
        // Get all children type of State
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] types = assembly.GetTypes();
        IEnumerable<Type> stateTypes = types.Where(t => t.IsSubclassOf(typeof(State)) && !t.IsAbstract);

        // Instanciate each type and add to dictionary
        foreach (Type type in stateTypes)
        {
            State instance = (State)Activator.CreateInstance(type);
            statesInstance[type] = instance;
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (currentState != null)
        {
            currentState.OnStateTriggerEnter(collision);
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (currentState != null)
        {
            currentState.OnStateTriggerExit(collision);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != null)
        {
            currentState.OnStateCollisionEnter(collision);
        }
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.OnStateUpdate();
        }
    }

    void FixedUpdate()
    {
        if (currentState != null)
        {
            currentState.OnStateFixedUpdate();
        }
    }

    public void OnMouseButtonDown()
    {
        if (currentState != null)
        {
            currentState.OnStateMouseButtonDown();
        }
    }

    public void OnMouseButtonUp()
    {
        if (currentState != null)
        {
            currentState.OnStateMouseButtonUp();
        }
    }

    public void OnBeginDrag()
    {
        if (currentState != null)
        {
            currentState.OnStateBeginDrag();
        }
    }

    public void OnDrag(Vector3 dragPos)
    {
        if (currentState != null)
        {
            currentState.OnStateDrag(dragPos);
        }
    }

    public void OnEndDrag()
    {
        if (currentState != null)
        {
            currentState.OnStateEndDrag();
        }
    }

    public T LoadState<T>() where T : State
    {
        if (statesInstance.TryGetValue(typeof(T), out State state))
        {
            state.OnStateEnter();
            currentState = state;
            return currentState as T;
        }
        return null;
    }

    public T ChangeState<T>() where T : State
    {
        if (statesInstance.TryGetValue(typeof(T), out State state))
        {
            if (currentState != null)
            {
                currentState.OnStateExit();
                //Debug.Log(currentState.GetType().Name + " ----> " + state.GetType().Name);
            }

            state.OnStateEnter();
            currentState = state;
            return currentState as T;
        }
        return null;
    }

    public T GetCurrentState<T>() where T : State
    {
        if (currentState is T)
        {
            return currentState as T;
        }
        return null;
    }

    public T GetState<T>() where T : State
    {
        if (statesInstance.TryGetValue(typeof(T), out State state))
        {
            return state as T;
        }
        return null;
    }

    public bool CheckCurrentState<T>() where T : State
    {
        return currentState is T;
    }
}
