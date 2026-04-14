using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("tbl_app_download_configs")]
    public class AppDownloadConfig : BaseEntity
    {
        [MaxLength(2048)]
        public string? IosUrl { get; set; }

        [MaxLength(2048)]
        public string? AndroidUrl { get; set; }
    }
}

