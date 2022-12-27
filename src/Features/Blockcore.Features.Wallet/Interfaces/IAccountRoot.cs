using System;
using System.Collections.Generic;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using Blockcore.Networks;
using NBitcoin;

namespace Blockcore.Features.Wallet.Interfaces
{
    public interface IAccountRoot
    {
        ICollection<HdAccount> Accounts { get; set; }
        int CoinType { get; set; }
        uint256 LastBlockSyncedHash { get; set; }
        int? LastBlockSyncedHeight { get; set; }

        HdAccount AddNewAccount(ExtPubKey accountExtPubKey, int accountIndex, Network network, DateTimeOffset accountCreationTime, int purpose);
        HdAccount AddNewAccount(string password, string encryptedSeed, byte[] chainCode, Network network, DateTimeOffset accountCreationTime, int purpose, int? accountIndex = null, string accountName = null);
        HdAccount CreateAccount(string password, string encryptedSeed, byte[] chainCode, Network network, DateTimeOffset accountCreationTime, int purpose, int newAccountIndex, string newAccountName = null);
        HdAccount GetAccountByName(string accountName);
        HdAccount GetFirstUnusedAccount(IWalletStore walletStore);
        HdAccount GetAccountByIndex(int index);
    }
}