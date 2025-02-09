using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = System.Random;

public class GameManager : MonoBehaviour
{
    #region 変数

    public int mineNumber; // 地雷の数
    // 行と列
    public int row; // 行数
    public int column; // 列数
    public GameObject prefabToSpawn; // 生成するプレハブを指定

    public Transform cellLayoutGroup; // 生成先の親オブジェクトのTransform

    public GameObject gameOverText; // ゲームオーバー時のテキスト
    public GameObject gamewinText; // ゲームクリア時のテキスト

    public bool GameOver; // ゲームオーバー状態

    public bool youWin; // 勝利状態

    /// <summary>
    /// 行列 0,1は1行2列を表す
    /// (0,0) (0,1) (0,2) (0,3)
    /// (1,0) (1,1) (1,2) (1,3) 
    /// </summary>

    bool[,] visited; // 探索済みのマスを追跡するための配列
    GameObject[,] cellModelList; // セルのモデルを格納する配列
    /// <summary>
    /// オブジェクトの状態を格納するデータ構造 -1は地雷 (マインスイーパー用)
    /// </summary>
    int[,] cellNumberList; // オブジェクトの状態を格納
// オブジェクトを格納し、その表示はデータに基づく
    /// <summary>
    /// 注意: y, x の順序
    /// </summary>
    public CellManager[,] cellList; // セルの状態を管理する2次元配列

    public static GameManager instance; // ゲームマネージャのシングルトンインスタンス

    public List<CellManager> restCellManagers; // 残りのセルを管理するリスト

    #endregion


    void Awake()
    {
        instance = this;
        firstMove = true;

    }


    // 毎フレーム呼び出されるメソッド
    
    public bool firstMove = true;
    void Update()
    {
        if (!GameOver)
        {
            // ユーザーがマウス左クリックを押した場合
            if (Input.GetMouseButtonDown(0) &&firstMove)
            {
                GameObject targetObject = GetClickedTarget();
                if (targetObject != null)
                {
                    int positionX = targetObject.GetComponent<SingleCellDisplay>().positionX;
                    int positionY = targetObject.GetComponent<SingleCellDisplay>().positionY;
                    Debug.Log("GameRestartOver");
                    
                    GameStart(positionX,positionY);
                    firstMove = false;
                }



            }
            
            if (Input.GetMouseButtonDown(0) &&!firstMove)
            {
                GameObject targetObject = GetClickedTarget();

                if (targetObject != null &&targetObject.CompareTag("Cell"))
                {
                    CellManager cellManager= targetObject.GetComponent<SingleCellDisplay>().cellManager;
                    if (cellManager.isFlaged == false
                        && cellManager.isOpen == false
                       )
                    {
                        Debug.Log("クリックされた座標: " + targetObject.GetComponent<SingleCellDisplay>().cellManager.position.Item2 + " " + targetObject.GetComponent<SingleCellDisplay>().cellManager.position.Item1);
                        ExploreEvent(cellManager);
                    }
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                GameObject targetObject = GetClickedTarget();
                if (targetObject != null)
                {
                    SetFlage(targetObject);
                }
            }
        }
    }


    #region ゲーム初期化

    #region GameSettings

    [SerializeField] TMP_InputField _row;
    [SerializeField] TMP_InputField _col;
    [SerializeField] TMP_InputField _mine;
    [SerializeField] GameObject cellPanel;
    [SerializeField]  GameObject gameSettingPanel;
    public void GameStartWithInputField()
    {
        column = int.Parse(_col.text);
        row = int.Parse(_col.text);
        mineNumber= int.Parse(_mine.text);
        cellPanel.GetComponent<GridLayoutGroup>().constraintCount=column;
        gameSettingPanel.SetActive(false);
        
        CreateCell();
        GameReStart();
    }
    public void GameStartBeginner()
    {
        column = 8;
        row = 8;
        mineNumber = 10;
        cellPanel.GetComponent<GridLayoutGroup>().constraintCount=column;
        gameSettingPanel.SetActive(false);
        
        CreateCell();
        GameReStart();
    }
    public void GameStartIntermediate()
    {
        column = 16;
        row = 16;
        mineNumber = 40;
        cellPanel.GetComponent<GridLayoutGroup>().constraintCount=column;
        gameSettingPanel.SetActive(false);
        cellPanel.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        
        CreateCell();
        GameReStart();
    }
    public void GameStartExpert()
    {
        column = 30;
        row = 16;
        mineNumber = 99;
        cellPanel.GetComponent<GridLayoutGroup>().constraintCount=column;
        gameSettingPanel.SetActive(false);
        cellPanel.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        
        CreateCell();
        GameReStart();
    }
    #endregion


    public void CreateCell()
    {
        cellModelList = new GameObject[row, column];
        for (int y = 0; y < row; y++)
        {
            for (int x = 0; x < column; x++)
            {
                GameObject cell = Instantiate(prefabToSpawn, cellLayoutGroup);
                cellModelList[y, x] = cell;
                cellModelList[y, x].GetComponent<SingleCellDisplay>().Init(x,y);
            }
        }
    }

    public void Init()
    {
        GameOver = false;
        youWin = false;
        
        foreach (var cellManager in cellModelList)
        {
            cellManager.GetComponent<SingleCellDisplay>().Init();
        }

        gameOverText.SetActive(false);
        gamewinText.SetActive(false);
        AIManager.instance.Init();

        restCellManagers = new List<CellManager>();
    }

    public void GameReStart()
    {
        firstMove = true;
        Init();

    }
    /*public void GameStart()
    {
        firstMove = true;
        
        Init();
        cellNumberList = new int[row, column];
        cellList = new CellManager[row, column];
        visited = new bool[row, column];

        DecideBoom(mineNumber); // 爆弾の配置を決定する
        calculateNumber(); // 隣接する爆弾の数を計算する

        for (int y = 0; y < row; y++)
        {
            for (int x = 0; x < column; x++)
            {
                GameObject cell = cellModelList[y, x];
                CellManager cellManager = new CellManager();
                cellManager.boomNumber = cellNumberList[y, x];
                cellManager.isBoom = false;

                if (cellNumberList[y, x] != -1)
                {
                    cellManager.isBoom = false;
                }
                else
                {
                    cellManager.isBoom = true;
                }

                cell.GetComponent<SingleCellDisplay>().cellManager = cellManager;

                cellManager.position = new Tuple<int, int>(y, x); // セルの座標を設定

                cellManager.cellModel = cell;
                cellList[y, x] = cellManager;
                int cellNumber = cellNumberList[y, x];
                if (cellNumber == -1)
                {
                    cell.GetComponent<SingleCellDisplay>().SetBomb(); // 爆弾をセット
                }
                else
                {
                    cell.GetComponent<SingleCellDisplay>().SetNumber(cellNumber); // 数字をセット
                }
            }
        }

        foreach (var VARIABLE in cellList)
        {
            restCellManagers.Add(VARIABLE); // 未探索のセルをリストに追加
        }

        AIManager.instance.cellList = cellList;
    }*/
    //第一歩死亡防止
    public void GameStart(int positionX,int positionY)
    {
        Init();
        cellNumberList = new int[row, column];
        cellList = new CellManager[row, column];
        visited = new bool[row, column];

        DecideBoom(mineNumber,positionX, positionY); // 爆弾の配置を決定する
        calculateNumber(); // 隣接する爆弾の数を計算する

        for (int y = 0; y < row; y++)
        {
            for (int x = 0; x < column; x++)
            {
                GameObject cell = cellModelList[y, x];
                CellManager cellManager = new CellManager();
                cellManager.boomNumber = cellNumberList[y, x];
                cellManager.isBoom = false;

                if (cellNumberList[y, x] != -1)
                {
                    cellManager.isBoom = false;
                }
                else
                {
                    cellManager.isBoom = true;
                }

                cell.GetComponent<SingleCellDisplay>().cellManager = cellManager;

                cellManager.position = new Tuple<int, int>(y, x); // セルの座標を設定

                cellManager.cellModel = cell;
                cellList[y, x] = cellManager;
                int cellNumber = cellNumberList[y, x];
                if (cellNumber == -1)
                {
                    cell.GetComponent<SingleCellDisplay>().SetBomb(); // 爆弾をセット
                }
                else
                {
                    cell.GetComponent<SingleCellDisplay>().SetNumber(cellNumber); // 数字をセット
                }
            }
        }

        foreach (var VARIABLE in cellList)
        {
            restCellManagers.Add(VARIABLE); // 未探索のセルをリストに追加
        }
        foreach (var cellManager in cellList)
        {
            cellManager.Probobability = (float)mineNumber /(float) (column * row);
        }
        AIManager.instance.cellList = cellList;
    }
    public void DecideBoom(int n)
    {
        List<Tuple<int, int>> resultList = GenerateRandomTuples(n);
        /*Debug.Log("爆弾数: " + resultList.Count);*/

        foreach (Tuple<int, int> bombPosition in resultList)
        {
            // 爆弾であれば -1 をセット
            cellNumberList[bombPosition.Item1, bombPosition.Item2] = -1;
        }
    }
    public void DecideBoom(int n,int positionX,int positionY)
    {
        List<Tuple<int, int>> resultList = GenerateRandomTuples(n,positionX,positionY);
        /*Debug.Log("爆弾数: " + resultList.Count);*/

        foreach (Tuple<int, int> bombPosition in resultList)
        {
            // 爆弾であれば -1 をセット
            cellNumberList[bombPosition.Item1, bombPosition.Item2] = -1;
        }
    }
    public void calculateNumber()
    {
        for (int row = 0; row < cellNumberList.GetLength(0); row++)
        {
            for (int col = 0; col < cellNumberList.GetLength(1); col++)
            {
                // 現在のセルが爆弾（値が -1）でない場合、隣接する爆弾の数を計算
                if (cellNumberList[row, col] != -1)
                {
                    int bombCount = 0; // 隣接する爆弾の数

                    // 現在のセルの周囲8マスを探索
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int neighborRow = row + i;
                            int neighborCol = col + j;

                            // 範囲外をチェックして無視
                            if (neighborRow >= 0 && neighborRow < cellNumberList.GetLength(0) &&
                                neighborCol >= 0 && neighborCol < cellNumberList.GetLength(1))
                            {
                                // 隣接セルが爆弾である場合、爆弾カウントを増やす
                                if (cellNumberList[neighborRow, neighborCol] == -1)
                                {
                                    bombCount++;
                                }
                            }
                        }
                    }

                    // 計算した隣接爆弾数を現在のセルに設定
                    cellNumberList[row, col] = bombCount;
                }
            }
        }
    }

    private List<Tuple<int, int>> GenerateRandomTuples(int n)
    {
        List<Tuple<int, int>> result = new List<Tuple<int, int>>();
        Random random = new Random();

        while (result.Count < n)
        {
            int number1 = random.Next(0, row);
            int number2 = random.Next(0, column);

            Tuple<int, int> newTuple = new Tuple<int, int>(number1, number2);

            // 重複をチェック
            bool isDuplicate = false;
            foreach (var existingTuple in result)
            {
                if (existingTuple.Item1 == newTuple.Item1 && existingTuple.Item2 == newTuple.Item2)
                {
                    isDuplicate = true;
                    break;
                }
            }

            // 重複していない場合、結果リストに追加
            if (!isDuplicate)
            {
                result.Add(newTuple);
            }
        }

        return result;
    }
    private List<Tuple<int, int>> GenerateRandomTuples(int n,int positionX,int positionY)
    {
        List<Tuple<int, int>> result = new List<Tuple<int, int>>();
        Random random = new Random();

        while (result.Count < n)
        {
            int number1 = random.Next(0, row);
            int number2 = random.Next(0, column);
            if (number1==positionY && number2==positionX)
            {
                continue;
            }

            Tuple<int, int> newTuple = new Tuple<int, int>(number1, number2);

            // 重複をチェック
            bool isDuplicate = false;
            foreach (var existingTuple in result)
            {
                if (existingTuple.Item1 == newTuple.Item1 && existingTuple.Item2 == newTuple.Item2)
                {
                    isDuplicate = true;
                    break;
                }
            }

            // 重複していない場合、結果リストに追加
            if (!isDuplicate)
            {
                result.Add(newTuple);
            }
        }

        return result;
    }

    private static GameObject GetClickedTarget()
    {
        Debug.Log("プレイヤーがクリックを開始");
        RaycastHit2D hit = Physics2D.Raycast(Input.mousePosition, Vector2.zero);

        // クリックしたオブジェクトが存在するか確認
        if (hit.collider != null)
        {
            Debug.Log("クリック対象を検出");
            return hit.transform.gameObject;
        }
        else
        {
            return null;
        }
    }

    public void SetFlage(GameObject targetObject)
    {
        SingleCellDisplay singleCellDisplay = targetObject.GetComponent<SingleCellDisplay>();
        if (singleCellDisplay != null)
        {
            if (singleCellDisplay.cellManager.isFlaged == false && singleCellDisplay.cellManager.isOpen == false)
            {
                singleCellDisplay.cellManager.isFlaged = true;
                singleCellDisplay.SetFlag();
            }
            else
            {
                singleCellDisplay.cellManager.isFlaged = false;
                singleCellDisplay.CloseFlag();
            }
        }
        CheckGameFinishedAndFinishGame();
    }

    #endregion




    #region セル探索

    public void Explore(int y, int x)
    {
        // 座標が範囲外か、すでに探索済みかを確認
        if (y < 0 || y >= row || x < 0 || x >= column || visited[y, x])
            return;

        // 現在のセルを訪問済みとしてマーク
        visited[y, x] = true;
        cellList[y, x].isOpen = true;
        if (restCellManagers.Contains(cellList[y, x]))
        {
            restCellManagers.Remove(cellList[y, x]); // リストからセルを削除
        }
        tempExploredCell.Add(cellList[y, x]); // 一時的に探索済みセルを記録
        
        // 現在のセルが0の場合、周囲の8マスを再帰的に探索
        if (cellNumberList[y, x] == 0)
        {
            Explore(y - 1, x - 1);
            Explore(y - 1, x);
            Explore(y - 1, x + 1);
            Explore(y, x - 1);
            Explore(y, x + 1);
            Explore(y + 1, x - 1);
            Explore(y + 1, x);
            Explore(y + 1, x + 1);
        }
    }

    public bool gameMode;
    // 探索したセルを記録
    List<CellManager> tempExploredCell;
    public void ExploreEvent(CellManager targetCell)
    {
        /*Debug.Log("周囲の探索を開始");*/
        visited = new bool[row, column];

        tempExploredCell = new List<CellManager>();
        // スクリプトが見つかった場合

        /*Debug.Log("クリックされたオブジェクトを検出");*/
        // スクリプトを有効化
        Tuple<int, int> cellPosition = targetCell.position;
        Explore(cellPosition.Item1, cellPosition.Item2);

        /*Debug.Log(myScript.cellManager.boomNumber);*/
        if (targetCell.boomNumber == -1)
        {
            /*Debug.Log("ゲームオーバー");*/
            youWin = false;
            gameOverText.SetActive(true);

            GameOver = true; // ゲーム終了フラグを設定
        }
        CheckGameFinishedAndFinishGame(); // ゲーム終了条件をチェック


        AIManager.instance.CalculateAllProb();
        #region  UI

        if (gameMode)
        {
            UpdateAllUI();
            // AI 自動処理部分を追加
            if (!GameOver )
            {
                foreach (var cellManager in cellList)
                {
                    cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.gameObject
                        .GetComponent<Image>().color = Color.gray;
                }
                
                AIManager.instance.UpdateUI();
            }
            
        }

        #endregion


    }
    public void ExploreEvent(List<CellManager> targetCellList)
    {
        /*Debug.Log("周囲の探索を開始");*/
        visited = new bool[row, column];

        tempExploredCell = new List<CellManager>();
        // スクリプトが見つかった場合
        /*Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();*/

        
        /*Debug.Log("クリックされたオブジェクトを検出");*/
        foreach (var cellManager in targetCellList)
        {
            Tuple<int, int> cellPosition = cellManager.position;
            Explore(cellPosition.Item1, cellPosition.Item2);

            /*Debug.Log(myScript.cellManager.boomNumber);*/
            if (cellManager.boomNumber == -1)
            {
                /*Debug.Log("ゲームオーバー");*/
                youWin = false;
                gameOverText.SetActive(true);

                GameOver = true; // ゲーム終了フラグを設定
            }
        }
        // 运行代码1
        /*stopwatch.Stop();*/
        /*
        Debug.Log($"代码片段2耗时：{stopwatch.ElapsedMilliseconds} ms");
        */
        
        /*stopwatch.Restart();*/
        CheckGameFinishedAndFinishGame(); // ゲーム終了条件をチェック


        AIManager.instance.CalculateAllProb();
        // 运行代码1
        /*stopwatch.Stop();
        Debug.Log($"代码片段3耗时：{stopwatch.ElapsedMilliseconds} ms");*/
        #region  UI 

        if (gameMode)
        {
            UpdateAllUI();
            // AI 自動処理部分を追加
            if (!GameOver )
            {
                foreach (var cellManager in cellList)
                {
                    cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.gameObject
                        .GetComponent<Image>().color = Color.gray;
                }
                
                AIManager.instance.UpdateUI();
            }
            
        }

        #endregion


    }

    public void UpdateAllUI()
    {
        foreach (var cellManager in cellList)
        {
            if (cellManager.isOpen)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().ShowCell();
            }

            if (cellManager.isFlaged)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().SetFlag();
            }


        }
    }

    #endregion



    #region ゲーム終了判定
    public bool CheckGameFinished()
    {
        foreach(CellManager cell in cellList)
        {
            if (cell.isOpen ==false && cell.isBoom ==false)
            {
                return false;
            }
        }
        return true;
    }

    public void CheckGameFinishedAndFinishGame()
    {
        if (CheckGameFinished())
        {
            youWin = true;
            gamewinText.SetActive(true);
            GameOver = true;
        }
    }

    

    #endregion

}
