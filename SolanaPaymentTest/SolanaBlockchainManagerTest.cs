using SolanaPaymentHD;
using SolanaPaymentHD.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolanaPaymentTest
{
    [TestClass]
    public class SolanaBlockchainManagerTest
    {

        private SolanaBlockchainManager _manager;
        string _Rpc = "https://api.mainnet-beta.solana.com";
        [TestInitialize]
        public void SetUp()
        {
            // Create an instance of SolanaBlockchainManager
            _manager = new SolanaBlockchainManager(_Rpc);
        }

        [TestMethod]
        public void TestLastPayer()
        {
        }

        [TestMethod]
        public void TestTransfer()
        {
        }

        [TestMethod]
        public void TestBalance()
        {
        }
    }
}
