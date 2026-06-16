using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.IO;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    [DllImport("sqlite3", EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db);

    [DllImport("sqlite3", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_close(IntPtr db);

    [DllImport("sqlite3", EntryPoint = "sqlite3_exec", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_exec(IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string sql, IntPtr callback, IntPtr args, out IntPtr errmsg);

    [DllImport("sqlite3", EntryPoint = "sqlite3_free", CallingConvention = CallingConvention.Cdecl)]
    private static extern void sqlite3_free(IntPtr ptr);

    private string dbPath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDatabase()
    {
        // Place database file in persistent data path
        dbPath = Path.Combine(Application.persistentDataPath, "analytics.db");
        Debug.Log("SQLite DB path: " + dbPath);

        // Create logging table if it does not exist
        ExecuteSQL(
            "CREATE TABLE IF NOT EXISTS ChallengeLogs (" +
            "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "challengeName TEXT, " +
            "isSuccess INTEGER, " +
            "timeSpent REAL, " +
            "skillIndex REAL, " +
            "timestamp TEXT" +
            ");"
        );
    }

    public void LogChallenge(string name, int success, float timeSpent, float skillIndex)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // Safe string formatting for sqlite query
        string sql = string.Format(
            "INSERT INTO ChallengeLogs (challengeName, isSuccess, timeSpent, skillIndex, timestamp) " +
            "VALUES ('{0}', {1}, {2:F3}, {3:F3}, '{4}');",
            name.Replace("'", "''"), success, timeSpent, skillIndex, timestamp
        );
        ExecuteSQL(sql);
        Debug.LogFormat("Logged challenge to DB: {0}, success: {1}, time: {2:F1}s, skill: {3:F2}", name, success, timeSpent, skillIndex);
    }

    private void ExecuteSQL(string sql)
    {
        IntPtr db = IntPtr.Zero;
        IntPtr errmsg = IntPtr.Zero;
        try
        {
            int rc = sqlite3_open(dbPath, out db);
            if (rc != 0)
            {
                Debug.LogError("Failed to open SQLite database: " + rc);
                return;
            }

            rc = sqlite3_exec(db, sql, IntPtr.Zero, IntPtr.Zero, out errmsg);
            if (rc != 0)
            {
                string error = Marshal.PtrToStringAnsi(errmsg);
                Debug.LogError("SQLite SQL Error: " + error + " | Query: " + sql);
                sqlite3_free(errmsg);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception executing SQL query: " + e.Message);
        }
        finally
        {
            if (db != IntPtr.Zero)
            {
                sqlite3_close(db);
            }
        }
    }
}
