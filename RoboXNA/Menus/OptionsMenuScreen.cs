using System;

namespace RoboXNA
{
    /// <summary>
    /// The options screen is brought up over the top of the main menu
    /// screen, and gives the user a chance to configure the game
    /// in various hopefully useful ways.
    /// </summary>
    class OptionsMenuScreen : MenuScreen
    {
        #region Fields

        MenuEntry buildingNumMenuEntry;
        MenuEntry soundMenuEntry;
        MenuEntry fullscreenEntry;

        static string[] numOfBuildings = { "15", "25", "40", "45" };
        static int currentDefinition = 0;

        static bool soundActivated = true;
        static bool isFullScreen = false;
        
        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public OptionsMenuScreen()
            : base("Options")
        {
            // Create our menu entries.
            buildingNumMenuEntry = new MenuEntry(string.Empty);
            soundMenuEntry = new MenuEntry(string.Empty);
            fullscreenEntry = new MenuEntry(string.Empty);

            SetMenuEntryText();

            MenuEntry back = new MenuEntry("Back");

            // Hook up menu event handlers
            buildingNumMenuEntry.Selected += GraphicLevelMenuEntrySelected;
            soundMenuEntry.Selected += SoundMenuEntrySelected;
            back.Selected += OnCancel;
            
            // Add entries to the menu.
            MenuEntries.Add(buildingNumMenuEntry);
            MenuEntries.Add(soundMenuEntry);
            MenuEntries.Add(back);
        }


        /// <summary>
        /// Fills in the latest values for the options screen menu text.
        /// </summary>
        void SetMenuEntryText()
        {
            buildingNumMenuEntry.Text = "Number of buildings: " + numOfBuildings[currentDefinition];
            soundMenuEntry.Text = "Sound " + (soundActivated ? "On" : "Off");
        }


        #endregion

        #region Handle Input

        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void GraphicLevelMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            currentDefinition = (currentDefinition + 1) % numOfBuildings.Length;
            RoboXNA.Menus.RoboXNA.Default.buildingsNumber = Convert.ToInt32(numOfBuildings[currentDefinition]);
            
            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the Frobnicate menu entry is selected.
        /// </summary>
        void SoundMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            soundActivated = !soundActivated;
            RoboXNA.Menus.RoboXNA.Default.soundActive = soundActivated;

            SetMenuEntryText();
        }
        
        #endregion
    }
}
