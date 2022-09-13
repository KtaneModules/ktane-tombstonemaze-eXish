namespace ColoredSquares
{
    public enum SquareColor
    {
        Black,
        White,

        Red,
        Blue,
        Green,
        Yellow,
        Magenta,

        // Juxtacolored Squares only
        DarkBlue,   // More distinguishable from Azure; please don’t use both Blue and DarkBlue in the same module...
        Orange,
        Cyan,
        Purple,
        Chestnut,
        Brown,
        Mauve,
        Azure,
        Jade,
        Forest,
        Gray
    }

    public enum SquaresToRecolor
    {
        All,
        NonwhiteOnly,
        NonblackOnly
    }
}
