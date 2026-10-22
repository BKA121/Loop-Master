using System;
using DG.Tweening;
using UnityEngine;

namespace LoopMaster.View
{
    /// <summary>
    /// Tiện ích bay thuần: đưa <paramref name="target"/> từ vị trí hiện tại tới đích theo vòng cung (Bezier bậc 2,
    /// điểm điều khiển nâng cao thêm <paramref name="arcHeight"/>), xoay dần sang <paramref name="endRotation"/>.
    /// Tween gắn target là chính Transform nên <c>DOTween.Kill(transform)</c> dọn được khi trả về pool.
    /// Nếu truyền <paramref name="endScaleX"/>, scale X của target được nội suy cùng tham số với xoay nên hai chuyển
    /// động luôn đồng bộ; <paramref name="startScaleX"/> bỏ trống = bắt đầu từ scale X hiện tại (không bật đột ngột).
    /// </summary>
    public static class BlockFlight
    {
        public static Tween Fly(Transform target, Vector3 end, Quaternion endRotation,
            float flightTime, float arcHeight, Ease ease, Action onLanded,
            float? startScaleX = null, float? endScaleX = null)
        {
            Vector3 start = target.position;
            Quaternion startRotation = target.rotation;
            Vector3 control = (start + end) * 0.5f + Vector3.up * arcHeight;
            float fromScaleX = startScaleX ?? target.localScale.x;

            return DOVirtual.Float(0f, 1f, flightTime, u =>
                {
                    float inv = 1f - u;
                    target.position = inv * inv * start + 2f * inv * u * control + u * u * end;
                    target.rotation = Quaternion.Slerp(startRotation, endRotation, u);

                    if (endScaleX.HasValue)
                    {
                        Vector3 scale = target.localScale;
                        scale.x = Mathf.Lerp(fromScaleX, endScaleX.Value, u);
                        target.localScale = scale;
                    }
                })
                .SetEase(ease)
                .SetTarget(target)
                .OnComplete(() => onLanded?.Invoke());
        }
    }
}
