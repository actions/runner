using System;
using System.ComponentModel;

namespace GitHub.Services.WebApi.Utilities.Internal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ClientSkuNames
    {
        public const string Unknown = "Unknown";

        public static class Dev12
        {
            public const string Ultimate = "Ultimate";
            public const string Premium = "Premium";
            public const string Pro = "Pro";
            public const string TeamExplorer = "TE";
            public const string Other = "Other";
        }

        public static class Dev14
        {
            public const string Enterprise = "Enterprise";
            public const string Ultimate = "Ultimate";
            public const string Premium = "Premium";
            public const string Pro = "Pro";
            public const string Community = "Community";
            public const string TeamExplorer = "TE";
            public const string Other = "Other";
        }

        public static class Dev15
        {
            public const string Enterprise = "Enterprise";
            public const string Pro = "Pro";
            public const string Community = "Community";
            public const string TE = "TE";
            public const string Sql = "Sql";
            public const string IntShell = "IntShell";
            public const string Other = "Other";
        }

        public static class Dev16
        {
            public const string Enterprise = "Enterprise";
            public const string Pro = "Pro";
            public const string Community = "Community";
            public const string TE = "TE";
            public const string Sql = "Sql";
            public const string IntShell = "IntShell";
            public const string Other = "Other";
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ClientSkuNumbers
    {
        public const int Unknown = 0;
        public const int FutureOutOfRange = 1;
        public const int Whidbey = 2;
        public const int Orcas = 3;
        public const int Dev10 = 4;

        // Dev11
        public const int Dev11TeamExplorer = 5;
        public const int Dev11Pro = 6;
        public const int Dev11Premium = 7;
        public const int Dev11Ultimate = 8;
        public const int Dev11Other = 9;
        public const int Dev11TeamExplorerEveryWhere = 11;
        public const int Dev11CrossPlatformCommandline = 12;
        public const int Dev11SdkForJava = 13;

        // Dev12
        public const int Dev12TeamExplorer = 14;
        public const int Dev12Pro = 15;
        public const int Dev12Premium = 16;
        public const int Dev12Ultimate = 17;
        public const int Dev12Other = 18;
        public const int Dev12TeamExplorerEveryWhere = 19;
        public const int Dev12CrossPlatformCommandline = 20;
        public const int Dev12SdkForJava = 21;

        // Dev14. From 30 to 49
        public const int Dev14TeamExplorer = 30;
        public const int Dev14Pro = 31;
        public const int Dev14Premium = 32;
        public const int Dev14Ultimate = 33;
        public const int Dev14TeamExplorerEveryWhere = 34;
        public const int Dev14CrossPlatformCommandline = 35;
        public const int Dev14SdkForJava = 36;
        public const int Dev14Enterprise = 37;
        public const int Dev14Community = 38;

        public const int Dev14Other = 49;

        // Dev15. From 50 to 69
        public const int Dev15Pro = 50;
        public const int Dev15TeamExplorerEveryWhere = 51;
        public const int Dev15CrossPlatformCommandline = 52;
        public const int Dev15SdkForJava = 53;
        public const int Dev15Enterprise = 54;
        public const int Dev15Community = 55;
        public const int Dev15TeamExplorer = 56;
        public const int Dev15Sql = 57;
        public const int Dev15IntShell = 58;
        public const int Dev15Other = 59;


        // Dev16. From 70 to 89
        public const int Dev16Pro = 70;
        public const int Dev16TeamExplorerEveryWhere = 71;
        public const int Dev16CrossPlatformCommandline = 72;
        public const int Dev16SdkForJava = 73;
        public const int Dev16Enterprise = 74;
        public const int Dev16Community = 75;
        public const int Dev16TeamExplorer = 76;
        public const int Dev16Sql = 77;
        public const int Dev16IntShell = 78;
        public const int Dev16Other = 79;
    }
}
