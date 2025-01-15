﻿using SolanaPaymentHD.Models;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Solnet;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Programs;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Solnet.Rpc.Types;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;

[assembly: InternalsVisibleTo("SolanaPaymentTest")]
namespace SolanaPaymentHD.Logic
{
    
    internal class SolanaBlockchainManager
    {
        private string _Rpc;

        public SolanaBlockchainManager(string rpc)
        {
            _Rpc = rpc;
        }

        public void UpdateRpc(string rpc)
        {
            _Rpc = rpc;
        }

        public async Task<decimal> CheckWalletBalance(string address)
        {
            // Initialize the RPC client
            var rpcClient = ClientFactory.GetClient(_Rpc); 

            // Get the balance for the given address
            var balanceResult = await rpcClient.GetBalanceAsync(address);

            if (!balanceResult.WasSuccessful)
            {
                throw new Exception($"Failed to fetch balance for address {address}: {balanceResult.Reason}");
            }

            // Solana returns balances in lamports (1 SOL = 1,000,000,000 lamports)
            decimal balanceInSol = (decimal)balanceResult.Result.Value / 1_000_000_000m;

            return balanceInSol;
        }

        public async Task TransferFunds(PaymentWallet wallet, decimal balance,string target)
        {

            // Initialize the rpc client and a wallet
            var rpcClient = ClientFactory.GetClient(_Rpc);

            // Get the source account
            var fromAccount = wallet;

            // Get a recent block hash to include in the transaction
            var blockHash = await rpcClient.GetLatestBlockHashAsync();
            
            var senderAccount = new Account(wallet.PrivateKey, wallet.Address);

            ulong amountInLamports = (ulong)(balance * 1_000_000_000m);

            // Create the transfer transaction
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(senderAccount.PublicKey)
                .AddInstruction(SystemProgram.Transfer(senderAccount.PublicKey, new PublicKey(target), amountInLamports))
                .Build(senderAccount);

            // Send the transaction
            var sendTransactionResult =await rpcClient.SendTransactionAsync(transaction);


        }

        public async Task<string?> GetLatestPayer(string address,decimal minamount)
        {
            var payeraddress = "";
            try
            {
                // Initialize the rpc client and a wallet
                var rpcClient = ClientFactory.GetClient(_Rpc);
                // Query transaction history for transactions to the given address
                var transactions = await rpcClient.GetSignaturesForAddressAsync(address,limit:5);
                var iii = 0;
                if (transactions != null && transactions.Result.Count > 0)
                {
                    foreach (var transsign in transactions.Result)
                    {
                        // Fetch transaction details to get the payer's address
                        var transactionDetails = await rpcClient.GetTransactionAsync(transsign.Signature);
                        File.WriteAllText(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + iii+".json", JsonSerializer.Serialize(transactionDetails));
                        iii++;
                        if (transactionDetails != null)
                        {
                            if (IsOnlyTransfer(transactionDetails.Result) == false) { continue; }
                            if (IsReceiveSol(transactionDetails.Result.Meta) == false) { continue; }
                            if (IsAboveMinimumAmount(transactionDetails.Result.Meta, minamount) == false) { continue; }

                            var resultbalance = transactionDetails.Result.Meta.PreBalances[0] - transactionDetails.Result.Meta.PostBalances[0];
                            decimal balanceInSol = (decimal)resultbalance / 1_000_000_000m;
                            // Extract the sender's address (payer)
                            var payerAddress = transactionDetails.Result.Transaction.Message.AccountKeys[0].ToString();
                            return payerAddress;
                        }
                    }

                }

                // No transactions found or error fetching details
                return null;
            }
            catch (Exception ex)
            {
                
                // Handle exceptions as per your application's error handling strategy
                Console.WriteLine($"Error fetching latest payer for address {address}: {ex.Message}");
                return null;
            }
        }

        private bool IsReceiveSol(TransactionMeta meta)
        {
            return meta.PreBalances[0] > meta.PostBalances[0];
        }

        private bool IsAboveMinimumAmount(TransactionMeta meta,decimal minimum)
        {
            var resultbalance = meta.PreBalances[0] - meta.PostBalances[0];
            decimal balanceInSol = (decimal)resultbalance / 1_000_000_000m;
            return balanceInSol >= minimum;
        }

        
        private bool IsOnlyTransfer(TransactionMetaSlotInfo transactionResult)
        {
            if (transactionResult == null) { return false; }
            return !(transactionResult.Meta.InnerInstructions != null && transactionResult.Meta.InnerInstructions.Length > 0);
        }
        
    }
    
}
