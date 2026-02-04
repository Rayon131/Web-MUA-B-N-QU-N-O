using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class ChatLieu
    {
        [Key]
        public int MaChatLieu { get; set; }

        public string TenChatLieu { get; set; }

        public string MoTa { get; set; }

        public int TrangThai { get; set; }

        // Navigation property
        public virtual ICollection<SanPham> SanPhams { get; set; }
    }
}
