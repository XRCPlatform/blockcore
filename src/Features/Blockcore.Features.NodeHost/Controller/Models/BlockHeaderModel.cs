using System;
using System.Collections.Generic;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Controllers.Converters;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.DataEncoders;
using Blockcore.Utilities;
using Newtonsoft.Json;

namespace Blockcore.Controllers.Models
{
    [JsonConverter(typeof(ToStringJsonConverter))]
    public class HexModel
    {
        public string Hex { get; set; }

        public HexModel(string hex)
        {
            this.Hex = hex;
        }

        public override string ToString()
        {
            return this.Hex;
        }
    }

    /// <summary>
    /// Data structure for block headers.
    /// Copied from RPC
    /// <see cref="https://bitcoin.org/en/developer-reference#getblockheader"/>
    /// </summary>
    public class BlockHeaderModel
    {
        /// <summary>
        /// Constructs a BlockHeaderModel from a block header object.
        /// </summary>
        /// <param name="blockHeader">The block header.</param>
        public BlockHeaderModel(BlockHeader blockHeader)
        {
            Guard.NotNull(blockHeader, nameof(blockHeader));

            this.Version = (uint)blockHeader.Version;
            this.PreviousBlockHash = blockHeader.HashPrevBlock.ToString();
            this.MerkleRoot = blockHeader.HashMerkleRoot.ToString();
            this.Time = blockHeader.Time;
            byte[] bytes = this.GetBytes(blockHeader.Bits.ToCompact());
            string encodedBytes = Encoders.Hex.EncodeData(bytes);
            this.Bits = encodedBytes;
            this.Nonce = (int)blockHeader.Nonce;
        }

        /// <summary>
        /// Constructs a BlockHeaderModel.
        /// Used when deserializing block header from Json.
        /// </summary>
        public BlockHeaderModel()
        {
        }

        /// <summary>
        /// The blocks version number.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public uint Version { get; private set; }

        /// <summary>
        /// The merkle root for this block encoded as hex in RPC byte order.
        /// </summary>
        [JsonProperty(PropertyName = "merkleroot")]
        public string MerkleRoot { get; private set; }

        /// <summary>
        /// The nonce which was successful at turning this particular block
        /// into one that could be added to the best block chain.
        /// </summary>
        [JsonProperty(PropertyName = "nonce")]
        public int Nonce { get; private set; }

        /// <summary>
        /// The target threshold this block's header had to pass.
        /// </summary>
        [JsonProperty(PropertyName = "bits")]
        public string Bits { get; private set; }

        /// <summary>
        /// The hash of the header of the previous block,
        /// encoded as hex in RPC byte order.
        /// </summary>
        [JsonProperty(PropertyName = "previousblockhash")]
        public string PreviousBlockHash { get; private set; }

        /// <summary>
        /// The block time in seconds since epoch (Jan 1 1970 GMT).
        /// </summary>
        [JsonProperty(PropertyName = "time")]
        public uint Time { get; private set; }

        /// <summary>
        /// Convert compact of miner challenge to byte format,
        /// serialized for transmission via RPC.
        /// </summary>
        /// <param name="compact">Compact representation of challenge.</param>
        /// <returns>Byte representation of challenge.</returns>
        /// <seealso cref="Target"/>
        private byte[] GetBytes(uint compact)
        {
            return new byte[]
            {
                (byte)(compact >> 24),
                (byte)(compact >> 16),
                (byte)(compact >> 8),
                (byte)(compact)
            };
        }

        /// <summary>
        ///   The Hash of the block.
        /// </summary>
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        /// <summary>
        ///   The number of confirmations for the current block.
        /// </summary>
        [JsonProperty(PropertyName = "confirmations")]
        public int Confirmations { get; set; }

        /// <summary>
        ///   The block height or index
        /// </summary>
        [JsonProperty(PropertyName = "height")]
        public int Height { get; set; }

        /// <summary>
        ///   The merkle root
        /// </summary>
        [JsonProperty(PropertyName = "merkleroot")]
        public string Merkleroot { get; set; }

        /// <summary>
        ///   The block version formatted in hexadecimal
        /// </summary>
        [JsonProperty(PropertyName = "versionhex")]
        public string VersionHex { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "mediantime")]
        public long Mediantime { get; set; }

        /// <summary>
        ///   The difficulty
        /// </summary>
        [JsonProperty(PropertyName = "difficulty")]
        public double Difficulty { get; set; }

        /// <summary>
        ///    Expected number of hashes required to produce the chain up to this block (in hex)
        /// </summary>
        [JsonProperty(PropertyName = "chainwork")]
        public string Chainwork { get; set; }

        /// <summary>
        ///   The hash of the next block
        /// </summary>
        [JsonProperty(PropertyName = "nextblockhash")]
        public string NextBlockHash { get; set; }

        /// <summary>
        /// Transaction count
        /// </summary>
        [JsonProperty(PropertyName = "nTx")]
        public int NTx { get; set; }
    }
}
