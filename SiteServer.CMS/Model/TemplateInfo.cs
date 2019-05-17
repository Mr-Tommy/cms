using System;
using Datory;
using SiteServer.CMS.Core;
using SiteServer.Plugin;
using SiteServer.Utils.Enumerations;

namespace SiteServer.CMS.Model
{
    [Table("siteserver_Template")]
    public class TemplateInfo : Entity
    {
        [TableColumn]
        public int SiteId { get; set; }

        [TableColumn]
        public string TemplateName { get; set; }

        [TableColumn]
        private string TemplateType { get; set; }

        public TemplateType Type
        {
            get => TemplateTypeUtils.GetEnumType(TemplateType);
            set => TemplateType = value.Value;
        }

        [TableColumn]
        public string RelatedFileName { get; set; }

        [TableColumn]
        public string CreatedFileFullName { get; set; }

        [TableColumn]
        public string CreatedFileExtName { get; set; }

        [TableColumn]
        private string IsDefault { get; set; }

        public bool Default
        {
            get => IsDefault == "True";
            set => IsDefault = value.ToString();
        }

        public string Content { get; set; }
    }
}
