using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mpb.Consensus.Cryptography;

namespace Mpb.Consensus.Test.Logic
{
    /// <summary>
    /// These testmethods are structured by using the AAA method (Arrange, Act, Assert).
    /// </summary>
    [TestClass]
    public class CyptographyTest
    {
        [TestMethod]
        public void GenerateKeys_Returns_ExpectedValueLengths()
        {
            var sut = new KeyGenerator();

            sut.GenerateKeys(out string publicKey, out string privateKey);

            Assert.AreEqual(88, publicKey.Length);
            Assert.AreEqual(512, privateKey.Length);
        }

        [TestMethod]
        public void GeneratePubKey_Returns_ExpectedPubKey()
        {
            var privateKey = "2a5a9964e175b551ee60621611f07cb45d10157334aa0377f58d9b6927b7949bc9985294e7183bb38fdac0de68098554f0102079d6c2657f57216e54d94b2a36cbc64d799ee7cd44d561e45aea240fec07467916961ac50632d23dc6aae6da43b08912a1bc0cb60943dba475de0b953211814d92d2139fb1014646cbaa069aee87a0aa60116e30fa906fd3bb764f27273c0ab4289947709ce9619f75c6ea5e3c40b6ee3e603420a3515201633fe43bb11e1e8e9f5172543f2f0d76b0ceba34a610977abf61e9c708e911d646450d5c0526ee56a75bbfeace61ae0f53bd70d4971e5b52a8697b50355786b53f9c0df921b0718ae0638c4b51acac5eb2da744a79";
            var expectedPublicKey = "Ndwd8RhQTCuP3i7zxoiZboScLYVto1kZsTKZu3ejzQpwzw7tpCohpNCwXSaRxDjUtTpyC1pgjTeNXDEZ5qeEmXSU";
            var sut = new KeyGenerator();

            var result = sut.GeneratePublicKey(privateKey);

            Assert.AreEqual(expectedPublicKey, result);
        }

        [TestMethod]
        public void GeneratePubKey_Throws_NullArgument()
        {
            var sut = new KeyGenerator();

            var ex = Assert.ThrowsException<FormatException>(
                    () => sut.GeneratePublicKey(null)
                );

            Assert.AreEqual("Empty private key", ex.Message);
        }

        [TestMethod]
        public void GeneratePubKey_Throws_InvalidValue()
        {
            var sut = new KeyGenerator();

            var ex = Assert.ThrowsException<FormatException>(
                    () => sut.GeneratePublicKey("H$h*(4W(hh34QG&*53GQH53HQH783GQ7***o%7QOG5#$qH")
                );

            Assert.AreEqual("Input string was not in a correct format.", ex.Message);
        }

        [TestMethod]
        public void SignString_ReturnsString()
        {
            var originalContent = "test abc";
            var privateKey = "31afa4a9d5f852d64a981a0bc1c2a392ebc09808071c90880ab8ff2432eed3157b7f04dea049f5892a11e4780bfd831c60ba6fd3f0e80a151e1c9e1df82ea45cc4058f41bec4f2a1cb0edef638177d0d34e2c3777eff259375efbf83abd790fc6c87b3056d425e9cc8ce08e10914dc069f91e8156a2cdec5e0fcccce046ef6f37de189b06477a0f28427afb0f743896012b8e41aef7a9140a9ee2570758f9859d81463288b2ba227f0f691437d2dcd9c736cbb4b89dc04035049067652307cf48f55d8e2e060d4e716bd29f8e94ec27b2088fc09d34cd0fb1060d842242bd4c0799cb42755ebbc1297d434ccb20b1901868b56ca3372214285dcd2808892c2eb";
            var sut = new Signer();

            var result = sut.SignString(originalContent, privateKey);
            Assert.IsFalse(String.IsNullOrWhiteSpace(result));
        }

        [TestMethod]
        public void ExistingSignatureIsValid_Successful()
        {
            var originalContent = "test string";
            var signature = "iKx1CJQ4jDvBZ5GeNCWQ6n4F9zMoPYF4wodwXYUePGpTD2mV1xdgH7HA3k3UL1tCqZ3PsKHBQncWNfAXvT6L1HduD9BxyWytc3";
            var publicKey = "RQCPCzrDM7Y6p6uY9WpwsdRcVMDjwyiJCmA8TTd2BHA1hr22eTjFFYDBG6tSgSXbhryTy6WYr8WHu8FrcJRpaqLf";
            var sut = new Signer();

            var result = sut.SignatureIsValid(signature, originalContent, publicKey);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SignStringAndSignatureIsValid_Successful()
        {
            var originalContent = "test string";
            var privateKey = "553c57ea38f82bdd0cd2dff6308c09b1f6c7f9d6a9406e9a15474458182883b3e190de71940404f551be2fdde2ad59b4680452fc92508514cebcd4f1b4d3c5e44e2b14a0ad981df1c30485ea1190b868ed90f014d68721900bc9a2d73013417df94a8fb698b906cfe660398dcb9a033e046c0323e3b10ec60a8494e3f545c1afc308f0c60001d0348a202d754e93c570c664d3c30f1d99483cc8ac142422d9a19e89cac2a93d3d029cce99c384987876be241d9171fda0a56ea8893855bc1067468bd6c4c902c78927d1274183d6c4a45035550004482fa1e9245401b305acdfbe317cf8c1b3adbb5d946aa0d55e2198e3e5e81f661063fe5c35dba4aec5d521";
            var publicKey = "P86FGHAHaeoPiaJpysH1ahJMtfTg6jLFsHpaeN4RMyXFXioayL26PjXcpNiyzc3r7Xpn8mx1MUgSsRQXbM3TfY9u";
            var sut = new Signer();

            var signature = sut.SignString(originalContent, privateKey);
            var result = sut.SignatureIsValid(signature, originalContent, publicKey);

            Assert.IsTrue(result);
        }
    }
}
