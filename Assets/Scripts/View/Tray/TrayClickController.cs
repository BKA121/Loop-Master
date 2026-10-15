using System.Collections.Generic;
using UnityEngine;
using LoopMaster.Core;

namespace LoopMaster.View
{
    /// <summary>
    /// Nhận bấm chuột vào khay: các khối lớn cùng màu liên tiếp ở trên cùng tách và bay lên băng chuyền, tối đa vừa sức chứa còn trống.
    /// Dữ liệu (model) cập nhật ngay khi băng chuyền giữ chỗ thành công; view chỉ chạy hoạt ảnh sau đó.
    /// Nhận băng chuyền/splitter qua <see cref="Initialize"/> từ <see cref="GameViewManager"/>.
    /// </summary>
    public class TrayClickController : MonoBehaviour
    {
        private ConveyorView _conveyorView;
        private BigBlockSplitter _splitter;
        private Camera _camera;

        public void Initialize(ConveyorView conveyorView, BigBlockSplitter splitter)
        {
            _conveyorView = conveyorView;
            _splitter = splitter;
            _camera = Camera.main;
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0) || _camera == null)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                TrayView tray = hit.collider.GetComponentInParent<TrayView>();
                if (tray != null)
                {
                    OnTrayClicked(tray);
                }
            }
        }

        private void OnTrayClicked(TrayView tray)
        {
            TrayModel model = tray.Model;
            if (model == null || !tray.CanInteract || _splitter == null)
            {
                return;
            }

            // Chỉ lấy tối đa số khối nhỏ băng chuyền còn chứa được; phần chưa bay lên ở lại khay (khối mỏng đi).
            TakePlan plan = model.PlanTake(_conveyorView != null ? _conveyorView.FreeSlots : 0);
            if (plan.Steps.Count == 0)
            {
                return;
            }

            int colorId = model.Peek().colorId;
            List<BigBlockView> blocks = tray.GetTopBlocks(plan.Steps.Count);
            var steps = new List<BigBlockSplitter.Step>(plan.Steps.Count);
            var wholeBlocks = new List<BigBlockView>();
            for (int i = 0; i < plan.Steps.Count; i++)
            {
                steps.Add(new BigBlockSplitter.Step
                {
                    block = blocks[i],
                    startSlices = plan.Steps[i].startSlices,
                    takeSlices = plan.Steps[i].takeSlices
                });

                if (plan.Steps[i].IsWhole)
                {
                    wholeBlocks.Add(blocks[i]);
                }
            }

            tray.IsBusy = true;
            bool started = _splitter.TrySplit(steps, tray.ArrowTransform, colorId, () =>
            {
                foreach (BigBlockView block in wholeBlocks)
                {
                    Destroy(block.gameObject);
                }

                tray.IsBusy = false;
            });

            if (!started)
            {
                tray.IsBusy = false;
                return;
            }

            model.ApplyTake(plan);
            tray.RemoveTopBlocks(wholeBlocks.Count);
        }
    }
}
