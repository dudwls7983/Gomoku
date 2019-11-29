using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EPiece
{
    Black = 0,
    White,
}

public class Piece : MonoBehaviour
{
    public Material blackPieceMaterial;
    public Material whitePieceMaterial;

    [HideInInspector]
    public EPiece PieceType { get; private set; }

    private MeshRenderer _renderer;
    // Start is called before the first frame update
    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPieceData(EPiece pieceType)
    {
        if (_renderer == null)
            return;

        switch (pieceType)
        {
            case EPiece.Black:
                _renderer.material = blackPieceMaterial;
                break;
            case EPiece.White:
                _renderer.material = whitePieceMaterial;
                break;
        }
        PieceType = pieceType;
    }
}
