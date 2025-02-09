using System.Diagnostics; // Stopwatch 所在命名空间
using UnityEngine;
using Debug = UnityEngine.Debug; // 使用 Debug.Log 输出日志

public static class TimerUtility
{
    private static Stopwatch stopwatch = new Stopwatch();

    /// <summary>
    /// 重新开始一个从0开始的计时器，并计时
    /// </summary>
    public static void StartTimer()
    {
        stopwatch.Restart(); // 重置并重新启动计时器
    }

    /// <summary>
    /// 停止计时并输出耗时
    /// </summary>
    /// <param name="methodName">方法名或标识符</param>
    public static void StopAndLog(string methodName)
    {
        stopwatch.Stop(); // 停止计时
        /*Debug.Log($"{methodName} 耗时：{stopwatch.ElapsedMilliseconds} ms");*/
    }
}