﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gomoku : MonoBehaviour
{
    public float emptySpace = 0.71f;
    public float allowPlaceDistance = 0.3f;
    public Vector2Int gridCounts = new Vector2Int(15, 15);
    public GameObject piecePrefab;

    private bool _enable = true;

    void Awake()
    {
        if (piecePrefab == null)
            _enable = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }


    // Update is called once per frame
    void Update()
    {
        if (_enable == false)
            return;

        if(Input.GetMouseButtonDown(0))
        {
            PlacePiece();
        }
    }

    delegate bool CanPlace(float value, out float output);
    private void PlacePiece()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Try to check that it is gomoku board.
            if (hit.collider != null && hit.collider.name.Equals(name))
            {
                // Find the nearest grid
                CanPlace canPlace = (float value, out float output) => {
                    var temp = value % emptySpace;
                    if (temp > (emptySpace / 2)) temp -= emptySpace;
                    if (temp < -(emptySpace / 2)) temp += emptySpace;
                    output = temp;
                    return Mathf.Abs(temp) < allowPlaceDistance;
                };

                Vector3 gridPosition = new Vector3(0, 0, -0.2f);

                // Check to find grid that place gomoku piece.
                if (canPlace(hit.point.x, out gridPosition.x) && canPlace(hit.point.y, out gridPosition.y))
                {
                    // Set position from grid
                    gridPosition.x = hit.point.x - gridPosition.x;
                    gridPosition.y = hit.point.y - gridPosition.y;

                    // Create the piece at grid.
                    var go = Instantiate(piecePrefab, gridPosition, Quaternion.identity, transform);
                    go.transform.localScale = transform.worldToLocalMatrix.MultiplyPoint(new Vector3(0.6f, 0.6f, 0.3f));

                    // Set Piece Information
                    var piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
                    piece.SetPieceImage((EPiece)Random.Range(0, 2));
                }
            }
        }
    }
}