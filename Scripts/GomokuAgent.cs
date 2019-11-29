using UnityEngine;
using MLAgents;

public class GomokuAgent : Agent
{
    public EPiece pieceType;

    public override void InitializeAgent()
    {
    }

    public override void CollectObservations()
    {
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
    }

    public override void AgentReset()
    {
    }

    public override float[] Heuristic()
    {
        var action = new float[2];

        action[0] = -Input.GetAxis("Horizontal");
        action[1] = Input.GetAxis("Vertical");
        return action;
    }
}
