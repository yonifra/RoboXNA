#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
#endregion

namespace RoboXNA
{
    /// <summary>
    /// The options screen is brought up over the top of the main menu
    /// screen, and gives the user a chance to configure the game
    /// in various hopefully useful ways.
    /// </summary>
    class AboutMenuScreen : MenuScreen
    {
        static string aboutInfo =   "RoboXNA v1.0" +
                                    "\n============" +
                                    "\nCreated by:" +
                                    "\nYoni Fraimorice - 015832702" +
                                    "\nWissam Mutlak - 301412789";

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public AboutMenuScreen()
            : base("About RoboXNA")
        {
            MenuEntry aboutText = new MenuEntry(aboutInfo);

            // Add entries to the menu.
            MenuEntries.Add(aboutText);
        }

        #endregion
    }
}
