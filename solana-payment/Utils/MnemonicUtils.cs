using Solnet.Wallet.Bip39;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolanaPaymentHD.Utils
{
    public static class MnemonicUtils
    {
        public static IEnumerable<string> GenerateMnemonic(bool TwentyFour)
        {
            var wordcount = WordCount.Twelve;
            if (TwentyFour) { wordcount = WordCount.TwentyFour; }
            var newMnemonic = new Mnemonic(WordList.English, wordcount);
            return newMnemonic.Words;
        }
    }
}
