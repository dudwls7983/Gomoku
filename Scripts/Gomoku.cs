﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ELine
{
    Up,         // ↑
    Diagonal_1, // ↗
    Right,      // →
    Diagonal_2, // ↘
    Down,       // ↓
    Diagonal_3, // ↙
    Left,       // ←
    Diagonal_4, // ↖
    Max,
}

public class Gomoku : MonoBehaviour
{
    public float emptySpace = 0.71f;
    public float allowPlaceDistance = 0.3f;
    public Vector2Int gridCounts = new Vector2Int(15, 15);
    public GameObject piecePrefab;

    public Dictionary<int, Piece> pieceList;

    private bool _enable = true;

    [HideInInspector]
    public int CurrentTurn
    {
        get { return _currentTurn; }
        set { _currentTurn = value % 2; }
    }
    private int _currentTurn = 0;

    #region const variable
    private const int Black = 0;
    private const int White = 1;
    #endregion

    void Awake()
    {
        if (piecePrefab == null)
            _enable = false;

        pieceList = new Dictionary<int, Piece>();
    }

    // Start is called before the first frame update
    void Start()
    {
        CurrentTurn = 0;
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
            // Raycast한 대상이 오목판인지 체크한다.
            if (hit.collider != null && hit.collider.name.Equals(name))
            {
                // 가까운 그리드 지점을 찾는다.
                CanPlace canPlace = (float value, out float output) => {
                    var temp = value % emptySpace;
                    if (temp > (emptySpace / 2)) temp -= emptySpace;
                    if (temp < -(emptySpace / 2)) temp += emptySpace;
                    output = temp;
                    return Mathf.Abs(temp) < allowPlaceDistance;
                };

                Vector3 gridPosition = new Vector3(0, 0, -0.2f);

                // Raycast 히트 지점이 오목알을 놓을수 있는 위치인지 체크한다.
                if (canPlace(hit.point.x, out gridPosition.x) && canPlace(hit.point.y, out gridPosition.y))
                {
                    // 그리드에 맞춰 위치벡터 값을 수정한다.
                    gridPosition.x = hit.point.x - gridPosition.x;
                    gridPosition.y = hit.point.y - gridPosition.y;

                    // 그리드 위치로부터 인덱스를 구한다.
                    int gridIndex = GetPieceIndexFromPosition(gridPosition);
                    if (pieceList.ContainsKey(gridIndex))
                        return;

                    // 규칙에 의해 놓을수 없는 지점인지 체크한다.
                    if (TestCanPlace(gridIndex) == false)
                        return;

                    // 그리드에 맞춰 오목알을 생성한다.
                    var go = Instantiate(piecePrefab, gridPosition, Quaternion.identity, transform);
                    go.transform.localScale = transform.worldToLocalMatrix.MultiplyPoint(new Vector3(0.6f, 0.6f, 0.3f));

                    // Piece 컴포넌트를 가져온다. 컴포넌트가 없다면 생성한다.
                    var piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
                    piece.SetPieceData((EPiece)CurrentTurn);

                    // 게임의 끝났는지 결정한다.
                    if(TestWinner(gridIndex) == true)
                    {
                        Debug.Log("Game End");
                        return;
                    }

                    // 턴을 넘긴다.
                    CurrentTurn++;

                    // 인덱스로부터 Piece 정보를 저장한다.
                    pieceList.Add(gridIndex, piece);
                }
            }
        }
    }

    /// <summary>
    /// 왼쪽 위를 기준으로 오른쪽으로 갈수록 1씩 증가하고 아래쪽으로 갈수록 열의 갯수 만큼 증가한다.
    /// </summary>
    /// <param name="position">인덱스를 가져올 그리드 위치</param>
    /// <returns></returns>
    private int GetPieceIndexFromPosition(Vector3 position)
    {
        position.x += (gridCounts.x / 2) * emptySpace;
        position.y += (gridCounts.y / 2) * emptySpace;

        int x = Mathf.RoundToInt(position.x / emptySpace);
        int y = gridCounts.y - Mathf.RoundToInt(position.y / emptySpace) - 1;
        
        return y * gridCounts.x + x;
    }

    private bool TestCanPlace(int index)
    {
        // 렌주룰에 따르면 흑은 33, 44, 6목에 착수가 불가능하다.
        if(CurrentTurn == Black)
        {
            // 6목 착수 방지
            for (int i = 0; i < (int)ELine.Max / 2; i++)
            {
                // 붙은 돌의 갯수가 5개가 넘는다면 착수가 불가능하다.
                if (GetPieceCount(index, CurrentTurn, (ELine)i) > 5)
                    return false;
            }

            // 0 = 33체크, 1 = 44체크
            bool[] disallowPlace = new bool[2] { false, false };
            for (int i = 0; i < (int)ELine.Max; i++)
            {
                // 각 라인의 열린 돌의 갯수 파악
                int count = GetPieceCountAllowEmpty(index, CurrentTurn, (ELine)i);

                // 43은 되지만 33, 44는 방지한다.
                if(count >= 3 && count <= 4)
                {
                    if (disallowPlace[count-3]) return false;
                    disallowPlace[count-3] = true;
                }
            }
        }
        return true;
    }

    private bool TestWinner(int index)
    {
        for (int i = 0; i < (int)ELine.Max / 2; i++)
        {
            // 붙어서 5개가 만들어진다면 승리이다.
            if (GetPieceCount(index, CurrentTurn, (ELine)i) == 5)
                return true;
        }
        return false;
    }

    private int GetPieceCount(int index, int currentTurn, ELine line)
    {
        int count = 1;
        switch (line)
        {
            case ELine.Up:
            case ELine.Down:
                count += GetPieceCount(index, currentTurn, -gridCounts.x, false);
                count += GetPieceCount(index, currentTurn, gridCounts.x, false);
                break;
            case ELine.Diagonal_1:
            case ELine.Diagonal_3:
                count += GetPieceCount(index, currentTurn, -gridCounts.x+1, false);
                count += GetPieceCount(index, currentTurn, gridCounts.x-1, false);
                break;
            case ELine.Right:
            case ELine.Left:
                count += GetPieceCount(index, currentTurn, -1, false);
                count += GetPieceCount(index, currentTurn, 1, false);
                break;
            case ELine.Diagonal_2:
            case ELine.Diagonal_4:
                count += GetPieceCount(index, currentTurn, -gridCounts.x - 1, false);
                count += GetPieceCount(index, currentTurn, gridCounts.x + 1, false);
                break;
            default:
                break;
        }
        return count;
    }

    private int GetPieceCountAllowEmpty(int index, int currentTurn, ELine line)
    {
        return 0;
    }

    private int GetPieceCount(int index, int currentTurn, int sequential, bool allowEmpty)
    {
        int count = 0;
        int currentIndex = index + sequential;
        while (IsValidIndex(currentIndex))
        {
            // 빈 공간이다.
            if (pieceList.ContainsKey(currentIndex) == false)
            {
                // 빈 공간을 허용한다면 한 번은 넘어간다.
                if (allowEmpty) allowEmpty = false;
                else break;
            }

            // 상대 오목알이 나오면 즉시 멈춘다.
            if (pieceList[currentIndex].PieceType != (EPiece)currentTurn)
                break;

            count++;
            currentIndex += sequential;
        }
        return count;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < (gridCounts.x * gridCounts.y);
    }
}
