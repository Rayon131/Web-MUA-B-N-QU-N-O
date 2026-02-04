using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class SystemSetting
    {
        [Key]
        // Khóa chính, ví dụ: "PromotionRule", "WebsiteName", ...
        public string SettingKey { get; set; }

        // Giá trị của cài đặt, ví dụ: "Stackable", "BestValue", ...
        public string SettingValue { get; set; }
    }
}
