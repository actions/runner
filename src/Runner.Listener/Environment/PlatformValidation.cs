using System;
using System.Runtime.InteropServices;
using GitHub.Runner.Common;
using System.Linq;

namespace GitHub.Runner.Listener
{
    public sealed class PlatformValidation
    {
        // list of supported platform
        private static readonly Constants.OSPlatform[] SupportedPlatforms = {Constants.OSPlatform.Linux, Constants.OSPlatform.Windows, Constants.OSPlatform.OSX};

        private PlatformValidation() => this.IsValid = true;

        public string Message { get; private set; }
        
        public bool IsValid { get; private set; }

        /**
         * Validate the binaries intended for one OS are not running on a different OS.
         */
        public static PlatformValidation Validate(Constants.OSPlatform platform, Func<OSPlatform, bool> checkOperatingSystem = null)
        {
            checkOperatingSystem ??= RuntimeInformation.IsOSPlatform; // default to runtime function if none is passed
            
            if (!SupportedPlatforms.Contains(platform))
            {
                // right now this should not be possible because there is no unsupported platform in the list of constants
                return new PlatformValidation
                {
                    Message =
                        $"Running the runner on this platform is not supported. The current platform is {RuntimeInformation.OSDescription} and it was built for {platform.ToString()}.",
                    IsValid = false
                };
            }

            if (!checkOperatingSystem(platform.ToInteropPlatform()))
            {
                return new PlatformValidation
                {
                    Message =
                        $"This runner version is built for {platform.ToString()}. Please install a correct build for your OS.",
                    IsValid = false
                };
            }

            return new PlatformValidation();
        }
    }
}
