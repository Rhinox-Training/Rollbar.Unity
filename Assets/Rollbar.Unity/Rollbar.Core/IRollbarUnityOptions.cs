using Rollbar.Common;

namespace Rollbar
{
    public interface IRollbarUnityOptions
        : IReconfigurable<IRollbarUnityOptions, IRollbarUnityOptions>
    {
        public int SecondsBeforeExceptionGetsRelogged { get; }
    }
}