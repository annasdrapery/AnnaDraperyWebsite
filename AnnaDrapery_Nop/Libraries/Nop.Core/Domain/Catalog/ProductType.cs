namespace Nop.Core.Domain.Catalog
{
    /// <summary>
    /// Represents a product type
    /// </summary>
    public enum ProductType
    {
        /// <summary>
        /// Simple
        /// </summary>
        SimpleProduct = 5,
        /// <summary>
        /// Grouped (product with variants)
        /// </summary>
        GroupedProduct = 10,
        Finials = 11,
        Poles = 12,
        Brackets = 13,
        Rings = 14,
        Accessories = 15,
        Holdbacks = 16,
        PackageSets = 17,
        CompleteRodSets = 20,
        BasicCurtainRods = 21,
        HeavyDutyCurtainRods = 30,
        DecorativeTraverseRods = 31,
        AdjustableTraverse = 32
    }
    public enum CollectionStyle
    {
        Metal = 1,
        Wood = 2,
        WroughtIron = 3,
        MixUsed = 4
    }
    public enum FinialStyle
    {
        Animal = 1,
        Arrow = 2,
        Ball = 3,
        Crown = 4,
        Cylinder = 5,
        EndCap = 6,
        Floral = 7,
        Geometric = 8,
        Glass = 9,
        Mirrored = 10,
        Modem = 11,
        OldWorld = 12,
        Pinecone = 13,
        Regal = 14,
        Scroll = 15,
        Traditional = 16,
        Trumpet = 17,
        Vase = 18
    }
    public enum PoleStyle
    {
        Bamboo = 1,
        FauxBamboo = 2,
        Fluted = 3,
        RopeHollow = 4,
        RopeSolid = 5,
        RoundHeavy = 6,
        RoundHmdHollow = 7,
        RoundHmdSolid = 8,
        RoundSolid = 9,
        Smooth = 10,
        Square = 11,
        SquareHmdHollow = 12,
        SquareHmdSolid = 13,
        SquareHollow = 14,
        TwistHammered = 15,
        TwistHollow = 16,
        WireRope = 17,
        WoodGrainSolid = 18
    }
    public enum BracketStyle
    {
        Unspecified = 1,
        Fixed = 2,
        Adjustable = 3,
        Double = 4
    }
    public enum AccessoriesStyle
    {
        AlligatorClips = 1,
        BatonOrWand = 2,
        CrestsAndEnhancers = 3,
        ElboxFixed = 4,
        FrenchReturnBracket = 5,
        Grommet = 6,
        HoldbackStem = 7,
        InsideMountSocket = 8,
        MetalCap = 9,
        PartsAndAccessories = 10,
        PlugAdapter = 11,
        RodConnector = 12,
        RodExtension = 13,
        RodSplice = 14,
        ScarfRing = 15,
        SwivelSocket = 16
    }
    public enum RodsType
    {
        Single = 1,
        Double = 2,
        Combo = 3,
        Standard = 4,
        SashRod = 5,
        CafeRod = 6,
        UtilityRod = 7,
        WideFace = 8,
        Tension = 9,
        Shapes = 10,
        Parts = 11
    }
    public enum RodsOption
    {
        Ripplefold = 1,
        WallMount = 2,
        CeilingMount = 3,
        BayOption = 4,
        Bow = 5,
        DoubleRod = 6,
        PinchPleat = 7,
        FauxRingCarriers = 8
    }
    public enum DecorativeType
    {
        DecorativeCombo = 1,
        DecorativeTraverse = 2,
        FrenchRods = 3,
        NonDecorative = 4
    }
   
}
