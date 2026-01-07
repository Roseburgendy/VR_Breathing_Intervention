namespace _Scripts.BreathGuideSystem
{
    /// <summary>
    /// Defines the type of meditative movement pattern 
    /// </summary>
    public enum MovementType
    {
        VerticalUp,      // Hands move upward (inhale)
        VerticalDown,    // Hands move downward (exhale)
        HorizontalOpen,  // Hands spread apart (inhale)
        HorizontalClose,  // Hands move together (exhale)
        CircleInhale,  // Left hand up,right hand down(inhale)
        CircleExhale  // Left Hand up Right Hand up(inhale)
    }
}