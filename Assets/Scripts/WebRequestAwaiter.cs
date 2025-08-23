using UnityEngine.Networking;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// UnityWebRequest�� awaitable�� ������ִ� Ȯ�� �޼ҵ�
public static class WebRequestExtensions
{
    public static TaskAwaiter<UnityWebRequest.Result> GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest.Result>();
        asyncOp.completed += _ => tcs.SetResult(asyncOp.webRequest.result);
        return tcs.Task.GetAwaiter();
    }
}