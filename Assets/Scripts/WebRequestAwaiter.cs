using UnityEngine.Networking;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// UnityWebRequest를 awaitable로 만들어주는 확장 메소드
public static class WebRequestExtensions
{
    public static TaskAwaiter<UnityWebRequest.Result> GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest.Result>();
        asyncOp.completed += _ => tcs.SetResult(asyncOp.webRequest.result);
        return tcs.Task.GetAwaiter();
    }
}