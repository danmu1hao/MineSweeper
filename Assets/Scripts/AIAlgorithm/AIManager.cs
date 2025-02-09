using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;
using Random = UnityEngine.Random;

public class AIManager : MonoBehaviour
{

    // 绑定三个 Toggle 和一个 InputField
    public Toggle useSToggle;
    public Toggle useEToggle;
    public Toggle useQToggle;
    public TMP_InputField repeatTimesInput;
    public TMP_InputField repeatrepeatTimesInput;
    
    public bool debugMode;

    #region 変数
    public TextMeshProUGUI solutionNumText;
    public int solutionNum;
    public float waitTime = 1f;
    public int repeatTimes = 100;
    public int repeatrepeatTimes;
    // 八方向
    private int[] _dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
    private int[] _dy = { 0, 0, -1, 1, -1, 1, -1, 1 };
    


    public static AIManager instance;

    public CellManager[,] cellList;
    public List<CellManager> safeCell;
    /*public List<CellManager> questionCell;*/

    public List<Tuple<List<CellManager>, int>> safeCellWithEquation;
    public List<CellManager> surroundCell;


    public int allCellNum;
    public int maxMine;


    public int row;
    public int column;

    public List<List<CellManager>> openCellList;


    #endregion
    void Start()
    {


        row = GameManager.instance.row;
        column = GameManager.instance.column;
        cellList = GameManager.instance.cellList;

        /*CalculateAllProb();*/
    }

    #region AI全自動
    //AIコルーチンを開始する
    public void StartAICoroutine()
    {
        StartCoroutine(AIAutoPlayAnime());
    }
    public void StopAICoroutine()
    {
        StopAllCoroutines();
    }

    // コルーチンを定義
    public IEnumerator AIAutoPlayAnime()
    {
        while (true)
        {
            AIAuto();
            yield return new WaitForSeconds(waitTime);
        }
    }

    public bool error=false;
    public void StartAI()
    {
        GameManager.instance.gameMode = false;
        
        useS=useSToggle.isOn;
        useE=useEToggle.isOn;
        useQ=useQToggle.isOn;
        repeatTimes = int.Parse(repeatTimesInput.text);
        repeatrepeatTimes = int.Parse(repeatrepeatTimesInput.text);
        
        
        for (int i = 0; i < repeatrepeatTimes; i++)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<string> strList = new List<string>();
            if (error)
            {
                break;
            }

            int times = 0;
            int wins = 0;
            int lose = 0;
            while (times < repeatTimes)
            {
                while (!GameManager.instance.GameOver)
                {
                    if (gameBreak)
                    {
                        Debug.Log("停止");
                        goto BreakAll;
                    }
                    AIAuto();
                }

                if (GameManager.instance.youWin == true)
                {
                    wins += 1;
                }
                else
                {
                    lose += 1;
                }

                times += 1;
                if (GameManager.instance.GameOver)
                {
                    GameManager.instance.GameReStart();
                }
                if (error)
                {
                    Debug.Log("停止");
                    break;
                }
            }

            stopwatch.Stop();
            
            strList.Add($"you win {wins} you lose {lose}");
            strList.Add($"you use "+stopwatch.ElapsedMilliseconds+" milisecons");
            
            Debug.Log("you win" + wins + "you lose" + lose);
            // 替换非法字符
            string sanitizedDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"); // 使用合法的文件名格式
            
            string fileName = "useS"+useS+"_"+"useE"+useE+"_"+"useQ"+useQ+"_"+GameManager.instance.row
                              +GameManager.instance.column+"_"+GameManager.instance.mineNumber+"_"+ repeatTimes + " " + i + "_" + sanitizedDateTime+ "_"+"Data.txt";
            WriteListToTxt(strList, fileName);
        }

        GameManager.instance.gameMode = true;
        BreakAll:
        Console.WriteLine(" 終了");


    }
    bool gameBreak=false;
    
    bool first = true;
    public void AIAuto()
    {

        useE=useEToggle.isOn;
        useQ=useQToggle.isOn;
        useS=useSToggle.isOn;
        //ゲームオーバー
        if (GameManager.instance.GameOver)
        {
            GameManager.instance.GameReStart(); // ゲームを再スタート
        }
        else
        {
            if (GameManager.instance.firstMove)
            {
                FirstMove(); // 最初の一手
                GameManager.instance.firstMove = false;
                return;
            
            }
            else
            {
                if (!AFNOrAMN())
                {
                    if (guessList.Count > 0)
                    {
                        GameManager.instance.ExploreEvent(guessList[0]); // セルを開く
                    }
                    else
                    {
                        gameBreak = true;
                    }
                }
                else
                {


                    /*stopwatch.Start();*/
                    List<CellManager> exploreList = new List<CellManager>();
                    foreach (var cellManager in surroundCell)
                    {
                        if (cellManager.Probobability <= 0.0001f)
                        {
                            exploreList.Add(cellManager);
                        }
                        else if (cellManager.Probobability > 0.9999f)
                        {
                            if (!cellManager.isFlaged)
                            {
                                GameManager.instance.SetFlage(cellManager.cellModel); // 爆弾のフラグを立てる
                            }
                        }

                    }
                    
                    GameManager.instance.ExploreEvent(exploreList); // 安全なセルを開く
                    /*stopwatch.Stop();
                    Debug.Log($"代码片段1耗时：{stopwatch.ElapsedMilliseconds} ms");*/
                }
            }


            GameManager.instance.CheckGameFinishedAndFinishGame();
        }

    }

    // AFNまたはAMNによる処理
    public bool AFNOrAMN()
    {
        bool isAFNorAMN = false;
        foreach (var cellManager in surroundCell)
        {
            if (cellManager.Probobability <= 0.0001f)
            {
                /*GameManager.instance.ExploreEvent(cellManager.cellModel); // 安全なセルを開く*/
                isAFNorAMN = true;
            }
            else if (cellManager.Probobability > 0.9999f)
            {
                if (!cellManager.isFlaged)
                {
                    /*GameManager.instance.SetFlage(cellManager.cellModel); // 爆弾のフラグを立てる*/
                    isAFNorAMN = true;
                }
            }
        }

        return isAFNorAMN;
    }

    public void FirstMove()
    {
        GameManager.instance.GameStart(0,0);
        GameManager.instance.ExploreEvent(cellList[0, 0]); // 最初のセルを開く
    }


    // 推測を行う
    List<CellManager> guessList;
    List<CellManager> guessList_S;
    List<CellManager> guessList_E;
    List<CellManager> guessList_Q;
    public bool useS;
    public bool useE;
    public bool useQ;
    
    public void AI_PSEQ_Calculate()
    {
        // 選択ロジックを追加
        // step1: 最も安全なセルを見つける
        // 最小のProbabilityを探す
        guessList_S = new List<CellManager>();
        guessList_E = new List<CellManager>();
        guessList_Q = new List<CellManager>();
        foreach (var cellManager in cellList)
        {
            cellManager.indexInCellGroup = -1;
        }
        
        guessList = FindAllWithMinNumber(GameManager.instance.restCellManagers);

        if (AIManager.instance.solutionNum < 500 &&
            (useS||useE||useQ))
        {
            if (useS)
            {
                guessList = PSEQ.FILTER_S(guessList);
                guessList_S = new List<CellManager>(guessList);
            }

            if (useE)
            {
                guessList = PSEQ.FILTER_E(guessList);
                guessList_E = new List<CellManager>(guessList);
            }

            if (useQ)
            {
                guessList = PSEQ.FILTER_Q(guessList);
                guessList_Q = new List<CellManager>(guessList);
            }
        }
        else
        {
            guessList = new List<CellManager>() { AIGuessBasic() };
        }

        


        return;
    }

    public void PSEQ_Visulize()
    {
        if (useS)
        {
            foreach (var cellManager in guessList_S)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.gameObject.GetComponent<Image>()
                    .color = new Color(0f, 0.35f, 0f);
            }   
        }

        if (useE)
        {
            foreach (var cellManager in guessList_E)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.gameObject.GetComponent<Image>()
                    .color = new Color(0f, 0.6f, 0f);
            } 
        }

        if (useQ)
        {
            foreach (var cellManager in guessList_Q)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.gameObject.GetComponent<Image>()
                    .color = new Color(0f, 1f, 0f);
            
            } 
        }
    }
    
    
    // 最小のProbabilityを持つオブジェクトをすべて見つける

    public List<CellManager> FindAllWithMinNumber(List<CellManager> classList)
    {
        if (classList == null || classList.Count == 0) return new List<CellManager>();

        // 一次遍历获取最小的Probobability
        float minNumber = classList.Min(x => x.Probobability);

        // 筛选出所有Probobability等于minNumber的对象
        return classList.Where(x => Mathf.Abs(x.Probobability - minNumber) < 0.0001f).ToList();
    }


    List<CellManager> FindAllWithMinNumber(List<CellManager> classList, Func<CellManager, float> sortKey)
    {
        if (classList == null || classList.Count == 0) return new List<CellManager>();

        // 渡されたsortKeyを使って並び替え
        classList = classList.OrderBy(sortKey).ToList();

        foreach (var VARIABLE in classList)
        {
            Debug.Log(VARIABLE.weight);
        }

        // 最小値を取得
        float minNumber = sortKey(classList[0]);

        List<CellManager> result = new List<CellManager>();
        foreach (var obj in classList)
        {
            if (Mathf.Abs(sortKey(obj) - minNumber) < 0.0001f)
            {
                result.Add(obj);
            }
        }

        return result;
    }
    public CellManager AIGuessBasic()
    {
        // 找到 surroundCell 中概率最小的 CellManager
        return surroundCell
            .OrderBy(cell => cell.Probobability) // 按概率升序排序
            .FirstOrDefault(); // 取第一个（概率最小的）
    }


    #endregion




    void Awake()
    {
        instance = this;
        safeCell = new List<CellManager>();
        surroundCell = new List<CellManager>();
        safeCellWithEquation = new List<Tuple<List<CellManager>, int>>();
    }

    public void Init()
    {
        safeCell.Clear();
        safeCellWithEquation.Clear();
        surroundCell.Clear();
        
        
        row = GameManager.instance.row;
        column=GameManager.instance.column;
        cellList = GameManager.instance.cellList;

    }



    



    // Start is called before the first frame update


    public void CheckOpen()
    {
        safeCell.Clear();  // 清空 safeCell 列表
        int rows = cellList.GetLength(0);
        int cols = cellList.GetLength(1);

        // 遍历所有 cell
        foreach (var cell in cellList)
        {
            if (cell.isOpen && HasUnopenedNeighbor(cell))
            {
                safeCell.Add(cell);  // 如果四周存在未打开的单元格，添加到 safeCell
            }
        }
    }

    // 检查当前 cell 四周是否存在未打开的单元格
    private bool HasUnopenedNeighbor(CellManager cell)
    {
        int x = cell.position.Item1;  // 当前 cell 的 x 坐标
        int y = cell.position.Item2;  // 当前 cell 的 y 坐标
        int rows = cellList.GetLength(0);
        int cols = cellList.GetLength(1);

        // 遍历周围8个方向
        for (int i = 0; i < 8; i++)
        {
            int newX = x + _dx[i];
            int newY = y + _dy[i];

            // 确保坐标在边界内
            if (newX >= 0 && newX < rows && newY >= 0 && newY < cols)
            {
                // 如果周围存在未打开的 cell (isOpen=false)，返回 true
                if (!cellList[newX, newY].isOpen)
                {
                    return true;
                }
            }
        }

        // 如果四周全部是 isOpen=true，则返回 false
        return false;
    }
    
    //要修正



    #region 全てのセルの確率計算
    /// <summary>
    /// 所有的解 第一层是区间 第二层是所有的解list 第三层的int[]才是单个解
    /// </summary>
    public List<CellGroup> allSurrondCellGroups;
    public void CalculateAllProb()
    {
        allSurrondCellGroups = new List<CellGroup>();
        CheckOpen();
        surroundCell = ExploreSurroundings(safeCell);


        DFSSearch dfsSearchScript = gameObject.GetComponent<DFSSearch>();
        List<List<CellManager>> groupedSafeCells = dfsSearchScript.FindSafeCellGroups();
        foreach (var VARIABLE in groupedSafeCells)
        {
            if (debugMode)
            {
                Debug.Log("有几个安全格子"+VARIABLE.Count());
            }
        }
        foreach (var safeCells in groupedSafeCells)
        {
            safeCellWithEquation = CollectEquation(safeCells);


            Tuple<List<CellManager>,List<int[]>> resultTuple= gameObject.GetComponent<SolveMineSweeper>().SolveMineSweeperMethodNew(safeCellWithEquation);
            if(resultTuple==null){return;}
            CellGroup cellGroup = new CellGroup();
            cellGroup.cellManagers = resultTuple.Item1;
            cellGroup.solutions=resultTuple.Item2;
            /*
            Debug.Log("cellManager Num is" +cellGroup.cellManagers.Count()+" solutionNum is"+ cellGroup.solutions.Count());
            */
            allSurrondCellGroups.Add(cellGroup);


        }
        CellManagerDataStore();
        
        //otherProb
        otherProb = OtherProb();
        foreach (var cellManager in cellList)
        {
            if (!surroundCell.Contains(cellManager))
            {
                cellManager.isSurrondCell = false;
                cellManager.Probobability = otherProb;
            }
        }

        
        

 
        
        if (allSurrondCellGroups != null && allSurrondCellGroups.Count > 0)
        {
            solutionNum = 1;
            foreach (var cellManagerGroup in allSurrondCellGroups)
            {
                solutionNum *= cellManagerGroup.solutions.Count;
            }
            solutionNumText.text = solutionNum.ToString();

        }
        else
        {
            solutionNumText.text = "0";
            solutionNum = 0;
        }
        AI_PSEQ_Calculate();

    }

    public void UpdateUI()
    {
        ChangeColor();
        if (!AFNOrAMN())
        {
            PSEQ_Visulize();
        }
        SetProbUI();
    }
    
    public void CellManagerDataStore()
    {
        foreach (var cellManager in cellList)
        {
            cellManager.cellGroup = null;
        }
        
        foreach (var cellGroup in allSurrondCellGroups)
        {
            foreach (var cellManager in cellGroup.cellManagers)
            {
                cellManager.cellGroup = cellGroup;
                /*Debug.Log(cellManager.cellGroup.cellManagers.Count);
                Debug.Log(cellManager.cellGroup.solutions.Count);*/
            }
        }
    }
    public List<Tuple<List<CellManager>, int>> CollectEquation(List<CellManager> safeCellListTemp)
    {
        safeCellWithEquation.Clear(); // 清空 surroundCell 列表

        // 遍历所有 safeCell 中的 cell
        foreach (var safe in safeCellListTemp)
        {
            int x = safe.position.Item1; // 当前 cell 的 x 坐标
            int y = safe.position.Item2;

            List<CellManager> cellManagersTemp = new List<CellManager>();

            // 遍历周围 8 个方向
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // 跳过自身
                    if (i == 0 && j == 0)
                        continue;
                    int newX = x + i;
                    int newY = y + j;

                    
                    // 确保坐标没有超出边界
                    if (newX >= 0 && newX < cellList.GetLength(0) && newY >= 0 && newY < cellList.GetLength(1))
                    {
                        var surroundingCell = cellList[newX, newY];

                        // 检查是否是未打开的 cell
                        if (!surroundingCell.isOpen)
                        {
                            cellManagersTemp.Add(surroundingCell);
                        }

                    }
                }
            }

            Tuple<List<CellManager>, int> tuple = new Tuple<List<CellManager>, int>(cellManagersTemp, safe.boomNumber);
            /*Debug.Log(safe.boomNumber);*/
            safeCellWithEquation.Add(tuple); // 将未打开的 cell 添加到 surroundCell
        }

        return safeCellWithEquation;

    }

    public float otherProb;
    float OtherProb()
    {
        int allCell = cellList.GetLength(0) * cellList.GetLength(1);
        int openedCell = 0;
        foreach (var cellManager in cellList)
        {
            if (cellManager.isOpen)
            {
                openedCell += 1;
            }
        }
        int otherNum = allCell - openedCell - surroundCell.Count;

        float restMine;
        Dictionary<int, int> oneCountSummary = gameObject.GetComponent<SolveMineSweeper>().oneCountSummary;
        int conditionNum = 0;
        foreach (var VARIABLE in oneCountSummary)
        {
            conditionNum += VARIABLE.Value;
        }

        if (debugMode)
        {
            Debug.Log("一共有多少种可能呢？" + conditionNum);
        }
        //一共有多少种可能呢？
        float expectation = 0;

        // 输出统计结果
        foreach (var pair in oneCountSummary)
        {
            if (debugMode)
            {
                Debug.Log(pair.Key + " 个 1 出现了 " + pair.Value + " 次");
            }
        }

        foreach (var VARIABLE in oneCountSummary)
        {
            if (debugMode)
            {
                Debug.Log(VARIABLE.Key * ((float)VARIABLE.Value / conditionNum));
                Debug.Log("那么，已知区域的雷的期望为。。" + expectation);
            }
            expectation += VARIABLE.Key * ((float)VARIABLE.Value / conditionNum);
        }


        restMine = GameManager.instance.mineNumber - expectation;
        otherProb = restMine / otherNum;
        /*otherProb *= 100;*/
        if (debugMode)
        {
            Debug.Log("那么，已知区域的雷的期望为。。" + expectation);
            Debug.Log("所以这就是概率了" + otherProb);
        }
        return otherProb < 0 ? 0 : otherProb;
    }

    #endregion
    
    #region UI処理

    public void ChangeColor()
    {
        for (int i = 0; i < allSurrondCellGroups.Count; i++)
        {
            foreach (var singleCellManager in allSurrondCellGroups[i].cellManagers)
            {
                Image image = singleCellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.gameObject
                    .GetComponent<Image>();
                if (i==0)
                {
                    image.color = Color.blue;
                }else if (i == 1)
                {
                    image.color = Color.magenta;
                }
                else if (i==2)
                {
                    image.color = Color.cyan;
                }else if (i==3)
                {
                    image.color = Color.yellow;
                }
                else
                {
                    image.color = Color.black;
                }
            }
        }
    }
    public void SetProbUI()
    {
        //init
        foreach (var cellManager in cellList)
        {
            cellManager.isSurrondCell = false;

            if (cellManager.isOpen)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
            }
        }


        foreach (var cellManager in surroundCell)
        {
            cellManager.cellModel.GetComponent<SingleCellDisplay>().prob.GetComponent<TextMeshProUGUI>().text =
                (100 * cellManager.Probobability).ToString("F3") + "%";
            cellManager.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(true);
            cellManager.isSurrondCell = true;
            if (cellManager.Probobability < 0.01f)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.gameObject.GetComponent<Image>()
                    .color = Color.green;   

            }
            else if (cellManager.Probobability > 0.9999f)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.gameObject.GetComponent<Image>()
                    .color = Color.red;
            }
        }


        foreach (var cellManager in (GameManager.instance.cellList))
        {
            if (!cellManager.isOpen)
            {
                if (!cellManager.isSurrondCell)
                {
                    cellManager.cellModel.GetComponent<SingleCellDisplay>().prob.GetComponent<TextMeshProUGUI>().text =
                        (100*otherProb).ToString("F3") + "%";
                    cellManager.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(true);
                }

                    

            }

        }

        /*foreach (var VARIABLE in surroundCell)
        {
            Debug.Log(VARIABLE.Probobability);
        }*/
    }


    public void CloseProb()
    {
        foreach (var cellManager in cellList)
        {
            cellManager.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
        }
    }

    public void ShowProb()
    {
        foreach (var cellManager in cellList)
        {
            if (!cellManager.isOpen)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(true);
            }
        }
    }
    #endregion

    #region tool

    public List<CellManager>  ExploreSurroundings( List<CellManager> safeCell)
    {
        List<CellManager> surroundCellListTemp = new List<CellManager>();
        // 遍历所有safeCell中的cell
        foreach (var safe in safeCell)
        {
            int x = safe.position.Item1; // 当前cell的x坐标
            int y = safe.position.Item2;

            // 遍历周围8个方向
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // 跳过自身
                    if (i == 0 && j == 0)
                        continue;

                    int newX = x + i;
                    int newY = y + j;

                    // 确保坐标没有超出边界
                    if (newX >= 0 && newX < cellList.GetLength(0) && newY >= 0 && newY < cellList.GetLength(1))
                    {
                        var surroundingCell = cellList[newX, newY];

                        // 检查是否是未打开的cell
                        if (!surroundingCell.isOpen)
                        {
                            if (!surroundCellListTemp.Contains(surroundingCell))
                            {
                                surroundCellListTemp.Add(surroundingCell);
                            }
                        }
                    }
                }
            }
        }

        return surroundCellListTemp;
    }
    public int  ExploreSurroundingsCount( CellManager safeCell)
    {
        List<CellManager> surroundCellListTemp = new List<CellManager>();
        int x = safeCell.position.Item1; // 当前cell的x坐标
        int y = safeCell.position.Item2;

        // 遍历周围8个方向
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                // 跳过自身
                if (i == 0 && j == 0)
                    continue;

                int newX = x + i;
                int newY = y + j;

                // 确保坐标没有超出边界
                if (newX >= 0 && newX < cellList.GetLength(0) && newY >= 0 && newY < cellList.GetLength(1))
                {
                    var surroundingCell = cellList[newX, newY];

                    // 检查是否是未打开的cell
                    if (!surroundingCell.isOpen)
                    {
                        if (!surroundCellListTemp.Contains(surroundingCell))
                        {
                            surroundCellListTemp.Add(surroundingCell);
                        }
                    }
                }
            }
        }

        return surroundCellListTemp.Count;
    }
    public List<CellManager> ExploreSurroundings( CellManager safeCell)
    {
        List<CellManager> surroundCellListTemp = new List<CellManager>();
        int x = safeCell.position.Item1; // 当前cell的x坐标
        int y = safeCell.position.Item2;

        // 遍历周围8个方向
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                // 跳过自身
                if (i == 0 && j == 0)
                    continue;

                int newX = x + i;
                int newY = y + j;

                // 确保坐标没有超出边界
                if (newX >= 0 && newX < cellList.GetLength(0) && newY >= 0 && newY < cellList.GetLength(1))
                {
                    var surroundingCell = cellList[newX, newY];

                    // 检查是否是未打开的cell
                    if (!surroundingCell.isOpen)
                    {
                        if (!surroundCellListTemp.Contains(surroundingCell))
                        {
                            surroundCellListTemp.Add(surroundingCell);
                        }
                    }
                }
            }
        }

        return surroundCellListTemp;
    }
    
    /// <summary>
    /// 勝率データをフォルダに出力
    /// </summary>
    /// <param name="stringList"></param>
    /// <param name="fileName"></param>
    void WriteListToTxt(List<string> stringList, string fileName)
    {
        // 定义 Datas 文件夹路径
        string folderPath = Directory.GetCurrentDirectory() + "/Datas";

        // 如果 Datas 文件夹不存在，则创建它
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"创建文件夹: {folderPath}");
        }

        // 拼接完整的文件路径
        string filePath = Path.Combine(folderPath, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                foreach (string line in stringList)
                {
                    writer.WriteLine(line);
                }
            }

            Debug.Log($"文件已成功写入: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"写入文件失败: {ex.Message}");
        }
    }



    #endregion
}
