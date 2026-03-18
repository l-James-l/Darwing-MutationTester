using Models.Attributes;

namespace Models.Enums;

/// <summary>
/// This enum should all the implemented specific mutation types.
/// </summary>
public enum SpecificMutation
{
    [MutationDescription("Replaces an addition operation with a subtraction. + => -")]
    AddToSubtract,

    [MutationDescription("Replaces a subtraction with an addition. - => +")]
    SubtractToAdd,

    [MutationDescription("Replaces an equality comparison with an inequality comparison. == => !=")]
    EqualToNotEqual,

    [MutationDescription("Replaces an inequality comparison with an equality comparison. != => ==")]
    NotEqualToEqual,

    [MutationDescription("Replaces an increment operation with a decrement. ++ => --")]
    IncrementToDecrement,

    [MutationDescription("Replaces a decrement operation with an increment. -- => ++")]
    DecrementToIncrement,

    [MutationDescription("Replace a greater than or equal to with a less than. >= -> <")]
    GreaterThanOrEqualToLessThan,

    [MutationDescription("Replace a greater than with a less than or equal to. > -> <=")]
    GreaterThanToLessThanOrEqual,

    [MutationDescription("Replace a less than or equal to with a greater than. <= -> >")]
    LessThanOrEqualToGreaterThan,

    [MutationDescription("Replace a less than with a greater than or equal to. < -> >=")]
    LessThanToGreaterThanOrEqualTo,
    
    [MutationDescription("Replace an AND comparison with an OR comparison. && => ||")]
    AndToOr,
    
    [MutationDescription("Replace an OR comparison with an AND comparison. || => &&")]
    OrToAnd,
    
    [MutationDescription("Replace a multiplication operation with a divide operation. * => /")]
    MultiplyToDivide,
    
    [MutationDescription("Replace a divide operation with a multiplication operation. / => *")]
    DivideToMultiply,
}
