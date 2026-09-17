using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Zoom
{
    internal static class UIElementsAnimator
    {
        internal static IVisualElementScheduledItem Animate(
            VisualElement element,
            Vector2 startValue,
            Vector2 endValue,
            TimeSpan duration,
            Func<float, float> easingFunction,
            Action<Vector2> onUpdate)
        {
            return Animate(
                element,
                startValue,
                endValue,
                duration,
                easingFunction,
                Vector2.Lerp,
                onUpdate);
        }

        internal static IVisualElementScheduledItem Animate(
            VisualElement element,
            float startValue,
            float endValue,
            TimeSpan duration,
            Func<float, float> easingFunction,
            Action<float> onUpdate)
        {
            return Animate(
                element,
                startValue,
                endValue,
                duration,
                easingFunction,
                Mathf.Lerp,
                onUpdate);
        }

        static IVisualElementScheduledItem Animate<T>(
            VisualElement element,
            T startValue,
            T endValue,
            TimeSpan duration,
            Func<float, float> easingFunction,
            Func<T, T, float, T> interpolator,
            Action<T> onUpdate)
        {
            float durationMs = (float)duration.TotalMilliseconds;
            float startTime = Time.realtimeSinceStartup * 1000f;

            return element.schedule.Execute(() =>
            {
                float elapsed = Time.realtimeSinceStartup * 1000f - startTime;
                float t = Mathf.Clamp01(elapsed / durationMs);
                float easedT = easingFunction(t);

                T currentValue = interpolator(startValue, endValue, easedT);
                onUpdate(currentValue);

            }).Every(16).Until(() =>
            {
                float elapsed = Time.realtimeSinceStartup * 1000f - startTime;
                return elapsed >= durationMs;
            });
        }
    }

    internal static class Easing
    {
        internal static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    }
}
