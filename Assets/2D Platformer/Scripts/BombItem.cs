using UnityEngine;

namespace Platformer
{
    // Script này gán cho các vật phẩm Bom nhặt được (ví dụ: thùng bom, quả bom nằm trên đất)
    public class BombItem : MonoBehaviour
    {
        [Tooltip("Số lượng bom Player nhận được khi nhặt.")]
        public int bombAmount = 1; 
    }
}