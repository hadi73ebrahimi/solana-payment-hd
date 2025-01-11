using SolanaPaymentHD.Models;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolanaPaymentHD.Logic
{
    internal class HdWalletGenerator
    {
        private int _currentIndex = 0;
        private readonly Wallet HdWallet;
        
        public HdWalletGenerator(string masterSeed)
        {
            if (string.IsNullOrWhiteSpace(masterSeed))
            {
                throw new ArgumentException("Master seed phrase cannot be null or empty.");
            }
            var mn = new Mnemonic(masterSeed, WordList.English);
            HdWallet = new Wallet(mn);
        }

        public PaymentWallet GenerateWallet()
        {
            var newwallet = HdWallet.GetAccount(_currentIndex);
            _currentIndex++;

            var address = newwallet.PublicKey;
            var privateKey = newwallet.PrivateKey;

            return new PaymentWallet
            {
                Address = address,
                PrivateKey = privateKey,
                InUse = false,
                InUseUntil = null
            };
        }
    }

}
