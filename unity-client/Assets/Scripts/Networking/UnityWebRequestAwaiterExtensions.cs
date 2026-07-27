using System;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace EmoScape.Networking
{
    /// <summary>
    /// Unity 2022.3's UnityWebRequestAsyncOperation has no native `await` support
    /// (that lands with Awaitable in 2023.1+), so this provides a minimal awaiter.
    /// </summary>
    public static class UnityWebRequestAwaiterExtensions
    {
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation op) => new UnityWebRequestAwaiter(op);
    }

    public struct UnityWebRequestAwaiter : INotifyCompletion
    {
        readonly UnityWebRequestAsyncOperation asyncOp;

        public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp)
        {
            this.asyncOp = asyncOp;
        }

        public bool IsCompleted => asyncOp.isDone;

        public void GetResult() { }

        public void OnCompleted(Action continuation) => asyncOp.completed += _ => continuation();
    }
}
