using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///　目標：すべての既知のセルに隣接するセルを探索する
/// </summary>
public class DFSSearch : MonoBehaviour
{
    public CellManager[,] cellList;

    public List<List<CellManager>> groupedSurroundCells = new List<List<CellManager>>();

    void Start()
    {
        groupedSurroundCells = new List<List<CellManager>>();
        // 初期化してグループ化メソッドを呼び出す
        /*FindOpenCellGroups();*/
    }

    // 主メソッド: 行列を走査して、隣接している isOpen=true のグループを探す
    public List<List<CellManager>> FindSafeCellGroups()
    {
        groupedSurroundCells = new List<List<CellManager>>();

        cellList = GameManager.instance.cellList;

        int rows = cellList.GetLength(0);
        int cols = cellList.GetLength(1);
        bool[,] visited = new bool[rows, cols];  // 訪問済み要素を記録する

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                // 現在のセルが未訪問かつ isOpen=true の場合
                if (cellList[i, j].isOpen && !visited[i, j])
                {
                    /*Debug.Log("探索中: " + i + " " + j);*/
                    List<CellManager> group = new List<CellManager>();
                    DFS(i, j, visited, group);  // DFSを使用して隣接する isOpen のセルを探す
                    if (group.Count() != 0)
                    {
                        groupedSurroundCells.Add(group);  // 見つけたグループをリストに追加する
                    }
                }
            }
        }

        // 結果を出力
        foreach (var group in groupedSurroundCells)
        {
            string groupInfo = "Group: ";
            foreach (var cell in group)
            {
                groupInfo += $"({cell.position.Item2}, {cell.position.Item1}) ";  // position が Tuple<int, int> 型であると仮定
            }
            /*Debug.Log(groupInfo);*/
        }

        groupedSurroundCells = MergeAllSafeCellGroups(groupedSurroundCells);

        // 結果を出力
        foreach (var group in groupedSurroundCells)
        {
            string groupInfo = "Group: ";
            foreach (var cell in group)
            {
                groupInfo += $"({cell.position.Item2}, {cell.position.Item1}) ";  // position が Tuple<int, int> 型であると仮定
            }
            /*Debug.Log(groupInfo);*/
        }

        return groupedSurroundCells;
    }

    // 深さ優先探索 (DFS) メソッド
    void DFS(int x, int y, bool[,] visited, List<CellManager> group)
    {
        visited[x, y] = true;
        if (HasUnopenedNeighbor(cellList[x, y]))
        {
            group.Add(cellList[x, y]);  // 現在のセルをグループに追加する
        }

        int rows = cellList.GetLength(0);
        int cols = cellList.GetLength(1);

        // 現在のセルおよびその周囲8方向（自分自身を含む）を走査
        for (int dx = -1; dx <= 1; dx++) // x方向
        {
            for (int dy = -1; dy <= 1; dy++) // y方向
            {
                // 新しい座標を計算する
                int newX = x + dx;
                int newY = y + dy;

                // 中心点（自分自身）をスキップ
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                // 境界を確認し、次のセルが isOpen=true かつ未訪問の場合
                if (newX >= 0 && newX < rows && newY >= 0 && newY < cols && cellList[newX, newY].isOpen && !visited[newX, newY])
                {
                    DFS(newX, newY, visited, group);
                }
            }
        }
    }

    // 現在のセルの周囲に未開封のセルが存在するか確認
    private bool HasUnopenedNeighbor(CellManager cell)
    {
        int x = cell.position.Item1;  // 現在のセルの x 座標
        int y = cell.position.Item2;  // 現在のセルの y 座標
        int rows = cellList.GetLength(0);
        int cols = cellList.GetLength(1);

        // 周囲8方向を走査
        for (int dx = -1; dx <= 1; dx++) // x方向
        {
            for (int dy = -1; dy <= 1; dy++) // y方向
            {
                // 新しい座標を計算する
                int newX = x + dx;
                int newY = y + dy;

                // 座標が境界内であることを確認
                if (newX >= 0 && newX < rows && newY >= 0 && newY < cols)
                {
                    // 周囲に未開封のセル (isOpen=false) がある場合は true を返す
                    if (!cellList[newX, newY].isOpen)
                    {
                        return true;
                    }
                }
            }
        }

        // 周囲がすべて isOpen=true の場合は false を返す
        return false;
    }

    // 主メソッド：入力された groupedSafeCells リストを受け取り、マージしてマージ後のリストを返す
    public List<List<CellManager>> MergeAllSafeCellGroups(List<List<CellManager>> inputGroupedSafeCells)
    {
        List<List<CellManager>> mergedSafeCells = new List<List<CellManager>>(inputGroupedSafeCells); // マージ後のリストのコピーを作成
        bool merged = true;

        // マージ可能なリストが存在する場合、マージを続ける
        while (merged)
        {
            merged = false;

            // mergedSafeCells 内のすべてのリストを走査し、マージを試みる
            for (int i = 0; i < mergedSafeCells.Count; i++)
            {
                for (int j = i + 1; j < mergedSafeCells.Count; j++)
                {
                    if (AreListsAdjacent(mergedSafeCells[i], mergedSafeCells[j]))
                    {
                        // 2つのリストをマージする
                        List<CellManager> mergedList = new List<CellManager>(mergedSafeCells[i]);
                        foreach (var cell in mergedSafeCells[j])
                        {
                            if (!mergedList.Contains(cell))
                            {
                                mergedList.Add(cell);
                            }
                        }

                        // 元の2つのリストを削除し、マージ後のリストを追加
                        mergedSafeCells.RemoveAt(j); // 後ろのインデックスを先に削除
                        mergedSafeCells.RemoveAt(i); // 前のインデックスを削除
                        mergedSafeCells.Add(mergedList); // マージ後のリストを追加

                        merged = true; // マージが行われたことを示すフラグを設定
                        /*Debug.Log("マージ完了");*/
                        // マージ後のリストも検出されるように、再度リストを走査
                        break;
                    }
                }

                if (merged)
                {
                    break; // 1回のマージ後、リストを再度走査
                }
            }
        }

        return mergedSafeCells; // マージ後のリストを返す
    }

    // 2つのリストが隣接条件を満たす要素を持つか確認
    private bool AreListsAdjacent(List<CellManager> list1, List<CellManager> list2)
    {
        foreach (var cell1 in list1)
        {
            foreach (var cell2 in list2)
            {
                if (IsAdjacent(cell1, cell2))
                {
                    return true; // 隣接要素が見つかった場合は true を返す
                }
            }
        }

        return false; // 隣接要素が見つからない場合は false を返す
    }

    // 2つの CellManager が隣接条件を満たすか確認
    private bool IsAdjacent(CellManager cell1, CellManager cell2)
    {
        int x1 = cell1.position.Item1;
        int y1 = cell1.position.Item2;
        int x2 = cell2.position.Item1;
        int y2 = cell2.position.Item2;

        // x と y 座標の絶対値の差が 2 以下か確認
        return Mathf.Abs(x1 - x2) <= 2 && Mathf.Abs(y1 - y2) <= 2;
    }
}
