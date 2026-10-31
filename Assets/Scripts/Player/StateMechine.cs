using System;
using System.Collections.Generic;
using System.Text;

public class StateMechine<T>
{

    public StateType currentStateID { get; private set; }
    public StateType previousStateID { get; private set; }
    public BaseState<T> currentState { get; private set; }
    private Dictionary<StateType, BaseState<T>> m_MechineState;
    public StateMechine()
    {
        m_MechineState = new Dictionary<StateType, BaseState<T>>();
    }
    public void AddState(StateType stateID, BaseState<T> state)
    {
        if (currentState == null)
        {
            currentStateID = stateID;
            currentState = state;
        }
        if (m_MechineState.ContainsKey(stateID))
            return;
        m_MechineState.Add(stateID, state);
    }
    public void RemoveState(StateType stateID)
    {
        if (m_MechineState.ContainsKey(stateID))
            m_MechineState.Remove(stateID);
    }
    public void ChangeState(StateType stateID)
    {
        if (m_MechineState.ContainsKey(stateID))
        {
            currentState.OnExit();
            previousStateID = currentStateID;
            currentStateID = stateID;
            currentState = m_MechineState[stateID];
            currentState.OnEnter();
        }
    }
    public void Update()
    {
        currentState.OnUpdate();
    }
}
