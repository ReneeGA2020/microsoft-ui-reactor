using System;

namespace RegeditWinUI.Models;

[Flags]
public enum FindFlags
{
    None = 0,
    Keys = 1,
    Values = 2,
    Data = 4,
    WholeString = 8
}

public enum NumberBase
{
    Hexadecimal,
    Decimal
}

public enum ExportFormat
{
    Win2000NT5 // REGEDIT5 / Windows Registry Editor Version 5.00
}
