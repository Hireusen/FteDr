#pragma warning disable IDE0060, CS0162

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.IO;

/// <summary>
/// 디버깅 기능을 제공하는 유틸리티 클래스입니다.
/// </summary>
public static class UDebug
{
    #region ─────────────────────────▶ 내부 멤버 ◀─────────────────────────
    // 로그 출력 여부를 결정합니다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const bool ENABLE_LOG = true;
    private const bool ENABLE_ONCE = false;
    private const bool ENABLE_PAUSE = false;
#else
    private const bool ENABLE_LOG = false;
    private const bool ENABLE_ONCE = true;
    private const bool ENABLE_PAUSE = false;
#endif

    private static readonly HashSet<string> _logHistory = new();
    private static GUIStyle _rectStyle;

    // 플레이 모드가 시작될 때 자동으로 로그 기록을 초기화합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetHistory() => _logHistory.Clear();

    // 매개변수를 받아 디버그 로그를 출력합니다.
    [HideInCallstack]
    private static void LogInternal
        (string msg, string file, int line, LogType type = LogType.Log,
        System.Object obj = null, bool once = ENABLE_ONCE)
    {
        if (!ENABLE_LOG) {
            return;
        }
        // 일회성 검사
        if (once) {
            string key = file + line;
            if (_logHistory.Contains(key)) {
                return;
            }
            _logHistory.Add(key);
        }
        // 메시지 조립
        string sColor = GetColorByLogType(type);
        string fileName = Path.GetFileName(file).Replace(".cs", "");
        msg = $"<color={sColor}>[{fileName}" + msg;
        // 유니티 로그 분기
        switch (type) {
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                UnityEngine.Debug.LogError(msg);
                break;
            case LogType.Warning:
                UnityEngine.Debug.LogWarning(msg);
                break;
            default:
                UnityEngine.Debug.Log(msg);
                break;
        }
        if (ENABLE_PAUSE) {
            UnityEngine.Debug.Break();
        }
    }

    // 로그 타입을 받아서 문자열 색깔을 반환
    private static string GetColorByLogType(LogType type)
    {
        switch (type) {
            case LogType.Exception:
                return "red";
            case LogType.Assert:
                return "red";
            case LogType.Error:
                return "red";
            case LogType.Warning:
                return "yellow";
            case LogType.Log:
                return "cyan";
        }
        return "white";
    }
    #endregion

    #region ─────────────────────────▶ 로그 함수 ◀─────────────────────────
    /// <summary>
    /// 오브젝트가 Null 또는 Fake Null일 경우 True를 반환하고 로그를 출력합니다.
    /// </summary>
    [HideInCallstack]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull<T>(
        T obj,
        LogType logType = LogType.Warning,
        System.Object target = null,
        [CallerArgumentExpression("obj")] string objName = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        bool isNull = false;
        // 검사
        if (obj is UnityEngine.Object unityObj) {
            isNull = unityObj == null;
        }
        else {
            isNull = obj == null;
        }
        // 로그
        if (isNull && ENABLE_LOG) {
            string msg = $"]</color> 오브젝트({objName})가 Null입니다.";
            LogInternal(msg, file, line, logType, target);
        }
        return isNull;
    }

    /// <summary>
    /// 조건이 True일 경우 True를 반환하고 로그를 출력합니다.
    /// </summary>
    [HideInCallstack]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTrue(
        bool condition,
        LogType logType = LogType.Warning,
        System.Object target = null,
        [CallerArgumentExpression("condition")] string msg = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (condition) {
            if (ENABLE_LOG) {
                msg = $"]</color> 조건식({msg})이 참입니다.";
                LogInternal(msg, file, line, logType, target);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 조건이 False일 경우 True를 반환하고 로그를 출력합니다.
    /// </summary>
    [HideInCallstack]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFalse(
        bool condition,
        LogType logType = LogType.Warning,
        System.Object target = null,
        [CallerArgumentExpression("condition")] string msg = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (!condition) {
            if (ENABLE_LOG) {
                msg = $"]</color> 조건식({msg})이 부정입니다.";
                LogInternal(msg, file, line, logType, target);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 로그를 출력합니다.
    /// </summary>
    [HideInCallstack]
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Print(
        object msg,
        LogType logType = LogType.Log,
        System.Object target = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        string sMsg = $"]</color> {msg}";
        LogInternal(sMsg, file, line, logType, target);
    }

    /// <summary>
    /// 로그를 출력합니다. (log 변수 편의성)
    /// </summary>
    [HideInCallstack]
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(
        bool printLog,
        object msg,
        LogType logType = LogType.Log,
        System.Object target = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (printLog) {
            string sMsg = $"]</color> {msg}";
            LogInternal(sMsg, file, line, logType, target);
        }
    }

    /// <summary>
    /// 로그를 출력합니다.
    /// </summary>
    [HideInCallstack]
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void PrintOnce(
        object msg,
        LogType logType = LogType.Log,
        System.Object target = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        string sMsg = $"]</color> {msg}";
        LogInternal(sMsg, file, line, logType, target, true);
    }
    #endregion

    #region ─────────────────────────▶ 레이 함수 ◀─────────────────────────
    /// <summary>
    /// 초록색 디버그 레이를 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Ray(Vector3 startPos, Vector3 dir, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(startPos, dir, Color.green, duration, depthTest);
    }

    /// <summary>
    /// 디버그 레이를 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Ray(Vector3 startPos, Vector3 dir, Color color, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(startPos, dir, color, duration, depthTest);
    }

    /// <summary>
    /// 초록색 디버그 레이를 앞으로 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void FrontRay(Vector3 startPos, float distance, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(startPos, distance * Vector3.forward, Color.green, duration, depthTest);
    }

    /// <summary>
    /// 디버그 레이를 앞으로 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void FrontRay(Vector3 startPos, float distance, Color color, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(startPos, distance * Vector3.forward, color, duration, depthTest);
    }

    /// <summary>
    /// 초록색 디버그 레이를 위로 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void UpRay(Vector3 startPos, float distance, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(startPos, distance * Vector3.up, Color.green, duration, depthTest);
    }

    /// <summary>
    /// 디버그 레이를 위로 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void UpRay(Vector3 startPos, float distance, Color color, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(startPos, distance * Vector3.up, color, duration, depthTest);
    }

    /// <summary>
    /// 초록색 디버그 레이를 아래로 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void DownRay(Vector3 startPos, float distance, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(startPos, distance * Vector3.down, Color.green, duration, depthTest);
    }

    /// <summary>
    /// 디버그 레이를 아래로 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void DownRay(Vector3 startPos, float distance, Color color, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(startPos, distance * Vector3.down, color, duration, depthTest);
    }

    /// <summary>
    /// 초록색 디버그 레이를 각도가 가리키는 방향으로 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void DegreeRay(Vector2 startPos, float distance, float degree, float duration = 0f, bool depthTest = true)
    {
        float radian = degree * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radian);
        float sin = Mathf.Sin(radian);
        Vector2 dir = new Vector2(cos, sin);
        UnityEngine.Debug.DrawRay(startPos, dir * distance, Color.green, duration, depthTest);
    }

    /// <summary>
    /// 디버그 레이를 각도가 가리키는 방향으로 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void DegreeRay(Vector2 startPos, float distance, float degree, Color color, float duration = 0f, bool depthTest = true)
    {
        float radian = degree * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radian);
        float sin = Mathf.Sin(radian);
        Vector2 dir = new Vector2(cos, sin);
        UnityEngine.Debug.DrawRay(startPos, dir * distance, color, duration, depthTest);
    }

    /// <summary>
    /// 초록색 디버그 라인을 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Line(Vector3 pos1, Vector3 pos2, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawLine(pos1, pos2, Color.green, duration, depthTest);
    }

    /// <summary>
    /// 디버그 라인을 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Line(Vector3 start, Vector3 end, Color color, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
    }

    /// <summary>
    /// 초록색 디버그 라인을 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Line(Vector2 pos1, Vector2 pos2, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawLine(pos1, pos2, Color.green, duration, depthTest);
    }

    /// <summary>
    /// 디버그 라인을 그립니다.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Line(Vector2 start, Vector2 end, Color color, float duration = 0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
    }

    /// <summary>
    /// 초록색 디버그 레이를 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Ray(bool drawRay, Vector3 startPos, Vector3 dir, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawRay(startPos, dir, Color.green, duration, depthTest);
        }
    }

    /// <summary>
    /// 디버그 레이를 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Ray(bool drawRay, Vector3 startPos, Vector3 dir, Color color, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawRay(startPos, dir, color, duration, depthTest);
        }
    }

    /// <summary>
    /// 초록색 디버그 레이를 앞으로 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void FrontRay(bool drawRay, Vector3 startPos, float distance, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawRay(startPos, distance * Vector3.forward, Color.green, duration, depthTest);
        }
    }

    /// <summary>
    /// 디버그 레이를 앞으로 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void FrontRay(bool drawRay, Vector3 startPos, float distance, Color color, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawRay(startPos, distance * Vector3.forward, color, duration, depthTest);
        }
    }

    /// <summary>
    /// 초록색 디버그 레이를 위로 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void UpRay(bool drawRay, Vector3 startPos, float distance, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawRay(startPos, distance * Vector3.up, Color.green, duration, depthTest);
        }
    }

    /// <summary>
    /// 디버그 레이를 위로 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void UpRay(bool drawRay, Vector3 startPos, float distance, Color color, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawRay(startPos, distance * Vector3.up, color, duration, depthTest);
        }
    }

    /// <summary>
    /// 초록색 디버그 레이를 아래로 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void DownRay(bool drawRay, Vector3 startPos, float distance, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawRay(startPos, distance * Vector3.down, Color.green, duration, depthTest);
        }
    }

    /// <summary>
    /// 디버그 레이를 아래로 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void DownRay(bool drawRay, Vector3 startPos, float distance, Color color, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawRay(startPos, distance * Vector3.down, color, duration, depthTest);
        }
    }

    /// <summary>
    /// 초록색 디버그 레이를 각도가 가리키는 방향으로 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void DegreeRay(bool drawRay, Vector2 startPos, float distance, float degree, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            float radian = degree * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radian);
            float sin = Mathf.Sin(radian);
            Vector2 dir = new Vector2(cos, sin);
            UnityEngine.Debug.DrawRay(startPos, dir * distance, Color.green, duration, depthTest);
        }
    }

    /// <summary>
    /// 디버그 레이를 각도가 가리키는 방향으로 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void DegreeRay(bool drawRay, Vector2 startPos, float distance, float degree, Color color, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            float radian = degree * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radian);
            float sin = Mathf.Sin(radian);
            Vector2 dir = new Vector2(cos, sin);
            UnityEngine.Debug.DrawRay(startPos, dir * distance, color, duration, depthTest);
        }
    }

    /// <summary>
    /// 초록색 디버그 라인을 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Line(bool drawRay, Vector3 pos1, Vector3 pos2, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawLine(pos1, pos2, Color.green, duration, depthTest);
        }
    }

    /// <summary>
    /// 디버그 라인을 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Line(bool drawRay, Vector3 start, Vector3 end, Color color, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
        }
    }

    /// <summary>
    /// 초록색 디버그 라인을 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Line(bool drawRay, Vector2 pos1, Vector2 pos2, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawLine(pos1, pos2, Color.green, duration, depthTest);
        }
    }

    /// <summary>
    /// 디버그 라인을 그립니다. (ray 변수 편의성)
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Line(bool drawRay, Vector2 start, Vector2 end, Color color, float duration = 0f, bool depthTest = true)
    {
        if (drawRay) {
            UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
        }
    }
    #endregion

    #region ─────────────────────────▶ GUI 함수 ◀─────────────────────────
    /// <summary>
    /// GUIStyle 변수에 접근합니다.
    /// </summary>
    public static GUIStyle RectStyle {
        get {
            if (_rectStyle == null) {
                _rectStyle = new GUIStyle();
                _rectStyle.normal.textColor = Color.black;
                _rectStyle.alignment = TextAnchor.MiddleCenter;
            }
            return _rectStyle;
        }
    }

    public enum EWhere { LeftUp, Up, RightUp, Left, Center, Right, LeftDown, Down, RightDown }
    /// <summary>
    /// 게임 화면에 텍스트를 출력합니다.
    /// OnGUI 이벤트 내부에서 호출해야 합니다.
    /// </summary>
    public static void DrawText(string text, int fontSize = 30, EWhere where = EWhere.Up)
    {
        float w = Screen.width, h = Screen.height;
        float halfW = w * 0.5f, halfH = h * 0.5f;
        float x = 0, y = 0;
        // 가로 정렬
        switch (where) {
            case EWhere.LeftUp:
            case EWhere.Left:
            case EWhere.LeftDown:
            case EWhere.Up:
            case EWhere.Center:
            case EWhere.Down:
                x = 0; break;
            case EWhere.RightUp:
            case EWhere.Right:
            case EWhere.RightDown:
                x = halfW; break;
        }
        // 세로 정렬
        switch (where) {
            case EWhere.LeftUp: case EWhere.Up: case EWhere.RightUp: y = 0; break;
            case EWhere.Left: case EWhere.Center: case EWhere.Right: y = 0; break; // 전체 높이 사용
            case EWhere.LeftDown: case EWhere.Down: case EWhere.RightDown: y = halfH; break;
        }
        // 크기 설정
        float rectW = (where == EWhere.Center || where == EWhere.Up || where == EWhere.Down) ? w : halfW;
        float rectH = (where == EWhere.Center || where == EWhere.Left || where == EWhere.Right) ? h : halfH;

        RectStyle.fontSize = fontSize;
        GUI.Label(new Rect(x, y, rectW, rectH), text, RectStyle);
    }
    #endregion
}

// C# 최신 문법을 구버전에서 쓰기 위한 코드입니다.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }
        public string ParameterName { get; }
    }
}
