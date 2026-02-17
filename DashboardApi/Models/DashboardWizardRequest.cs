using System.Collections.Generic;

namespace DashboardApi.Models
{
    public class DashboardWizardRequest
    {
        public string? DashboardTitle { get; set; }
        /// <summary>Category for folder mapping: Browse Product, Browse Path, Account, Checkout, Gift Registry, API. When set, dashboard is placed in that category's folder under Personal.</summary>
        public string? Category { get; set; }
        public bool UseDefaults { get; set; }
        public TemplateVariables? Variables { get; set; }
        public Dictionary<string, object>? Panels { get; set; }
    }

    public class TemplateVariables
    {
        public string? Timeslice { get; set; }
        public string? Domain { get; set; }
        public string? DomainPrefix { get; set; }
        public string? Environment { get; set; }
    }
}