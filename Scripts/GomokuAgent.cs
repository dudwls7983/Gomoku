using UnityEngine;
using MLAgents;
using System.Collections.Generic;

public class GomokuAgent : Agent
{
    private readonly List<int> maskedActions = new List<int>();

    protected EPiece pieceType;

    public GomokuAgent targetAgent = null;

    public override void InitializeAgent()
    {
    }

    public override void CollectObservations()
    {
        Gomoku gomoku = Gomoku.Instance;
        maskedActions.Clear();
        
        for (int y = 0; y < gomoku.gridCounts.y; y++)
        {
            for (int x = 0; x < gomoku.gridCounts.x; x++)
            {
                int index = y * gomoku.gridCounts.x + x;
                if(gomoku.pieceList.ContainsKey(index) == false)
                {
                    AddVectorObs(true);
                    AddVectorObs(false);
                    AddVectorObs(false);
                }
                else
                {
                    AddVectorObs(false);
                    AddVectorObs(gomoku.pieceList[index].PieceType == pieceType);
                    AddVectorObs(gomoku.pieceList[index].PieceType != pieceType);

                    maskedActions.Add(index);
                }
            }
        }
        SetActionMask(0, maskedActions);
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        if (vectorAction == null)
            return;

        if (IsMyTurn() == false)
            return;

        Gomoku gomoku = Gomoku.Instance;
        if (gomoku == null)
            return;

        if(gomoku.pieceList.Count == gomoku.gridCounts.x * gomoku.gridCounts.y)
        {
            gomoku.RestartBoard();
            return;
        }

        // 턴 소모에 따른 부정적인 보상
        AddReward(-0.05f);

        int cellIndex = Mathf.RoundToInt(vectorAction[0]);
        if (cellIndex < 0 && cellIndex >= (gomoku.gridCounts.x * gomoku.gridCounts.y))
            return;

        if (gomoku.pieceList.ContainsKey(cellIndex))
            return;
        
        if(gomoku.TestCanPlace(cellIndex, out int reward))
        {
            if(gomoku.PlacePeace(cellIndex))
            {
                Debug.Log((pieceType == EPiece.Black ? "Black" : "White") + " Win!");
                Done();
                AddReward(1.0f);
                //targetAgent.AddReward(-1.0f);
            }
            else
            {
                int x = cellIndex % gomoku.gridCounts.x;
                int y = cellIndex / gomoku.gridCounts.x;

                // 연속된 돌을 놓는 것에대한 긍정적인 보상
                if (reward >= 3) AddReward(0.02f);
                if (reward >= 4) AddReward(0.08f);

                // 상대 연속된 돌을 막는 것에대한 긍정적인 보상
                int reward2 = gomoku.GetPreventReward(cellIndex);
                if (reward2 >= 3) AddReward(0.01f);
                if (reward2 >= 4) AddReward(0.04f);
            }
        }
    }

    public override void AgentReset()
    {
        Gomoku.Instance.RestartBoard();
    }

    public override float[] Heuristic()
    {
        var action = new float[2];

        action[0] = -Input.GetAxis("Horizontal");
        action[1] = Input.GetAxis("Vertical");
        return action;
    }

    private bool IsMyTurn()
    {
        return Gomoku.Instance.CurrentTurn == (int)pieceType;
    }
}
