using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ELine
{
    UpDown,     // ↑↓
    Diagonal_1, // ↗↙
    LeftRight,  // ←→
    Diagonal_2, // ↘↖
    Max,
}

public class Gomoku : MonoBehaviour
{
    public float emptySpace = 0.71f;
    public float allowPlaceDistance = 0.3f;
    public Vector2Int gridCounts = new Vector2Int(15, 15);
    public GameObject piecePrefab;

    [SerializeField] private GomokuAgent blackAgent = null;
    [SerializeField] private GomokuAgent whiteAgent = null;

    public Dictionary<int, Piece> pieceList;

    private bool _enable = true;

    public static Gomoku Instance
    {
        get
        {
            if(_instance == null)
            {
                var go = new GameObject();
                _instance = go.AddComponent<Gomoku>();
            }
            return _instance;
        }
        set
        {
            if(_instance != null)
            {
                Destroy(value.gameObject);
                return;
            }
            _instance = value;
            DontDestroyOnLoad(_instance);
        }
    }
    private static Gomoku _instance = null;

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
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        CurrentTurn = 0;
        blackAgent.RequestDecision();
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
                    
                    // 게임의 끝났는지 결정한다.
                    if(PlacePeace(gridIndex) == true)
                    {
                        Debug.Log("Game End");
                        return;
                    }
                }
            }
        }
    }

    public bool PlacePeace(int index)
    {
        Vector3 gridPosition = new Vector3(((index % gridCounts.x) - (gridCounts.x / 2)) * emptySpace, ((gridCounts.y / 2) - (index / gridCounts.y)) * emptySpace, -0.2f);

        var go = Instantiate(piecePrefab, gridPosition, Quaternion.identity, transform);
        go.transform.localScale = transform.worldToLocalMatrix.MultiplyPoint(new Vector3(0.6f, 0.6f, 0.3f));

        // Piece 컴포넌트를 가져온다. 컴포넌트가 없다면 생성한다.
        var piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
        piece.SetPieceData((EPiece)CurrentTurn);

        // 턴을 넘긴다.
        CurrentTurn++;

        // 인덱스로부터 Piece 정보를 저장한다.
        pieceList.Add(index, piece);

        return TestWinner(index, CurrentTurn);
    }

    public bool TestCanPlace(int index)
    {
        float reward;
        return TestCanPlace(index, out reward);
    }

    public bool TestCanPlace(int index, out float reward)
    {
        reward = 0f;
        // 렌주룰에 따르면 흑은 33, 44, 6목에 착수가 불가능하다.
        if (CurrentTurn == Black)
        {
            // 6목 착수 방지
            for (int i = 0; i < (int)ELine.Max; i++)
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
                reward = 0.1f * count;

                // 43은 되지만 33, 44는 방지한다.
                if (count >= 3 && count <= 4)
                {
                    if (disallowPlace[count - 3]) return false;
                    disallowPlace[count - 3] = true;
                }
            }
        }
        return true;
    }

    public void RestartBoard()
    {
        foreach (var gameObject in pieceList.Values)
        {
            Destroy(gameObject.gameObject);
        }
        pieceList.Clear();
        CurrentTurn = Black;
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

    private bool TestWinner(int index, int currentTurn)
    {
        for (int i = 0; i < (int)ELine.Max; i++)
        {
            int count = GetPieceCount(index, CurrentTurn, (ELine)i);

            // 붙어서 5개가 만들어진다면 승리이다.
            if (count == 5)
                return true;

            // 흰색은 5개가 넘어도 승리이다.
            if (currentTurn == White && count > 5)
                return true;
        }
        return false;
    }

    private int GetPieceCount(int index, int currentTurn, ELine line)
    {
        int count = 1;
        switch (line)
        {
            case ELine.UpDown:
                count += GetPieceCount(index, currentTurn, -gridCounts.x, false);
                count += GetPieceCount(index, currentTurn, gridCounts.x, false);
                break;
            case ELine.Diagonal_1:
                count += GetPieceCount(index, currentTurn, -gridCounts.x+1, false);
                count += GetPieceCount(index, currentTurn, gridCounts.x-1, false);
                break;
            case ELine.LeftRight:
                count += GetPieceCount(index, currentTurn, 1, false);
                count += GetPieceCount(index, currentTurn, -1, false);
                break;
            case ELine.Diagonal_2:
                count += GetPieceCount(index, currentTurn, gridCounts.x + 1, false);
                count += GetPieceCount(index, currentTurn, -gridCounts.x - 1, false);
                break;
        }
        return count;
    }

    private int GetPieceCountAllowEmpty(int index, int currentTurn, ELine line)
    {
        int tempCount1, tempCount2;
        switch (line)
        {
            case ELine.UpDown:
                tempCount1 = GetPieceCount(index, currentTurn, -gridCounts.x, true);
                tempCount1 += GetPieceCount(index, currentTurn, gridCounts.x, false);

                tempCount2 = GetPieceCount(index, currentTurn, -gridCounts.x, false);
                tempCount2 += GetPieceCount(index, currentTurn, gridCounts.x, true);
                break;
            case ELine.Diagonal_1:
                tempCount1 = GetPieceCount(index, currentTurn, -gridCounts.x + 1, true);
                tempCount1 += GetPieceCount(index, currentTurn, gridCounts.x - 1, false);

                tempCount2 = GetPieceCount(index, currentTurn, -gridCounts.x + 1, false);
                tempCount2 += GetPieceCount(index, currentTurn, gridCounts.x - 1, true);
                break;
            case ELine.LeftRight:
                tempCount1 = GetPieceCount(index, currentTurn, 1, false);
                tempCount1 += GetPieceCount(index, currentTurn, -1, true);

                tempCount2 = GetPieceCount(index, currentTurn, 1, true);
                tempCount2 += GetPieceCount(index, currentTurn, -1, false);
                break;
            case ELine.Diagonal_2:
                tempCount1 = GetPieceCount(index, currentTurn, gridCounts.x + 1, true);
                tempCount1 += GetPieceCount(index, currentTurn, -gridCounts.x - 1, false);

                tempCount2 = GetPieceCount(index, currentTurn, gridCounts.x + 1, false);
                tempCount2 += GetPieceCount(index, currentTurn, -gridCounts.x - 1, true);
                break;
            default:
                tempCount1 = 0;
                tempCount2 = 0;
                break;
        }
        return (tempCount1 > tempCount2 ? tempCount1 : tempCount2) + 1;
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
                // 빈 공간을 허용 안 한다면 바로 값을 반환한다.
                if (allowEmpty == false) return count;
                
                // 빈 공간을 허용 한다면 한 번은 넘어간다.
                allowEmpty = false;
                currentIndex += sequential;
                continue;
            }

            // 상대 오목알이 나오면 닫힌 돌이다.
            if (pieceList[currentIndex].PieceType != (EPiece)currentTurn)
                return count;

            count++;
            currentIndex += sequential;
        }

        if (IsValidIndex(currentIndex) == false)
            count--;

        return count;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < (gridCounts.x * gridCounts.y);
    }
}
