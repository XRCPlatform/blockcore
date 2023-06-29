using System.Linq;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.Rules.UtxosetRules;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.Miner;
using Blockcore.Mining;
using Blockcore.Networks;
using Blockcore.Networks.XRC.Consensus;
using Blockcore.Utilities;
using Blockcore.Utilities.Extensions;
using Microsoft.Extensions.Logging;
using Blockcore.NBitcoin;

namespace Blockcore.Networks.XRC.Components
{
    public class XRCPowBlockDefinition : BlockDefinition
    {
        private readonly IConsensusRuleEngine consensusRules;
        private readonly ILogger logger;

        public XRCPowBlockDefinition(
            IConsensusManager consensusManager,
            IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory,
            ITxMempool mempool,
            MempoolSchedulerLock mempoolLock,
            MinerSettings minerSettings,
            Network network,
            IConsensusRuleEngine consensusRules,
            NodeDeployments nodeDeployments,
            BlockDefinitionOptions options = null)
            : base(consensusManager, dateTimeProvider, loggerFactory, mempool, mempoolLock, minerSettings, network, nodeDeployments)
        {
            this.consensusRules = consensusRules;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public override void AddToBlock(TxMempoolEntry mempoolEntry)
        {
            this.AddTransactionToBlock(mempoolEntry.Transaction);
            this.UpdateBlockStatistics(mempoolEntry);
            this.UpdateTotalFees(mempoolEntry.Fee);
        }

        /// <summary>
        /// Configures (resets) the builder to its default state
        /// before constructing a new block.
        /// </summary>
        private void Configure()
        {
            this.BlockSize = 4000;
            this.BlockTemplate = new BlockTemplate(this.Network);
            this.BlockTx = 0;
            this.BlockWeight = 4000;
            this.BlockSigOpsCost = 400;
            this.fees = 0;
            this.inBlock = new TxMempool.SetEntries();
            this.IncludeWitness = false;
        }

        /// <inheritdoc/>
        public override BlockTemplate Build(ChainedHeader chainTip, Script scriptPubKey)
        {
            this.Configure();

            this.ChainTip = chainTip;

            this.block = this.BlockTemplate.Block;
            this.scriptPubKey = scriptPubKey;

            this.CreateCoinbase();
            this.ComputeBlockVersion();

            this.MedianTimePast = Utils.DateTimeToUnixTime(this.ChainTip.GetMedianTimePast());
            this.LockTimeCutoff = MempoolValidator.StandardLocktimeVerifyFlags.HasFlag(Transaction.LockTimeFlags.MedianTimePast)
                ? this.MedianTimePast
                : this.block.Header.Time;

            // Decide whether to include witness transactions
            this.IncludeWitness = this.IsWitnessEnabled(chainTip);

            // Add transactions from the mempool
            this.AddTransactions(out int nPackagesSelected, out int nDescendantsUpdated);

            this.LastBlockTx = this.BlockTx;
            this.LastBlockSize = this.BlockSize;
            this.LastBlockWeight = this.BlockWeight;

            var coinviewRule = this.ConsensusManager.ConsensusRules.GetRule<CheckUtxosetRule>();
            this.coinbase.Outputs[0].Value = this.fees + coinviewRule.GetProofOfWorkReward(this.height);
            this.BlockTemplate.TotalFee = this.fees;

            // We need the fee details per transaction to be readily available in case we have to remove transactions from the block later.
            this.BlockTemplate.FeeDetails = this.inBlock.Select(i => new { i.TransactionHash, i.Fee }).ToDictionary(d => d.TransactionHash, d => d.Fee);

            if (this.IncludeWitness)
            {
                this.AddOrUpdateCoinbaseCommitmentToBlock(this.block);
            }

            int nSerializeSize = this.block.GetSerializedSize();
            this.logger.LogDebug("Serialized size is {0} bytes, block weight is {1}, number of txs is {2}, tx fees are {3}, number of sigops is {4}.", nSerializeSize, this.block.GetBlockWeight(this.Network.Consensus), this.BlockTx, this.fees, this.BlockSigOpsCost);

            this.UpdateHeaders();

            return this.BlockTemplate;
        }

        /// <inheritdoc/>
        public override void UpdateHeaders()
        {
            base.UpdateBaseHeaders();

            this.block.Header.Bits = ((XRCBlockHeader)this.block.Header).GetWorkRequired(this.Network, this.ChainTip);
        }
    }
}
