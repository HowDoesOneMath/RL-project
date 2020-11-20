using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IActionable
{
    void Perform(int state);

    float Poll(int state);

    bool IsComplete(int state);
}

public class DecisionData<T> where T : System.Enum
{
    public DecisionData(List<IActionable> legalActions)
    {
        int matrixSize = 1 << System.Enum.GetValues(typeof(T)).Length;
        stateRewards = new Jatrix(matrixSize, 1);
        stateValues = new Jatrix(matrixSize, 1);

        viableActions = legalActions;

        stateToActionToState = new Jatrix[matrixSize];

        for (int i = 0; i < matrixSize; ++i)
        {
            stateToActionToState[i] = new Jatrix(legalActions.Count, matrixSize);
        }
    }

    int chosenAction = -1;

    public List<IActionable> viableActions;
    public Jatrix stateRewards;
    public Jatrix stateValues;

    public Jatrix[] stateToActionToState;

    public int state { get; private set; } = 0;

    public void SetState(int toSet, bool value)
    {
        if (value)
        {
            state |= (1 << toSet);
        }
        else
        {
            state &= ~(1 << toSet);
        }
    }

    public bool GetState(int toGet)
    {
        return ((1 << toGet) & state) > 0;
    }

    public void DoSomething()
    {
        if (chosenAction < 0 || viableActions[chosenAction].IsComplete(state))
        {
            ChooseAction();
        }

        viableActions[chosenAction].Perform(state);
    }

    public void ChooseAction()
    {
        Jatrix actionResults = stateValues * stateToActionToState[state];

        float sumTotal = 0f;

        for (int i = 0; i < actionResults.width; ++i)
        {
            sumTotal += actionResults[i, 0];
        }

        float randomValue = Random.Range(float.Epsilon, sumTotal);

        for (int i = 0; i < actionResults.width; ++i)
        {
            sumTotal -= actionResults[i, 0];

            if (sumTotal <= 0)
            {
                chosenAction = i;
                break;
            }
        }
    }
}

public enum EStates
{
    LeftClose,
    FrontLeftClose,
    FrontClose,
    FrontRightClose,
    RightClose,
    Crashed,
}