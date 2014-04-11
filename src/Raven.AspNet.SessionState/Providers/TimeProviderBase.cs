using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raven.AspNet.SessionState.Providers
{
    public abstract class TimeProviderBase
    {
        private static TimeProviderBase current = DefaultTimeProvider.Instance;

        public static TimeProviderBase Current
        {
            get { return TimeProviderBase.current; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                TimeProviderBase.current = value;
            }
        }

        public abstract DateTime UtcNow { get; }

        public static void ResetToDefault()
        {
            TimeProviderBase.current = DefaultTimeProvider.Instance;
        }
    }

    public class DefaultTimeProvider : TimeProviderBase
    {
        private readonly static DefaultTimeProvider instance = 
            new DefaultTimeProvider();

        private DefaultTimeProvider() { }

        public override DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }

        public static DefaultTimeProvider Instance
        {
            get { return DefaultTimeProvider.instance; }
        }
    }
}
