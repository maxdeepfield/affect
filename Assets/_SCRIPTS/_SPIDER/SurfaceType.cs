/// <summary>
/// Represents the type of surface the spider is currently on.
/// Used by TerrainAdapter to determine foot orientation and gravity direction.
/// </summary>
public enum SurfaceType
{
    /// <summary>
    /// Normal upward-facing surface (floor).
    /// Gravity points down, feet orient upward.
    /// </summary>
    Ground,

    /// <summary>
    /// Vertical surface (wall).
    /// Gravity points perpendicular to wall, feet orient to wall normal.
    /// </summary>
    Wall,

    /// <summary>
    /// Inverted surface (ceiling).
    /// Gravity points up relative to spider, feet orient downward.
    /// </summary>
    Ceiling,

    /// <summary>
    /// No surface detected within range.
    /// Spider uses fallback rest positions.
    /// </summary>
    None
}
