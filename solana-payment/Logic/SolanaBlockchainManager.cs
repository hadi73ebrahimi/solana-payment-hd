using SolanaPaymentHD.Models;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solana_payment.Logic
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
            // Simulate checking wallet balance from Solana API
            await Task.Delay(500); // Simulated API call
            return new Random().Next(0, 10); // Simulated balance
        }

        public async Task TransferFunds(PaymentWallet wallet, decimal balance)
        {

            // Initialize the rpc client and a wallet
            var rpcClient = ClientFactory.GetClient(Cluster.MainNet);
            var wallet = new Wallet();
            // Get the source account
            var fromAccount = wallet.GetAccount(0);
            // Get the destination account
            var toAccount = wallet.GetAccount(1);
            // Get a recent block hash to include in the transaction
            var blockHash = rpcClient.GetLatestBlockHash();

            // Initialize a transaction builder and chain as many instructions as you want before building the message
            var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(fromAccount).
                    AddInstruction(ComputeBudgetProgram.SetComputeUnitLimit(30000)).
                    AddInstruction(ComputeBudgetProgram.SetComputeUnitPrice(1000000)).
                    AddInstruction(MemoProgram.NewMemo(fromAccount, "Hello from Sol.Net :)")).
                    AddInstruction(SystemProgram.Transfer(fromAccount, toAccount.GetPublicKey, 100000)).
                    Build(fromAccount);

            var firstSig = rpcClient.SendTransaction(tx);
            
        }

        public async Task<string> GetLatestPayer(string address)
        {
            // Simulate checking wallet balance from Solana API
            await Task.Delay(500); // Simulated API call
            return ""; // Simulated balance
        }

    }
}
