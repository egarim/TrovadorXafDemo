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
    public enum IpfsUploadStatus
    {
        NotUploaded,
        Uploading,
        Uploaded,
        Failed
    }

    [DefaultClassOptions]
    [DefaultProperty("DisplayName")]
    public class ArtworkVersion : BaseObject
    {
        public ArtworkVersion(Session session) : base(session) { }

        public override void AfterConstruction()
        {
            base.AfterConstruction();
            CreatedDate = DateTime.Now;
            IpfsUploadStatus = IpfsUploadStatus.NotUploaded;
            
            // Auto-increment version number
            if (Artwork != null)
            {
                var maxVersion = Artwork.ArtworkVersions.Max(v => (int?)v.VersionNumber) ?? 0;
                VersionNumber = maxVersion + 1;
            }
        }

        Artwork artwork;
        int versionNumber;
        DateTime createdDate;
        string description;
        string createdBy;
        bool isCurrentVersion;
        string ipfsHash;
        DateTime? ipfsUploadDate;
        IpfsUploadStatus ipfsUploadStatus;

        [Association("Artwork-ArtworkVersions")]
        [RuleRequiredField]
        public Artwork Artwork
        {
            get => artwork;
            set => SetPropertyValue(nameof(Artwork), ref artwork, value);
        }

        
        public int VersionNumber
        {
            get => versionNumber;
            set => SetPropertyValue(nameof(VersionNumber), ref versionNumber, value);
        }

        public DateTime CreatedDate
        {
            get => createdDate;
            set => SetPropertyValue(nameof(CreatedDate), ref createdDate, value);
        }

        [Size(SizeAttribute.Unlimited)]
        public string Description
        {
            get => description;
            set => SetPropertyValue(nameof(Description), ref description, value);
        }

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string CreatedBy
        {
            get => createdBy;
            set => SetPropertyValue(nameof(CreatedBy), ref createdBy, value);
        }

        public bool IsCurrentVersion
        {
            get => isCurrentVersion;
            set => SetPropertyValue(nameof(IsCurrentVersion), ref isCurrentVersion, value);
        }

        [VisibleInListView(true)]
        [ImageEditor(ListViewImageEditorMode = ImageEditorMode.PictureEdit,
            DetailViewImageEditorMode = ImageEditorMode.PictureEdit,
            ListViewImageEditorCustomHeight = 100)]
        public byte[] ImageData
        {
            get { return GetPropertyValue<byte[]>(nameof(ImageData)); }
            set { SetPropertyValue<byte[]>(nameof(ImageData), value); }
        }

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        [VisibleInListView(false)]
        public string IpfsHash
        {
            get => ipfsHash;
            set => SetPropertyValue(nameof(IpfsHash), ref ipfsHash, value);
        }

        [VisibleInListView(false)]
        public DateTime? IpfsUploadDate
        {
            get => ipfsUploadDate;
            set => SetPropertyValue(nameof(IpfsUploadDate), ref ipfsUploadDate, value);
        }

        [VisibleInListView(true)]
        public IpfsUploadStatus IpfsUploadStatus
        {
            get => ipfsUploadStatus;
            set => SetPropertyValue(nameof(IpfsUploadStatus), ref ipfsUploadStatus, value);
        }

        [PersistentAlias("Concat('Version ', [VersionNumber], ' - ', [Description])")]
        public string DisplayName
        {
            get => Convert.ToString(EvaluateAlias(nameof(DisplayName)));
        }

        [PersistentAlias("Not IsNull([IpfsHash])")]
        public bool IsUploadedToIpfs
        {
            get => Convert.ToBoolean(EvaluateAlias(nameof(IsUploadedToIpfs)));
        }

        protected override void OnSaving()
        {
            // Ensure only one version is marked as current
            if (IsCurrentVersion && Artwork != null)
            {
                foreach (var version in Artwork.ArtworkVersions.Where(v => v != this))
                {
                    version.IsCurrentVersion = false;
                }
            }
            base.OnSaving();
        }
    }
}