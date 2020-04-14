namespace Overby.LINQPad.FileDriver.TypeInference
{
    enum ParsedValue
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

        TrueString1,
        FalseString1,
        TrueString,
        FalseString,

        DateTimeOffset,
        DateTime,

        Timespan,

        Guid,
    }
}
