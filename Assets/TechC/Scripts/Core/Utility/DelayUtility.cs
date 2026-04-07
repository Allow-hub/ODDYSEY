using System;
using System.Collections;
using UnityEngine;
// using Cysharp.Threading.Tasks;

namespace TechC.Core.Util
{
    public static class DelayUtility
    {
        // ================================
        // 非ポーズ対応：UniTask版
        // ================================

        // public static async UniTask RunAfterDelay(float delaySeconds, Action callback)
        // {
        //     await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds));
        //     callback?.Invoke();
        // }

        // public static async UniTask RunAfterDelay(float delaySeconds, Func<UniTask> asyncCallback)
        // {
        //     await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds));
        //     if (asyncCallback != null)
        //     {
        //         await asyncCallback();
        //     }
        // }

        // ================================
        // 非ポーズ対応：Coroutine版
        // ================================

        public static IEnumerator RunAfterDelayCoroutine(float delaySeconds, Action callback)
        {
            yield return new WaitForSeconds(delaySeconds);
            callback?.Invoke();
        }

        public static IEnumerator RunAfterDelayCoroutine(float delaySeconds, Func<IEnumerator> coroutineCallback)
        {
            yield return new WaitForSeconds(delaySeconds);
            if (coroutineCallback != null)
            {
                yield return coroutineCallback();
            }
        }

        public static Coroutine StartDelayedAction(MonoBehaviour monoBehaviour, float delaySeconds, Action callback)
        {
            return monoBehaviour.StartCoroutine(RunAfterDelayCoroutine(delaySeconds, callback));
        }

        public static Coroutine StartDelayedCoroutine(MonoBehaviour monoBehaviour, float delaySeconds, Func<IEnumerator> coroutineCallback)
        {
            return monoBehaviour.StartCoroutine(RunAfterDelayCoroutine(delaySeconds, coroutineCallback));
        }

        // ================================
        // ポーズ対応：UniTask版
        // ================================

        // public static async UniTask RunAfterDelayWithPause(float delaySeconds, Action callback, Func<bool> isPausedFunc)
        // {
        //     float elapsed = 0f;
        //     while (elapsed < delaySeconds)
        //     {
        //         if (isPausedFunc != null && isPausedFunc())
        //         {
        //             await UniTask.Yield();
        //             continue;
        //         }
        //         elapsed += Time.deltaTime;
        //         await UniTask.Yield();
        //     }
        //     callback?.Invoke();
        // }

        // public static UniTask StartDelayedActionWithPauseAsync(float delaySeconds, Action callback, Func<bool> isPausedFunc)
        // {
        //     return RunAfterDelayWithPause(delaySeconds, callback, isPausedFunc);
        // }

        // ================================
        // ポーズ対応：Coroutine版
        // ================================

        public static IEnumerator RunAfterDelayCoroutineWithPause(float delaySeconds, Action callback, Func<bool> isPausedFunc)
        {
            float elapsed = 0f;
            while (elapsed < delaySeconds)
            {
                if (isPausedFunc != null && isPausedFunc())
                {
                    yield return null;
                    continue;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            callback?.Invoke();
        }

        public static Coroutine StartDelayedActionWithPause(MonoBehaviour monoBehaviour, float delaySeconds, Func<bool> isPausedFunc, Action callback)
        {
            return monoBehaviour.StartCoroutine(RunAfterDelayCoroutineWithPause(delaySeconds, callback, isPausedFunc));
        }

        // ================================
        // 一定間隔で繰り返し実行（Coroutine）
        // ================================

        public static IEnumerator RunRepeatedly(float duration, float interval, Action callback)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                callback?.Invoke();
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }
        }

        public static Coroutine StartRepeatedAction(MonoBehaviour monoBehaviour, float duration, float interval, Action callback)
        {
            return monoBehaviour.StartCoroutine(RunRepeatedly(duration, interval, callback));
        }

        // ================================
        // ポーズ対応：一定間隔で繰り返し実行（Coroutine）
        // ================================

        public static IEnumerator RunRepeatedlyWithPause(float duration, float interval, Action callback, Func<bool> isPausedFunc)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // ポーズ中は進めない
                if (isPausedFunc != null && isPausedFunc())
                {
                    yield return null;
                    continue;
                }
                callback?.Invoke();
                float intervalElapsed = 0f;
                while (intervalElapsed < interval)
                {
                    if (isPausedFunc != null && isPausedFunc())
                    {
                        yield return null;
                        continue;
                    }
                    intervalElapsed += Time.deltaTime;
                    yield return null;
                }
                elapsed += interval;
            }
        }

        public static Coroutine StartRepeatedActionWithPause(MonoBehaviour monoBehaviour, float duration, float interval, Func<bool> isPausedFunc, Action callback)
        {
            return monoBehaviour.StartCoroutine(RunRepeatedlyWithPause(duration, interval, callback, isPausedFunc));
        }
        // ================================
        // boolで繰り返し制御：Coroutine版（非ポーズ対応）
        // ================================

        public static IEnumerator RunRepeatedlyWhile(Func<bool> shouldContinueFunc, float interval, Action callback)
        {
            while (shouldContinueFunc == null || shouldContinueFunc())
            {
                callback?.Invoke();
                yield return new WaitForSeconds(interval);
            }
        }

        public static Coroutine StartRepeatedActionWhile(MonoBehaviour monoBehaviour, Func<bool> shouldContinueFunc, float interval, Action callback)
        {
            return monoBehaviour.StartCoroutine(RunRepeatedlyWhile(shouldContinueFunc, interval, callback));
        }

        // ================================
        // boolで繰り返し制御：ポーズ対応版（Coroutine）
        // ================================

        public static IEnumerator RunRepeatedlyWhileWithPause(Func<bool> shouldContinueFunc, float interval, Action callback, Func<bool> isPausedFunc)
        {
            while (shouldContinueFunc == null || shouldContinueFunc())
            {
                // ポーズしている間は処理しない
                if (isPausedFunc != null && isPausedFunc())
                {
                    yield return null;
                    continue;
                }

                callback?.Invoke();

                float intervalElapsed = 0f;
                while (intervalElapsed < interval)
                {
                    if (isPausedFunc != null && isPausedFunc())
                    {
                        yield return null;
                        continue;
                    }

                    intervalElapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        public static Coroutine StartRepeatedActionWhileWithPause(MonoBehaviour monoBehaviour, Func<bool> shouldContinueFunc, float interval, Func<bool> isPausedFunc, Action callback)
        {
            return monoBehaviour.StartCoroutine(RunRepeatedlyWhileWithPause(shouldContinueFunc, interval, callback, isPausedFunc));
        }
    }
}
