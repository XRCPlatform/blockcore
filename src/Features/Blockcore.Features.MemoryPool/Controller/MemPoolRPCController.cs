using System;
using System.Collections.Generic;
using System.Net;
using Blockcore.Base;
using Blockcore.Configuration;
using Blockcore.Controllers;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.MemoryPool.Models;
using Blockcore.Interfaces;
using Blockcore.NBitcoin;
using Blockcore.Networks;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonErrors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static Blockcore.Features.MemoryPool.TxMempool;

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

        /// <summary>
        /// If txid is in the mempool, returns all in-mempool ancestors.
        /// </summary>
        /// <param name="txid">The transaction id (must be in mempool).</param>
        /// <param name="verbose">True for a json object, false for array of transaction ids</param>
        /// <returns>(List, GetMemPoolEntry or List, string) Return object with informations.</returns>
        [ActionName("getmempoolancestors")]
        [ActionDescription("If txid is in the mempool, returns all in-mempool ancestors.")]
        public List<string> GetMempoolAncestors(string txid, bool verbose)
        {
            Guard.NotEmpty(txid, nameof(txid));

            var entryTx = this.MemPool.GetEntry(new uint256(txid));
            Guard.NotNull(entryTx, "entryTx does not exist.");

            var setAncestors = new SetEntries();
            long nNoLimit = long.MaxValue;
            this.MemPool.CalculateMemPoolAncestors(entryTx, setAncestors, nNoLimit, nNoLimit, nNoLimit, nNoLimit, out dummy, false);

            var listTxHash = new List<string>();
            if (setAncestors != null)
            {
                foreach (var entry in setAncestors)
                {
                    listTxHash.Add(entry.TransactionHash.ToString());
                }
            }

            return listTxHash;
        }

        /// <summary>
        /// If txid is in the mempool, returns all in-mempool descendants.
        /// </summary>
        /// <param name="txid">The transaction id (must be in mempool).</param>
        /// <param name="verbose">True for a json object, false for array of transaction ids.</param>
        /// <returns>(List, GetMemPoolEntry or List, string) Return object with informations.</returns>
        [ActionName("getmempooldescendants")]
        [ActionDescription("If txid is in the mempool, returns all in-mempool descendants.")]
        public List<string> GetMempoolDescendants(string txid, bool verbose)
        {
            Guard.NotEmpty(txid, nameof(txid));
            var entryTx = this.MemPool.GetEntry(new uint256(txid));
            var setDescendants = new SetEntries();
            this.MemPool.CalculateDescendants(entryTx, setDescendants);

            var listTxHash = new List<string>();

            if (setDescendants != null)
            {
                foreach (var entry in setDescendants)
                {
                    listTxHash.Add(entry.TransactionHash.ToString());
                }
            }

            return listTxHash;
        }
    }
}
