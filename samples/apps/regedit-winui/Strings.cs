// Auto-generated accessor for embedded Strings.resx
// This provides a simpler API than the generated Designer.cs
using System.Resources;
using System.Reflection;

namespace RegeditWinUI;

internal static class Strings
{
    private static readonly ResourceManager _rm = new("RegeditWinUI.Resources.Strings", Assembly.GetExecutingAssembly());
    private static string Get(string name) => _rm.GetString(name) ?? name;

    public static string AppTitle => Get("AppTitle");
    public static string Computer => Get("Computer");

    // File menu
    public static string MenuFile => Get("MenuFile");
    public static string MenuImport => Get("MenuImport");
    public static string MenuExport => Get("MenuExport");
    public static string MenuLoadHive => Get("MenuLoadHive");
    public static string MenuUnloadHive => Get("MenuUnloadHive");
    public static string MenuConnectNetworkRegistry => Get("MenuConnectNetworkRegistry");
    public static string MenuDisconnectNetworkRegistry => Get("MenuDisconnectNetworkRegistry");
    public static string MenuPrint => Get("MenuPrint");
    public static string MenuExit => Get("MenuExit");

    // Edit menu
    public static string MenuEdit => Get("MenuEdit");
    public static string MenuModify => Get("MenuModify");
    public static string MenuModifyBinary => Get("MenuModifyBinary");
    public static string MenuNew => Get("MenuNew");
    public static string MenuNewKey => Get("MenuNewKey");
    public static string MenuNewStringValue => Get("MenuNewStringValue");
    public static string MenuNewBinaryValue => Get("MenuNewBinaryValue");
    public static string MenuNewDWORDValue => Get("MenuNewDWORDValue");
    public static string MenuNewQWORDValue => Get("MenuNewQWORDValue");
    public static string MenuNewMultiStringValue => Get("MenuNewMultiStringValue");
    public static string MenuNewExpandStringValue => Get("MenuNewExpandStringValue");
    public static string MenuDelete => Get("MenuDelete");
    public static string MenuRename => Get("MenuRename");
    public static string MenuCopyKeyName => Get("MenuCopyKeyName");
    public static string MenuFind => Get("MenuFind");
    public static string MenuFindNext => Get("MenuFindNext");
    public static string MenuPermissions => Get("MenuPermissions");

    // View menu
    public static string MenuView => Get("MenuView");
    public static string MenuStatusBar => Get("MenuStatusBar");
    public static string MenuAddressBar => Get("MenuAddressBar");
    public static string MenuRefresh => Get("MenuRefresh");

    // Favorites menu
    public static string MenuFavorites => Get("MenuFavorites");
    public static string MenuAddToFavorites => Get("MenuAddToFavorites");
    public static string MenuRemoveFavorite => Get("MenuRemoveFavorite");

    // Help menu
    public static string MenuHelp => Get("MenuHelp");
    public static string MenuAbout => Get("MenuAbout");

    // Columns
    public static string ColumnName => Get("ColumnName");
    public static string ColumnType => Get("ColumnType");
    public static string ColumnData => Get("ColumnData");

    // Dialogs
    public static string DialogEditString => Get("DialogEditString");
    public static string DialogEditExpandString => Get("DialogEditExpandString");
    public static string DialogEditMultiString => Get("DialogEditMultiString");
    public static string DialogEditDWORD => Get("DialogEditDWORD");
    public static string DialogEditQWORD => Get("DialogEditQWORD");
    public static string DialogEditBinary => Get("DialogEditBinary");
    public static string DialogFind => Get("DialogFind");
    public static string DialogExport => Get("DialogExport");
    public static string DialogAddFavorite => Get("DialogAddFavorite");
    public static string DialogRemoveFavorite => Get("DialogRemoveFavorite");

    // Labels
    public static string LabelValueName => Get("LabelValueName");
    public static string LabelValueData => Get("LabelValueData");
    public static string LabelBase => Get("LabelBase");
    public static string LabelHexadecimal => Get("LabelHexadecimal");
    public static string LabelDecimal => Get("LabelDecimal");
    public static string LabelFindWhat => Get("LabelFindWhat");
    public static string LabelLookAt => Get("LabelLookAt");
    public static string LabelKeys => Get("LabelKeys");
    public static string LabelValues => Get("LabelValues");
    public static string LabelData => Get("LabelData");
    public static string LabelMatchWholeString => Get("LabelMatchWholeString");
    public static string LabelFavoriteName => Get("LabelFavoriteName");
    public static string LabelExportAll => Get("LabelExportAll");
    public static string LabelExportSelectedBranch => Get("LabelExportSelectedBranch");
    public static string LabelExportRange => Get("LabelExportRange");

    // Buttons
    public static string ButtonOK => Get("ButtonOK");
    public static string ButtonCancel => Get("ButtonCancel");
    public static string ButtonFindNext => Get("ButtonFindNext");

    // Messages
    public static string ConfirmDeleteKey => Get("ConfirmDeleteKey");
    public static string ConfirmDeleteValue => Get("ConfirmDeleteValue");
    public static string ConfirmDelete => Get("ConfirmDelete");
    public static string SearchComplete => Get("SearchComplete");
    public static string SearchTitle => Get("SearchTitle");
    public static string ImportSuccess => Get("ImportSuccess");
    public static string ImportError => Get("ImportError");
    public static string ErrorAccessDenied => Get("ErrorAccessDenied");
    public static string ErrorCannotCreate => Get("ErrorCannotCreate");
    public static string ErrorCannotRename => Get("ErrorCannotRename");
    public static string NotImplemented => Get("NotImplemented");
    public static string AboutText => Get("AboutText");
    public static string Searching => Get("Searching");
}
