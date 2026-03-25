namespace DuctRegedit.Models;

public enum ActiveDialog
{
    None,
    EditString,
    EditMultiString,
    EditDword,
    EditQword,
    EditBinary,
    Find,
    AddFavorite,
    RemoveFavorite,
    Export,
    RenameKey,
    RenameValue,
    About,
    ConnectRemote,
    LoadHive,
    UnloadHive,
}

[Flags]
public enum FindFlags
{
    None = 0,
    Keys = 1,
    Values = 2,
    Data = 4,
    WholeStringOnly = 8,
}

public enum ExportFormat
{
    RegistrationFiles,
    Win9xNt4Files,
}

public enum NumberBase
{
    Hexadecimal,
    Decimal,
}

public enum NewValueKind
{
    Key,
    StringValue,
    BinaryValue,
    DwordValue,
    QwordValue,
    MultiStringValue,
    ExpandableStringValue,
}
