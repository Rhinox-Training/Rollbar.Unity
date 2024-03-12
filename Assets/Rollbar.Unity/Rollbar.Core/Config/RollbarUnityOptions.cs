using Rollbar.Common;

namespace Rollbar
{
    public class RollbarUnityOptions
        : ReconfigurableBase<RollbarUnityOptions, IRollbarUnityOptions>
            , IRollbarUnityOptions
    {
        public override Validator GetValidator()
        {
            return null;
        }

        public RollbarUnityOptions Reconfigure(RollbarUnityOptions likeMe)
        {
            return base.Reconfigure(likeMe);
        }

        /// <summary>
        /// Reconfigures this object similar to the specified one.
        /// </summary>
        /// <param name="likeMe">The pre-configured instance to be cloned in terms of its configuration/settings.</param>
        /// <returns>Reconfigured instance.</returns>
        IRollbarUnityOptions IReconfigurable<IRollbarUnityOptions, IRollbarUnityOptions>.Reconfigure(IRollbarUnityOptions likeMe)
        {
            return this.Reconfigure(likeMe);
        }

        public int SecondsBeforeExceptionGetsRelogged { get; set; } = 300;
    }
}