using System;
using System.Collections.Generic;

namespace LoopMaster.Core
{
    /// <summary>
    /// Lớp DTO thuần túy chỉ chứa dữ liệu ánh xạ từ JSON.
    /// Không chứa logic, không chứa GameObject hay Runtime State.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int v;       // Phiên bản file (Version)
        public int h;       // Loại grid / offset
        public int s;       // Kích thước lưới
        public int l;       // Thông số l
        public float ps;    // Tỷ lệ khoảng cách (Piece Spacing)

        public List<TrayData> b = new List<TrayData>();     // Khay chứa khối
        public List<PathData> p = new List<PathData>();     // Băng chuyền (Đường Spline)
        public List<MarkerData> m = new List<MarkerData>(); // Máy móc/Điểm đánh dấu (Enter, Exit)

        /// <summary>
        /// Trả về dữ liệu cứng (Hardcoded) của Level 1 (ID: 521893)
        /// Dùng để dựng Core Game ngay mà chưa cần viết File Loader.
        /// </summary>
        public static LevelData GetLevel1MockData()
        {
            return new LevelData
            {
                h = 0,
                s = 8,
                l = 4,
                b = new List<TrayData>
                {
                    new TrayData
                    {
                        i = 5,
                        p = new float[] { -0.8f, 0f, -0.6f },
                        r = new float[] { 0f, 0f, 0f },
                        l = 0,
                        e = 0,
                        c = new List<BlockItemData>
                        {
                            new BlockItemData { t = 1, s = 0, k = 0 },
                            new BlockItemData { t = 1, s = 0, k = 0 }
                        }
                    },
                    new TrayData
                    {
                        i = 9,
                        p = new float[] { 0.8f, 0f, -0.6f },
                        r = new float[] { 0f, 0f, 0f },
                        l = 0,
                        e = 0,
                        c = new List<BlockItemData>
                        {
                            new BlockItemData { t = 1, s = 0, k = 0 },
                            new BlockItemData { t = 1, s = 0, k = 0 }
                        }
                    }
                },
                p = new List<PathData>
                {
                    new PathData
                    {
                        c = 0, // Không phải vòng khép kín
                        p = new List<PathPointData>
                        {
                            new PathPointData
                            {
                                p = new float[] { -2.8f, 0.2f, 2.2f },
                                i = new float[] { -2.8f, 0.2f, 2.2f },
                                o = new float[] { -2.8f, 0.2f, 2.2f }
                            },
                            new PathPointData
                            {
                                p = new float[] { 2.8f, 0.2f, 2.2f },
                                i = new float[] { 2.8f, 0.2f, 2.2f },
                                o = new float[] { 2.8f, 0.2f, 2.2f }
                            }
                        }
                    }
                },
                m = new List<MarkerData>
                {
                    new MarkerData
                    {
                        n = "Enter",
                        p = new float[] { -2.8f, 0f, 2.2f },
                        r = new float[] { 0f, 180f, 0f },
                        s = new float[] { 1f, 1f, 1f }
                    },
                    new MarkerData
                    {
                        n = "Exit",
                        p = new float[] { 2.8f, 0f, 2.2f },
                        r = new float[] { 0f, 0f, 0f },
                        s = new float[] { 1f, 1f, 1f }
                    }
                }
            };
        }
    }

    [Serializable]
    public class TrayData
    {
        public int t;           
        public int i;           
        public int iv2;         
        
        public float[] p;       
        public float[] r;       
        
        public List<BlockItemData> c = new List<BlockItemData>();
        
        public int l;           
        public int e;           
        public int n;           
        public int lc;          
        public int al;          
    }

    [Serializable]
    public class BlockItemData
    {
        public int t;           
        public int s;           
        public int k;           
    }

    [Serializable]
    public class PathData
    {
        public int c;           // Khép kín (Closed). 1 = vòng lặp
        public List<PathPointData> p = new List<PathPointData>(); 
    }

    [Serializable]
    public class PathPointData
    {
        public float[] p;       
        public float[] i;       
        public float[] o;       
    }

    [Serializable]
    public class MarkerData
    {
        public string n;        // Tên điểm đánh dấu (Enter, Exit)
        public float[] p;       // Tọa độ
        public float[] r;       // Góc xoay
        public float[] s;       // Kích thước (Scale)
    }
}
