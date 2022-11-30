using System;
using ISWiAuto22;

namespace ISMUpdate
{
    public class Version
    {
        public Version(string input)
        {
            string[] parts = input.Split('.', ',');

            if (parts.Length > 0) Major = int.Parse(parts[0]);
            if (parts.Length > 1) Minor = int.Parse(parts[1]);
            if (parts.Length > 2) Build = int.Parse(parts[2]);
        }

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }

        public override string ToString()
        {
            return $"{Major}.{Minor:D3}.{Build:D5}";
        }

        public void Apply(VersionChange change)
        {
            Major = ApplyChange(Major, change.Major);
            Minor = ApplyChange(Minor, change.Minor);
            Build = ApplyChange(Build, change.Build);
        }

        protected int ApplyChange(int version, string change)
        {
            if (!string.IsNullOrEmpty(change) && change != "+0")
            {
                int newValue = int.Parse(change);
                if (change.IndexOfAny(new[] { '+', '-' }) == 0)
                {
                    newValue = version + newValue;
                }

                version = newValue;
            }
            return version;
        }
    }

    public class VersionChange
    {
        public VersionChange(string input)
        {
            string[] parts = input.Split('.', ',');

            if (parts.Length > 0) Major = parts[0];
            if (parts.Length > 1) Minor = parts[1];
            if (parts.Length > 2) Build = parts[2];
        }

        public string Major { get; set; } = "+0";
        public string Minor { get; set; } = "+0";
        public string Build { get; set; } = "+0";
    }

    class Program
    {
        private const string ProductCode = "/prod";
        private const string PackageCode = "/pack";
        private const string Version = "/v";
        private const string StringObj = "/s";

        static void Main(string[] args)
        {
            string projPath = "";
            if (args.Length > 0)
            {
                projPath = args[0].ToLower();
            }

            ISWiProject wiProj = new ISWiProject();
            wiProj.OpenProject(projPath, false);

            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                string[] parts = arg.Split(new[] { ':' }, 2);
                string option = parts[0].ToLower();
                string value = parts.Length > 1 ? parts[1] : "";

                switch (option)
                {
                    case ProductCode:
                        wiProj.ProductCode = wiProj.GenerateGUID();
                        break;
                    case PackageCode:
                        try
                        {
                            wiProj.PackageCode = wiProj.GenerateGUID();
                        }
                        catch (Exception exc)
                        {
                            // package code is not available for InstallScript MSI projects
                            // skip this error
                        }

                        break;
                    case StringObj:
                        string[] sPair = value.Split(new[] { ':' }, 2);
                        string sName  = sPair[0].Trim('"');
                        string sValue = sPair[1].Trim('"');

                        foreach (dynamic lang in wiProj.ISWiLanguages)
                        {
                            if (lang != null && 
                                lang.ISWiStringEntries[sName] != null && 
                                !string.Equals(lang.ISWiStringEntries[sName].Value, sValue))
                            {
                                lang.ISWiStringEntries[sName].Value = sValue;
                            }
                        }

                        break;
                    case Version:
                        Version projVersion = new Version(wiProj.ProductVersion);
                        VersionChange change = new VersionChange(parts[1].Trim('"'));

                        projVersion.Apply(change);

                        wiProj.ProductVersion = projVersion.ToString();
                        break;
                }
            }

            wiProj.SaveProject();
            wiProj.CloseProject();
        }
    }
}
