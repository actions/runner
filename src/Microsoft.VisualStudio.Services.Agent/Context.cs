using System;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IContext
    {
        void Error(Exception ex);

        void Error(String message);

        void Error(String format, params Object[] args);

        void Warning(String message);

        void Warning(String format, params Object[] args);

        void Info(String message);

        void Info(String format, params Object[] args);

        void Verbose(String message);

        void Verbose(String format, params Object[] args);
    }

    public abstract class Context : IContext
    {
        public void Error(Exception ex)
        {
            this.Write(LogLevel.Error, ex.Message);
            this.Write(LogLevel.Verbose, ex.ToString());
        }

        public void Error(String message)
        {
            this.Write(LogLevel.Error, message);
        }

        public void Error(String format, params Object[] args)
        {
            this.Write(LogLevel.Error, Format(format, args));
        }

        public void Warning(String message)
        {
            this.Write(LogLevel.Warning, message);
        }

        public void Warning(String format, params Object[] args)
        {
            this.Write(LogLevel.Warning, Format(format, args));
        }

        public void Info(String message)
        {
            this.Write(LogLevel.Info, message);
        }

        public void Info(String format, params Object[] args)
        {
            this.Write(LogLevel.Info, Format(format, args));
        }

        public void Verbose(String message)
        {
            this.Write(LogLevel.Verbose, message);
        }

        public void Verbose(String format, params Object[] args)
        {
            this.Write(LogLevel.Verbose, Format(format, args));
        }

        protected abstract void Write(LogLevel level, String message);

        private static String Format(String format, params Object[] args)
        {
            return String.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}