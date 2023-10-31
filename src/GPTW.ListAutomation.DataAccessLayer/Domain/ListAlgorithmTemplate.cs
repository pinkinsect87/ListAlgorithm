namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class ListAlgorithmTemplate
    {
        public ListAlgorithmTemplate()
        {
            ListRequests = new HashSet<ListRequest>();
        }

        public int TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public int TemplateTypeId { get; set; }
        public string TemplateVersion { get; set; }
        public string? ManifestFileInfo { get; set; }
        public string? ManifestFileXml { get; set; }

        public virtual TemplateType TemplateType { get; set; }
        public virtual ICollection<ListRequest> ListRequests { get; set; }
    }
}
