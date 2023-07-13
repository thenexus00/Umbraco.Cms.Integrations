﻿using Umbraco.Cms.Integrations.Crm.Hubspot.Models;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Cms.Integrations.Crm.Hubspot
{
    public class HubspotFormPickerConfiguration
    {
        [ConfigurationField(key: "settings", name: "Settings", view: "~/App_Plugins/UmbracoCms.Integrations/Crm/Hubspot/views/settings.html")]
        public HubspotFormPickerSettings Settings { get; set; }

        public HubspotFormPickerConfiguration()
        {
            Settings = new HubspotFormPickerSettings();
        }
    }
}
