using NuGet.Frameworks;
using SolanaPaymentHD.Utils;
using SolanaPaymentHD;
using System.Diagnostics;
namespace SolanaPaymentTest
{
    [TestClass]
    public class ServiceTest
    {
        string rpc = "https://api.mainnet-beta.solana.com";
        string seed = "caution junior piece nest muscle include thank venture entire rough ask trend";
        string targetwallet = "C57XeiA2fru7MoHsa71QTYxZNFrdHBdxjSNSHbrgBWsc";
        [TestMethod]
        public void TestHdWalletsMonitor()
        {
            var Payserv = new SolPaymentService(seed, targetwallet, rpc);
            for (int i = 0; i < 20; i++)
            {
                Assert.IsNotNull(Payserv.GetHDwalletAddress(i));
                Console.WriteLine(Payserv.GetHDwalletAddress(i));
            }
        }

        [TestMethod]
        public void TestMnemonicGenerator()
        {
            var words24 = MnemonicUtils.GenerateMnemonic(true);
            var words = MnemonicUtils.GenerateMnemonic(false);
            Assert.IsTrue(words24.Count() == 24, "MnemonicGenerator Isn't 24 words");
            Assert.IsTrue(words.Count() == 12, "MnemonicGenerator Isn't 12 words");
            Console.WriteLine(String.Join(' ',words24));
        }

        [TestMethod]
        public async Task TestPaymentPool()
        {
            var Payserv = new SolPaymentService(seed, targetwallet, rpc);
            Payserv.UpdateWalletExpirationTime(1);
            var newpaywallet = Payserv.GetNewPaymentWallet("sdffhgs");
            Assert.IsTrue(Payserv.GetInUseWalletsCount() == 1, "In use wallet don't match the number{0}", Payserv.GetInUseWalletsCount());
            Assert.IsNotNull(newpaywallet);
            Console.WriteLine(newpaywallet);
            await Task.Delay(61000);
            Assert.IsTrue(Payserv.GetInUseWalletsCount() == 0, "In use wallet don't match the number{0}", Payserv.GetInUseWalletsCount());

        }
    }
}