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
        if (gomoku == null || gomoku.pieceList.Count == gomoku.gridCounts.x * gomoku.gridCounts.y)
            return;

        int cellIndex = Mathf.RoundToInt(vectorAction[0]);
        if (cellIndex < 0 && cellIndex >= (gomoku.gridCounts.x * gomoku.gridCounts.y))
            return;

        if (gomoku.pieceList.ContainsKey(cellIndex))
            return;

        float reward;
        if(gomoku.TestCanPlace(cellIndex, out reward))
        {
            if(gomoku.PlacePeace(cellIndex))
            {
                Debug.Log((pieceType == EPiece.Black ? "Black" : "White") + " Win!");
                Done();
                AddReward(1.0f);
                targetAgent.AddReward(-1.0f);
            }
            else
            {
                int x = cellIndex % gomoku.gridCounts.x;
                int y = cellIndex / gomoku.gridCounts.x;

                // 중앙에 가깝게 놓을수록 긍정적인 보상
                if ((x >= 5 && x <= 9) || (y >= 5 && y <= 9))
                {
                    AddReward(1f);
                }
                // 연속된 돌을 놓는 것에대한 긍정적인 보상
                AddReward(reward * reward);
                // 시간의 소모에 따른 부정적인 보상
                AddReward(-0.1f);
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
