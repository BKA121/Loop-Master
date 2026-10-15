using System;
using DG.Tweening;
using UnityEngine;

namespace LoopMaster.View
{
    /// <summary>
    /// Tiện ích bay thuần: đưa <paramref name="target"/> từ vị trí hiện tại tới đích theo vòng cung (Bezier bậc 2,
    /// điểm điều khiển nâng cao thêm <paramref name="arcHeight"/>), xoay dần sang <paramref name="endRotation"/>.
    /// Tween gắn target là chính Transform nên <c>DOTween.Kill(transform)</c> dọn được khi trả về pool.
    /// </summary>
    public static class BlockFlight
    {
        public static Tween Fly(Transform target, Vector3 end, Quaternion endRotation,
            float flightTime, float arcHeight, Ease ease, Action onLanded)
        {
            Vector3 start = target.position;
            Quaternion startRotation = target.rotation;
            Vector3 control = (start + end) * 0.5f + Vector3.up * arcHeight;

            return DOVirtual.Float(0f, 1f, flightTime, u =>
                {
                    float inv = 1f - u;
                    target.position = inv * inv * start + 2f * inv * u * control + u * u * end;
                    target.rotation = Quaternion.Slerp(startRotation, endRotation, u);
                })
                .SetEase(ease)
                .SetTarget(target)
                .OnComplete(() => onLanded?.Invoke());
        }
    }
}
