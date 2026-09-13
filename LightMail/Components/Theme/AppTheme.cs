using MudBlazor;

namespace MailApp.Theme;

public static class AppTheme
{
    public static MudTheme Dark => new()
    {
        PaletteDark = new PaletteDark
        {
            // Accent Colors
            Primary = "#D2C0A4",
            Secondary = "#8B7355",
            Tertiary = "#E1D6CC",

            // Backgrounds
            Background = "#1A1714",
            Surface = "#24201C",

            // AppBar / Drawer
            AppbarBackground = "#201B17",
            AppbarText = "#F5EBDD",

            DrawerBackground = "#201B17",
            DrawerText = "#F5EBDD",
            DrawerIcon = "#C8B092",

            // Text
            TextPrimary = "#F5EBDD",
            TextSecondary = "#C6B7A5",

            // UI
            ActionDefault = "#D2C0A4",
            Divider = "#3B322B",

            // Status
            Success = "#4ADE80",
            Warning = "#FBBF24",
            Error = "#F87171",
            Info = "#60A5FA",

            Black = "#000000",
            White = "#FFFFFF"
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "260px",
            AppbarHeight = "64px"
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[]
                {
                    "Inter",
                    "Roboto",
                    "Helvetica",
                    "Arial",
                    "sans-serif"
                }
            }
        }
    };
}


/*
namespace MailApp.Theme;
public static class AppTheme
{
    public static MudTheme Dark => new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#d2c0a4",           
            Secondary = "#453c32",       
            Tertiary = "#e1d6cc",

            Background = "#916959",         
            Surface = "#b8a488",          

            AppbarBackground = "#6b4d41",
            AppbarText = "#faeccd",

            DrawerBackground = "#6b4d41",
            DrawerText = "#faeccd",
            DrawerIcon = "#926959",

            TextPrimary = "#453c32",
            TextSecondary = "#453c32",

            ActionDefault = "#453c32",
            Divider = "#453c32",

            Success = "#34D399",
            Warning = "#FBBF24",
            Error = "#F87171",
            Info = "#60A5FA",

            Black = "#0B0A10",
            White = "#FFFFFF"
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "260px",
            AppbarHeight = "64px"
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "Roboto", "Helvetica", "Arial", "sans-serif" }
            }
        }
    };
}*/
