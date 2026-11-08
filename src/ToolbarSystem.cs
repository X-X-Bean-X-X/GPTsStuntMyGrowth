using Colossal.UI.Binding;
using Game;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Menu;

namespace StuntYourGrowth
{
    public sealed class ToolbarSystem : UISystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            AddBinding(new TriggerBinding(Runtime.Id, "openSettings", OpenSettings));
        }

        private void OpenSettings()
        {
            if (Runtime.Options == null || GameManager.instance.gameMode != GameMode.Game ||
                GameManager.instance.isGameLoading) return;
            World.GetExistingSystemManaged<OptionsUISystem>()?.OpenPage(Runtime.Options.id, null, false);
        }
    }
}
