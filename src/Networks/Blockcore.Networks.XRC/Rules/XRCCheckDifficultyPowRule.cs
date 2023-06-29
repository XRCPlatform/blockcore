using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Rules;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.BouncyCastle.math;
using Blockcore.Networks.XRC.Consensus;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.XRC.Rules
{
    public class XRCCheckDifficultyPowRule : HeaderValidationConsensusRule
    {
        private static BigInteger pow256 = BigInteger.ValueOf(2).Pow(256);

        public override void Run(RuleContext context)
        {
            if (!CheckProofOfWork((XRCBlockHeader)context.ValidationContext.ChainedHeaderToValidate.Header))
                ConsensusErrors.HighHash.Throw();

            Target nextWorkRequired = ((XRCBlockHeader)context.ValidationContext.ChainedHeaderToValidate.Header)
                .GetWorkRequired(
                context.ValidationContext.ChainedHeaderToValidate,
                (XRCConsensus)this.Parent.Network.Consensus);

            XRCBlockHeader header = (XRCBlockHeader)context.ValidationContext.ChainedHeaderToValidate.Header;

            // Check proof of work.
            if (header.Bits != nextWorkRequired)
            {
                this.Logger.LogTrace("(-)[BAD_DIFF_BITS]");
                ConsensusErrors.BadDiffBits.Throw();
            }
        }

        private bool CheckProofOfWork(XRCBlockHeader header)
        {
            BigInteger bits = header.Bits.ToBigInteger();
            if ((bits.CompareTo(BigInteger.Zero) <= 0) || (bits.CompareTo(pow256) >= 0))
                return false;

            return header.GetPoWHash() <= header.Bits.ToUInt256();
        }
    }
}