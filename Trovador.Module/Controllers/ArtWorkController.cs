using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trovador.Module.BusinessObjects;

namespace Trovador.Module.Controllers
{
    public class ArtWorkController : ViewController
    {
        SingleChoiceAction Language;
        SimpleAction GenerateDescriptions;
        string SelectedLanguage;
        public ArtWorkController() : base()
        {
            // Target required Views (use the TargetXXX properties) and create their Actions.
            GenerateDescriptions = new SimpleAction(this, "Generate Descriptions", "View");
            GenerateDescriptions.Execute += GenerateDescriptions_Execute;

            Language = new SingleChoiceAction(this, "Language", "View");
            Language.ItemType = SingleChoiceActionItemType.ItemIsMode;
            Language.Execute += Language_Execute;
            // Create some items
            //Language.Items.Add(new ChoiceActionItem("MyItem1", "My Item 1", 1));
            Language.Items.Add(new ChoiceActionItem("Russian", "Russian", 9));
            Language.Items.Add(new ChoiceActionItem("English", "English", 1));
            Language.Items.Add(new ChoiceActionItem("Spanish", "Spanish", 2));
            Language.Items.Add(new ChoiceActionItem("French", "French", 3));
            Language.Items.Add(new ChoiceActionItem("German", "German", 4));
            Language.Items.Add(new ChoiceActionItem("Italian", "Italian", 5));
            Language.Items.Add(new ChoiceActionItem("Portuguese", "Portuguese", 6));
            Language.Items.Add(new ChoiceActionItem("Chinese", "Chinese", 7));
            Language.Items.Add(new ChoiceActionItem("Japanese", "Japanese", 8));
           
        }
        private void Language_Execute(object sender, SingleChoiceActionExecuteEventArgs e)
        {
            SelectedLanguage = e.SelectedChoiceActionItem.Caption;
            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112738/).
        }
        private async void GenerateDescriptions_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            IEnumerable<ExplanationLevel> Levels = this.ObjectSpace.CreateCollection(typeof(ExplanationLevel)).Cast<ExplanationLevel>();
            Artwork artwork = this.View.CurrentObject as Artwork; 
            AiEngine AiEngine = new AiEngine();
            foreach (ExplanationLevel explanationLevel in Levels)
            {

                var text=  await AiEngine.Explain(explanationLevel, artwork, SelectedLanguage);
                ArtworkDescription description = this.ObjectSpace.CreateObject<ArtworkDescription>();
                description.Artwork = artwork;
                description.Language = SelectedLanguage;
                description.ExplanationLevel = explanationLevel;
                description.Text = text;

            }
            this.ObjectSpace.CommitChanges();
            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112737/).
        }
        protected override void OnActivated()
        {
            base.OnActivated();
            this.Language.SelectedItem = this.Language.Items.FirstOrDefault();
            this.SelectedLanguage = this.Language.SelectedItem.Caption;
            // Perform various tasks depending on the target View.
        }
        protected override void OnDeactivated()
        {
            // Unsubscribe from previously subscribed events and release other references and resources.
            base.OnDeactivated();
        }
        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            // Access and customize the target View control.
        }
    }
}
