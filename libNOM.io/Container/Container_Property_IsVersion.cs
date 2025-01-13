namespace libNOM.io;


// This partial class contains internal properties.
// EXTERNAL RELEASE: Add new IsVersion flag.
public partial class Container : IContainer
{
    public bool IsVersion211BeyondWithVehicleCam => IsVersion(GameVersionEnum.BeyondWithVehicleCam); // { get; }

    public bool IsVersion220Synthesis => IsVersion(GameVersionEnum.Synthesis); // { get; }

    public bool IsVersion226SynthesisWithJetpack => IsVersion(GameVersionEnum.SynthesisWithJetpack); // { get; }

    public bool IsVersion230LivingShip => IsVersion(GameVersionEnum.LivingShip); // { get; }

    public bool IsVersion240ExoMech => IsVersion(GameVersionEnum.ExoMech); // { get; }

    public bool IsVersion250Crossplay => IsVersion(GameVersionEnum.Crossplay); // { get; }

    public bool IsVersion260Desolation => IsVersion(GameVersionEnum.Desolation); // { get; }

    public bool IsVersion300Origins => IsVersion(GameVersionEnum.Origins); // { get; }

    public bool IsVersion310NextGeneration => IsVersion(GameVersionEnum.NextGeneration); // { get; }

    public bool IsVersion320Companions => IsVersion(GameVersionEnum.Companions); // { get; }

    public bool IsVersion330Expeditions => IsVersion(GameVersionEnum.Expeditions); // { get; }

    public bool IsVersion340Beachhead => IsVersion(GameVersionEnum.Beachhead); // { get; }

    public bool IsVersion350Prisms => IsVersion(GameVersionEnum.Prisms); // { get; }

    public bool IsVersion351PrismsWithByteBeatAuthor => IsVersion(GameVersionEnum.PrismsWithByteBeatAuthor); // { get; }

    public bool IsVersion360Frontiers => IsVersion(GameVersionEnum.Frontiers); // { get; }

    public bool IsVersion370Emergence => IsVersion(GameVersionEnum.Emergence); // { get; }

    public bool IsVersion380Sentinel => IsVersion(GameVersionEnum.Sentinel); // { get; }

    public bool IsVersion381SentinelWithWeaponResource => IsVersion(GameVersionEnum.SentinelWithWeaponResource); // { get; }

    public bool IsVersion384SentinelWithVehicleAI => IsVersion(GameVersionEnum.SentinelWithVehicleAI); // { get; }

    public bool IsVersion385Outlaws => IsVersion(GameVersionEnum.Outlaws); // { get; }

    public bool IsVersion390Leviathan => IsVersion(GameVersionEnum.Leviathan); // { get; }

    public bool IsVersion394Endurance => IsVersion(GameVersionEnum.Endurance); // { get; }

    public bool IsVersion400Waypoint => IsVersion(GameVersionEnum.Waypoint); // { get; }

    public bool IsVersion404WaypointWithAgileStat => IsVersion(GameVersionEnum.WaypointWithAgileStat); // { get; }

    public bool IsVersion405WaypointWithSuperchargedSlots => IsVersion(GameVersionEnum.WaypointWithSuperchargedSlots); // { get; }

    public bool IsVersion410Fractal => IsVersion(GameVersionEnum.Fractal); // { get; }

    public bool IsVersion420Interceptor => IsVersion(GameVersionEnum.Interceptor); // { get; }

    public bool IsVersion430Singularity => IsVersion(GameVersionEnum.Singularity); // { get; }

    public bool IsVersion440Echoes => IsVersion(GameVersionEnum.Echoes); // { get; }

    public bool IsVersion450Omega => IsVersion(GameVersionEnum.Omega); // { get; }

    public bool IsVersion452OmegaWithMicrosoftV2 => IsVersion(GameVersionEnum.OmegaWithMicrosoftV2); // { get; }

    public bool IsVersion460Orbital => IsVersion(GameVersionEnum.Orbital); // { get; }

    public bool IsVersion470Adrift => IsVersion(GameVersionEnum.Adrift); // { get; }

    public bool IsVersion500WorldsPartI => IsVersion(GameVersionEnum.WorldsPartI); // { get; }

    public bool IsVersion510Aquarius => IsVersion(GameVersionEnum.Aquarius); // { get; }

    public bool IsVersion520TheCursed => IsVersion(GameVersionEnum.TheCursed); // { get; }

    public bool IsVersion525TheCursedWithCrossSave => IsVersion(GameVersionEnum.TheCursedWithCrossSave); // { get; }
}
