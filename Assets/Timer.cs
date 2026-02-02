using System;
using System.Collections;
using UnityEngine;

public static class Timer
{
    public static void SetTimeout(Action fn, float delaySeconds)
    {
        CoroutineRunner.Instance.StartCoroutine(Run(fn, delaySeconds));
    }

    private static IEnumerator Run(Action fn, float delay)
    {
        yield return new WaitForSeconds(delay);
        fn?.Invoke();
    }
}
