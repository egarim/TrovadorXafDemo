using DevExpress.ExpressApp;
using Trovador.Module.BusinessObjects;

namespace Trovador.Module.Services
{
    public interface IApplicationSettingsService
    {
        ApplicationSettings GetSettings(IObjectSpace objectSpace);
        string GetIpfsNodeUrl(IObjectSpace objectSpace);
        string GetIpfsGatewayUrl(IObjectSpace objectSpace);
        int GetIpfsTimeoutMinutes(IObjectSpace objectSpace);
        bool IsIpfsEnabled(IObjectSpace objectSpace);
        string GetIpfsApiKey(IObjectSpace objectSpace);
        bool ShouldAutoPin(IObjectSpace objectSpace);
    }
}