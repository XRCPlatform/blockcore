using System;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Rules;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.XRC.Rules
{
    public class XRCCheckTimeDelayPowRule : HeaderValidationConsensusRule
    {
        /// <inheritdoc />
        /// <exception cref="ConsensusErrors.TimeTooNew">Thrown if block's timestamp is mining attack.</exception>
        public override void Run(RuleContext context)
        {
            ChainedHeader chainedHeader = context.ValidationContext.ChainedHeaderToValidate;

            // Mining attack protection.
            if (chainedHeader.Header.BlockTime < (chainedHeader.Previous.Header.BlockTime + TimeSpan.FromMinutes(8)))
            {
                this.Logger.LogTrace("(-)[TIME_TOO_NEW]");
                ConsensusErrors.TimeTooNew.Throw();
            }
        }
    }
}
