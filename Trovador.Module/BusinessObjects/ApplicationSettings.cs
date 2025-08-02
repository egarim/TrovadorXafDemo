using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Trovador.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty("Name")]
   
    [NavigationItem("Administration")]
    public class ApplicationSettings : BaseObject
    {
        public ApplicationSettings(Session session) : base(session) { }

        public override void AfterConstruction()
        {
            base.AfterConstruction();
            // Set default values
            Name = "Application Settings";
            IpfsNodeUrl = "http://localhost:5001";
            IpfsGatewayUrl = "https://ipfs.io/ipfs/";
            IpfsTimeoutMinutes = 5;
            IpfsAutoPin = true;
        }

        string name;
        string ipfsNodeUrl;
        string ipfsGatewayUrl;
        int ipfsTimeoutMinutes;
        bool ipfsAutoPin;
        bool ipfsEnabled;
        string ipfsApiKey;

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        [RuleRequiredField]
        public string Name
        {
            get => name;
            set => SetPropertyValue(nameof(Name), ref name, value);
        }

        [Category("IPFS Configuration")]
        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        [RuleRequiredField]
        [ToolTip("URL of the IPFS node API endpoint (e.g., http://localhost:5001)")]
        public string IpfsNodeUrl
        {
            get => ipfsNodeUrl;
            set => SetPropertyValue(nameof(IpfsNodeUrl), ref ipfsNodeUrl, value);
        }

        [Category("IPFS Configuration")]
        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        [ToolTip("URL of the IPFS gateway for public access (e.g., https://ipfs.io/ipfs/)")]
        public string IpfsGatewayUrl
        {
            get => ipfsGatewayUrl;
            set => SetPropertyValue(nameof(IpfsGatewayUrl), ref ipfsGatewayUrl, value);
        }

        [Category("IPFS Configuration")]
        [RuleRange(1, 60)]
        [ToolTip("Timeout for IPFS operations in minutes")]
        public int IpfsTimeoutMinutes
        {
            get => ipfsTimeoutMinutes;
            set => SetPropertyValue(nameof(IpfsTimeoutMinutes), ref ipfsTimeoutMinutes, value);
        }

        [Category("IPFS Configuration")]
        [ToolTip("Automatically pin uploaded content to prevent garbage collection")]
        public bool IpfsAutoPin
        {
            get => ipfsAutoPin;
            set => SetPropertyValue(nameof(IpfsAutoPin), ref ipfsAutoPin, value);
        }

        [Category("IPFS Configuration")]
        [ToolTip("Enable or disable IPFS functionality")]
        public bool IpfsEnabled
        {
            get => ipfsEnabled;
            set => SetPropertyValue(nameof(IpfsEnabled), ref ipfsEnabled, value);
        }

        [Category("IPFS Configuration")]
        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        [PasswordPropertyText]
        [ToolTip("API key for hosted IPFS services (Pinata, Infura, etc.)")]
        public string IpfsApiKey
        {
            get => ipfsApiKey;
            set => SetPropertyValue(nameof(IpfsApiKey), ref ipfsApiKey, value);
        }

        [PersistentAlias("Concat([IpfsNodeUrl], ' (', IIF([IpfsEnabled], 'Enabled', 'Disabled'), ')')")]
        public string IpfsStatus
        {
            get => Convert.ToString(EvaluateAlias(nameof(IpfsStatus)));
        }

        // Static method to get settings instance
        public static ApplicationSettings GetInstance(IObjectSpace objectSpace)
        {
            var settings = objectSpace.FindObject<ApplicationSettings>(null);
            if (settings == null)
            {
                settings = objectSpace.CreateObject<ApplicationSettings>();
                objectSpace.CommitChanges();
            }
            return settings;
        }

        // Validation to ensure URL format
        [RuleFromBoolProperty("ValidIpfsNodeUrl", DefaultContexts.Save)]
        public bool IsValidIpfsNodeUrl
        {
            get
            {
                if (string.IsNullOrEmpty(IpfsNodeUrl))
                    return false;
                return Uri.TryCreate(IpfsNodeUrl, UriKind.Absolute, out _);
            }
        }

        [RuleFromBoolProperty("ValidIpfsGatewayUrl", DefaultContexts.Save)]
        public bool IsValidIpfsGatewayUrl
        {
            get
            {
                if (string.IsNullOrEmpty(IpfsGatewayUrl))
                    return true; // Gateway URL is optional
                return Uri.TryCreate(IpfsGatewayUrl, UriKind.Absolute, out _);
            }
        }
    }
}