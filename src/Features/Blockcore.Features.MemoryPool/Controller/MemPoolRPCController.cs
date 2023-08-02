using System.Collections.Generic;
using Blockcore.Base;
using Blockcore.Configuration;
using Blockcore.Controllers;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.MemoryPool.Models;
using Blockcore.Interfaces;
using Blockcore.NBitcoin;
using Blockcore.Networks;
using Blockcore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Blockcore.Features.MemoryPool.Controller
{
    public class MemPoolRPCController : FeatureController
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>An interface implementation used to retrieve unspent transactions from a pooled source.</summary>
        private readonly IPooledGetUnspentTransaction pooledGetUnspentTransaction;

        /// <summary>An interface implementation used to retrieve unspent transactions.</summary>
        private readonly IGetUnspentTransaction getUnspentTransaction;

        /// <summary>
        /// The mempool manager.
        /// </summary>
        public MempoolManager MempoolManager { get; private set; }

        /// <summary>
        ///  Actual mempool.
        /// </summary>
        public ITxMempool MemPool { get; private set; }

        public MemPoolRPCController(
            ILoggerFactory loggerFactory,
            MempoolManager mempoolManager,
            ITxMempool mempool,
            IPooledGetUnspentTransaction pooledGetUnspentTransaction = null,
            IGetUnspentTransaction getUnspentTransaction = null,
            IFullNode fullNode = null,
            NodeSettings nodeSettings = null,
            Network network = null,
            IChainState chainState = null)
            : base(
                  fullNode: fullNode,
                  nodeSettings: nodeSettings,
                  network: network,
                  chainState: chainState)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.pooledGetUnspentTransaction = pooledGetUnspentTransaction;
            this.getUnspentTransaction = getUnspentTransaction;
            this.MempoolManager = mempoolManager;
            this.MemPool = mempool;
        }

        private GetMemPoolEntry GetMemPoolEntryFromTx(TxMempoolEntry entry)
        {
            var resultEntry = new GetMemPoolEntry
            {
                Fee = entry.Fee.ToUnit(MoneyUnit.BTC),
                ModifiedFee = entry.ModifiedFee,
                Size = entry.GetTxSize(),
                Time = entry.Time,
                Height = entry.EntryHeight,
                WtxId = entry.TransactionHash.ToString(),
                DescendantCount = entry.CountWithDescendants,
                DescendantFees = entry.ModFeesWithDescendants.ToUnit(MoneyUnit.BTC),
                DescendantSize = entry.SizeWithDescendants,
                AncestorCount = entry.CountWithAncestors,
                AncestorFees = entry.ModFeesWithAncestors.ToUnit(MoneyUnit.BTC),
                AncestorSize = entry.SizeWithAncestors
            };

            var parents = this.MemPool.GetMemPoolParents(entry);

            if (parents != null)
            {
                resultEntry.Depends = new List<string>();
                foreach (var item in parents)
                {
                    resultEntry.Depends.Add(item.TransactionHash.ToString());
                }
            }

            return resultEntry;
        }

        /// <summary>
        /// Returns mempool data for given transaction.
        /// </summary>
        /// <param name="txid">The transaction id (must be in mempool).</param>
        /// <returns>(GetMemPoolEntry) Return object with informations.</returns>
        [ActionName("getmempoolentry")]
        [ActionDescription("Returns mempool data for given transaction.")]
        public GetMemPoolEntry GetMempoolEntry(string txid)
        {
            Guard.NotEmpty(txid, "txid");
            var entry = this.MemPool.GetEntry(new uint256(txid));
            return GetMemPoolEntryFromTx(entry);
        }
    }
}
