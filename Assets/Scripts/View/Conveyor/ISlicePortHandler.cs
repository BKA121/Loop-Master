namespace LoopMaster.View
{
    /// <summary>
    /// Bên xử lý các "cổng" (mũi tên đầu khay) trên băng chuyền: <see cref="ConveyorView"/> chỉ phát hiện khối vượt cổng
    /// rồi hỏi bên này khay có nhận không; quyết định gameplay nằm ở phía khay.
    /// </summary>
    public interface ISlicePortHandler
    {
        /// <summary>Số khối nhỏ màu <paramref name="colorId"/> khay ở cổng này còn nhận được (0 = không nhận).</summary>
        int GetAcceptCount(int portIndex, int colorId);

        /// <summary>Cổng đã nhận trước <paramref name="count"/> khối (cụm nối đuôi); khay giữ chỗ cho chúng.</summary>
        void OnClaimed(int portIndex, int colorId, int count);

        /// <summary>
        /// Một khối đã được nhận trước vừa tới cổng và rời băng chuyền. Bên này phải bay <paramref name="slice"/> vào khay
        /// rồi tự trả nó về pool khi tới nơi.
        /// </summary>
        void OnAbsorb(int portIndex, SliceView slice, int colorId);
    }
}
