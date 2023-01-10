using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Features.Miner;
using Blockcore.Mining;
using Blockcore.Networks;
using NBitcoin;

namespace Blockcore.Networks.XRC.Components
{
    public sealed class XRCBlockProvider : IBlockProvider
    {
        private readonly Network network;

        /// <summary>Defines how proof of work blocks are built.</summary>
        private readonly XRCPowBlockDefinition powBlockDefinition;

        /// <param name="definitions">A list of block definitions that the builder can utilize.</param>
        public XRCBlockProvider(Network network, IEnumerable<BlockDefinition> definitions)
        {
            this.network = network;

            this.powBlockDefinition = definitions.OfType<XRCPowBlockDefinition>().FirstOrDefault();
        }

        /// <inheritdoc/>
        public BlockTemplate BuildPosBlock(ChainedHeader chainTip, Script script)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public BlockTemplate BuildPowBlock(ChainedHeader chainTip, Script script)
        {
            return this.powBlockDefinition.Build(chainTip, script);
        }

        /// <inheritdoc/>
        public void BlockModified(ChainedHeader chainTip, Block block)
        {
            this.powBlockDefinition.BlockModified(chainTip, block);
        }
    }
}
    
