using UnityEngine;
using MLAgents;
using System.Collections.Generic;

public class GomokuWhiteAgent : GomokuAgent
{
    private readonly List<int> maskedActions = new List<int>();

    public override void InitializeAgent()
    {
        pieceType = EPiece.White;
    }
}
