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
    public class ArtWorkDescriptionController : ViewController
    {
        SimpleAction ToAudio;
        public ArtWorkDescriptionController() : base()
        {

            this.TargetObjectType = typeof(ArtworkDescription);
            this.TargetViewType = ViewType.DetailView;

            // Target required Views (use the TargetXXX properties) and create their Actions.
            TargetObjectType = typeof(ArtworkDescription);
            ToAudio = new SimpleAction(this, "Play Audio", "View");
            ToAudio.Execute += ToAudio_Execute;
            
        }
        protected virtual void ToAudio_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            // This method will be overridden in platform-specific controllers
            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112737/).
        }
        protected override void OnActivated()
        {
            base.OnActivated();
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
