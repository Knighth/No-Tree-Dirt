using ICities;
using System;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;


namespace NoTreeDirt
{
	public class Mod : IUserMod
	{
        internal const string MOD_NAME = "No Tree Dirt";
        internal const string MOD_DESC = "Disables dirt around trees";
        internal const string MOD_DBG_PREFIX = "NoTreeDirt";
        internal const string MOD_CONFIGPATH = "NoTreeDirt_config.xml";
        public static Configuration config;
        public static bool isEnabled = false;
        public static bool DEBUG_LOG_ON = false;
        public static byte DEBUG_LOG_LEVEL = 0;

        public string Description
		{
			get
			{
				return MOD_DESC;
			}
		}

		public string Name
		{
			get
			{
				return MOD_NAME;
			}
		}

		public Mod()
		{
		}

        public void OnEnabled()
        {
            ReloadConfigValues(false,false);
            isEnabled = true;
            Helper.dbgLog(" has been enabled.");  
        }

        public void OnRemoved()
        {
            isEnabled = false;
            Helper.dbgLog(" has been disabled or unloaded.");
        }

        /// <summary>
        /// Called to either initially load, or force a reload our config file var; 
        /// called by mod initialization and again potentially at mapload hense the options. 
        /// </summary>
        /// <param name="bForceReread">Set to true to flush the old object and create a new one.</param>
        /// <param name="bNoReloadVars">Set this to true to NOT reload the values from the new read of config file to our class level counterpart vars</param>
        public static void ReloadConfigValues(bool bForceReread, bool bNoReloadVars)
        {
            try
            {
                if (bForceReread)
                {
                    config = null;
                    if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Config re-read requested."); }
                }
                config = Configuration.Deserialize(MOD_CONFIGPATH);
                if (config == null)
                {
                    config = new Configuration();
                    //reset of setting should pull defaults
                    Helper.dbgLog("Existing config was null. Created new one.");
                    Configuration.Serialize(MOD_CONFIGPATH, config); //let's write it.
                }
                if (config != null && bNoReloadVars == false) //set\refresh our vars by default.
                {
                    DEBUG_LOG_ON = config.DebugLogging;
                    DEBUG_LOG_LEVEL = config.DebugLoggingLevel;
                    if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL == 0) { DEBUG_LOG_LEVEL = 1; }
                    if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Vars refreshed"); }
                }
                if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Reloaded Config data."); }
            }
            catch (Exception ex)
            { Helper.dbgLog("Exception while loading config values.", ex, true); }

        }


        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelper hp = (UIHelper)helper;
            //get a reference to our options panel itself.
            UIScrollablePanel panel = (UIScrollablePanel)hp.self;
            //subscribe to options tab visibilitychange events.
            panel.eventVisibilityChanged += eventVisibilityChanged;
            UIHelperBase group = helper.AddGroup(MOD_NAME + " Options");
            group.AddCheckbox("Change trees on map load.", config.UpdateTreeAssets, OnUpdateTreeAssetsChange);
            group.AddCheckbox("Update existing trees on map load.", config.UpdateResetTrees, OnUpdateExistingTreesChange);
            group.AddCheckbox("Change props on map load.", config.UpdateExistingProps, OnUpdateExistingPropsChange);
            group.AddCheckbox("Enable debug logging.", config.DebugLogging, OnLoggingChange);
        }

        /// <summary>
        /// Handles the option tab visibility changed event.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="value"></param>
        private void eventVisibilityChanged(UIComponent component, bool value)
        {
            if (value)
            {
                //unsubscribe to future events we only need to do this once.
                component.eventVisibilityChanged -= eventVisibilityChanged;
                //Go kick off setting up our tool tips...
                component.parent.StartCoroutine(DoToolTips(component));
            }
        }

        private void OnUpdateTreeAssetsChange(bool en)
        {
            config.UpdateTreeAssets = en;
            Configuration.Serialize(MOD_CONFIGPATH,config);
        }

        private void OnUpdateExistingTreesChange(bool en)
        {
            config.UpdateResetTrees = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

        private void OnUpdateExistingPropsChange(bool en)
        {
            config.ResetExistingProps = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

        private void OnLoggingChange(bool en)
        {
            config.DebugLogging  = en;
            DEBUG_LOG_ON = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

        /// <summary>
        /// Sets up tool tips. Would have been much easier 
        /// if they would have let us specify the name of the components during creation but they don't.
        /// we've gotta loop though them looking for matches.
        /// We could of course just like created our own but that's more work and overkill for what we're doing.
        /// </summary>
        /// <param name="component">The UIComponent ref we need to work on</param>
        /// <returns></returns>
        private System.Collections.IEnumerator DoToolTips(UIComponent component)
        {
            //pause for 1/2 second then come back and do rest.
            //we do this to avoid some flakyness with the gui if we do it without waiting like 10ms
            yield return new WaitForSeconds(0.300f);  
            try
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Refreshing tooltips."); }
                UICheckBox[] cb = component.GetComponentsInChildren<UICheckBox>(true);
                if (cb != null && cb.Length > 0)
                {
                    for (int i = 0; i < (cb.Length); i++)
                    {
                        switch (cb[i].text)
                        {
                            case "Enable debug logging.":
                                cb[i].tooltip = "Enables detailed logging for debugging purposes\n See config file for even more options, unless there are problems you probably don't want to enable this.";
                                break;
                            case "Change trees on map load.":
                                cb[i].tooltip = "If enabled the mod will modify existing trees on any map you load. \n You may not see actual changes till the map is saved and reloaded.\n If disabled the mod will not touch trees";
                                break;
                            case "Update existing trees on map load.":
                                cb[i].tooltip = "If enabled the mod will update the already created tress to remove dirt\n while the map loads so you can see changes.\n *Please note this can add additional time, about\n 10-13seconds per 100k created trees*";
                                break;
                            case "Change props on map load.":
                                cb[i].tooltip = "This option doesn't do anything... yet";
                                break;
                            default:
                                break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Helper.dbgLog("", ex, true);
            }
            yield break;  //bust out of the co-routine never to return.
        }

	}
}