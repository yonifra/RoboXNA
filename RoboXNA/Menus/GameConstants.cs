namespace RoboXNA.Menus
{
    /// <summary>
    /// This class contains all the game constants and is accessible from all classes in the project
    /// </summary>
    class GameConstants
    {
        #region Camera constants
        public const float NearClip = 1.0f;
        public const float FarClip = 1000.0f;
        public const float ViewAngle = 45.0f;
        #endregion

        #region Character constants

        /// <summary>
        /// Character walking velocity
        /// </summary>
        public const float Velocity = 0.2f;

        /// <summary>
        /// Controls how fast the character will turn
        /// </summary>
        public const float TurnSpeed = 0.025f;

        /// <summary>
        /// Maximum range of the floor (how much does the character walks before he falls off the floor)
        /// </summary>
        public const int MaxRange = 98;

        /// <summary>
        /// Modifies the character model size (and affects its bounding sphere accordingly)
        /// </summary>
        public const float CharacterSize = 0.065f;

        /// <summary>
        /// Determines the speed that will be added to the walking speed of the character
        /// </summary>
        public const float RunningSpeed = 0.8f;

        #endregion

        #region General constants
        public const int MaxRangeTerrain = 98;
        public const int NumberOfBuildings = 15;
        public const int BuildingTypes = 4;
        public const int MinDistance = 10;
        public const int MaxDistance = 90;
        public const int Spacing = 20;
        #endregion

        #region Bounding sphere constants
        public const float CharacterBoundingSphereFactor = 0.7f;
        public const float BuildingBoundingSphereFactor = 0.55f;
        #endregion
    }
}
