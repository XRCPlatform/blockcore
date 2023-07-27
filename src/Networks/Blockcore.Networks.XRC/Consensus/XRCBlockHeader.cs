using System.Collections.Generic;
using System;
using System.IO;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Networks.XRC.Crypto;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.Crypto;
using Blockcore.NBitcoin.BouncyCastle.math;
using Blockcore.Networks.XRC.Consensus;
using System.Linq;

namespace Blockcore.Networks.XRC.Consensus
{
    public class XRCBlockHeader : PosBlockHeader
    {
        private const int X13_HASH_MINERUNCOMPATIBLE = 1;
        private const int X13_HASH_MINERCOMPATIBLE = 2;
        int MedianTimeSpan = 11;
        int MedianTimeSpanV2 = 5;

        public XRCConsensusProtocol Consensus { get; set; }

        public XRCBlockHeader(XRCConsensusProtocol consensus)
        {
            this.Consensus = consensus;
        }

        public override uint256 GetHash()
        {
            uint256 hash = null;
            uint256[] innerHashes = this.hashes;

            if (innerHashes != null)
                hash = innerHashes[0];

            if (hash != null)
                return hash;

            using (var hs = new HashStream())
            {
                this.ReadWriteHashingStream(new BitcoinStream(hs, true));
                hash = hs.GetHash();
            }

            innerHashes = this.hashes;
            if (innerHashes != null)
            {
                innerHashes[0] = hash;
            }

            return hash;
        }

        public override uint256 GetPoWHash()
        {
            using (var ms = new MemoryStream())
            {
                this.ReadWriteHashingStream(new BitcoinStream(ms, true));
                
                if (this.Time > this.Consensus.PowDigiShieldX11Time)
                {
                    return XRCHashX11.Instance.Hash(this.ToBytes());
                }
                //block HardFork - height: 1648, time - 1541879606, hash - a75312cab7cf2a6ee89ab33bcb0ab9f96676fbc965041d50b889d9469eff6cdb 
                else if (this.Time > this.Consensus.PowLimit2Time)
                {
                    return XRCHashX13.Instance.Hash(this.ToBytes(), X13_HASH_MINERCOMPATIBLE);
                }
                else
                {
                    return XRCHashX13.Instance.Hash(this.ToBytes(), X13_HASH_MINERUNCOMPATIBLE);
                }
            }
        }

        public new Target GetWorkRequired(Network network, ChainedHeader prev)
        {
            return GetWorkRequired(new ChainedHeader(this, this.GetHash(), prev), (XRCConsensus)network.Consensus);
        }

        public Target GetWorkRequired(ChainedHeader chainedHeaderToValidate, XRCConsensus consensus)
        {
            // Genesis block.
            if (chainedHeaderToValidate.Height == 0)
                return consensus.PowLimit2;

            var XRCConsensusProtocol = (XRCConsensusProtocol)consensus.ConsensusFactory.Protocol;

            //hard fork
            if (chainedHeaderToValidate.Height == XRCConsensusProtocol.PowLimit2Height + 1)
                return consensus.PowLimit;

            //hard fork 2 - DigiShield + X11
            if ((chainedHeaderToValidate.Height > XRCConsensusProtocol.PowDigiShieldX11Height)
                && (chainedHeaderToValidate.Height <= XRCConsensusProtocol.PowDigiShieldX11V2Height))
                return GetWorkRequiredDigiShield(chainedHeaderToValidate, consensus);

            //hard fork 3 - DigiShield V2 + X11
            //  if (chainedHeaderToValidate.Height > XRCConsensusProtocol.PowDigiShieldX11V2Height)
            //    return GetWorkRequiredDigiShieldV2(chainedHeaderToValidate, consensus);

            Target proofOfWorkLimit;

            // Hard fork to higher difficulty
            if (chainedHeaderToValidate.Height > XRCConsensusProtocol.PowLimit2Height)
            {
                proofOfWorkLimit = consensus.PowLimit;
            }
            else
            {
                proofOfWorkLimit = consensus.PowLimit2;
            }

            ChainedHeader lastBlock = chainedHeaderToValidate.Previous;
            int height = chainedHeaderToValidate.Height;

            if (lastBlock == null)
                return proofOfWorkLimit;

            long difficultyAdjustmentInterval = GetDifficultyAdjustmentInterval(consensus);

            // Only change once per interval.
            if ((height) % difficultyAdjustmentInterval != 0)
            {
                if (consensus.PowAllowMinDifficultyBlocks)
                {
                    // Special difficulty rule for testnet:
                    // If the new block's timestamp is more than 2* 10 minutes
                    // then allow mining of a min-difficulty block.
                    if (chainedHeaderToValidate.Header.BlockTime > (lastBlock.Header.BlockTime + TimeSpan.FromTicks(consensus.TargetSpacing.Ticks * 2)))
                        return proofOfWorkLimit;

                    // Return the last non-special-min-difficulty-rules-block.
                    ChainedHeader chainedHeader = lastBlock;
                    while ((chainedHeader.Previous != null) && ((chainedHeader.Height % difficultyAdjustmentInterval) != 0) && (chainedHeader.Header.Bits == proofOfWorkLimit))
                        chainedHeader = chainedHeader.Previous;

                    return chainedHeader.Header.Bits;
                }

                return lastBlock.Header.Bits;
            }

            // Go back by what we want to be 14 days worth of blocks.
            long pastHeight = lastBlock.Height - (difficultyAdjustmentInterval - 1);

            ChainedHeader firstChainedHeader = chainedHeaderToValidate.GetAncestor((int)pastHeight);
            if (firstChainedHeader == null)
                throw new NotSupportedException("Can only calculate work of a full chain");

            if (consensus.PowNoRetargeting)
                return lastBlock.Header.Bits;

            // Limit adjustment step.
            TimeSpan actualTimespan = lastBlock.Header.BlockTime - firstChainedHeader.Header.BlockTime;
            if (actualTimespan < TimeSpan.FromTicks(consensus.TargetTimespan.Ticks / 4))
                actualTimespan = TimeSpan.FromTicks(consensus.TargetTimespan.Ticks / 4);
            if (actualTimespan > TimeSpan.FromTicks(consensus.TargetTimespan.Ticks * 4))
                actualTimespan = TimeSpan.FromTicks(consensus.TargetTimespan.Ticks * 4);

            // Retarget.
            BigInteger newTarget = lastBlock.Header.Bits.ToBigInteger();
            newTarget = newTarget.Multiply(BigInteger.ValueOf((long)actualTimespan.TotalSeconds));
            newTarget = newTarget.Divide(BigInteger.ValueOf((long)consensus.TargetTimespan.TotalSeconds));

            var finalTarget = new Target(newTarget);
            if (finalTarget > proofOfWorkLimit)
                finalTarget = proofOfWorkLimit;

            return finalTarget;
        }

        public Target GetWorkRequiredDigiShield(ChainedHeader chainedHeaderToValidate, XRCConsensus consensus)
        {
            var nAveragingInterval = 10 * 5; // block
            var multiAlgoTargetSpacingV4 = 10 * 60; // seconds
            var nAveragingTargetTimespanV4 = nAveragingInterval * multiAlgoTargetSpacingV4;
            var nMaxAdjustDownV4 = 16;
            var nMaxAdjustUpV4 = 8;
            var nMinActualTimespanV4 = TimeSpan.FromSeconds(nAveragingTargetTimespanV4 * (100 - nMaxAdjustUpV4) / 100);
            var nMaxActualTimespanV4 = TimeSpan.FromSeconds(nAveragingTargetTimespanV4 * (100 + nMaxAdjustDownV4) / 100);

            var height = chainedHeaderToValidate.Height;
            Target proofOfWorkLimit = consensus.PowLimit2;
            ChainedHeader lastBlock = chainedHeaderToValidate.Previous;
            ChainedHeader firstBlock = chainedHeaderToValidate.GetAncestor(height - nAveragingInterval);

            var XRCConsensusProtocol = (XRCConsensusProtocol)consensus.ConsensusFactory.Protocol;

            if (((height - XRCConsensusProtocol.PowDigiShieldX11Height) <= (nAveragingInterval + this.MedianTimeSpan))
                && (consensus.CoinType == (int)XRCCoinType.CoinTypes.XRCMain))
            {
                return new Target(new uint256("000000000001a61a000000000000000000000000000000000000000000000000"));
            }

            // Limit adjustment step
            // Use medians to prevent time-warp attacks
            TimeSpan nActualTimespan = GetAverageTimePast(lastBlock, this.MedianTimeSpan) - GetAverageTimePast(firstBlock, this.MedianTimeSpan);
            nActualTimespan = TimeSpan.FromSeconds(nAveragingTargetTimespanV4
                                    + (nActualTimespan.TotalSeconds - nAveragingTargetTimespanV4) / 4);

            if (nActualTimespan < nMinActualTimespanV4)
                nActualTimespan = nMinActualTimespanV4;
            if (nActualTimespan > nMaxActualTimespanV4)
                nActualTimespan = nMaxActualTimespanV4;

            // Retarget.
            BigInteger newTarget = lastBlock.Header.Bits.ToBigInteger();

            newTarget = newTarget.Multiply(BigInteger.ValueOf((long)nActualTimespan.TotalSeconds));
            newTarget = newTarget.Divide(BigInteger.ValueOf((long)nAveragingTargetTimespanV4));

            var finalTarget = new Target(newTarget);
            if (finalTarget > proofOfWorkLimit)
                finalTarget = proofOfWorkLimit;

            return finalTarget;
        }

        public Target GetWorkRequiredDigiShieldV2(ChainedHeader chainedHeaderToValidate, XRCConsensus consensus)
        {
            var nAveragingInterval = 16; // block
            var multiAlgoTargetSpacingV4 = 10 * 60; // seconds
            var nAveragingTargetTimespanV4 = nAveragingInterval * multiAlgoTargetSpacingV4;
            var nMaxAdjustDownV4 = 80;
            var nMaxAdjustUpV4 = 64;
            var nMinActualTimespanV4 = TimeSpan.FromSeconds(nAveragingTargetTimespanV4 * (100 - nMaxAdjustUpV4) / 100);
            var nMaxActualTimespanV4 = TimeSpan.FromSeconds(nAveragingTargetTimespanV4 * (100 + nMaxAdjustDownV4) / 100);

            var height = chainedHeaderToValidate.Height;
            Target proofOfWorkLimit = consensus.PowLimit2;
            ChainedHeader lastBlock = chainedHeaderToValidate.Previous;
            ChainedHeader firstBlock = chainedHeaderToValidate.GetAncestor(height - nAveragingInterval);

            // Limit adjustment step
            // Use medians to prevent time-warp attacks
            TimeSpan nActualTimespan = GetAverageTimePast(lastBlock, this.MedianTimeSpanV2) - GetAverageTimePast(firstBlock, this.MedianTimeSpanV2);
            nActualTimespan = TimeSpan.FromSeconds(nAveragingTargetTimespanV4
                                    + (nActualTimespan.TotalSeconds - nAveragingTargetTimespanV4) / 4);

            if (nActualTimespan < nMinActualTimespanV4)
                nActualTimespan = nMinActualTimespanV4;
            if (nActualTimespan > nMaxActualTimespanV4)
                nActualTimespan = nMaxActualTimespanV4;

            // Retarget.
            BigInteger newTarget = lastBlock.Header.Bits.ToBigInteger();

            newTarget = newTarget.Multiply(BigInteger.ValueOf((long)nActualTimespan.TotalSeconds));
            newTarget = newTarget.Divide(BigInteger.ValueOf((long)nAveragingTargetTimespanV4));

            var finalTarget = new Target(newTarget);
            if (finalTarget > proofOfWorkLimit)
                finalTarget = proofOfWorkLimit;

            return finalTarget;
        }

        public DateTimeOffset GetAverageTimePast(ChainedHeader chainedHeaderToValidate, int medianTimeSpan)
        {
            var median = new List<DateTimeOffset>();

            ChainedHeader chainedHeader = chainedHeaderToValidate;
            for (int i = 0; i < medianTimeSpan && chainedHeader != null; i++, chainedHeader = chainedHeader.Previous)
                median.Add(chainedHeader.Header.BlockTime);

            median.Sort();

            DateTimeOffset firstTimespan = median.First();
            DateTimeOffset lastTimespan = median.Last();
            TimeSpan differenceTimespan = lastTimespan - firstTimespan;
            var timespan = differenceTimespan.TotalSeconds / 2;
            DateTimeOffset averageDateTime = firstTimespan.AddSeconds((long)timespan);

            return averageDateTime;
        }

        private long GetDifficultyAdjustmentInterval(IConsensus consensus)
        {
            return (long)consensus.TargetTimespan.TotalSeconds / (long)consensus.TargetSpacing.TotalSeconds;
        }
    }
}
