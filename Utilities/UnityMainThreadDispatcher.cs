using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Hikaria.AdminSystem.Utilities
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> _executionQueue = new();

        public void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }

        public static void Enqueue(IEnumerator action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(() =>
                {
                    _instance.StartCoroutine(action.WrapToIl2Cpp());
                });
            }
        }

        public static void Enqueue(Action action)
        {
            Enqueue(_instance.ActionWrapper(action));
        }

        public static Task EnqueueAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            void WrappedAction()
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            Enqueue(_instance.ActionWrapper(WrappedAction));
            return tcs.Task;
        }


        private IEnumerator ActionWrapper(Action a)
        {
            a();
            yield return null;
        }

        private static UnityMainThreadDispatcher _instance = null;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        void OnDestroy()
        {
            _instance = null;
        }

        public void Init()
        {
            throw new NotImplementedException();
        }
    }
}