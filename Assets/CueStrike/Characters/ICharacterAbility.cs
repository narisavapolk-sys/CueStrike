namespace CueStrike.Characters
{
    /// <summary>
    /// Interface for all character ability controllers.
    /// Each playable character implements this to provide unique gameplay modifiers.
    /// </summary>
    public interface ICharacterAbility
    {
        /// <summary>
        /// Name of this ability
        /// </summary>
        string AbilityName { get; }

        /// <summary>
        /// Short description
        /// </summary>
        string AbilityDescription { get; }

        /// <summary>
        /// Called when the character is spawned
        /// </summary>
        void OnCharacterSpawned();

        /// <summary>
        /// Get accuracy modifier (0-1 added to base accuracy)
        /// </summary>
        float GetAccuracyModifier();

        /// <summary>
        /// Get power modifier (0-1 multiplier)
        /// </summary>
        float GetPowerModifier();

        /// <summary>
        /// Get speed modifier (0-1 multiplier for shot timer)
        /// </summary>
        float GetSpeedModifier();

        /// <summary>
        /// Get visibility bonus (0-1, how much extra trajectory info is shown)
        /// </summary>
        float GetVisibilityBonus();

        /// <summary>
        /// Is the ability currently active?
        /// </summary>
        bool IsAbilityActive();
    }
}