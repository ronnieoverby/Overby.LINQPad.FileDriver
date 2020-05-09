namespace Overby.LINQPad.FileDriver.TypeInference
{
    public enum ParsedValue
    {
        EmptyString,

        String,

        Char,

        Double,
        Decimal,
        BigInt,
        Int64,
        Int32,
        Int16,
        Byte,

        One, Zero, // could be bool/numeric/char

        Bool,

        DateTime,

        Timespan,

        Guid,
    }
}
