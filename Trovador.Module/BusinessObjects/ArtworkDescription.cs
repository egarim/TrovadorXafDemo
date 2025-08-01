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
    public class ArtworkDescription : BaseObject
    { // Inherit from a different class to provide a custom primary key, concurrency and deletion behavior, etc. (https://docs.devexpress.com/eXpressAppFramework/113146/business-model-design-orm/business-model-design-with-xpo/base-persistent-classes).
        // Use CodeRush to create XPO classes and properties with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/118557
        public ArtworkDescription(Session session)
            : base(session)
        {
        }
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            // Place your initialization code here (https://docs.devexpress.com/eXpressAppFramework/112834/getting-started/in-depth-tutorial-winforms-webforms/business-model-design/initialize-a-property-after-creating-an-object-xpo?v=22.1).
        }

        string language;
        string text;
        ExplanationLevel explanationLevel;
        Artwork artwork;

        
        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string Language
        {
            get => language;
            set => SetPropertyValue(nameof(Language), ref language, value);
        }

        [Association("Artwork-ArtworkDescriptions")]
        public Artwork Artwork
        {
            get => artwork;
            set => SetPropertyValue(nameof(Artwork), ref artwork, value);
        }

        public ExplanationLevel ExplanationLevel
        {
            get => explanationLevel;
            set => SetPropertyValue(nameof(ExplanationLevel), ref explanationLevel, value);
        }
        
        [Size(SizeAttribute.Unlimited)]
        public string Text
        {
            get => text;
            set => SetPropertyValue(nameof(Text), ref text, value);
        }
    }
}