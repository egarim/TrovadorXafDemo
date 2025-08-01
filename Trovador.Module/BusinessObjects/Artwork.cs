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
    //[ImageName("BO_Contact")]
    //[DefaultProperty("DisplayMemberNameForLookupEditorsOfThisType")]
    //[DefaultListViewOptions(MasterDetailMode.ListViewOnly, false, NewItemRowPosition.None)]
    //[Persistent("DatabaseTableName")]
    // Specify more UI options using a declarative approach (https://docs.devexpress.com/eXpressAppFramework/112701/business-model-design-orm/data-annotations-in-data-model).
    public class Artwork : BaseObject
    { // Inherit from a different class to provide a custom primary key, concurrency and deletion behavior, etc. (https://docs.devexpress.com/eXpressAppFramework/113146/business-model-design-orm/business-model-design-with-xpo/base-persistent-classes).
        // Use CodeRush to create XPO classes and properties with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/118557
        public Artwork(Session session)
            : base(session)
        {
        }
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            // Initialize the Description with the default value
           
            // Place your initialization code here (https://docs.devexpress.com/eXpressAppFramework/112834/getting-started/in-depth-tutorial-winforms-webforms/business-model-design/initialize-a-property-after-creating-an-object-xpo?v=22.1).
        }

        Artist artist;
        DateOnly year;
        string name;
        string title;
        string medium;
        string stylePeriod;
        string description;

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string Name
        {
            get => name;
            set => SetPropertyValue(nameof(Name), ref name, value);
        }

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string Title
        {
            get => title;
            set => SetPropertyValue(nameof(Title), ref title, value);
        }

        [Association("Artist-Artworks")]
        public Artist Artist
        {
            get => artist;
            set => SetPropertyValue(nameof(Artist), ref artist, value);
        }

        public DateOnly Year
        {
            get => year;
            set => SetPropertyValue(nameof(Year), ref year, value);
        }

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string Medium
        {
            get => medium;
            set => SetPropertyValue(nameof(Medium), ref medium, value);
        }

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string StylePeriod
        {
            get => stylePeriod;
            set => SetPropertyValue(nameof(StylePeriod), ref stylePeriod, value);
        }

        [Size(SizeAttribute.Unlimited)]
        public string Description
        {
            get => description;
            set => SetPropertyValue(nameof(Description), ref description, value);
        }

        [VisibleInListView(true)]
        [ImageEditor(ListViewImageEditorMode = ImageEditorMode.PictureEdit,
    DetailViewImageEditorMode = ImageEditorMode.PictureEdit,
    ListViewImageEditorCustomHeight = 100)]
        public byte[] ArtworkImage
        {
            get { return GetPropertyValue<byte[]>(nameof(ArtworkImage)); }
            set { SetPropertyValue<byte[]>(nameof(ArtworkImage), value); }
        }

        [Association("Artwork-ArtworkDescriptions")]
        public XPCollection<ArtworkDescription> ArtworkDescriptions
        {
            get
            {
                return GetCollection<ArtworkDescription>(nameof(ArtworkDescriptions));
            }
        }
    }
}